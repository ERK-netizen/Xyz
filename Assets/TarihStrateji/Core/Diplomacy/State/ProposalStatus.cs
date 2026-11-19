namespace TarihStrateji.Core.Diplomacy.State
{
    /// <summary>Teklif durumu: yalnız <see cref="Pending"/> → kabul/ret/süre dolumu. Değerler kayda yazılır.</summary>
    public enum ProposalStatus : byte
    {
        Pending = 1,
        Accepted = 2,
        Rejected = 3,
        Expired = 4,
    }
}
