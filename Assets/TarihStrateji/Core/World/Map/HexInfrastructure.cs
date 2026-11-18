using System;

namespace TarihStrateji.Core.World.Map
{
    /// <summary>
    /// Hücre altyapısı. Altyapı **bina slotu tüketmez** (CLAUDE.md §2.3): bina slotları yalnız
    /// şehir sektörlerinde tutulur ve bu yapıdan bağımsızdır.
    /// </summary>
    [Serializable]
    public readonly struct HexInfrastructure : IEquatable<HexInfrastructure>
    {
        public const int MaxRoadLevel = 3;
        public const int MaxRailLevel = 2;

        public static readonly HexInfrastructure None = new HexInfrastructure(0, 0, false);

        public HexInfrastructure(int roadLevel, int railLevel, bool hasPort)
        {
            if (roadLevel < 0 || roadLevel > MaxRoadLevel || railLevel < 0 || railLevel > MaxRailLevel)
            {
                throw new ArgumentOutOfRangeException(nameof(roadLevel), WorldErrorKeys.AltyapiSeviyesiGecersiz);
            }

            RoadLevel = roadLevel;
            RailLevel = railLevel;
            HasPort = hasPort;
        }

        public int RoadLevel { get; }

        public int RailLevel { get; }

        /// <summary>Liman bağlantısı; hücrede liman sektörlü bir şehir gerektirir.</summary>
        public bool HasPort { get; }

        public bool HasLandInfrastructure => RoadLevel > 0 || RailLevel > 0;

        public HexInfrastructure WithRoad(int level) => new HexInfrastructure(level, RailLevel, HasPort);

        public HexInfrastructure WithRail(int level) => new HexInfrastructure(RoadLevel, level, HasPort);

        public HexInfrastructure WithPort(bool hasPort) => new HexInfrastructure(RoadLevel, RailLevel, hasPort);

        public bool Equals(HexInfrastructure other) =>
            RoadLevel == other.RoadLevel && RailLevel == other.RailLevel && HasPort == other.HasPort;

        public override bool Equals(object obj) => obj is HexInfrastructure other && Equals(other);

        public override int GetHashCode() => (RoadLevel * 31) + (RailLevel * 7) + (HasPort ? 1 : 0);
    }
}
