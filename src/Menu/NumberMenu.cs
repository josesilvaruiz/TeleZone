using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace TeleZone;

public class NumberMenuInstance
{
    public string Html { get; set; } = "";
    public List<Action<CCSPlayerController>> Actions { get; set; } = new();
}

public class NumberMenu
{
    private readonly Dictionary<int, NumberMenuInstance> _active = new();

    public void Open(CCSPlayerController player, string title, List<(string Label, Action<CCSPlayerController> OnSelect)> options)
    {
        var sb = new StringBuilder();
        sb.Append($"<font color='#aa44ff' class='fontSize-m'><b>{title}</b></font><br>");
        for (int i = 0; i < options.Count; i++)
            sb.Append($"<font color='white' class='fontSize-m'><b>{i + 1}.</b> {options[i].Label}</font><br>");
        sb.Append("<font color='gray' class='fontSize-s'>Type a number · 0 to close</font>");

        _active[player.Slot] = new NumberMenuInstance
        {
            Html = sb.ToString(),
            Actions = options.Select(o => o.OnSelect).ToList()
        };
    }

    // Returns true if the input was consumed (suppresses chat message)
    public bool HandleSay(CCSPlayerController player, string message)
    {
        if (!_active.TryGetValue(player.Slot, out var menu)) return false;
        if (!int.TryParse(message.Trim(), out int n)) return false;

        if (n == 0) { Close(player); return true; }
        if (n < 1 || n > menu.Actions.Count) return false;

        var action = menu.Actions[n - 1];
        Close(player);
        action(player);
        return true;
    }

    public void Close(CCSPlayerController player)
    {
        _active.Remove(player.Slot);
        player.PrintToCenterHtml(" ");
    }

    public void RemovePlayer(int slot) => _active.Remove(slot);

    public bool HasActiveMenu(int slot) => _active.ContainsKey(slot);

    // Keeps the menu visible — call from OnTick
    public void OnTick()
    {
        foreach (var (slot, menu) in _active)
        {
            var player = Utilities.GetPlayerFromSlot(slot);
            if (player == null || !player.IsValid) continue;
            var html = menu.Html;
            Server.NextFrame(() => player.PrintToCenterHtml(html));
        }
    }
}
