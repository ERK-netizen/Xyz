using System.Collections.Generic;
using TarihStrateji.Core.Diplomacy.Definitions;
using TarihStrateji.Core.Diplomacy.State;
using TarihStrateji.Core.Domain.Commands;
using TarihStrateji.Core.Domain.Ids;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.Domain.Time;
using TarihStrateji.Core.World.Geography;

namespace TarihStrateji.Core.Diplomacy
{
    /// <summary>
    /// Savaş ilanı, teklifler ve barış. Tek taraflı barış yoktur: savaş yalnız muhatabın kabul ettiği
    /// bir barış teklifiyle (<see cref="Respond"/>) veya kapitülasyonla (<see cref="CapitulationRules"/>)
    /// biter. Savunanın müttefikleri saldırgana karşı savaşa otomatik katılır (savunma ittifakı).
    /// </summary>
    public static class DiplomacyRules
    {
        public static ValidationResult CheckDeclareWar(DiplomacyState state, GeoWorld world, CountryId attacker, CountryId defender, IReadOnlyList<RegionId> goals, GameDate today)
        {
            if (!ValidPair(attacker, defender))
            {
                return ValidationResult.Fail(DiplomacyErrorKeys.UlkeGecersiz);
            }

            if (state.IsExiled(attacker))
            {
                return ValidationResult.Fail(DiplomacyErrorKeys.SurgunHukumeti);
            }

            if (state.AtWar(attacker, defender))
            {
                return ValidationResult.Fail(DiplomacyErrorKeys.ZatenSavasta);
            }

            if (state.HasTreaty(TreatyKind.NonAggression, attacker, defender, today))
            {
                return ValidationResult.Fail(DiplomacyErrorKeys.SaldirmazlikPakti);
            }

            if (state.HasTreaty(TreatyKind.Alliance, attacker, defender, today))
            {
                return ValidationResult.Fail(DiplomacyErrorKeys.Muttefik);
            }

            if (state.HasTreaty(TreatyKind.Peace, attacker, defender, today))
            {
                return ValidationResult.Fail(DiplomacyErrorKeys.Ateskes);
            }

            for (int i = 0; i < goals.Count; i++)
            {
                if (!world.TryGetControl(goals[i], out RegionControl region) || region.Owner != defender || IndexOf(goals, goals[i]) != i)
                {
                    return ValidationResult.Fail(DiplomacyErrorKeys.SavasHedefiGecersiz);
                }
            }

            return ValidationResult.Ok;
        }

        /// <summary>Savaşı başlatır; savunanın müttefikleri antlaşma kimliği sırasıyla katılır. Açılan savaşları döndürür.</summary>
        public static List<WarId> DeclareWar(DiplomacyState state, CountryId attacker, CountryId defender, IReadOnlyList<RegionId> goals, GameDate today)
        {
            var opened = new List<WarId>();
            War main = Open(state, attacker, defender, today, WarId.None);
            for (int i = 0; i < goals.Count; i++)
            {
                main.GoalList.Add(goals[i]);
            }

            opened.Add(main.Id);
            for (int i = 0; i < state.Treaties.Count; i++)
            {
                Treaty t = state.Treaties.ValueAt(i);
                if (t.Kind != TreatyKind.Alliance || !t.IsActiveOn(today) || (t.A != defender && t.B != defender))
                {
                    continue;
                }

                CountryId ally = t.Other(defender);
                if (ally == attacker || state.AtWar(ally, attacker) || state.HasTreaty(TreatyKind.Alliance, ally, attacker, today))
                {
                    continue;
                }

                opened.Add(Open(state, attacker, ally, today, main.Id).Id);
            }

            state.RebuildIndex();
            return opened;
        }

        /// <summary>Kurulum ve testler için: müttefik çağrısı olmadan tek bir savaş çifti açar.</summary>
        public static WarId StartWar(DiplomacyState state, CountryId attacker, CountryId defender, GameDate today)
        {
            War war = Open(state, attacker, defender, today, WarId.None);
            state.RebuildIndex();
            return war.Id;
        }

