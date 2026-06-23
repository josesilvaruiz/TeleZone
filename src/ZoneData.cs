using System.Globalization;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace TeleZone
{
    public class TeleporterPair
    {
        public int Id { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SourceC1 { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SourceC2 { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DestPos { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DestAng { get; set; }
    }

    public class KillZone
    {
        public int Id { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Center { get; set; }

        public float Radius { get; set; } = 50f;
    }

    public class MapTeleData
    {
        public List<TeleporterPair> Pairs { get; set; } = new();
        public List<KillZone> KillZones { get; set; } = new();
    }

    public class AdminToolState
    {
        public bool IsMarkingZone { get; set; }
        public string? SourceC1 { get; set; }
        public string? SourceC2 { get; set; }
        public string? DestPos { get; set; }
        public string? DestAng { get; set; }
        public Dictionary<int, CBeam> WireBeams { get; set; } = new();
    }

    public static class ZoneMath
    {
        public static (float X, float Y, float Z) ParseVec(string s)
        {
            var p = s.Trim().Split(' ');
            return (float.Parse(p[0], CultureInfo.InvariantCulture),
                    float.Parse(p[1], CultureInfo.InvariantCulture),
                    float.Parse(p[2], CultureInfo.InvariantCulture));
        }

        public static (float P, float Y, float R) ParseAng(string s)
        {
            var p = s.Trim().Split(' ');
            return (float.Parse(p[0], CultureInfo.InvariantCulture),
                    float.Parse(p[1], CultureInfo.InvariantCulture),
                    float.Parse(p[2], CultureInfo.InvariantCulture));
        }

        public static string VecToStr(float x, float y, float z) =>
            FormattableString.Invariant($"{x} {y} {z}");

        public static string AngToStr(float p, float y, float r) =>
            FormattableString.Invariant($"{p} {y} {r}");

        public static bool IsInsideBox(
            (float X, float Y, float Z) pt,
            (float X, float Y, float Z) c1,
            (float X, float Y, float Z) c2) =>
            pt.X >= Math.Min(c1.X, c2.X) && pt.X <= Math.Max(c1.X, c2.X) &&
            pt.Y >= Math.Min(c1.Y, c2.Y) && pt.Y <= Math.Max(c1.Y, c2.Y) &&
            pt.Z >= Math.Min(c1.Z, c2.Z) && pt.Z <= Math.Max(c1.Z, c2.Z);
    }
}
