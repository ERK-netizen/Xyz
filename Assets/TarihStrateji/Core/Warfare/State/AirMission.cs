namespace TarihStrateji.Core.Warfare.State
{
    /// <summary>Hava kanadı görevi. Değerler kayıt biçimine yazılır; asla yeniden numaralanmaz.</summary>
    public enum AirMission
    {
        /// <summary>Üste bekler, teşkilat toparlar.</summary>
        None = 0,

        /// <summary>Hedef hücre üzerinde düşman uçaklarıyla çarpışır (hava üstünlüğü).</summary>
        AirSuperiority = 1,

        /// <summary>Kara hücresindeki muharebeye yakın hava desteği.</summary>
        GroundSupport = 2,

        /// <summary>Deniz hücresindeki filo muharebesine hava desteği.</summary>
        NavalStrike = 3,
    }
}
