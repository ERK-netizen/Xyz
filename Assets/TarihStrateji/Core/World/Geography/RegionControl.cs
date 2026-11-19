using TarihStrateji.Core.Domain.Ids;

namespace TarihStrateji.Core.World.Geography
{
    /// <summary>
    /// Bölgenin dinamik kontrol durumu. <see cref="Owner"/> hukukî sahiptir ve yalnız barışla değişir;
    /// <see cref="Controller"/> fiilî kontrolcüdür. Çekişmede <see cref="Challenger"/> ve kesintisiz
    /// tutma günü (<see cref="DaysHeld"/>) izlenir.
    /// </summary>
    public sealed class RegionControl
    {
        public RegionControl(RegionId region, CountryId owner)
        {
            Region = region;
            Owner = owner;
            Controller = owner;
            Stage = RegionControlStage.Secured;
        }

        public RegionId Region { get; }

        public CountryId Owner { get; set; }

        public CountryId Controller { get; set; }

        public RegionControlStage Stage { get; set; }

        public CountryId Challenger { get; set; }

        public int DaysHeld { get; set; }
    }
}
