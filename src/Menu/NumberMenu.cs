using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace TeleZone;

public class NumberMenu
{
    private readonly Dictionary<int, List<(string, Action<CCSPlayerController>)>> _menus = new();

    public void Open(CCSPlayerController player, string title, List<(string, Action<CCSPlayerController>)> options)
    {
        _menus[player.Slot] = options;
        player.PrintToChat($" {ChatColors.LightPurple}━━━━ {title} ━━━━");
        for (int i = 0; i < options.Count; i++)
            player.PrintToChat($" {ChatColors.Yellow}!{i + 1}  {ChatColors.White}{options[i].Item1}");
        player.PrintToChat($" {ChatColors.Red}!0  {ChatColors.Grey}Close");
    }

    public bool TrySelect(CCSPlayerController player, int number)
    {
        if (!_menus.TryGetValue(player.Slot, out var options))
            return false;

        if (number == 0)
        {
            _menus.Remove(player.Slot);
            player.PrintToChat($" {ChatColors.LightPurple}[TeleZone] {ChatColors.Grey}Menu closed.");
            return true;
        }

        if (number >= 1 && number <= options.Count)
        {
            options[number - 1].Item2(player);
            return true;
        }

        return false;
    }

    public void Close(int slot) => _menus.Remove(slot);

    public bool HasMenu(int slot) => _menus.ContainsKey(slot);
}