        private static War Open(DiplomacyState state, CountryId attacker, CountryId defender, GameDate today, WarId calledBy)
        {
            var war = new War(new WarId(state.WarIds.Allocate()), attacker, defender, today, calledBy);
            state.Wars.Add(war.Id, war);
            return war;
        }

        // ------------------------------------------------------------------ teklifler

        public static ValidationResult CheckPropose(DiplomacyState state, GeoWorld world, DiplomacyParameters prm, ProposalKind kind, CountryId from, CountryId to, IReadOnlyList<RegionTransfer> transfers, GameDate today)
        {
            if (!ValidPair(from, to))
            {
                return ValidationResult.Fail(DiplomacyErrorKeys.UlkeGecersiz);
            }

            for (int i = 0; i < state.Proposals.Count; i++)
            {
                Proposal p = state.Proposals.ValueAt(i);
                if (p.Status == ProposalStatus.Pending && p.Kind == kind && ((p.From == from && p.To == to) || (p.From == to && p.To == from)))
                {
                    return ValidationResult.Fail(DiplomacyErrorKeys.TeklifZatenBekliyor);
                }
            }

            return CheckTerms(state, world, prm, kind, from, to, transfers, today);
        }

        /// <summary>Teklif koşulları (teklif anında ve kabul anında yeniden denetlenir).</summary>
        public static ValidationResult CheckTerms(DiplomacyState state, GeoWorld world, DiplomacyParameters prm, ProposalKind kind, CountryId from, CountryId to, IReadOnlyList<RegionTransfer> transfers, GameDate today)
        {
            switch (kind)
            {
                case ProposalKind.Peace:
                    if (!state.AtWar(from, to))
                    {
                        return ValidationResult.Fail(DiplomacyErrorKeys.SavasYok);
                    }

                    return CheckTransfers(world, prm, from, to, transfers);
                case ProposalKind.NonAggression:
                case ProposalKind.Alliance:
                    if (transfers.Count > 0)
                    {
                        return ValidationResult.Fail(DiplomacyErrorKeys.DevirGecersiz);
                    }

                    if (state.AtWar(from, to))
                    {
                        return ValidationResult.Fail(DiplomacyErrorKeys.ZatenSavasta);
                    }

                    TreatyKind treaty = kind == ProposalKind.Alliance ? TreatyKind.Alliance : TreatyKind.NonAggression;
                    return state.HasTreaty(treaty, from, to, today) ? ValidationResult.Fail(DiplomacyErrorKeys.AntlasmaZatenVar) : ValidationResult.Ok;
                default:
                    return ValidationResult.Fail(DiplomacyErrorKeys.TanimGecersiz);
            }
        }

        /// <summary>
        /// Barış maddeleri: her bölge iki taraftan birine ait ve yeni sahibi diğer taraf olmalı; bir
        /// bölge en fazla bir kez geçer. Bir tarafın kendine istediği bölgede <see cref="ControlShare"/>
        /// <see cref="DiplomacyParameters.DemandControlShare"/>'dan büyük olmalıdır
        /// (işgalle desteklenmeyen talep yok); bir taraf kendi bölgesini serbestçe devredebilir.
        /// </summary>
        public static ValidationResult CheckTransfers(GeoWorld world, DiplomacyParameters prm, CountryId from, CountryId to, IReadOnlyList<RegionTransfer> transfers)
        {
            for (int i = 0; i < transfers.Count; i++)
            {
                RegionTransfer t = transfers[i];
                if (!world.TryGetControl(t.Region, out RegionControl region) || (t.NewOwner != from && t.NewOwner != to))
                {
                    return ValidationResult.Fail(DiplomacyErrorKeys.DevirGecersiz);
                }

                CountryId giver = t.NewOwner == from ? to : from;
                if (region.Owner != giver)
                {
                    return ValidationResult.Fail(DiplomacyErrorKeys.DevirGecersiz);
                }

                for (int j = 0; j < i; j++)
                {
                    if (transfers[j].Region == t.Region)
                    {
                        return ValidationResult.Fail(DiplomacyErrorKeys.DevirGecersiz);
                    }
                }

                if (ControlShare(world, region.Region, t.NewOwner) <= prm.DemandControlShare && t.NewOwner == from)
                {
                    return ValidationResult.Fail(DiplomacyErrorKeys.IsgalYetersiz);
                }
            }

            return ValidationResult.Ok;
        }

