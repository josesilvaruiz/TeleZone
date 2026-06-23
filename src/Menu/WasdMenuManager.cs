using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace TeleZone;

public class WasdMenuManager
{
    private readonly Dictionary<int, WasdMenuPlayer> _players = new();

    public void RegisterPlayer(CCSPlayerController player)
    {
        _players[player.Slot] = new WasdMenuPlayer { Player = player, Buttons = player.Buttons };
    }

    public void UnregisterPlayer(int slot)
    {
        if (_players.TryGetValue(slot, out var p))
            p.Close();
        _players.Remove(slot);
    }

    public void OpenMenu(CCSPlayerController player, WasdMenu menu)
    {
        if (!_players.TryGetValue(player.Slot, out var mp))
        {
            mp = new WasdMenuPlayer { Player = player, Buttons = player.Buttons };
            _players[player.Slot] = mp;
        }
        mp.OpenMenu(menu);
    }

    public void CloseMenu(CCSPlayerController player)
    {
        if (_players.TryGetValue(player.Slot, out var mp))
            mp.Close();
    }

    public bool HasActiveMenu(int slot) => _players.TryGetValue(slot, out var p) && p.HasActiveMenu;

    public WasdMenu CreateMenu(string title) => new WasdMenu { Title = title };

    public void OnTick()
    {
        foreach (var mp in _players.Values.Where(p => p.HasActiveMenu))
        {
            var cur = mp.Player.Buttons;
            var prev = mp.Buttons;

            if ((prev & PlayerButtons.Forward) == 0 && (cur & PlayerButtons.Forward) != 0)
                mp.ScrollUp();
            else if ((prev & PlayerButtons.Back) == 0 && (cur & PlayerButtons.Back) != 0)
                mp.ScrollDown();
            else if ((prev & PlayerButtons.Use) == 0 && (cur & PlayerButtons.Use) != 0)
                mp.Choose();
            else if (((long)cur & 8589934592L) == 8589934592L) // R key
                mp.Close();

            mp.Buttons = cur;

            if (mp.CenterHtml != "")
            {
                var player = mp.Player;
                Server.NextFrame(() => player.PrintToCenterHtml(mp.CenterHtml));
            }
        }
    }
}
