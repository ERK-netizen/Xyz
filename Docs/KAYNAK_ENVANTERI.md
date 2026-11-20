# Kaynak envanteri ve atıflar

Bu belge oyunun coğrafi verisinin kaynaklarını, lisanslarını ve uygulanan düzeltmeleri kaydeder
(TS-GDD-TDD-3.1 §22; `Docs/V31_GECIS_PLANI.md` §4 ve §6). Paketler `Tools/ContentCompiler` ile üretilir;
her paket kaynak dosyanın SHA-256'sını taşır.

## 1914 MVP (`Assets/TarihStrateji/Unity/Bootstrap/Resources/Senaryolar/1914_mvp.bytes`)

| Alan | Değer |
|---|---|
| Kaynak | CShapes 2.0 (`CShapes-2.0.geojson`), Gleditsch–Ward kodlaması — ETH Zürich, International Conflict Research |
| Adres | https://icr.ethz.ch/data/cshapes/ |
| Atıf | Schvitz, G., Girardin, L., Rüegger, S., Weidmann, N. B., Cederman, L.-E., & Gleditsch, K. S. (2022). Mapping the International System, 1886-2019: The CShapes 2.0 Dataset. *Journal of Conflict Resolution*, 66(1), 144-161. |
| Lisans | CC BY-NC-SA 4.0: atıf zorunlu, ticari kullanım yok; türetilmiş paket (`1914_mvp.bytes`) aynı lisansla paylaşılır |
| Seçim | Yalnız 28.06.1914'te geçerli kayıtlar (`gwsdate ≤ tarih ≤ gwedate`); ad `cntry_name` |
| Doğruluk | Biger (1995) sınır ansiklopedisine dayalı kodlama; oyun ölçeğinde sadeleştirilmiş kabul edilir |
| Kaynak SHA-256 | `384b1ea90b9419f30a858d7ec237c85a22c60d1b35b5f85f215a1204f9989d42` (`data/1914_mvp.json` içinde sabit) |
| Kapsam | 12,0°–46,5° D, 29,5°–48,0° K (Balkanlar, Anadolu, Doğu Akdeniz, Kafkasya'nın batısı) |

Depo sahibinin 19.11.2026 kararıyla birincil kaynak CShapes 2.0'dır. İlk sürüm (Adım 14) bu ortamdan
CShapes'e erişilemediği için historical-basemaps (GPL-3.0) ile üretilmişti; 20.11.2026'da `icr.ethz.ch`
erişimi açılınca paket CShapes'ten yeniden derlendi ve historical-basemaps bağımlılığı kalktı. Kullanım
kişiseldir, ticari değildir. Oyun ticari yayına çıkacaksa kaynaklar yeniden seçilmelidir.

### Kaynak adı → senaryo ülkesi

| Senaryo ülkesi | CShapes kayıtları (28.06.1914) |
|---|---|
| Osmanlı İmparatorluğu | Turkey (Ottoman Empire) |
| Yunanistan, Bulgaristan, Sırbistan, Karadağ, Arnavutluk | Greece, Bulgaria, Serbia, Montenegro, Albania |
| Romanya | Rumania |
| Avusturya-Macaristan | Austria-Hungary |
| İtalya | Italy/Sardinia, Libya (1912'den beri İtalyan) |
| Rus İmparatorluğu | Russia (Soviet Union) kaydının 1914 sınırları (Kafkasya, Besarabya dahil) |
| Birleşik Krallık | Egypt (1882'den beri İngiliz işgali), Malta |
| İran | Iran (Persia) |

Kapsamdaki Almanya kırıntısı (Bavyera'nın güneydoğu ucu) senaryoya alınmaz; sahipsiz kara olarak kalır.
Deniz ızgarasının kara maskesi CShapes'in tüm yıllarındaki kayıtlarıdır: 1914'te hiçbir devlete ait olmayan
kara (ör. Necd, Kuveyt) deniz sayılmaz.

### Tarihî düzeltmeler (derleyicide `corrections`)

| Düzeltme | Gerekçe |
|---|---|
| Kıbrıs Osmanlı'dan Birleşik Krallık'a | CShapes hukukî sahibi (Osmanlı) gösterir; ada 1878 Kıbrıs Sözleşmesi'nden beri İngiliz idaresinde, 5.11.1914'te ilhak. Senaryo fiilî yönetimi esas alır. |
| Onikiadalar Yunanistan'dan İtalya'ya | CShapes adaları Yunanistan'da gösterir; Mayıs 1912'den itibaren İtalyan işgalindedir (1923 Lozan ile İtalyan, 1947'de Yunan). |

historical-basemaps için gereken Doğu Trakya, Kars–Ardahan, Besarabya ve Kafkasya düzeltmeleri CShapes'te
doğru olduğundan kaldırıldı (ilgili testler `GeoPackageTests.HistoricalCorrections_AreApplied` geçmeye devam eder).

### Başkentler

Senaryo başkentleri (`Scenario1914.CapitalCityKeys`) CShapes'in `capname` alanıyla örtüşür: İstanbul, Atina,
Sofya, Belgrad, Çetine, Dıraç (Durres), Bükreş, Roma. Viyana, Petersburg, Londra ve Tahran kapsam dışıdır.

### Türetilmiş veri (kaynakta yok, derleyici üretir)

- İdari bölgeler: her ülke kendi şehirlerinin Voronoi hücreleriyle bölünür (tarihî vilayet sınırı değildir).
- Şehir koordinatları ve kademeleri elle girilmiştir (`data/1914_mvp.json`); kıyıda kalan şehirler kaba kıyı
  verisinde sınır içine kaydırılır (derleyici günlüğe yazar).
- Kara yolları Delaunay üçgenlemesinden (denizi geçmeyen, komşu bölgeler arası); 1914 demiryolları elle
  listelenmiş çiftlerdir. Boğaz geçişleri ve dar deniz geçitleri elle tanımlıdır; açık deniz 0,75° ızgaradır.
- Nüfus: ülke toplamı (1914 tahmini, bin kişi) × 1/5 ölçek, bölgelere kademe² × √alan ağırlığıyla dağıtılır.

## 1939 senaryosu (`Assets/TarihStrateji/Unity/Bootstrap/Resources/Senaryolar/1939_mvp.bytes`)

Kaynak, lisans ve kapsam 1914 ile aynıdır (CShapes 2.0, CC BY-NC-SA 4.0, aynı SHA-256; 12,0°–46,5° D, 29,5°–48,0° K);
seçim tarihi 01.09.1939. Yapılandırma `Tools/ContentCompiler/data/1939_mvp.json`; ad anahtarları `ulke.1939.*`,
`sehir.1939.*`. Şehir listesi 1914'ünkidir, her şehrin sahibi 01.09.1939 CShapes sınırından bulunur; Amman, Tiran,
Antakya ve İskenderun eklendi; başkent kademeleri yükseltildi (Ankara, Belgrad, Sofya, Atina, Bükreş, Bağdat, Şam,
Beyrut, Kahire, Budapeşte). 14 ülke, 133 bölge/şehir. Nüfuslar kapsam içindeki tahmini 1939 nüfusudur (bin kişi, 1/5 ölçekle oyuna girer).

| Senaryo ülkesi | CShapes kayıtları (01.09.1939) |
|---|---|
| Türkiye Cumhuriyeti | Turkey (Ottoman Empire) — Hatay dahil (Haziran 1939) |
| Yunanistan, Bulgaristan, Yugoslavya, Romanya, Macaristan, Mısır, Irak, İran | Greece, Bulgaria, Yugoslavia, Rumania, Hungary, Egypt, Iraq, Iran (Persia) |
| Alman Reich | Austria, Germany (Prussia) |
| İtalya Krallığı | Italy/Sardinia, Libya, Albania |
| Sovyetler Birliği | Russia (Soviet Union) |
| Birleşik Krallık | Cyprus, Malta, Palestine, Jordan |
| Fransa (Levant mandası) | Syria, Lebanon |

Kapsamdaki Polonya, Çekoslovakya ve Suudi Arabistan kırıntıları senaryoya alınmaz (sahipsiz kara).

| Düzeltme | Gerekçe |
|---|---|
| Onikiadalar Yunanistan'dan İtalya'ya | 1912'den beri İtalyan, 1923 Lozan ile İtalya'ya bırakıldı; CShapes Yunanistan'da gösterir. |
| Arnavutluk → İtalya (kaynak adı eşlemesi) | 7–12 Nisan 1939 İtalyan işgali ve İtalyan tacına bağlanma; CShapes (Gleditsch–Ward) ayrı tutar. |
| Avusturya → Alman Reich (kaynak adı eşlemesi) | Mart 1938 Anschluss; CShapes ayrı tutar. |
