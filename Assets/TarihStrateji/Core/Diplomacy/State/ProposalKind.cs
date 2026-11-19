namespace TarihStrateji.Core.Diplomacy.State
{
    /// <summary>Teklif türü. Değerler kayıt biçimine yazılır; asla yeniden numaralanmaz.</summary>
    public enum ProposalKind : byte
    {
        Peace = 1,
        NonAggression = 2,
        Alliance = 3,
    }
}
