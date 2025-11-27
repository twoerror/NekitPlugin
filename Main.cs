namespace NekitPlugin
{
    using Exiled.API.Enums;
    using Exiled.API.Features;
    using Exiled.Events.EventArgs.Player;
    using Exiled.Events.EventArgs.Server;
    using System.Collections.Generic;
    using MEC;

    public class NekitPlugin : Plugin<Config>
    {
        private static readonly NekitPlugin Singleton = new();
        private NekitPlugin() { }
        public static NekitPlugin Instance => Singleton;

        public override PluginPriority Priority { get; } = PluginPriority.Last;
        
        private CoroutineHandle _checkCoroutine;
        private bool _isChecking = false;

        public override void OnEnabled()
        {
            RegisterEvents();
            Log.Info("Плагин NekitPlugin включен");
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            UnRegisterEvents();
            StopChecking();
            base.OnDisabled();
            Log.Info("Плагин NekitPlugin выключен");
        }

        private void RegisterEvents()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded += OnRoundEnded;
            Exiled.Events.Handlers.Player.Verified += OnPlayerVerified;
            Exiled.Events.Handlers.Player.Destroying += OnPlayerDestroying;
            Exiled.Events.Handlers.Player.Left += OnPlayerLeft;
            
            Log.Debug("Все события зарегистрированы");
        }

        private void UnRegisterEvents()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Server.RoundEnded -= OnRoundEnded;
            Exiled.Events.Handlers.Player.Verified -= OnPlayerVerified;
            Exiled.Events.Handlers.Player.Destroying -= OnPlayerDestroying;
            Exiled.Events.Handlers.Player.Left -= OnPlayerLeft;
            
            Log.Debug("Все события отписаны");
        }

        private void OnWaitingForPlayers()
        {
            Log.Info("Сервер ожидает игроков, запуск проверки");
            StartChecking();
        }

        private void OnRoundStarted()
        {
            Log.Info("Раунд начался! Остановка проверки");
            StopChecking();
        }

        private void OnRoundEnded(RoundEndedEventArgs ev)
        {
            Log.Info("Раунд завершен! Остановка проверки");
            StopChecking();
        }

        private void OnPlayerVerified(VerifiedEventArgs ev)
        {
            Log.Debug($"Игрок {ev.Player.Nickname} полностью подключен. Всего игроков: {Player.Dictionary.Count}");
            
            if (ShouldCheckPlayers())
                CheckPlayersImmediately();
        }

        private void OnPlayerDestroying(DestroyingEventArgs ev)
        {
            Log.Debug($"Игрок {ev.Player.Nickname} отключился. Всего игроков: {Player.Dictionary.Count}");
            
            if (ShouldCheckPlayers())
                CheckPlayersImmediately();
        }

        private void OnPlayerLeft(LeftEventArgs ev)
        {
            Log.Debug($"Игрок {ev.Player.Nickname} вышел. Всего игроков: {Player.Dictionary.Count}");
            
            if (ShouldCheckPlayers())
                CheckPlayersImmediately();
        }

        private void StartChecking()
        {
            if (_isChecking) return;
            
            _isChecking = true;
            _checkCoroutine = Timing.RunCoroutine(CheckPlayersRoutine());
            Log.Debug("Запущена корутина проверки игроков");
        }

        private void StopChecking()
        {
            if (!_isChecking) return;
            
            _isChecking = false;
            Timing.KillCoroutines(_checkCoroutine);
            Log.Debug("Остановлена корутина проверки игроков");
        }

        private IEnumerator<float> CheckPlayersRoutine()
        {
            while (_isChecking)
            {
                if (ShouldCheckPlayers())
                {
                    CheckPlayersState();
                }
                yield return Timing.WaitForSeconds(Config.CheckInterval);
            }
        }

        private void CheckPlayersImmediately()
        {
            if (ShouldCheckPlayers())
            {
                CheckPlayersState();
            }
        }

        private bool ShouldCheckPlayers()
        {
            return !Round.IsStarted && Round.IsLobby;
        }

        private void CheckPlayersState()
        {
            int currentPlayers = Player.Dictionary.Count;
            
            Log.Debug($"Проверка игроков: {currentPlayers}/{Config.MinPlayers}");

            if (currentPlayers < Config.MinPlayers)
            {
                if (!Round.IsLobbyLocked)
                {
                    Round.IsLobbyLocked = true;
                    Log.Warn($"Недостаточно игроков. Требуется: {Config.MinPlayers}. Сейчас: {currentPlayers}. Лобби заблокировано.");
                }
            }
            else
            {
                if (Round.IsLobbyLocked)
                {
                    Round.IsLobbyLocked = false;
                    Log.Info($"Достигнут минимум игроков ({Config.MinPlayers}). Лобби разблокировано.");
                }
            }
        }
    }
}