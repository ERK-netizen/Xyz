using System.Collections.Generic;
using TarihStrateji.Core.AI.Definitions;
using TarihStrateji.Core.AI.Perception;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Intelligence.State;
using TarihStrateji.Core.Warfare.Definitions;
using TarihStrateji.Core.Warfare.State;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.AI.Layers
{
    /// <summary>
    /// Stratejik katman: kendi gücü (kendi birlik güçleri toplamı), düşmanlar ve yabancı ülkelerin
    /// tahmini gücü. Yabancı güç yalnız temas raporlarından (son görülme bellek süresi içinde) ve
    /// kamuya açık şehir sayısından türetilir: <c>max(Σ temas tahmini, şehir × varsayılan garnizon)</c>.
    /// </summary>
    public sealed class StrategicAssessment
    {
        private readonly AiPerception _p;
        private readonly AiParameters _prm;

        public StrategicAssessment(AiPerception perception, AiParameters parameters)
        {
            _p = perception;
            _prm = parameters;
            for (int i = 0; i < perception.OwnUnits.Count; i++)
            {
                LandUnit u = perception.OwnUnits[i];
                OwnPower += u.Strength(perception.Warfare.UnitTypes[u.Type]);
            }

            for (int i = 0; i < perception.Diplomacy.Wars.Count; i++)
            {
                War w = perception.Diplomacy.Wars.ValueAt(i);
                if (w.Involves(perception.Country))
                {
                    int at = Enemies.BinarySearch(w.Opponent(perception.Country));
                    if (at < 0)
                    {
                        Enemies.Insert(~at, w.Opponent(perception.Country));
                    }
                }
            }

            int owned = 0;
            int held = 0;
            for (int i = 0; i < perception.World.Cities.Count; i++)
            {
                perception.World.TryGetCity(perception.World.Cities[i], out City city);
                if (city.Owner == perception.Country)
                {
                    owned++;
                    if (city.Controller == perception.Country)
                    {
                        held++;
                    }
                }
            }

            OwnedCities = owned;
            HeldCityShare = owned == 0 ? Fixed64.One : Fixed64.FromRatio(held, owned);
        }

        public Fixed64 OwnPower { get; }

        public int OwnedCities { get; }

        public Fixed64 HeldCityShare { get; }

        /// <summary>Savaşta olunan ülkeler (kimlik sırasıyla).</summary>
        public List<CountryId> Enemies { get; } = new List<CountryId>();

        public bool AtWar => Enemies.Count > 0;

        /// <summary>Ülkenin tahmini kara gücü (savaş sisi altında).</summary>
        public Fixed64 EstimatedPower(CountryId country)
        {
            Fixed64 seen = Fixed64.Zero;
            ObservedWorld view = _p.Observed;
            for (int i = 0; i < view.Contacts.Count; i++)
            {
                ContactReport c = view.Contacts[i];
                if (c.Kind == SightingKind.LandUnit && c.Owner == country)
                {
                    seen += c.EstimatedStrength;
                }
            }

            int cities = 0;
            for (int i = 0; i < _p.World.Cities.Count; i++)
            {
                _p.World.TryGetCity(_p.World.Cities[i], out City city);
                if (city.Owner == country)
                {
                    cities++;
                }
            }

            return Fixed64.Max(seen, _prm.GarrisonEstimatePerCity * cities);
        }

        /// <summary>Tüm düşmanların tahmini toplam gücü.</summary>
        public Fixed64 EnemyPower()
        {
            Fixed64 sum = Fixed64.Zero;
            for (int i = 0; i < Enemies.Count; i++)
            {
                sum += EstimatedPower(Enemies[i]);
            }

            return sum;
        }

        /// <summary>Kaybediyor muyuz: şehirlerimizi yitiriyoruz veya güç oranı eşiğin altında.</summary>
        public bool IsLosing()
        {
            if (!AtWar)
            {
                return false;
            }

            Fixed64 enemy = EnemyPower();
            return HeldCityShare < _prm.LosingHeldShare || (enemy.IsPositive && OwnPower < enemy * _prm.LosingPowerRatio);
        }

        public static UnitTypeDefinition TypeOf(AiPerception p, LandUnit unit) => p.Warfare.UnitTypes[unit.Type];
    }
}
