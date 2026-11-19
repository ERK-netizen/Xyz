namespace TarihStrateji.Core.Domain.Randomness
{
    /// <summary>
    /// PRNG anahtarının alt sistem bileşeni. Değerler kayıt ve yeniden oynatma uyumluluğunun
    /// parçasıdır: mevcut bir değer asla değiştirilmez, yenisi sona eklenir.
    /// </summary>
    public enum RngSubsystem : ulong
    {
        Kernel = 1,
        Events = 2,
        Diplomacy = 3,
        Politics = 4,
        Research = 5,
        Population = 6,
        Economy = 7,
        Logistics = 8,
        Movement = 9,
        Combat = 10,
        Intelligence = 11,
        AI = 12,
        Tactical = 13,
        NavalCombat = 14,
        AirCombat = 15,
    }
}
