using CounterStrikeSharp.API.Core;

namespace TeleZone;

public class WasdMenu
{
    public string Title { get; set; } = "";
    public LinkedList<WasdMenuOption> Options { get; set; } = new();

    public void Add(string display, Action<CCSPlayerController, WasdMenuOption> onChoose)
    {
        Options.AddLast(new WasdMenuOption
        {
            OptionDisplay = display,
            OnChoose = onChoose,
            Index = Options.Count,
            Parent = this
        });
    }
}
