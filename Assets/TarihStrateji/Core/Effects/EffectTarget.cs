using System;
using System.Globalization;
using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.Effects
{
    /// <summary>Etkinin uygulandığı varlık: kapsam + kimlik değeri (Global için 0).</summary>
    [Serializable]
    public readonly struct EffectTarget : IEquatable<EffectTarget>, IComparable<EffectTarget>
    {
        public static readonly EffectTarget Global = new EffectTarget(EffectScope.Global, 0UL);

        public EffectTarget(EffectScope scope, ulong id)
        {
            Scope = scope;
            Id = id;
        }

        public EffectScope Scope { get; }

        public ulong Id { get; }

        public bool IsValid => Scope == EffectScope.Global ? Id == 0UL : Id != 0UL;

        public static EffectTarget Of(CountryId id) => new EffectTarget(EffectScope.Country, id.Value);

        public static EffectTarget Of(RegionId id) => new EffectTarget(EffectScope.Region, id.Value);

        public static EffectTarget Of(CityId id) => new EffectTarget(EffectScope.City, id.Value);

        public static EffectTarget Of(LandUnitId id) => new EffectTarget(EffectScope.LandUnit, id.Value);

        public static EffectTarget Of(FleetId id) => new EffectTarget(EffectScope.Fleet, id.Value);

        public static EffectTarget Of(AirWingId id) => new EffectTarget(EffectScope.AirWing, id.Value);

        public static EffectTarget Of(CharacterId id) => new EffectTarget(EffectScope.Character, id.Value);

        public static bool operator ==(EffectTarget a, EffectTarget b) => a.Scope == b.Scope && a.Id == b.Id;

        public static bool operator !=(EffectTarget a, EffectTarget b) => !(a == b);

        public bool Equals(EffectTarget other) => this == other;

        public override bool Equals(object obj) => obj is EffectTarget other && this == other;

        public override int GetHashCode() => ((int)Scope * 397) ^ Id.GetHashCode();

        public int CompareTo(EffectTarget other)
        {
            int byScope = Scope.CompareTo(other.Scope);
            return byScope != 0 ? byScope : Id.CompareTo(other.Id);
        }

        public override string ToString() => Scope + ":" + Id.ToString(CultureInfo.InvariantCulture);
    }
}
