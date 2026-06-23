using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json;

namespace TeleZone
{
    public partial class TeleZonePlugin : BasePlugin
    {
        public override string ModuleName => "TeleZone";
        public override string ModuleVersion => "1.2.0";
        public override string ModuleAuthor => "Torment";
        public override string ModuleDescription => "Admin-defined teleporter zones and kill zones, saved per map";

        internal static TeleZonePlugin Instance { get; private set; } = null!;
        internal string GameDir { get; private set; } = string.Empty;
        internal string CurrentMapName { get; private set; } = string.Empty;
        internal List<TeleporterPair> CurrentPairs { get; private set; } = new();
        internal List<KillZone> CurrentKillZones { get; private set; } = new();
        internal Dictionary<int, AdminToolState> AdminStates { get; } = new();
        internal WasdMenuManager MenuManager { get; } = new();
        internal readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private int _tick;
        private readonly Dictionary<int, int> _teleCooldownTick = new();
        private readonly Dictionary<int, int> _killCooldownTick = new();

        public override void Load(bool hotReload)
        {
            Instance = this;
            GameDir = Server.GameDirectory;
            CurrentMapName = Server.MapName;

            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnTick>(OnTick);
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

            if (hotReload)
                LoadCurrentMap();
        }

        public override void Unload(bool hotReload)
        {
            RemoveListener<Listeners.OnMapStart>(OnMapStart);
            RemoveListener<Listeners.OnTick>(OnTick);
            DeregisterEventHandler<EventPlayerConnectFull>(OnPlayerConnect);
            DeregisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        }

        private void OnMapStart(string mapName)
        {
            CurrentMapName = mapName;
            CurrentPairs.Clear();
            CurrentKillZones.Clear();
            AdminStates.Clear();
            _teleCooldownTick.Clear();
            _killCooldownTick.Clear();
            _tick = 0;
            Server.NextFrame(LoadCurrentMap);
        }

        private HookResult OnPlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid)
            {
                AdminStates[player.Slot] = new AdminToolState();
                MenuManager.RegisterPlayer(player);
            }
            return HookResult.Continue;
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null)
            {
                CleanAdminBeams(player.Slot);
                AdminStates.Remove(player.Slot);
                _teleCooldownTick.Remove(player.Slot);
                _killCooldownTick.Remove(player.Slot);
                MenuManager.UnregisterPlayer(player.Slot);
            }
            return HookResult.Continue;
        }

        private void OnTick()
        {
            _tick++;
            MenuManager.OnTick();

            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
                    continue;

                var pawn = player.PlayerPawn?.Value;
                if (pawn == null || !pawn.IsValid) continue;

                var origin = pawn.CBodyComponent?.SceneNode?.AbsOrigin;
                if (origin == null) continue;

                float px = origin.X, py = origin.Y, pz = origin.Z;
                var pos = (px, py, pz);

                // Admin zone tool wireframe while marking corner 2
                if (AdminStates.TryGetValue(player.Slot, out var state) &&
                    state.IsMarkingZone && state.SourceC1 != null)
                {
                    DrawWireframe(ZoneMath.ParseVec(state.SourceC1), pos, player.Slot, state);
                }

                // Teleport check — 1-second cooldown (64 ticks)
                if (!_teleCooldownTick.TryGetValue(player.Slot, out int lastTele) || _tick - lastTele >= 64)
                {
                    foreach (var pair in CurrentPairs)
                    {
                        if (pair.SourceC1 == null || pair.SourceC2 == null || pair.DestPos == null)
                            continue;

                        if (!ZoneMath.IsInsideBox(pos, ZoneMath.ParseVec(pair.SourceC1), ZoneMath.ParseVec(pair.SourceC2)))
                            continue;

                        var dest = ZoneMath.ParseVec(pair.DestPos);
                        QAngle? teleAng = null;
                        if (pair.DestAng != null)
                        {
                            var ang = ZoneMath.ParseAng(pair.DestAng);
                            teleAng = new QAngle(ang.P, ang.Y, ang.R);
                        }

                        pawn.Teleport(new Vector(dest.X, dest.Y, dest.Z), teleAng, new Vector(0, 0, 0));
                        _teleCooldownTick[player.Slot] = _tick;
                        break;
                    }
                }

                // Kill zone check — 2-second cooldown (128 ticks)
                if (!_killCooldownTick.TryGetValue(player.Slot, out int lastKill) || _tick - lastKill >= 128)
                {
                    foreach (var kz in CurrentKillZones)
                    {
                        if (kz.Center == null) continue;
                        var c = ZoneMath.ParseVec(kz.Center);
                        float dx = px - c.X, dy = py - c.Y, dz = pz - c.Z;
                        if (dx * dx + dy * dy + dz * dz > kz.Radius * kz.Radius)
                            continue;

                        _killCooldownTick[player.Slot] = _tick;
                        int slot = player.Slot;
                        Server.NextFrame(() =>
                        {
                            var p = Utilities.GetPlayerFromSlot(slot);
                            p?.PlayerPawn?.Value?.CommitSuicide(false, true);
                        });
                        break;
                    }
                }
            }
        }
    }
}