        public static ProposalId Propose(DiplomacyState state, DiplomacyParameters prm, ProposalKind kind, CountryId from, CountryId to, IReadOnlyList<RegionTransfer> transfers, GameDate today)
        {
            var p = new Proposal(new ProposalId(state.ProposalIds.Allocate()), kind, from, to, today, today.AddDays(prm.ProposalLifetimeDays - 1));
            for (int i = 0; i < transfers.Count; i++)
            {
                p.TransferList.Add(transfers[i]);
            }

            state.Proposals.Add(p.Id, p);
            return p.Id;
        }

        public static ValidationResult CheckRespond(DiplomacyState state, GeoWorld world, DiplomacyParameters prm, CountryId country, ProposalId id, bool accept, GameDate today)
        {
            if (!state.Proposals.TryGetValue(id, out Proposal p))
            {
                return ValidationResult.Fail(DiplomacyErrorKeys.TeklifBulunamadi);
            }

            if (p.To != country)
            {
                return ValidationResult.Fail(DiplomacyErrorKeys.TeklifBizeDegil);
            }

            if (p.Status != ProposalStatus.Pending || today > p.LastDay)
            {
                return ValidationResult.Fail(DiplomacyErrorKeys.TeklifBeklemedeDegil);
            }

            return accept ? CheckTerms(state, world, prm, p.Kind, p.From, p.To, p.Transfers, today) : ValidationResult.Ok;
        }

        /// <summary>
        /// Yanıt. Kabul edilen antlaşma imzalanır; kabul edilen barış <see cref="ConcludePeace"/> ile
        /// uygulanır. Barış uygulandıysa <c>true</c> döner (Simulation birlikleri yerleştirir).
        /// </summary>
        public static bool Respond(DiplomacyState state, GeoWorld world, DiplomacyParameters prm, ProposalId id, bool accept, GameDate today)
        {
            Proposal p = state.Proposals[id];
            if (!accept)
            {
                p.Status = ProposalStatus.Rejected;
                return false;
            }

            p.Status = ProposalStatus.Accepted;
            switch (p.Kind)
            {
                case ProposalKind.NonAggression:
                    Sign(state, TreatyKind.NonAggression, p.From, p.To, today, prm.NonAggressionDays);
                    return false;
                case ProposalKind.Alliance:
                    Sign(state, TreatyKind.Alliance, p.From, p.To, today, prm.AllianceDays);
                    return false;
                default:
                    ConcludePeace(state, world, prm, p.From, p.To, p.Transfers, today);
                    return true;
            }
        }

        /// <summary>
        /// Barışı uygular: çift savaşı biter, bölgeler devredilir, iki taraf arasındaki işgaller
        /// sahibine döner (bölge ve şehir), ateşkes antlaşması imzalanır, ilgili bekleyen barış
        /// teklifleri düşer.
        /// </summary>
        public static void ConcludePeace(DiplomacyState state, GeoWorld world, DiplomacyParameters prm, CountryId a, CountryId b, IReadOnlyList<RegionTransfer> transfers, GameDate today)
        {
            if (state.TryGetWar(a, b, out War war))
            {
                state.Wars.Remove(war.Id);
                state.RebuildIndex();
            }

            for (int i = 0; i < transfers.Count; i++)
            {
                world.TransferRegionOwnership(transfers[i].Region, transfers[i].NewOwner);
            }

            RestoreControl(world, a, b);
            if (prm.TruceDays > 0)
            {
                Sign(state, TreatyKind.Peace, a, b, today, prm.TruceDays);
            }

            for (int i = 0; i < state.Proposals.Count; i++)
            {
                Proposal p = state.Proposals.ValueAt(i);
                if (p.Status == ProposalStatus.Pending && p.Kind == ProposalKind.Peace && ((p.From == a && p.To == b) || (p.From == b && p.To == a)))
                {
                    p.Status = ProposalStatus.Expired;
                }
            }
        }

