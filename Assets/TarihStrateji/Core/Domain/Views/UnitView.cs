using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Domain.Views
{
    /// <summary>
    /// Sunum için birlik görünümü. Oyuncu yalnız kendi birliklerini ve savaş sisi altında gördüğü
    /// temasları görür (<see cref="IsOwn"/> değilse güç tahmini olabilir: <see cref="IsExact"/>).
    /// </summary>
    public readonly struct UnitView
    {
        public UnitView(ulong id, UnitViewKind kind, CountryId owner, int cellIndex, Fixed64 strength, bool isExact, bool isOwn)
        {
            Id = id;
            Kind = kind;
            Owner = owner;
            CellIndex = cellIndex;
            Strength = strength;
            IsExact = isExact;
            IsOwn = isOwn;
        }

        public ulong Id { get; }

        public UnitViewKind Kind { get; }

        public CountryId Owner { get; }

        public int CellIndex { get; }

        public Fixed64 Strength { get; }

        public bool IsExact { get; }

        public bool IsOwn { get; }
    }
}
