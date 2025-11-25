namespace NekitPlugin
{
    using Exiled.Events.EventArgs.Player;
    using Exiled.Events.EventArgs.Server;
    using Exiled.API.Features;
    
    public class NekitPlugin : Plugin<Config>
    {
        public static NekitPlugin Instance { get; } = new();
        private NekitPlugin() { }
        
        private bool isRoundDelayed;

        public override void OnEnabled() 
        {
            RegisterEvents();
            Log.Info("NekitPlugin включен");
        }

        public override void OnDisabled() 
        {
            UnRegisterEvents();
            Log.Info("NekitPlugin выключен");
        }

        private void RegisterEvents()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            Exiled.Events.Handlers.Player.Verified += OnPlayerVerified;
            Exiled.Events.Handlers.Player.Destroying += OnPlayerDestroying;
        }

        private void UnRegisterEvents()
        {
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            Exiled.Events.Handlers.Player.Verified -= OnPlayerVerified;
            Exiled.Events.Handlers.Player.Destroying -= OnPlayerDestroying;
        }

        private void OnWaitingForPlayers() => ResetRoundState();
        private void OnRoundStarted() => isRoundDelayed = false;

        private void OnPlayerVerified(VerifiedEventArgs ev) => CheckPlayers();
        private void OnPlayerDestroying(DestroyingEventArgs ev) => CheckPlayers();

        private void ResetRoundState()
        {
            isRoundDelayed = false;
            Round.IsLobbyLocked = false;
        }

        private void CheckPlayers()
        {
            if (Round.IsStarted) return;

            int currentPlayers = Player.Dictionary.Count;
            
            if (currentPlayers < Config.MinPlayers)
            {
                if (!isRoundDelayed)
                {
                    isRoundDelayed = true;
                    Log.Warn($"Недостаточно игроков: {currentPlayers}/{Config.MinPlayers}");
                }
                Round.IsLobbyLocked = true;
                Round.Restart();
            }
            else if (isRoundDelayed)
            {
                isRoundDelayed = false;
                Round.IsLobbyLocked = false;
                Log.Info($"Достигнут минимум игроков: {Config.MinPlayers}");
            }
        }
    }
}