        /// <summary>İki taraf arasında karşılıklı işgal edilen bölge ve şehirlerin kontrolü sahibine döner.</summary>
        public static void RestoreControl(GeoWorld world, CountryId a, CountryId b)
        {
            for (int i = 0; i < world.Regions.Count; i++)
            {
                if (world.TryGetControl(world.Regions[i], out RegionControl control)
                    && Crossed(control.Owner, control.Controller, a, b))
                {
                    GeoWorld.ResetController(control);
                }
            }

            for (int i = 0; i < world.Cities.Count; i++)
            {
                world.TryGetCity(world.Cities[i], out City city);
                if (Crossed(city.Owner, city.Controller, a, b))
                {
                    city.Controller = city.Owner;
                }
            }
        }

        /// <summary>P3: süresi dolan teklifler düşer, süresi dolan antlaşmalar kaldırılır.</summary>
        public static void ExpireDay(DiplomacyState state, GameDate today)
        {
            for (int i = 0; i < state.Proposals.Count; i++)
            {
                Proposal p = state.Proposals.ValueAt(i);
                if (p.Status == ProposalStatus.Pending && today > p.LastDay)
                {
                    p.Status = ProposalStatus.Expired;
                }
            }

            var ended = new List<TreatyId>();
            for (int i = 0; i < state.Treaties.Count; i++)
            {
                Treaty t = state.Treaties.ValueAt(i);
                if (today > t.LastDay)
                {
                    ended.Add(t.Id);
                }
            }

            for (int i = 0; i < ended.Count; i++)
            {
                state.Treaties.Remove(ended[i]);
            }
        }

        /// <summary>
        /// Bölgede <paramref name="country"/>'nin fiilî payı: bölge kontrolü ve bölgedeki her şehrin kontrolü
        /// birer pay sayılır (held / (1 + şehir sayısı)). Bölge yoksa 0.
        /// </summary>
        public static Fixed64 ControlShare(GeoWorld world, RegionId region, CountryId country)
        {
            if (!world.TryGetControl(region, out RegionControl control))
            {
                return Fixed64.Zero;
            }

            int total = 1;
            int held = control.Controller == country ? 1 : 0;
            for (int i = 0; i < world.Cities.Count; i++)
            {
                world.TryGetCity(world.Cities[i], out City city);
                if (city.Region == region)
                {
                    total++;
                    if (city.Controller == country)
                    {
                        held++;
                    }
                }
            }

            return Fixed64.FromRatio(held, total);
        }

        internal static void Sign(DiplomacyState state, TreatyKind kind, CountryId a, CountryId b, GameDate today, int days)
        {
            var t = new Treaty(new TreatyId(state.TreatyIds.Allocate()), kind, a, b, today, today.AddDays(days - 1));
            state.Treaties.Add(t.Id, t);
        }

        private static bool Crossed(CountryId owner, CountryId controller, CountryId a, CountryId b)
        {
            return (owner == a && controller == b) || (owner == b && controller == a);
        }

        private static bool ValidPair(CountryId a, CountryId b)
        {
            return a.IsValid && b.IsValid && a != b && a.Value <= 0xFFFFFFFFUL && b.Value <= 0xFFFFFFFFUL;
        }

        private static int IndexOf(IReadOnlyList<RegionId> list, RegionId id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == id)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
