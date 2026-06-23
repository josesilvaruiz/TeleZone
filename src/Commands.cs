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
            MenuSystem.Open(player, "TELEZONE TOOL", new()
            {
                ("Marcar Zona A (esquina 1/2)", p => StepMarkSource(p)),
                ("Fijar Destino B (pos + angulo)", p => StepSetDest(p)),
                ("Guardar par",                  p => StepSavePair(p)),
                ("Listar pares",                 p => ListPairs(p)),
                ("Eliminar par...",              p => OpenRemoveMenu(p)),
                ("Recargar zonas",               p => ReloadZones(p)),
            });
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
                player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.White}Esquina 1 guardada. Ve a la esquina opuesta y vuelve a elegir esta opcion.");
            }
            else
            {
                state.SourceC2 = ZoneMath.VecToStr(ox, oy, oz);
                state.IsMarkingZone = false;
                CleanAdminBeams(player.Slot);
                player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Green}Zona A definida. Ahora fija el destino B.");
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
            player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Green}Destino B fijado. Ahora guarda con opcion 3.");
        }

        private void StepSavePair(CCSPlayerController player)
        {
            if (!AdminStates.TryGetValue(player.Slot, out var state)) return;

            if (state.SourceC1 == null || state.SourceC2 == null || state.DestPos == null)
            {
                player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Red}Faltan pasos: marca la zona A (opciones 1 dos veces) y el destino B (opcion 2).");
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

            player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Green}Par #{newId} guardado y activo.");
        }

        private void ListPairs(CCSPlayerController player)
        {
            if (CurrentPairs.Count == 0)
            {
                player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Grey}Sin pares en {CurrentMapName}.");
                return;
            }
            player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.White}Pares en {CurrentMapName}: {CurrentPairs.Count}");
            foreach (var p in CurrentPairs)
                player.PrintToChat($"  {ChatColors.Yellow}#{p.Id} {ChatColors.Grey}A: {p.SourceC1} | B: {p.DestPos}");
        }

        private void OpenRemoveMenu(CCSPlayerController player)
        {
            if (CurrentPairs.Count == 0)
            {
                player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Grey}No hay pares para eliminar.");
                return;
            }

            var options = CurrentPairs.Select(pair =>
            {
                int pid = pair.Id;
                return ($"Eliminar par #{pid}", (Action<CCSPlayerController>)(p =>
                {
                    CurrentPairs.RemoveAll(x => x.Id == pid);
                    SaveMapData(p);
                    p.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Red}Par #{pid} eliminado.");
                }));
            }).ToList();

            MenuSystem.Open(player, "ELIMINAR PAR", options);
        }

        private void ReloadZones(CCSPlayerController player)
        {
            CurrentPairs.Clear();
            CurrentKillZones.Clear();
            LoadCurrentMap();
            player.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.White}Zonas recargadas. {CurrentPairs.Count} teleport(s), {CurrentKillZones.Count} kill zone(s).");
        }

        // ── Kill zones ──────────────────────────────────────────────────────────

        [ConsoleCommand("css_killzones", "Kill Zone admin menu")]
        [RequiresPermissions("@css/cheats")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void CmdKillZoneMenu(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !player.IsValid) return;
            MenuSystem.Open(player, "KILLZONE TOOL", new()
            {
                ("Añadir zona (radio 50)",  p => AddKillZone(p, 50f)),
                ("Añadir zona (radio 100)", p => AddKillZone(p, 100f)),
                ("Añadir zona (radio 200)", p => AddKillZone(p, 200f)),
                ("Listar zonas",            p => ListKillZones(p)),
                ("Eliminar zona...",        p => OpenRemoveKillZoneMenu(p)),
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
            player.PrintToChat($" {ChatColors.Red}[KILLZONE] {ChatColors.Green}Kill Zone #{newId} añadida (radio: {radius} u).");
        }

        private void ListKillZones(CCSPlayerController player)
        {
            if (CurrentKillZones.Count == 0)
            {
                player.PrintToChat($" {ChatColors.Red}[KILLZONE] {ChatColors.Grey}Sin kill zones en {CurrentMapName}.");
                return;
            }
            player.PrintToChat($" {ChatColors.Red}[KILLZONE] {ChatColors.White}Kill Zones en {CurrentMapName}: {CurrentKillZones.Count}");
            foreach (var kz in CurrentKillZones)
                player.PrintToChat($"  {ChatColors.Yellow}#{kz.Id} {ChatColors.Grey}Centro: {kz.Center} | Radio: {kz.Radius}");
        }

        private void OpenRemoveKillZoneMenu(CCSPlayerController player)
        {
            if (CurrentKillZones.Count == 0)
            {
                player.PrintToChat($" {ChatColors.Red}[KILLZONE] {ChatColors.Grey}No hay kill zones para eliminar.");
                return;
            }

            var options = CurrentKillZones.Select(kz =>
            {
                int kid = kz.Id;
                float kr = kz.Radius;
                return ($"Eliminar Kill Zone #{kid} (radio {kr})", (Action<CCSPlayerController>)(p =>
                {
                    CurrentKillZones.RemoveAll(x => x.Id == kid);
                    SaveMapData(p);
                    p.PrintToChat($" {ChatColors.Red}[KILLZONE] {ChatColors.Red}Kill Zone #{kid} eliminada.");
                }));
            }).ToList();

            MenuSystem.Open(player, "ELIMINAR KILL ZONE", options);
        }
    }
}
