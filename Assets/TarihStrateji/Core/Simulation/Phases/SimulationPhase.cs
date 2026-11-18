namespace TarihStrateji.Core.Simulation.Phases
{
    /// <summary>Günlük faz sırası (CLAUDE.md §2.5). Sayısal değer yürütme sırasıdır.</summary>
    public enum SimulationPhase
    {
        P0DayOpen = 0,
        P1Orders = 1,
        P2EventsAndEffects = 2,
        P3Diplomacy = 3,
        P4Politics = 4,
        P5Research = 5,
        P6Population = 6,
        P7Workforce = 7,
        P8Production = 8,
        P9Construction = 9,
        P10TradeAndBudget = 10,
        P11Logistics = 11,
        P12Movement = 12,
        P13Combat = 13,
        P14ControlAndIntelligence = 14,
        P15AiInvariantsAndSave = 15,
    }
}
