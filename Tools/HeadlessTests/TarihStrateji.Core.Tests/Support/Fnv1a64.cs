namespace TarihStrateji.Core.Tests.Support
{
    /// <summary>Altın (golden) determinizm testleri için basit, platformdan bağımsız hash.</summary>
    public sealed class Fnv1a64
    {
        private const ulong Offset = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        public ulong Value { get; private set; } = Offset;

        public void Add(long value)
        {
            ulong v = unchecked((ulong)value);
            for (int i = 0; i < 8; i++)
            {
                Value ^= (v >> (i * 8)) & 0xFFUL;
                Value = unchecked(Value * Prime);
            }
        }
    }
}
