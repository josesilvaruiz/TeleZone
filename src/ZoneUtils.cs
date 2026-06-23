using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;
using System.Text.Json;

namespace TeleZone
{
    public partial class TeleZonePlugin
    {
        private string MapDataPath(string mapName)
        {
            // Server.GameDirectory can end in "csgo" or not depending on the server setup.
            // We check and avoid doubling the csgo folder.
            string baseDir = GameDir.TrimEnd('/', '\\');
            if (!baseDir.EndsWith("csgo", StringComparison.OrdinalIgnoreCase))
                baseDir = Path.Join(baseDir, "csgo");
            return Path.Join(baseDir, "cfg", "TeleZone", "MapData", $"{mapName}.json");
        }

        internal void LoadCurrentMap()
        {
            string path = MapDataPath(CurrentMapName);
            if (!File.Exists(path)) return;

            try
            {
                var data = JsonSerializer.Deserialize<MapTeleData>(File.ReadAllText(path), JsonOptions);
                if (data != null)
                {
                    CurrentPairs = data.Pairs ?? new();
                    CurrentKillZones = data.KillZones ?? new();
                    Server.PrintToConsole($"[TeleZone] Loaded {CurrentPairs.Count} teleporter(s) and {CurrentKillZones.Count} kill zone(s) for {CurrentMapName}");
                }
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[TeleZone] Error loading map data: {ex.Message}");
            }
        }

        internal void SaveMapData(CCSPlayerController? notifyPlayer = null)
        {
            string path = MapDataPath(CurrentMapName);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonSerializer.Serialize(
                    new MapTeleData { Pairs = CurrentPairs, KillZones = CurrentKillZones }, JsonOptions));
                Server.PrintToConsole($"[TeleZone] Saved to: {path}");
                notifyPlayer?.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Grey}Saved to: {path}");
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[TeleZone] ERROR saving ({path}): {ex.Message}");
                notifyPlayer?.PrintToChat($" {ChatColors.LightPurple}[TELEZONE] {ChatColors.Red}Error saving: {ex.Message}");
            }
        }

        internal void CleanAdminBeams(int slot)
        {
            if (!AdminStates.TryGetValue(slot, out var state)) return;
            foreach (var beam in state.WireBeams.Values)
            {
                if (beam != null && beam.IsValid)
                    beam.Remove();
            }
            state.WireBeams.Clear();
        }

        // Draws a wireframe box. c1 = fixed first corner, c8 = current (moving) corner.
        internal void DrawWireframe(
            (float X, float Y, float Z) c1,
            (float X, float Y, float Z) c8,
            int slot,
            AdminToolState state)
        {
            // Build all 8 corners of the AABB
            var corners = new (float X, float Y, float Z)[]
            {
                (c1.X, c1.Y, c1.Z), // 0 = c1
                (c1.X, c8.Y, c1.Z), // 1
                (c8.X, c8.Y, c1.Z), // 2
                (c8.X, c1.Y, c1.Z), // 3
                (c8.X, c1.Y, c8.Z), // 4
                (c1.X, c1.Y, c8.Z), // 5
                (c1.X, c8.Y, c8.Z), // 6
                (c8.X, c8.Y, c8.Z), // 7 = c8
            };

            // 12 edges of the box
            (int A, int B)[] edges =
            [
                (0,1),(1,2),(2,3),(3,0), // bottom face
                (4,5),(5,6),(6,7),(7,4), // top face
                (0,5),(1,6),(2,7),(3,4)  // vertical edges
            ];

            for (int i = 0; i < edges.Length; i++)
                DrawWire(corners[edges[i].A], corners[edges[i].B], slot, i, state);
        }

        private void DrawWire(
            (float X, float Y, float Z) start,
            (float X, float Y, float Z) end,
            int slot,
            int idx,
            AdminToolState state)
        {
            try
            {
                if (state.WireBeams.TryGetValue(idx, out var existing) && existing != null && existing.IsValid)
                    existing.Remove();

                var beam = Utilities.CreateEntityByName<CBeam>("beam");
                if (beam == null) return;

                beam.Render = Color.Cyan;
                beam.Width = 1.5f;
                beam.Teleport(
                    new Vector(start.X, start.Y, start.Z),
                    new QAngle(0, 0, 0),
                    new Vector(0, 0, 0));
                beam.EndPos.X = end.X;
                beam.EndPos.Y = end.Y;
                beam.EndPos.Z = end.Z;
                beam.FadeMinDist = 9999;
                beam.DispatchSpawn();

                state.WireBeams[idx] = beam;
            }
            catch (Exception ex)
            {
                Server.PrintToConsole($"[TeleZone] DrawWire error: {ex.Message}");  // intentionally English
            }
        }
    }
}
