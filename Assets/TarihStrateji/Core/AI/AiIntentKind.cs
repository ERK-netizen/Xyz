namespace TarihStrateji.Core.AI
{
    /// <summary>YZ'nin verdiği karar türü; Simulation bunu ilgili emre çevirir.</summary>
    public enum AiIntentKind : byte
    {
        Research = 1,
        SelectFocus = 2,
        ChooseEventOption = 3,
        AcceptProposal = 4,
        RejectProposal = 5,
        ProposePeace = 6,
        ProposeAlliance = 7,
        DeclareWar = 8,
        Recruit = 9,
        Move = 10,
        Attack = 11,
        Build = 12,
        SetTaxRate = 13,
    }
}
