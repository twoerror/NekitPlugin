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
        private CoroutineHandle checkPlayersCoroutine;

        public override void OnEnabled() 
        {
            RegisterEvents();
            Log.Info($"Плагин NekitPlugin включен");
            base.OnEnabled();       
        }

        public override void OnDisabled() 
        {
            UnRegisterEvents();
            if (checkPlayersCoroutine.IsRunning)
                Timing.KillCoroutines(checkPlayersCoroutine);
            base.OnDisabled();
            Log.Info($"Плагин NekitPlugin выключен");
        }

        private void RegisterEvents()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Server.EndingRound += OnEndingRound;
            Exiled.Events.Handlers.Player.Joined += OnPlayerJoined;
            Exiled.Events.Handlers.Player.Left += OnPlayerLeft;
            Exiled.Events.Handlers.Player.Authenticated += OnPlayerAuthenticated;
            
            Log.Debug("Все события зарегистрированы");
        }

        private void UnRegisterEvents()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.EndingRound -= OnEndingRound;
            Exiled.Events.Handlers.Player.Joined -= OnPlayerJoined;
            Exiled.Events.Handlers.Player.Left -= OnPlayerLeft;
            Exiled.Events.Handlers.Player.Authenticated -= OnPlayerAuthenticated;
            
            Log.Debug("Все события отписаны");
        }

        private void OnWaitingForPlayers()
        {
            isRoundDelayed = false;
            if (checkPlayersCoroutine.IsRunning)
                Timing.KillCoroutines(checkPlayersCoroutine);
            Log.Info("Сервер ожидает игроков, сброс состояния раунда");
        }

        private void OnRoundStarted()
        {
            Log.Info("Раунд начался!");
            isRoundDelayed = false;
            if (checkPlayersCoroutine.IsRunning)
                Timing.KillCoroutines(checkPlayersCoroutine);
        }

        private void OnEndingRound(EndingRoundEventArgs ev)
        {
            int authenticatedPlayers = GetAuthenticatedPlayersCount();
            
            if (authenticatedPlayers < Config.MinPlayers)
            {
                ev.IsAllowed = false;
                Log.Info($"Окончание раунда заблокировано: недостаточно аутентифицированных игроков ({authenticatedPlayers}/{Config.MinPlayers})");
           }
        }

        private void OnPlayerJoined(JoinedEventArgs ev)
        {
            Log.Info($"Игрок {ev.Player.Nickname} присоединился. Всего подключений: {Player.List.Count}");
            
            if (!Round.IsStarted && Round.IsLobby)
            {
                checkPlayersCoroutine = Timing.CallDelayed(2f, CheckPlayersForRoundStart);
            }
        }

        private void OnPlayerLeft(LeftEventArgs ev)
        {
            Log.Info($"Игрок {ev.Player.Nickname} вышел. Всего подключений: {Player.List.Count}");
            
            if (!Round.IsStarted && Round.IsLobby)
            {
                CheckPlayersForRoundStart();
            }
        }

        private void OnPlayerAuthenticated(Exiled.Events.EventArgs.Player.AuthenticatedEventArgs ev)
        {
            Log.Debug($"Игрок {ev.Player.Nickname} аутентифицирован");
            
            if (!Round.IsStarted && Round.IsLobby)
            {
                CheckPlayersForRoundStart();
            }
        }

        private int GetAuthenticatedPlayersCount()
        {
            int count = 0;
            foreach (Player player in Player.List)
            {
                if (player.IsAuthenticated)
                    count++;
            }
            return count;
        }

        private void CheckPlayersForRoundStart()
        {
            if (Round.IsStarted)
                return;

            int authenticatedPlayers = GetAuthenticatedPlayersCount();
            int totalPlayers = Player.List.Count;
            
            Log.Debug($"Проверка игроков: Аутентифицировано: {authenticatedPlayers}/{Config.MinPlayers}, Всего подключений: {totalPlayers}, isRoundDelayed: {isRoundDelayed}");

            if (authenticatedPlayers < Config.MinPlayers)
            {
                if (!isRoundDelayed)
                {
                    Log.Warn($"Недостаточно аутентифицированных игроков для начала раунда. Требуется: {Config.MinPlayers}. Сейчас: {authenticatedPlayers}.");
                    isRoundDelayed = true;
                }
                
                Round.IsLobbyLocked = true;
                Log.Debug("Лобби заблокировано - недостаточно аутентифицированных игроков");
            }
            else if (authenticatedPlayers >= Config.MinPlayers)
            {
                Round.IsLobbyLocked = false;

                if (isRoundDelayed)
                {
                    Log.Info($"Достигнут минимум аутентифицированных игроков ({Config.MinPlayers}). Запуск раунда.");
                    isRoundDelayed = false;
                }
            }
        }
    }
}