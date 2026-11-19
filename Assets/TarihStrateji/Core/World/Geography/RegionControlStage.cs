namespace TarihStrateji.Core.World.Geography
{
    /// <summary>Operasyonel bölge kontrol aşaması (§4.4): çekişmeli → işgal → güvenli.</summary>
    public enum RegionControlStage : byte
    {
        Secured = 0,
        Contested = 1,
        Occupied = 2,
    }
}
