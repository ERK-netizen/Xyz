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
| A07 biten savaşın süresi, P50 | ≥ 120 gün | 1914–1918 yıpratma savaşları; büyük güçler ilk yıl barış yapmadı. Küçük devletlerin erken teslimi (Sırbistan 1915, Karadağ 1916) kısa süreleri açıklar ama ortancayı belirlememeli |
| A06 / A02 biten / başlayan savaş çifti | ≤ 0,6 | İlk yılda savaşların çoğu sürer |
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
