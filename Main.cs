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
            Exiled.Events.Handlers.Server.EndingRound += OnEndingRound;
            Exiled.Events.Handlers.Player.Verified += OnPlayerVerified;
            Exiled.Events.Handlers.Player.Destroying += OnPlayerDestroying;
            
            Log.Debug("Все события зарегистрированы");
        }

        private void UnRegisterEvents()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.EndingRound -= OnEndingRound;
            Exiled.Events.Handlers.Player.Verified -= OnPlayerVerified;
            Exiled.Events.Handlers.Player.Destroying -= OnPlayerDestroying;
            
            Log.Debug("Все события отписаны");
        }

        private void OnWaitingForPlayers()
        {
            isRoundDelayed = false;
            Log.Info("Сервер ожидает игроков, сброс состояния раунда");
            CheckPlayersForRoundState();
        }

        private void OnRoundStarted()
        {
            Log.Info("Раунд начался!");
            isRoundDelayed = false;
        }

        private void OnEndingRound(EndingRoundEventArgs ev)
        {
            if (ev.IsRoundEnded && Player.Dictionary.Count < Config.MinPlayers)
            {
                ev.IsAllowed = false;
                Log.Info($"Окончание раунда заблокировано: недостаточно игроков ({Player.Dictionary.Count}/{Config.MinPlayers})");
            }
        }

        private void OnPlayerVerified(VerifiedEventArgs ev)
        {
            Log.Info($"Игрок {ev.Player.Nickname} полностью подключен. Всего игроков: {Player.Dictionary.Count}");
            
            if (Round.IsLobby && !Round.IsStarted)
            {
                CheckPlayersForRoundState();
            }
        }

        private void OnPlayerDestroying(DestroyingEventArgs ev)
        {
            Log.Info($"Игрок {ev.Player.Nickname} отключился. Всего игроков: {Player.Dictionary.Count}");
            
            if (Round.IsLobby && !Round.IsStarted)
            {
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
                Log.Debug("Лобби заблокировано - недостаточно игроков");
            }
            else if (currentPlayers >= Config.MinPlayers)
            {
                Round.IsLobbyLocked = false;
                Log.Info($"Достигнут минимум игроков ({Config.MinPlayers}). Запуск раунда.");
                if (isRoundDelayed)
                {     
                    isRoundDelayed = false;
                }
            }
        }
    }
}