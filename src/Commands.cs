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
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void CmdTeleZoneMenu(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsValid) return;
            MenuSystem.Open(player, "TELEZONE TOOL", new()
            {
                ("Mark Zone A (corner 1/2)",      p => StepMarkSource(p)),
                ("Set Destination B (pos+angle)", p => StepSetDest(p)),
                ("Save pair",                     p => StepSavePair(p)),
                ("List pairs",                    p => ListPairs(p)),
                ("Remove pair...",                p => OpenRemoveMenu(p)),
                ("Reload zones",                  p => ReloadZones(p)),
            });
        }

        private void StepMarkSource(CCSPlayerController player)
        {
            if (!AdminStates.TryGetValue(player.Slot, out var state)) return;
            var origin = player.Pawn.Value?.CBodyComponent?.SceneNode?.AbsOrigin;
            if (origin == null) return;
            float ox = origin.X, oy = origin.Y, oz = origin.Z;

            bool noclip = player.Pawn.Value?.MoveType == MoveType_t.MOVETYPE_NOCLIP;

            if (!state.IsMarkingZone)
            {
                float saveZ = noclip ? -16384f : oz;
                state.SourceC1 = ZoneMath.VecToStr(ox, oy, saveZ);
                state.SourceC2 = null;
                state.IsMarkingZone = true;
                player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.White}Corner 1 saved{(noclip ? " (noclip: any height)" : "")}. Walk to the opposite corner and select !1 again.");
            }
            else
            {
                float saveZ = noclip ? 16384f : oz;
                state.SourceC2 = ZoneMath.VecToStr(ox, oy, saveZ);
                state.IsMarkingZone = false;
                CleanAdminBeams(player.Slot);
                player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Green}Zone A defined{(noclip ? " (noclip: any height)" : "")}. Now set destination B (!2).");
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
            player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Green}Destination B set. Save with !3.");
        }

        private void StepSavePair(CCSPlayerController player)
        {
            if (!AdminStates.TryGetValue(player.Slot, out var state)) return;

            if (state.SourceC1 == null || state.SourceC2 == null || state.DestPos == null)
            {
                player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Red}Incomplete: mark Zone A (!1 twice) and Destination B (!2) first.");
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

            var options = CurrentPairs.Select(pair =>
            {
                int pid = pair.Id;
                return ($"Remove pair #{pid}", (Action<CCSPlayerController>)(p =>
                {
                    CurrentPairs.RemoveAll(x => x.Id == pid);
                    SaveMapData(p);
                    p.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Red}Pair #{pid} removed.");
                }));
            }).ToList();

            MenuSystem.Open(player, "REMOVE PAIR", options);
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
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void CmdKillZoneMenu(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsValid) return;
            MenuSystem.Open(player, "KILLZONE TOOL", new()
            {
                ("Add kill zone (radius 50)",  p => AddKillZone(p, 50f)),
                ("Add kill zone (radius 100)", p => AddKillZone(p, 100f)),
                ("Add kill zone (radius 200)", p => AddKillZone(p, 200f)),
                ("List kill zones",            p => ListKillZones(p)),
                ("Remove kill zone...",        p => OpenRemoveKillZoneMenu(p)),
            });
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
            player.PrintToChat($" {ChatColors.Red}[KILLZONE] {ChatColors.Green}Kill Zone #{newId} placed (radius: {radius} units).");
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

            var options = CurrentKillZones.Select(kz =>
            {
                int kid = kz.Id;
                float kr = kz.Radius;
                return ($"Remove Kill Zone #{kid} (radius {kr})", (Action<CCSPlayerController>)(p =>
                {
                    CurrentKillZones.RemoveAll(x => x.Id == kid);
                    SaveMapData(p);
                    p.PrintToChat($" {ChatColors.Red}[KILLZONE] {ChatColors.Red}Kill Zone #{kid} removed.");
                }));
            }).ToList();

            MenuSystem.Open(player, "REMOVE KILL ZONE", options);
        }
    }
}
