using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TarihStrateji.Unity.Bootstrap
{
    /// <summary>
    /// Cihaz ölçümünün sayıları (Adım 17): kare süreleri, gün adımı süreleri (mikrosaniye, tamsayı), kopya kurma
    /// ve kayıt süresi, anlık görüntü boyutu, bellek. Yüzdelikler sıralı örnekte en yakın sıra yöntemiyle
    /// (P = ⌊(n−1)·p/100 + ½⌋). Yalnız ölçer ve raporlar; oyun kuralı değildir. Rapor Türkçe düz metindir
    /// (ondalık virgül), cihazdaki dosyaya ve günlüğe aynen yazılır.
    /// </summary>
    public sealed class MeasurementLog
    {
        private readonly List<long> _frames = new List<long>(512);
        private readonly List<long> _days = new List<long>(128);

        public IReadOnlyList<long> FrameMicros => _frames;

        public IReadOnlyList<long> DayMicros => _days;

        public long CopyMicros { get; set; }

        public long SaveMicros { get; set; }

        public long SnapshotBytes { get; set; }

        public long ManagedBytes { get; set; }

        public long AllocatedBytes { get; set; }

        public string Device { get; set; } = string.Empty;

        public void AddFrame(long micros) => _frames.Add(micros);

        public void AddDay(long micros) => _days.Add(micros);

        /// <summary>Sıralanmamış örneğin p. yüzdeliği (boşsa 0); örnek değiştirilmez.</summary>
        public static long Percentile(IReadOnlyList<long> values, int p)
        {
            if (values == null || values.Count == 0)
            {
                return 0L;
            }

            var sorted = new List<long>(values);
            sorted.Sort();
            int clamped = p < 0 ? 0 : p > 100 ? 100 : p;
            int index = (((sorted.Count - 1) * clamped) + 50) / 100;
            return sorted[index];
        }

        /// <summary>Mikrosaniye → "12,3 ms".</summary>
        public static string Millis(long micros)
        {
            long tenths = (micros + (micros >= 0 ? 50L : -50L)) / 100L;
            long magnitude = tenths < 0 ? -tenths : tenths;
            return (tenths < 0 ? "-" : string.Empty) + (magnitude / 10L).ToString(CultureInfo.InvariantCulture) + ","
                + (magnitude % 10L).ToString(CultureInfo.InvariantCulture) + " ms";
        }

        /// <summary>Bayt → "3,4 MB" (1 MB = 1 048 576 bayt).</summary>
        public static string Megabytes(long bytes)
        {
            long tenths = ((bytes * 10L) + 524_288L) / 1_048_576L;
            return (tenths / 10L).ToString(CultureInfo.InvariantCulture) + "," + (tenths % 10L).ToString(CultureInfo.InvariantCulture) + " MB";
        }

        /// <summary>Kare süresinden saniyedeki kare (P50 kareye göre, tam sayı).</summary>
        public static long FramesPerSecond(long frameMicros) => frameMicros <= 0 ? 0L : (1_000_000L + (frameMicros / 2L)) / frameMicros;

        /// <summary>Tek satırlık özet (ekranda gösterilir).</summary>
        public string Summary()
        {
            long dayP50 = Percentile(_days, 50);
            long frameP50 = Percentile(_frames, 50);
            return "Ölçüm: gün P50 " + Millis(dayP50) + ", P95 " + Millis(Percentile(_days, 95)) + " · kare P50 " + Millis(frameP50)
                + " (" + FramesPerSecond(frameP50).ToString(CultureInfo.InvariantCulture) + " fps) · kayıt " + Millis(SaveMicros)
                + " · bellek " + Megabytes(AllocatedBytes);
        }

        /// <summary>Ayrıntılı rapor (dosyaya yazılır).</summary>
        public string Report(string when)
        {
            var b = new StringBuilder(1024);
            b.Append("Tarih Strateji — cihaz ölçümü (Adım 17)\n");
            b.Append("Zaman: ").Append(when).Append('\n');
            b.Append("Cihaz: ").Append(Device).Append("\n\n");
            Line(b, "Kare süresi (", _frames);
            Line(b, "Gün adımı, YZ dahil (", _days);
            b.Append("Kopya kurma (anlık görüntü + geri yükleme): ").Append(Millis(CopyMicros)).Append('\n');
            b.Append("Kayıt (atomik yazım): ").Append(Millis(SaveMicros)).Append(" · anlık görüntü ").Append(Megabytes(SnapshotBytes)).Append('\n');
            b.Append("Bellek: yönetilen ").Append(Megabytes(ManagedBytes)).Append(" · ayrılmış toplam ").Append(Megabytes(AllocatedBytes)).Append('\n');
            b.Append("\nHedefler (Docs/CIHAZ_OLCUMU.md): gün adımı P95 ≤ 50 ms (4 gün/kare hızında 60 fps'e yakın), kare P50 ≤ 33,3 ms (≥ 30 fps), kayıt ≤ 500 ms.\n");
            b.Append(Summary()).Append('\n');
            return b.ToString();
        }

        private static void Line(StringBuilder b, string title, IReadOnlyList<long> values)
        {
            b.Append(title).Append(values.Count.ToString(CultureInfo.InvariantCulture)).Append(" örnek): P50 ").Append(Millis(Percentile(values, 50)))
                .Append(" · P95 ").Append(Millis(Percentile(values, 95))).Append(" · en çok ").Append(Millis(Percentile(values, 100))).Append('\n');
        }

        /// <summary>Zamanı dosya adına uygun biçimde verir (yalnız sunum/rapor; kural kodunda saat kullanılmaz).</summary>
        public static string FileStamp(DateTime local) => local.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
    }
}
