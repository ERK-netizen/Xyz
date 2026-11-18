namespace TarihStrateji.Core.World.Map
{
    /// <summary>Araziye bağlı sabit kurallar.</summary>
    public static class TerrainRules
    {
        public const int TerrainTypeCount = 9;

        public static bool IsWater(TerrainType terrain)
        {
            return terrain == TerrainType.Ocean || terrain == TerrainType.Lake;
        }

        public static bool IsDefined(TerrainType terrain)
        {
            return (int)terrain >= 0 && (int)terrain < TerrainTypeCount;
        }
    }
}
