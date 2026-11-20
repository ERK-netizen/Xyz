# Android cihaz ölçümü (Adım 17)

Bulut ortamında Android cihaz yoktur; ölçüm oyunun içindeki **Ölçüm** düğmesiyle cihazda yapılır. Ölçüm oyuncunun
oyununu ilerletmez ve kayıtlarına dokunmaz: asıl oyunun anlık görüntüsünden otomatik kayıtsız ayrı bir çekirdek
kopyası kurulur ve ölçüm o kopyada koşar (`Unity/Bootstrap/DeviceMeasurement.cs`).

## Nasıl yapılır

1. GitHub Actions'ta en son başarılı "Android derlemesi" koşusunun `tarih-strateji-android` yapıtını indirip
   APK'yı telefona kurun (bilinmeyen kaynaklardan kuruluma izin gerekir; APK hata ayıklama anahtarıyla imzalıdır).
2. Oyunu açın, bir kez +30 gün ile ilerleyin (savaşlar başlasın, ölçüm gerçekçi olsun).
3. Üstteki **Ölçüm** düğmesine basın. Yaklaşık 5 saniye kare ölçülür, sonra 60 gün kopyada işlenir; ilerleme üst
   özette görünür ("Ölçülüyor: gün 23/60").
4. Bitince özet satırı üst panelde kalır, ör. `Ölçüm: gün P50 18,4 ms, P95 31,0 ms · kare P50 16,7 ms (60 fps) ·
   kayıt 140,2 ms · bellek 180,3 MB · olcum_20261120_153012.txt`.
5. Ayrıntılı rapor dosyası: `Android/data/com.tarihstrateji.oyun/files/olcum_YYYYAAGG_SSDDss.txt`
   (bilgisayara USB ile bağlayıp dosya gezgininden, ya da `adb pull`). Aynı metin `adb logcat -s Unity` günlüğünde.
6. Rapor dosyasını ya da özet satırını bu depoya/konuşmaya iletin; sonuçlar aşağıdaki tabloya işlenir.

## Ölçülenler

| Ölçü | Nasıl | Hedef |
|---|---|---|
| Kare süresi P50 / P95 | Normal oyunda 240 kare, `Time.unscaledDeltaTime` | P50 ≤ 33,3 ms (≥ 30 fps); hedef 16,7 ms |
| Gün adımı P50 / P95 / en çok | Kopyada YZ dahil 60 gün, her `AdvanceDay` `Stopwatch` ile | P95 ≤ 50 ms; 4 gün/kare hızı bu sınırla akıcı kalır |
| Kopya kurma | Anlık görüntü + `SimulationKernel.Restore` | yalnız bilgi (kayıttan yükleme süresine yakın) |
| Kayıt | `GameSaveService.Save` (atomik yazım) ayrı `olcum` klasörüne + anlık görüntü boyutu | ≤ 500 ms |
| Bellek | `GC.GetTotalMemory`, `Profiler.GetTotalAllocatedMemoryLong` | yalnız bilgi; 2 GB RAM'li cihazda ≤ 400 MB |

Durum hash'i 30 günde bir hesaplanır (`HashIntervalDays` = 30); 60 günlük örnekte bu iki gün P95/en çok değerini
yükseltebilir, raporda "en çok" ayrıca verilir.

## Masaüstü karşılaştırması (headless, cihaz değil)

1914 MVP, 12 ülke YZ'li: 365 gün ≈ 4,7 sn → gün başına ortalama ≈ 13 ms (Adım 17 denge taraması, 32 tohum
2 dk 31 sn). Telefonda 2–4 kat yavaş beklenir.

## Cihaz sonuçları

| Tarih | Cihaz | Gün P50 / P95 | Kare P50 | Kayıt | Bellek | Not |
|---|---|---|---|---|---|---|
| — | (henüz ölçülmedi) | | | | | Depo sahibinin cihazında ölçülecek |
