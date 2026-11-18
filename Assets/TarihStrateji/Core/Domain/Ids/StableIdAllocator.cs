using System;

namespace TarihStrateji.Core.Domain.Ids
{
    /// <summary>
    /// Tek bir varlık türü için monoton kimlik dağıtıcısı. Silinen bir varlığın kimliği
    /// asla yeniden verilmez; dağıtıcının durumu (<see cref="NextValue"/>) kayda yazılır.
    /// </summary>
    [Serializable]
    public sealed class StableIdAllocator
    {
        private ulong _nextValue;

        public StableIdAllocator()
            : this(1UL)
        {
        }

        /// <param name="nextValue">Kayıttan geri yüklenen bir sonraki değer (en az 1).</param>
        public StableIdAllocator(ulong nextValue)
        {
            if (nextValue == 0UL)
            {
                throw new ArgumentOutOfRangeException(nameof(nextValue), DomainErrorKeys.KimlikGecersiz);
            }

            _nextValue = nextValue;
        }

        /// <summary>Bir sonraki çağrıda verilecek değer.</summary>
        public ulong NextValue => _nextValue;

        /// <summary>Yeni, daha önce hiç verilmemiş bir ham kimlik değeri ayırır.</summary>
        public ulong Allocate()
        {
            if (_nextValue == ulong.MaxValue)
            {
                throw new InvalidOperationException(DomainErrorKeys.KimlikTukendi);
            }

            ulong value = _nextValue;
            _nextValue++;
            return value;
        }

        /// <summary>
        /// İçerikten (DEF) gelen sabit bir kimliği rezerve eder: dağıtıcı bu değerin
        /// altına bir daha inmez. Zaten verilmiş bir aralıktaki değer için işlem yapmaz.
        /// </summary>
        public void Reserve(ulong value)
        {
            if (value == 0UL)
            {
                throw new ArgumentOutOfRangeException(nameof(value), DomainErrorKeys.KimlikGecersiz);
            }

            if (value == ulong.MaxValue)
            {
                throw new InvalidOperationException(DomainErrorKeys.KimlikTukendi);
            }

            if (value >= _nextValue)
            {
                _nextValue = value + 1UL;
            }
        }
    }
}
