using TarihStrateji.Core.Domain.Numerics;

namespace TarihStrateji.Core.Economy.Rules
{
    /// <summary>
    /// Bir toplamı ağırlıklara orantılı ve **tam** bölüştürür: Σ sonuç == toplam, her pay kendi
    /// üst sınırını aşmaz. Yuvarlama farkı (birkaç 10⁻⁶ birim) dizin sırasıyla düzeltilir.
    /// </summary>
    public static class ProRata
    {
        /// <param name="total">Bölüştürülecek miktar; Σ caps'i aşmamalı.</param>
        /// <param name="caps">Her payın üst sınırı (ağırlık olarak da kullanılır).</param>
        public static void Allocate(Fixed64 total, Fixed64[] caps, int count, Fixed64[] result)
        {
            Fixed64 weightSum = Fixed64.Zero;
            for (int i = 0; i < count; i++)
            {
                weightSum += caps[i];
                result[i] = Fixed64.Zero;
            }

            if (!total.IsPositive || !weightSum.IsPositive)
            {
                return;
            }

            if (total > weightSum)
            {
                total = weightSum;
            }

            Fixed64 assigned = Fixed64.Zero;
            for (int i = 0; i < count; i++)
            {
                Fixed64 share = Fixed64.MulDiv(total, caps[i], weightSum);
                result[i] = Fixed64.Min(share, caps[i]);
                assigned += result[i];
            }

            long diff = (total - assigned).Raw;
            for (int i = 0; diff > 0L && i < count; i++)
            {
                long room = (caps[i] - result[i]).Raw;
                long add = room < diff ? room : diff;
                result[i] = Fixed64.FromRaw(result[i].Raw + add);
                diff -= add;
            }

            for (int i = count - 1; diff < 0L && i >= 0; i--)
            {
                long take = result[i].Raw < -diff ? result[i].Raw : -diff;
                result[i] = Fixed64.FromRaw(result[i].Raw - take);
                diff += take;
            }
        }
    
        /// <summary>
        /// Toplamı ağırlıklara orantılı ve tam bölüştürür; üst sınır yoktur (ör. satıcılara
        /// ödenen para, satılan miktarla orantılıdır ama miktarla sınırlı değildir).
        /// Σ sonuç == toplam; ağırlığı sıfır olan pay almaz.
        /// </summary>
        public static void AllocateByWeight(Fixed64 total, Fixed64[] weights, int count, Fixed64[] result)
        {
            Fixed64 weightSum = Fixed64.Zero;
            int last = -1;
            for (int i = 0; i < count; i++)
            {
                result[i] = Fixed64.Zero;
                if (weights[i].IsPositive)
                {
                    weightSum += weights[i];
                    last = i;
                }
            }

            if (!total.IsPositive || last < 0)
            {
                return;
            }

            Fixed64 assigned = Fixed64.Zero;
            for (int i = 0; i < count; i++)
            {
                if (weights[i].IsPositive)
                {
                    result[i] = Fixed64.MulDiv(total, weights[i], weightSum);
                    assigned += result[i];
                }
            }

            // Yuvarlama farkı her pay için en fazla 1 birimdir; ağırlıklı paylar üzerinde dairesel
            // olarak birer birim düzeltilir. Fark negatifse, Σ > toplam olduğundan pozitif pay vardır.
            long diff = (total - assigned).Raw;
            int index = 0;
            while (diff != 0L)
            {
                if (weights[index].IsPositive)
                {
                    if (diff > 0L)
                    {
                        result[index] = Fixed64.FromRaw(result[index].Raw + 1L);
                        diff--;
                    }
                    else if (result[index].IsPositive)
                    {
                        result[index] = Fixed64.FromRaw(result[index].Raw - 1L);
                        diff++;
                    }
                }

                index = index + 1 == count ? 0 : index + 1;
            }
        }
    }
}
