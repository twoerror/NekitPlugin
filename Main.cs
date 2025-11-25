namespace NekitPlugin
{
    using Exiled.API;
    using Exiled.API.Enums;
    using Exiled.Events.EventArgs.Player;
    using Exiled.Events.EventArgs.Server;
    using System.Collections.Generic;
    using Exiled.API.Features;
    using MEC;

    public class NekitPlugin : Plugin<Config>
    {
        private static readonly NekitPlugin Singleton = new();
        private NekitPlugin() { }
        public static NekitPlugin Instance => Singleton;
        public override PluginPriority Priority { get; } = PluginPriority.Last;

        private bool isRoundDelayed = false;
        private int lastPlayerCount = 0;

        public override void OnEnabled() 
        {
            RegisterEvents();
            Log.Info($"Плагин NekitPlugin включен");
            base.OnEnabled();       
        }

        public override void OnDisabled() 
        {
            UnRegisterEvents();
            base.OnDisabled();
            Log.Info($"Плагин NekitPlugin выключен");
        }

        private void RegisterEvents()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Player.Verified += OnPlayerVerified;
            Exiled.Events.Handlers.Player.Destroying += OnPlayerDestroying;
            Exiled.Events.Handlers.Player.Left += OnPlayerLeft;
            
            Log.Debug("Все события зарегистрированы");
        }

        private void UnRegisterEvents()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Player.Verified -= OnPlayerVerified;
            Exiled.Events.Handlers.Player.Destroying -= OnPlayerDestroying;
            Exiled.Events.Handlers.Player.Left -= OnPlayerLeft;
            
            Log.Debug("Все события отписаны");
        }

        private void OnWaitingForPlayers()
        {
            isRoundDelayed = false;
            lastPlayerCount = 0;
            Log.Info("Сервер ожидает игроков, сброс состояния раунда");
        }

        private void OnRoundStarted()
        {
            Log.Info("Раунд начался!");
            isRoundDelayed = false;
        }

        private void OnPlayerVerified(VerifiedEventArgs ev)
        {
            int currentPlayers = Player.Dictionary.Count;
            Log.Info($"Игрок {ev.Player.Nickname} полностью подключен. Всего игроков: {currentPlayers}");
            
            // Проверяем только если количество изменилось и мы в лобби
            if (currentPlayers != lastPlayerCount && Round.IsLobby && !Round.IsStarted)
            {
                lastPlayerCount = currentPlayers;
                CheckPlayersForRoundState();
            }
        }

        private void OnPlayerDestroying(DestroyingEventArgs ev)
        {
            int currentPlayers = Player.Dictionary.Count - 1;
            Log.Info($"Игрок {ev.Player.Nickname} отключился. Всего игроков: {currentPlayers}");
            
            if (Round.IsLobby && !Round.IsStarted)
            {
                lastPlayerCount = currentPlayers;
                CheckPlayersForRoundState();
            }
        }

        private void OnPlayerLeft(LeftEventArgs ev)
        {
            int currentPlayers = Player.Dictionary.Count;
            Log.Info($"Игрок {ev.Player.Nickname} вышел. Всего игроков: {currentPlayers}");
            
            if (Round.IsLobby && !Round.IsStarted)
            {
                lastPlayerCount = currentPlayers;
                CheckPlayersForRoundState();
            }
        }

        private void CheckPlayersForRoundState()
        {
            if (Round.IsStarted)
                return;

            int currentPlayers = Player.Dictionary.Count;
            Log.Debug($"Проверка игроков: {currentPlayers}/{Config.MinPlayers}, isRoundDelayed: {isRoundDelayed}");

            if (currentPlayers < Config.MinPlayers)
            {
                if (!isRoundDelayed)
                {
                    Log.Warn($"Недостаточно игроков для начала раунда. Требуется: {Config.MinPlayers}. Сейчас: {currentPlayers}.");
                    isRoundDelayed = true;
                }
                
                Round.IsLobbyLocked = true;
                
                if (Round.InProgress)
                {
                    Round.Restart();
                    Log.Info("Отсчет раунда прерван из-за недостатка игроков");
                }
                
                Log.Debug("Лобби заблокировано - недостаточно игроков");
            }
            else if (currentPlayers >= Config.MinPlayers)
            {
                Round.IsLobbyLocked = false;
                
                if (isRoundDelayed)
                {
                    Log.Info($"Достигнут минимум игроков ({Config.MinPlayers}). Запуск раунда.");
                    isRoundDelayed = false;
                }
                
                Log.Debug($"Достаточно игроков для начала раунда: {currentPlayers}/{Config.MinPlayers}");
            }
        }
    }
}