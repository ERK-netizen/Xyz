using System;
using TarihStrateji.Core.Domain.Numerics;
using TarihStrateji.Core.World.Map;

namespace TarihStrateji.Core.Warfare.Definitions
{
    /// <summary>Muharebe ve yıpranma parametreleri (içerik verisi).</summary>
    public sealed class WarfareParameters
    {
        private readonly Fixed64[] _terrainDefense;

        public WarfareParameters(
            Fixed64[] terrainDefense,
            Fixed64 fortressBonusPerSector,
            Fixed64 baseDamage,
            Fixed64 strengthLossPerDamage,
            Fixed64 retreatOrganization,
            Fixed64 minimumAttackOrganization,
            Fixed64 garrisonDefense,
            Fixed64 besiegedGarrisonFactor,
            Fixed64 outOfSupplyOrganizationLoss,
            Fixed64 outOfSupplyStrengthLoss,
            Fixed64 organizationRecovery,
            Fixed64 reinforcementRate,
            Fixed64 destroyedBelowStrength,
            Fixed64 initialOrganization,
            Fixed64 randomSpread)
        {
            if (terrainDefense == null || terrainDefense.Length != TerrainRules.TerrainTypeCount)
            {
                throw new ArgumentException(WarfareErrorKeys.TanimGecersiz);
            }

            for (int i = 0; i < terrainDefense.Length; i++)
            {
                if (!terrainDefense[i].IsPositive)
                {
                    throw new ArgumentException(WarfareErrorKeys.TanimGecersiz);
                }
            }

            Fixed64[] fractions = { strengthLossPerDamage, retreatOrganization, minimumAttackOrganization, besiegedGarrisonFactor,
                outOfSupplyOrganizationLoss, outOfSupplyStrengthLoss, organizationRecovery, reinforcementRate, destroyedBelowStrength,
                initialOrganization, randomSpread };
            for (int i = 0; i < fractions.Length; i++)
            {
                if (fractions[i].IsNegative || fractions[i] > Fixed64.One)
                {
                    throw new ArgumentException(WarfareErrorKeys.TanimGecersiz);
                }
            }

            if (fortressBonusPerSector.IsNegative || !baseDamage.IsPositive || garrisonDefense.IsNegative)
            {
                throw new ArgumentException(WarfareErrorKeys.TanimGecersiz);
            }

            _terrainDefense = (Fixed64[])terrainDefense.Clone();
            FortressBonusPerSector = fortressBonusPerSector;
            BaseDamage = baseDamage;
            StrengthLossPerDamage = strengthLossPerDamage;
            RetreatOrganization = retreatOrganization;
            MinimumAttackOrganization = minimumAttackOrganization;
            GarrisonDefense = garrisonDefense;
            BesiegedGarrisonFactor = besiegedGarrisonFactor;
            OutOfSupplyOrganizationLoss = outOfSupplyOrganizationLoss;
            OutOfSupplyStrengthLoss = outOfSupplyStrengthLoss;
            OrganizationRecovery = organizationRecovery;
            ReinforcementRate = reinforcementRate;
            DestroyedBelowStrength = destroyedBelowStrength;
            InitialOrganization = initialOrganization;
            RandomSpread = randomSpread;
        }

        public Fixed64 TerrainDefense(TerrainType terrain) => _terrainDefense[(int)terrain];

        /// <summary>Savunanın elindeki her kale sektörü için savunma çarpanına eklenen.</summary>
        public Fixed64 FortressBonusPerSector { get; }

        /// <summary>Eşit güçte bir muharebenin her tarafa verdiği teşkilat hasarı (azami teşkilat oranı).</summary>
        public Fixed64 BaseDamage { get; }

        /// <summary>Hasarın güç kaybına dönüşme oranı.</summary>
        public Fixed64 StrengthLossPerDamage { get; }

        /// <summary>Savunanların teşkilat oranı bunun altına düşerse geri çekilirler.</summary>
        public Fixed64 RetreatOrganization { get; }

        public Fixed64 MinimumAttackOrganization { get; }

        /// <summary>Birliksiz şehrin sektör başına garnizon direnci (saldırı gücü birimiyle).</summary>
        public Fixed64 GarrisonDefense { get; }

        public Fixed64 BesiegedGarrisonFactor { get; }

        public Fixed64 OutOfSupplyOrganizationLoss { get; }

        public Fixed64 OutOfSupplyStrengthLoss { get; }

        public Fixed64 OrganizationRecovery { get; }

        public Fixed64 ReinforcementRate { get; }

        public Fixed64 DestroyedBelowStrength { get; }

        public Fixed64 InitialOrganization { get; }

        /// <summary>Muharebe zarının yarı genişliği: çarpan ∈ [1 − s, 1 + s].</summary>
        public Fixed64 RandomSpread { get; }
    }
}
