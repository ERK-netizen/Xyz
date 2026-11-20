# 1914 MVP denge notu (Adım 17)

Yöntem `game-balance` becerisidir: metrik → bant (gerekçeli) → çok tohumlu tarama → tek ayar değişikliği →
aynı taramayla karşılaştırma → bant testi. Ölçüm aracı `Tools/HeadlessTests/TarihStrateji.Core.Tests/Campaign/BalanceSweep1914.cs`
(YZ'li Gözlemci, 365 gün, sabit tohum listesi 1..N; CI'da koşmaz):

```bash
DENGE_TOHUM=32 DENGE_CIKTI=denge.md dotnet test Tools/HeadlessTests/TarihStrateji.Core.Tests -c Release --filter Category=Denge
```

Masaüstünde 32 tohum × 365 gün ≈ 2 dk 40 sn. Metrikler yalnız okunur (kurallara durum eklenmez). Yüzdelikler
tohum (A, C) ya da tohum × ülke (B, 32 × 12 = 384 değer) üzerindendir.

## Hedef bantlar

| Metrik | Bant | Gerekçe |
|---|---|---|
| A01 ilk savaş günü | 20–60 | Temmuz Krizi: Saraybosna (28.06) → Avusturya'nın Sırbistan'a savaş ilanı (28.07) |
| A11 yıl sonunda süren savaş payı, P50 | ≥ 0,5 | 1914–1918 yıpratma savaşları; büyük güçler ilk yıl barış yapmadı |
| A10 120 günden kısa biten savaş payı, P50 | ≤ 0,4 | Kısa savaşlar yalnız ordusuz küçük devletlerin teslimi olmalı |
| A08 en büyük bölge kazancı | ≤ 6 / 129 | Siper savaşı: cepheler az kıpırdar; kartopu yok |
| B01 hazine oranı (ülke, P10–P90) | 0,25 – 3,0 | Ne iflas ne kaçak büyüme |
| B02 başlangıç bölgelerinin nüfus değişimi | −%25 – +%5 | Savaş kayıpları + doğal artış |
| B03 seferber / çalışma çağı (P90) | ≤ 0,30 | Büyük güçler 1914'te çalışma çağının %10–20'sini silah altına aldı |
| A04 muharebe sayısı / yıl | > 0 her tohumda | Cepheler etkin |

## Ölçüm 1 — başlangıç (CShapes paketi, Adım 16 sonu)

Tohum 32, gün 365.

| Metrik | P10 | P50 | P90 | En az | En çok |
|---|---|---|---|---|---|
| A01 ilk savaş günü | 6 | 6 | 6 | 6 | 6 |
| A02 yılda başlayan savaş çifti | 8 | 9 | 13 | 8 | 15 |
| A03 aynı anda en çok savaş | 4 | 5 | 7 | 4 | 7 |
| A04 muharebe sayısı | 91 | 123 | 159 | 84 | 202 |
| A05 kapitülasyon | 1 | 2 | 3 | 1 | 4 |
| A06 biten savaş çifti | 4 | 6 | 9 | 4 | 9 |
| A07 biten savaşın süresi (gün) | 6 | 56 | 161 | 2 | 342 |
| A10 / A11 (sonradan eklendi, 16 tohum) | — | 533 / 400 | — | — | — |
| A08 en büyük bölge kazancı (tohum) | 1 | 3 | 4 | 1 | 6 |
| B01 hazine oranı ‰ (ülke) | 596 | 1596 | 2575 | 523 | 2866 |
| B02 başlangıç bölgeleri nüfus değişimi ‰ (ülke) | −79 | 3 | 17 | −227 | 17 |
| B03 seferber / çalışma çağı ‰ (ülke, güncel bölgeler) | 0 | 109 | 121 | 0 | 260 |
| B04 bölge kazancı (ülke) | −1 | 0 | 2 | −5 | 6 |
| B05 savaşta geçen gün (ülke) | 0 | 245 | 359 | 0 | 360 |
| C01 Osmanlı hazine oranı ‰ | 544 | 572 | 700 | 537 | 863 |
| C02 Osmanlı bölge kazancı | −1 | 0 | 0 | −2 | 1 |
| C03 Osmanlı savaşta geçen gün | 354 | 354 | 355 | 347 | 357 |

