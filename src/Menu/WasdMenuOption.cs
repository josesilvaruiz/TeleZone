using CounterStrikeSharp.API.Core;

namespace TeleZone;

public class WasdMenuOption
{
    public WasdMenu? Parent { get; set; }
    public string OptionDisplay { get; set; } = "";
    public Action<CCSPlayerController, WasdMenuOption>? OnChoose { get; set; }
    public int Index { get; set; }
}
