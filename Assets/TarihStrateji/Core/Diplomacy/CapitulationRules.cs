using System.Collections.Generic;
using TarihStrateji.Core.Diplomacy.Definitions;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.World;
using TarihStrateji.Core.World.Cities;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Diplomacy
{
    /// <summary>
    /// Kapitülasyon ve sürgün (P3). Savaştaki bir ülkenin sahip olduğu şehirlerden kontrolünde
    /// kalanların oranı <see cref="DiplomacyParameters.CapitulationThreshold"/>'un altına inerse:
    /// <list type="bullet">
    /// <item>Düşmanlarından biriyle savaşan bir müttefiki varsa hükümet o müttefiğe sürgüne gider;
    /// savaş sürer. Oran eşiğe dönünce sürgün biter; ev sahibi artık uygun değilse ülke teslim olur.</item>
    /// <item>Yoksa teslim olur: her düşmanla barış yapılır; düşmanın hücrelerinin
    /// <see cref="DiplomacyParameters.DemandControlShare"/>'dan fazlasını kontrol ettiği bölgeler ona geçer.</item>
    /// </list>
    /// Kapitülasyon tek taraflı barış değildir: yenilen tarafın direnme imkânı kalmadığında kuralla
    /// uygulanan teslimdir (CLAUDE.md §2.6).
    /// </summary>
    public static class CapitulationRules
    {
        public const string CapitulatedNote = "bilgi.diplomasi.teslimOldu";
        public const string ExiledNote = "bilgi.diplomasi.surguneGitti";
        public const string ReturnedNote = "bilgi.diplomasi.surgundenDondu";

        /// <summary>Sahip olunan şehirlerden fiilen kontrol edilenlerin oranı; şehri yoksa 1.</summary>
        public static Fixed64 HeldCityShare(WorldMap world, CountryId country)
        {
            int owned = 0;
            int held = 0;
            for (int i = 0; i < world.Cities.Count; i++)
            {
                City city = world.Cities.ValueAt(i);
                HexCell cell = world.Cell(city.Hex);
                if (cell.Owner == country)
                {
                    owned++;
                    if (cell.Controller == country)
                    {
                        held++;
                    }
                }
            }

            return owned == 0 ? Fixed64.One : Fixed64.FromRatio(held, owned);
        }

        /// <summary>Günlük denetim; teslim olan ülkeler <paramref name="capitulated"/>'a eklenir.</summary>
        public static void ApplyDay(DiplomacyState state, WorldMap world, DiplomacyParameters prm, GameDate today, List<CountryId> capitulated, List<string> notes)
        {
            var belligerents = new List<CountryId>();
            for (int i = 0; i < state.Wars.Count; i++)
            {
                War w = state.Wars.ValueAt(i);
                AddSorted(belligerents, w.Attacker);
                AddSorted(belligerents, w.Defender);
            }

            for (int i = 0; i < belligerents.Count; i++)
            {
                CountryId country = belligerents[i];
                if (!state.IsAtAnyWar(country))
                {
                    continue; // bu gün içinde başka bir teslimle barışa ulaştı
                }

                bool beaten = HeldCityShare(world, country) < prm.CapitulationThreshold;
                if (state.Exiles.TryGetValue(country, out CountryId host))
                {
                    if (!beaten)
                    {
                        state.Exiles.Remove(country);
                        notes.Add(ReturnedNote + " @ " + country.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else if (!IsValidHost(state, country, host, today))
                    {
                        state.Exiles.Remove(country);
                        Capitulate(state, world, prm, country, today, capitulated, notes);
                    }

                    continue;
                }

                if (!beaten)
                {
                    continue;
                }

                CountryId newHost = FindHost(state, country, today);
                if (newHost.IsValid)
                {
                    state.Exiles.Add(country, newHost);
                    notes.Add(ExiledNote + " @ " + country.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                else
                {
                    Capitulate(state, world, prm, country, today, capitulated, notes);
                }
            }
        }

        /// <summary>Ülke tüm düşmanlarına teslim olur (düşman kimliği sırasıyla barış).</summary>
        public static void Capitulate(DiplomacyState state, WorldMap world, DiplomacyParameters prm, CountryId country, GameDate today, List<CountryId> capitulated, List<string> notes)
        {
            var enemies = new List<CountryId>();
            for (int i = 0; i < state.Wars.Count; i++)
            {
                War w = state.Wars.ValueAt(i);
                if (w.Involves(country))
                {
                    AddSorted(enemies, w.Opponent(country));
                }
            }

            var transfers = new List<RegionTransfer>();
            for (int e = 0; e < enemies.Count; e++)
            {
                transfers.Clear();
                for (int r = 0; r < world.Regions.Count; r++)
                {
                    StrategicRegion region = world.Regions.ValueAt(r);
                    if (!region.IsSea && region.Owner == country && DiplomacyRules.ControlShare(world, region, enemies[e]) > prm.DemandControlShare)
                    {
                        transfers.Add(new RegionTransfer(region.Id, enemies[e]));
                    }
                }

                DiplomacyRules.ConcludePeace(state, world, prm, enemies[e], country, transfers, today);
            }

            capitulated.Add(country);
            notes.Add(CapitulatedNote + " @ " + country.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>Ev sahibi: ülkenin düşmanlarından biriyle savaşan, geçerli ittifaklı müttefik (antlaşma sırasıyla ilk).</summary>
        public static CountryId FindHost(DiplomacyState state, CountryId country, GameDate today)
        {
            for (int i = 0; i < state.Treaties.Count; i++)
            {
                Treaty t = state.Treaties.ValueAt(i);
                if (t.Kind == TreatyKind.Alliance && t.IsActiveOn(today) && (t.A == country || t.B == country) && IsValidHost(state, country, t.Other(country), today))
                {
                    return t.Other(country);
                }
            }

            return CountryId.None;
        }

        private static bool IsValidHost(DiplomacyState state, CountryId country, CountryId host, GameDate today)
        {
            if (!state.HasTreaty(TreatyKind.Alliance, country, host, today) || state.IsExiled(host))
            {
                return false;
            }

            for (int i = 0; i < state.Wars.Count; i++)
            {
                War w = state.Wars.ValueAt(i);
                if (w.Involves(country) && state.AtWar(host, w.Opponent(country)))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddSorted(List<CountryId> list, CountryId id)
        {
            int index = list.BinarySearch(id);
            if (index < 0)
            {
                list.Insert(~index, id);
            }
        }
    }
}
