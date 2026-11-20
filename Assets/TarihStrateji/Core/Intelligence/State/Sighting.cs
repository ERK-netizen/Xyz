using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Intelligence.State
{
    /// <summary>
    /// Gerçek dünyadaki bir varlığın bu günkü konumu ve gücü (Simulation üretir). Yalnız görünür
    /// düğümde olanlar bir ülkenin <see cref="ObservedWorld"/>'üne temas raporu olarak geçer.
    /// </summary>
    public readonly struct Sighting
    {
        public Sighting(SightingKind kind, ulong entityId, CountryId owner, TransportNodeId node, Fixed64 strength)
        {
            Kind = kind;
            EntityId = entityId;
            Owner = owner;
            Node = node;
            Strength = strength;
        }

        public SightingKind Kind { get; }

        public ulong EntityId { get; }

        public CountryId Owner { get; }

        /// <summary>Varlığın bulunduğu ya da son ulaştığı düğüm.</summary>
        public TransportNodeId Node { get; }

        public Fixed64 Strength { get; }
    }
}
