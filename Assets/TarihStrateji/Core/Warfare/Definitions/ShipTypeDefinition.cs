using System.Collections.Generic;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Economy.Definitions;

namespace TarihStrateji.Core.Warfare.Definitions
{
    /// <summary>
    /// Filo türü (bir filo tek türden gemi grubudur). <see cref="Attack"/> deniz muharebesindeki
    /// güçtür; nakliye filosunda 0 olabilir (katkı yapmaz, hasar alır). Mürettebat nüfustan seferber edilir, gemiler
    /// ortak envanterden çekilir (yoktan üretilmez). <see cref="TransportCapacity"/> taşınabilecek
    /// kara birliği sayısıdır; 0 olan filo çıkarma yapamaz ve konvoy taşıyamaz (CLAUDE.md §2.6).
    /// <see cref="ShoreBombardment"/> komşu kıyı muharebesine verilen ortak destek gücüdür.
    /// </summary>
    public sealed class ShipTypeDefinition
    {
        private readonly GoodAmount[] _equipment;

        public ShipTypeDefinition(
            UnitTypeId id,
            string key,
            long crew,
            GoodAmount[] equipment,
            Fixed64 attack,
            Fixed64 maxOrganization,
            Fixed64 speedPerDay,
            int transportCapacity,
            Fixed64 shoreBombardment)
        {
            Id = id;
            Key = key;
            Crew = crew;
            _equipment = equipment ?? new GoodAmount[0];
            Attack = attack;
            MaxOrganization = maxOrganization;
            SpeedPerDay = speedPerDay;
            TransportCapacity = transportCapacity;
            ShoreBombardment = shoreBombardment;
        }

        public UnitTypeId Id { get; }

        public string Key { get; }

        public long Crew { get; }

        public IReadOnlyList<GoodAmount> Equipment => _equipment;

        public Fixed64 Attack { get; }

        public Fixed64 MaxOrganization { get; }

        /// <summary>Günlük seyir mesafesi (km).</summary>
        public Fixed64 SpeedPerDay { get; }

        public int TransportCapacity { get; }

        public Fixed64 ShoreBombardment { get; }
    }
}
