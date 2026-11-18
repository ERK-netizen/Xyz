using System;

namespace TarihStrateji.Core.Characters.Definitions
{
    /// <summary>Karakterin üstlenebileceği roller (bayrak).</summary>
    [Flags]
    public enum CharacterRole : byte
    {
        None = 0,
        Leader = 1,
        General = 2,
        Admiral = 4,
        Minister = 8,
    }
}
