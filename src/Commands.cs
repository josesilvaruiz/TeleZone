using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace TeleZone
{
    public partial class TeleZonePlugin
    {
        // ── Teleporter zones ────────────────────────────────────────────────────

        [ConsoleCommand("css_telezones", "TeleZone admin menu")]
        [RequiresPermissions("@css/cheats")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void CmdTeleZoneMenu(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsValid) return;

            var menu = MenuManager.CreateMenu("TELEZONE TOOL");
            menu.Add("Mark Zone A (corner 1/2)",      (p, _) => StepMarkSource(p));
            menu.Add("Set Destination B (pos+angle)", (p, _) => StepSetDest(p));
            menu.Add("Save pair",                     (p, _) => StepSavePair(p));
            menu.Add("List pairs",                    (p, _) => ListPairs(p));
            menu.Add("Remove pair...",                (p, _) => OpenRemoveMenu(p));
            menu.Add("Reload zones",                  (p, _) => ReloadZones(p));
            MenuManager.OpenMenu(player, menu);
        }

        private void StepMarkSource(CCSPlayerController player)
        {
            if (!AdminStates.TryGetValue(player.Slot, out var state)) return;
            var origin = player.Pawn.Value?.CBodyComponent?.SceneNode?.AbsOrigin;
            if (origin == null) return;
            float ox = origin.X, oy = origin.Y, oz = origin.Z;

            if (!state.IsMarkingZone)
            {
                state.SourceC1 = ZoneMath.VecToStr(ox, oy, oz);
                state.SourceC2 = null;
                state.IsMarkingZone = true;
                player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.White}Corner 1 saved. Walk to the opposite corner and select this option again.");
            }
            else
            {
                state.SourceC2 = ZoneMath.VecToStr(ox, oy, oz);
                state.IsMarkingZone = false;
                CleanAdminBeams(player.Slot);
                player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Green}Zone A defined. Now set destination B (option 2).");
            }
        }

        private void StepSetDest(CCSPlayerController player)
        {
            if (!AdminStates.TryGetValue(player.Slot, out var state)) return;
            var origin = player.Pawn.Value?.CBodyComponent?.SceneNode?.AbsOrigin;
            if (origin == null) return;
            var angles = player.PlayerPawn.Value?.EyeAngles;

            state.DestPos = ZoneMath.VecToStr(origin.X, origin.Y, origin.Z);
            state.DestAng = angles != null ? ZoneMath.AngToStr(angles.X, angles.Y, angles.Z) : null;
            player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Green}Destination B set. Save with option 3.");
        }

        private void StepSavePair(CCSPlayerController player)
        {
            if (!AdminStates.TryGetValue(player.Slot, out var state)) return;

            if (state.SourceC1 == null || state.SourceC2 == null || state.DestPos == null)
            {
                player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Red}Incomplete: mark Zone A (option 1 twice) and Destination B (option 2) first.");
                return;
            }

            int newId = CurrentPairs.Count > 0 ? CurrentPairs.Max(p => p.Id) + 1 : 1;
            CurrentPairs.Add(new TeleporterPair
            {
                Id = newId,
                SourceC1 = state.SourceC1,
                SourceC2 = state.SourceC2,
                DestPos = state.DestPos,
                DestAng = state.DestAng
            });
            SaveMapData(player);

            state.SourceC1 = null;
            state.SourceC2 = null;
            state.DestPos = null;
            state.DestAng = null;
            state.IsMarkingZone = false;

            player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Green}Pair #{newId} saved and active.");
        }

        private void ListPairs(CCSPlayerController player)
        {
            if (CurrentPairs.Count == 0)
            {
                player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Grey}No pairs on {CurrentMapName}.");
                return;
            }
            player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.White}Pairs on {CurrentMapName}: {CurrentPairs.Count}");
            foreach (var p in CurrentPairs)
                player.PrintToChat($"  {ChatColors.Yellow}#{p.Id} {ChatColors.Grey}A: {p.SourceC1} | B: {p.DestPos}");
        }

        private void OpenRemoveMenu(CCSPlayerController player)
        {
            if (CurrentPairs.Count == 0)
            {
                player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Grey}No pairs to remove.");
                return;
            }

            var menu = MenuManager.CreateMenu("REMOVE PAIR");
            foreach (var pair in CurrentPairs)
            {
                int pid = pair.Id;
                menu.Add($"Remove pair #{pid}", (p, _) =>
                {
                    CurrentPairs.RemoveAll(x => x.Id == pid);
                    SaveMapData(p);
                    p.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Red}Pair #{pid} removed.");
                    MenuManager.CloseMenu(p);
                });
            }
            MenuManager.OpenMenu(player, menu);
        }

        private void ReloadZones(CCSPlayerController player)
        {
            CurrentPairs.Clear();
            CurrentKillZones.Clear();
            LoadCurrentMap();
            player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.White}Zones reloaded — {CurrentPairs.Count} teleporter(s), {CurrentKillZones.Count} kill zone(s).");
        }

        // ── Kill zones ──────────────────────────────────────────────────────────

        [ConsoleCommand("css_killzones", "Kill Zone admin menu")]
        [RequiresPermissions("@css/cheats")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void CmdKillZoneMenu(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsValid) return;

            var menu = MenuManager.CreateMenu("KILLZONE TOOL");
            menu.Add("Add kill zone (radius 50)",  (p, _) => AddKillZone(p, 50f));
            menu.Add("Add kill zone (radius 100)", (p, _) => AddKillZone(p, 100f));
            menu.Add("Add kill zone (radius 200)", (p, _) => AddKillZone(p, 200f));
            menu.Add("List kill zones",            (p, _) => ListKillZones(p));
            menu.Add("Remove kill zone...",        (p, _) => OpenRemoveKillZoneMenu(p));
            MenuManager.OpenMenu(player, menu);
        }

        private void AddKillZone(CCSPlayerController player, float radius)
        {
            var origin = player.Pawn.Value?.CBodyComponent?.SceneNode?.AbsOrigin;
            if (origin == null) return;

            int newId = CurrentKillZones.Count > 0 ? CurrentKillZones.Max(k => k.Id) + 1 : 1;
            CurrentKillZones.Add(new KillZone
            {
                Id = newId,
                Center = ZoneMath.VecToStr(origin.X, origin.Y, origin.Z),
                Radius = radius
            });
            SaveMapData(player);
            player.PrintToChat($" {ChatColors.Red}[KILLZONE] {ChatColors.Green}Kill Zone #{newId} placed at your position (radius: {radius} units).");
        }

        private void ListKillZones(CCSPlayerController player)
        {
            if (CurrentKillZones.Count == 0)
            {
                player.PrintToChat($" {ChatColors.Red}[KILLZONE] {ChatColors.Grey}No kill zones on {CurrentMapName}.");
                return;
            }
            player.PrintToChat($" {ChatColors.Red}[KILLZONE] {ChatColors.White}Kill Zones on {CurrentMapName}: {CurrentKillZones.Count}");
            foreach (var kz in CurrentKillZones)
                player.PrintToChat($"  {ChatColors.Yellow}#{kz.Id} {ChatColors.Grey}Center: {kz.Center} | Radius: {kz.Radius}");
        }

        private void OpenRemoveKillZoneMenu(CCSPlayerController player)
        {
            if (CurrentKillZones.Count == 0)
            {
                player.PrintToChat($" {ChatColors.Red}[KILLZONE] {ChatColors.Grey}No kill zones to remove.");
                return;
            }

            var menu = MenuManager.CreateMenu("REMOVE KILL ZONE");
            foreach (var kz in CurrentKillZones)
            {
                int kid = kz.Id;
                float kr = kz.Radius;
                menu.Add($"Remove Kill Zone #{kid} (radius {kr})", (p, _) =>
                {
                    CurrentKillZones.RemoveAll(x => x.Id == kid);
                    SaveMapData(p);
                    p.PrintToChat($" {ChatColors.Red}[KILLZONE] {ChatColors.Red}Kill Zone #{kid} removed.");
                    MenuManager.CloseMenu(p);
                });
            }
            MenuManager.OpenMenu(player, menu);
        }
    }
}
