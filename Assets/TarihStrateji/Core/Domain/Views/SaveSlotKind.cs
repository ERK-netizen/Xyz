namespace TarihStrateji.Core.Domain.Views
{
    /// <summary>Kayıt yuvasının türü (Adım 20).</summary>
    public enum SaveSlotKind
    {
        /// <summary>Oyuncunun elle aldığı yuva (<c>yuva_1..3</c>); üzerine yazılabilir, silinebilir.</summary>
        Manual = 0,

        /// <summary>Otomatik kayıt halkası (<c>otomatik_*</c>, 30 günde bir).</summary>
        Autosave = 1,

        /// <summary>Uygulama arka plana alınınca/kapanınca alınan kayıt (<c>son_oturum</c>).</summary>
        LastSession = 2,
    }
}
