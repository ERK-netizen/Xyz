using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Intelligence.State
{
    /// <summary>
    /// Gerçek dünyadaki bir varlığın bu günkü konumu ve gücü (Simulation üretir). Yalnız görünür
    /// hücrede olanlar bir ülkenin <see cref="ObservedWorld"/>'üne temas raporu olarak geçer.
    /// </summary>
    public readonly struct Sighting
    {
        public Sighting(SightingKind kind, ulong entityId, CountryId owner, HexId hex, Fixed64 strength)
        {
            Kind = kind;
            EntityId = entityId;
            Owner = owner;
            Hex = hex;
            Strength = strength;
        }

        public SightingKind Kind { get; }

        public ulong EntityId { get; }

        public CountryId Owner { get; }

        public HexId Hex { get; }

        public Fixed64 Strength { get; }
    }
}
