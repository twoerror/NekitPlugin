namespace NekitPlugin
{
	using Exiled.API.Features;
	using Exiled.API.Interfaces;
	using System.ComponentModel; 

	public sealed class Config : IConfig
	{
		[Description("Включен ли плагин")]
        public bool IsEnabled { get; set; } = true;		
        
        [Description("Режим отладки")]
        public bool Debug { get; set; } = false;
        
        [Description("Минимальное количество игроков для начала раунда")]
        public int MinPlayers { get; set; } = 4;
        
	}
}