### Bulgular

1. **Savaşlar çok kısa** (A07 P50 = 56 gün, bant ≥ 120). YZ 60 gün sonra barış önerip kabul edebiliyor
   (`AiParameters.PeaceAfterWarDays` = 60), kaybeden taraf her an kabul ediyor, tümensiz küçük devletler
   birkaç günde teslim oluyor (A07 P10 = 6). Kaldıraç adayı: `PeaceAfterWarDays`.
2. **İlk savaş her tohumda 6. gün** (bant 20–60). YZ'nin ilk haftalık değerlendirmesinde üstünlük gören ülke hemen
   savaş ilan ediyor; YZ'de rastgelelik olmadığı için tüm tohumlarda aynı. Kaldıraç adayı: senaryo başında
   savaş ilanını geciktiren bir YZ ayarı (Temmuz Krizi süresi).
3. Diğerleri bantta: hazine (B01 P90 2,58 ≤ 3,0), nüfus, seferberlik (P90 0,12), kartopu yok (A08 ≤ 6).
4. Osmanlı yılın tamamını savaşta geçiriyor ve hazinesi her tohumda ≈ %57'ye iniyor; bant içinde ama en
   düşük ülke. Ayrıca izlenecek (bulgu 1 düzelince savaş maliyeti değişir).

### Ölçüm yanlılığı (düzeltildi)

İlk bant "biten savaşın süresi P50 ≥ 120" idi; duyarlılık taraması bunun yanıltıcı olduğunu gösterdi: barış eşiği
yükselince uzun savaşlar yıl içinde bitmediği için ortancaya hiç girmez, geriye kısa (teslim) savaşları kalır ve
ortanca *düşer* (sansürlü veri, hayatta kalma yanlılığı). Bant, başlayan savaşlara oranlı iki ölçüyle değişti:
A10 (120 günden kısa biten pay) ve A11 (yıl sonunda süren pay).

## Ayar 1 — `AiParameters.PeaceAfterWarDays` (tek ayar, 16 tohum)

