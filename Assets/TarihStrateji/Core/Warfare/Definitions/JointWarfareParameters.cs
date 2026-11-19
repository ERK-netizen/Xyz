using System;
using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Warfare.Definitions
{
    /// <summary>
    /// Deniz, hava ve ortak harekât ayarları (veri; kodda sabit yoktur).
    /// <list type="bullet">
    /// <item><see cref="AmphibiousAttackFactor"/>: gemiden yapılan çıkarma saldırısının güç çarpanı.</item>
    /// <item><see cref="ConvoySupplyLevel"/>: deniz yoluyla ikmal edilen işgal limanının kaynak seviyesi.</item>
    /// <item><see cref="PortRecovery"/>: kendi limanındaki filonun günlük teşkilat toparlanması (azami oranı).</item>
    /// <item><see cref="AirMissionOrganizationCost"/> / <see cref="AirRecovery"/>: görevdeki kanadın günlük
    /// teşkilat gideri / üste bekleyen kanadın toparlanması.</item>
    /// <item><see cref="AirWingsPerCity"/>: bir şehrin hava üssü olarak barındırabileceği kanat sayısı.</item>
    /// </list>
    /// </summary>
    public sealed class JointWarfareParameters
    {
        public JointWarfareParameters(
            Fixed64 amphibiousAttackFactor,
            Fixed64 convoySupplyLevel,
            Fixed64 portRecovery,
            Fixed64 airMissionOrganizationCost,
            Fixed64 airRecovery,
            int airWingsPerCity)
        {
            Fixed64[] fractions = { amphibiousAttackFactor, convoySupplyLevel, portRecovery, airMissionOrganizationCost, airRecovery };
            for (int i = 0; i < fractions.Length; i++)
            {
                if (fractions[i].IsNegative || fractions[i] > Fixed64.One)
                {
                    throw new ArgumentException(WarfareErrorKeys.TanimGecersiz);
                }
            }

            if (airWingsPerCity < 0)
            {
                throw new ArgumentException(WarfareErrorKeys.TanimGecersiz);
            }

            AmphibiousAttackFactor = amphibiousAttackFactor;
            ConvoySupplyLevel = convoySupplyLevel;
            PortRecovery = portRecovery;
            AirMissionOrganizationCost = airMissionOrganizationCost;
            AirRecovery = airRecovery;
            AirWingsPerCity = airWingsPerCity;
        }

        public Fixed64 AmphibiousAttackFactor { get; }

        public Fixed64 ConvoySupplyLevel { get; }

        public Fixed64 PortRecovery { get; }

        public Fixed64 AirMissionOrganizationCost { get; }

        public Fixed64 AirRecovery { get; }

        public int AirWingsPerCity { get; }
    }
}
