namespace TarihStrateji.Core.Characters.Definitions
{
    /// <summary>Pozisyon kuralları.</summary>
    public static class Positions
    {
        public const int Count = 7;

        public static bool IsDefined(PositionKind position) => (int)position >= 0 && (int)position < Count;

        public static CharacterRole RequiredRole(PositionKind position)
        {
            switch (position)
            {
                case PositionKind.HeadOfGovernment:
                    return CharacterRole.Leader;
                case PositionKind.ChiefOfGeneralStaff:
                    return CharacterRole.General;
                case PositionKind.ChiefOfNavalStaff:
                    return CharacterRole.Admiral;
                default:
                    return CharacterRole.Minister;
            }
        }
    }
}