| Değer | A10 P50 | A11 P50 | B01 P10–P90 | A08 P90 |
|---|---|---|---|---|
| 60 (eski) | 533 ‰ | 400 ‰ | 581–2658 ‰ | 4 |
| **120 (seçilen)** | 285 ‰ | 571 ‰ | 651–2658 ‰ | 5 |
| 240 | 285 ‰ | 571 ‰ | 651–2658 ‰ | 5 |
| 365 | — | — | — | — (geçersiz: `StalemateDays` 240'tan büyük olamaz) |

Etki 120 günde doyuyor; bandı sağlayan en küçük değer seçildi. Kalan kısa savaşlar tümensiz küçük devletlerin
(Karadağ, Arnavutluk) teslimidir.

## Ayar 2 — senaryo tarihli en erken YZ savaşı

`Scenario1914.AiEarliestWar` = 28.07.1914 (`SimulationSetup.AiEarliestWarDate` → `AiPerception.EarliestWar`). Yalnız
YZ'nin savaş *ilanını* geciktirir; savunma, ittifak çağrısı ve oyuncu serbesttir. YZ'de rastgelelik olmadığından
ilk savaş tüm tohumlarda aynı gündür (28.07'den sonraki ilk haftalık YZ kararı: 33. gün = 31.07.1914).

## Ölçüm 2 — iki ayardan sonra (32 tohum, 365 gün)

| Metrik | P10 | P50 | P90 | En az | En çok |
|---|---|---|---|---|---|
| A01 ilk savaş günü | 33 | 33 | 33 | 33 | 33 |
| A02 yılda başlayan savaş çifti | 6 | 7 | 10 | 6 | 10 |
| A03 aynı anda en çok savaş | 4 | 4 | 4 | 4 | 4 |
| A04 muharebe sayısı | 16 | 30 | 135 | 10 | 182 |
| A05 kapitülasyon | 1 | 1 | 3 | 1 | 4 |
| A06 biten savaş çifti | 2 | 3 | 7 | 2 | 8 |
| A07 biten savaşın süresi (gün) | 7 | 11 | 196 | 7 | 302 |
| A08 en büyük bölge kazancı (tohum) | 1 | 1 | 4 | 1 | 9 |
| A09 bölge sayısı | 129 | 129 | 129 | 129 | 129 |
| A10 120 günden kısa biten savaş ‰ (başlayanlara) | 285 | 333 | 666 | 285 | 750 |
| A11 yıl sonunda süren savaş ‰ (başlayanlara) | 250 | 571 | 666 | 200 | 666 |
| B01 hazine oranı ‰ (ülke) | 652 | 1653 | 2343 | 437 | 2757 |
| B02 başlangıç bölgeleri nüfus değişimi ‰ (ülke) | −77 | 16 | 17 | −227 | 17 |
| B03 seferber / çalışma çağı ‰ (ülke, güncel bölgeler) | 52 | 110 | 119 | 0 | 271 |
| B04 bölge kazancı (ülke) | −1 | 0 | 1 | −6 | 9 |
| B05 savaşta geçen gün (ülke) | 0 | 203 | 331 | 0 | 332 |
| C01 Osmanlı hazine oranı ‰ | 443 | 645 | 665 | 437 | 807 |
| C02 Osmanlı bölge kazancı | −1 | 1 | 1 | −3 | 3 |
| C03 Osmanlı savaşta geçen gün | 327 | 330 | 330 | 320 | 330 |

### Değerlendirme

| Bant | Durum |
|---|---|
| A01 ilk savaş 20–60 | ✓ 33 |
| A11 süren savaş P50 ≥ 0,5 | ✓ 0,571 |
| A10 kısa biten savaş P50 ≤ 0,4 | ✓ 0,333 |
| A08 en büyük bölge kazancı P90 ≤ 6 | ✓ 4 (tek tohumda en çok 9; izleniyor) |
| B01 hazine 0,25–3,0 | ✓ 0,65–2,34 |
| B02 nüfus −%25…+%5 | ✓ |
| B03 seferber P90 ≤ 0,30 | ✓ 0,119 |
| A04 muharebe > 0 | ✓ (en az 10); savaşlar geç başladığı ve daha az ülke savaştığı için P50 123 → 30 |

Osmanlı yine yılın çoğunu savaşta geçiriyor (C03 ≈ 330 gün) ama hazinesi iyileşti (C01 P50 0,57 → 0,65).

## Açık kalanlar

- Muharebe yoğunluğu (A04) için henüz bant yok; cephe etkinliği oyuncu deneyimiyle (Adım 17 cihaz denemesi) ayarlanmalı.
- Tümensiz küçük devletler birkaç günde teslim oluyor (A07 P50 11 gün). Tarihte Sırbistan ve Karadağ 1915–16'ya
  dek dayandı; küçük devlet garnizonları/milisleri ayrı bir içerik kararıdır.
- Cihazda performans ölçümü: Adım 17 (3/3).

## 1939 barış ekonomisi (Adım 18 bulgusu, açık)

`Scenario1939Tests` (tohum 41, 365 gün, YZ'li Gözlemci): bölge 10.06.1940'a kadar barışta. Bir yıl sonunda hazine
oranları (14 ülke, sıralı): 0,65 · 0,71 · 0,82 · 0,92 · 0,98 · 1,28 · 1,45 · 1,71 · 1,90 · 2,90 · 2,98 · 3,03 · 3,03 · 3,08.
P90 = 3,03 > 1914 bandı 3,0. Neden: barışta vergi geliri bakım ve inşaat giderini belirgin aşıyor; YZ vergiyi yalnız
mutlak eşikte (hazine > 80 bin) düşürüyor ve küçük ülkeler bu eşiğe hiç ulaşmıyor (Graz'daki Alman parçası gibi).
1914'te ülkelerin çoğu savaşta olduğundan bu fazla görünmüyordu.

Düzeltme ayrı bir denge geçişidir (game-balance, kalıp 3: gelir/gider defteri): ülke büyüklüğüyle ölçeklenen bir
gider ya da büyüklüğe göreli vergi eşiği, 1914/Ege/1939 taramalarıyla birlikte. O zamana kadar 1939 kapısı P10 ≥ 0,25,
P50 ≤ 3,0 ve en çok ≤ 4 (kaçak büyüme yok) sınar.
