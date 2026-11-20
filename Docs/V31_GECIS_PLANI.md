# TS-GDD-TDD-3.1 → Bu depo: Durum eşlemesi ve geçiş planı

Belge tarihi: 19 Kasım 2026 · Ana şartname: **TS-GDD-TDD-3.1** (depo sahibinin 19.11.2026 talimatı;
harita kararı ADR-011). Bu dosya şartnamenin bu depoya nasıl uygulanacağını tanımlar. Şartname
"MEVCUT / KISMİ / PLAN / KABUL EDİLDİ" terimlerini zorunlu kılar; aşağıda aynı terimler kullanılır.

## 1. Başlangıç noktası düzeltmesi (§0.1–0.2)

Şartname başlangıç kaynağı olarak "v2.2 Unity ayrıştırılmış C# paketi"ni (zip, `Assets/Scripts/*`,
10 senaryo, 118 teknoloji, 23 tesis, 395 kişi, 312 şehir, 74.880 hücre atlası) gösterir. **O paket bu
depoda yoktur.** Bu depo ayrı yazılmış bir projedir ve şartnamenin F0/F1 hedeflerinin önemli kısmını
zaten karşılar; bu yüzden §31'deki "v2.2'yi derle" adımı burada "bu depodan devam et" olarak okunur.
v2.2 katalogları (kişi, teknoloji, tesis, şehir, senaryo) depo sahibi tarafından eklenirse içerik
kaynağı olarak §22 hattından geçirilir.

| Şartname alanı | Bu depoda gerçek durum | Statü |
|---|---|---|
| Unity derlemesi (F0-01) | Unity 6000.3.28f1, CI'da derleniyor | KABUL EDİLDİ (Editor/CI) |
| EditMode testleri (F0-02) | CI'da koşuyor ve geçiyor | KABUL EDİLDİ (CI) |
| Android ARM64 IL2CPP (F0-03) | APK CI'da üretiliyor | KISMİ: cihaz testi yapılmadı |
| Tek emir kapısı, oyuncu/YZ yetkisi (F1-01) | `SimulationKernel.Submit/Validate`, YZ aynı kapı | MEVCUT/KODDA + headless test |
| owner/controller (F1-02) | Bölge/hücre/sektör düzeyinde | KISMİ: altıgene bağlı |
| Teleport/ücretsiz çıkarma engeli (F1-03) | A*, `transportCapacity > 0` | KISMİ: altıgen A* |
| AP, resolutionId, kayıp muhasebesi (F1-04) | Var, testli | KISMİ: hedef = altıgen |
| İki taraflı barış (F1-05) | `Pending → Accepted`, testli | MEVCUT/KODDA + test |
| Sandbox yetkisi (F1-06) | `GameModeRules` | MEVCUT/KODDA + test |
| Fixed64 S=1.000.000, SHA-256, RNG (F1-07) | Var, altın testli | MEVCUT/KODDA + test |
| Atomik kayıt, halka, hata bildirimi (F1-08) | Var | MEVCUT/KODDA + test |
| Nüfus kohortu, ekonomi, araştırma, siyaset, karakter (F2-01..05) | Var, küçük katalog | KISMİ (içerik az) |
| Deniz, hava, konvoy, abluka (F2-06..08) | Var | KISMİ: altıgene bağlı |
| Diplomasi, kapitülasyon/sürgün, istihbarat, YZ, olay/odak (F2-09..12) | Var | KISMİ |
| Gerçek sınırlı dünya haritası (F3, F4) | Yok: 720×18 altıgen deneme şeridi | PLAN |
| Mobil arayüz (F5-01) | UI Toolkit dikey HUD, altıgen harita | KISMİ |

## 2. Altıgene bağlı kod envanteri (EK A.1)

`HexCoord`/`HexId`/`HexGrid`/`SectorId` kullanan yaklaşık 81 kural dosyası ve 30 test dosyası var:
World 13, Simulation 32, Warfare 15, Economy 6, Intelligence 5, AI 4, Logistics 2, Domain 2,
Effects 1, Content 1; Unity sunumunda harita katmanının tamamı. EK A.1 gereği bunlar gizlenmeyecek,
**kaldırılacak**: kural, kayıt ve YZ kodunda zorunlu `HexId`, `SectorId`, `AttackHex`,
`targetHexId` başvurusu kalmayacak (MAP-G10).

## 3. Yeni coğrafi çekirdek (Core.World) — teknik kararlar

- **GeoPoint**: `LatitudeE6`, `LongitudeE6` (int, mikro-derece). Kural kodunda float yok.
- **Mesafe**: büyük çember, mevcut deterministik `FixedTrig` ile metre cinsinden (`long`).
  Hareket ve ikmal mesafesi yalnız ulaşım grafındaki kenar uzunluklarından okunur.
- **Ülke ve bölge geometrisi**: `MultiPolygon` / `Polygon`, E6 tamsayı köşeler, delik ve ada
  destekli; nokta-içinde-çokgen testi 128-bit tamsayı çapraz çarpımla. ±180° dikişi içerik
  derleyicide kesilir. Geometri değişmez içeriktir; kayıtta yalnız `contentManifestHash`.
- **BorderGraph**: bölge komşuluğu ortak kara sınırı uzunluğu ve geçiş noktalarıyla derlenir;
  görsel yakınlık komşuluk yaratmaz.
- **TransportGraph**: düğümler (şehir, yerleşim, kavşak, geçit, köprü, liman), kenarlar (yol,
  demiryolu, arazi geçişi, deniz yolu) `lengthMeters`, `capacity`, `era`, arazi profili.
  A*/Dijkstra bu grafta; sezgisel = büyük çember mesafesi (kabul edilebilir).
- **Birlik konumu**: düğümde ya da kenar üzerinde (`edgeId` + kat edilen metre); `regionId`
  türetilir. Çok günlü hareket ara konumu kaydeder.
- **Şehir**: `CityId`, `GeoPoint`, `regionId`, `developmentLevel` 1–10, yapı slotu = 4 × seviye
  (denge verisi), garnizon, `SiegeState`, `legalOwner`/`controller`. Sektör yok (ADR-004 geçersiz).
- **Bölge kontrolü**: `contested → occupied → secured`; şehir/lojistik düğüm kontrolü, düşman
  kuvveti, tutma süresi ve ikmalden. Hukukî sahiplik yalnız barış/ilhakla değişir.
- **Deniz**: `SeaRegion` çokgenleri + `SeaLaneGraph`; abluka liman–deniz bölgesi ilişkisinden.
- **Hava**: üs–hedef büyük çember menzili (mevcut kural korunur, hedef `TargetRef` olur).
- **TargetRef**: `EnemyUnit | City | Region | GeoPoint | Fleet | AirBase` + kimlik/nokta.

## 4. Veri hattı (§22) ve kaynak seçimi

`Tools/ContentCompiler`: GeoJSON → doğrulama (kapalı halka, kendini kesmeme, dikiş, delik) →
sadeleştirme (LOD 0–3) → E6 tamsayı ikili paket + üçgenleme (Unity çizimi için önceden) + kaynak
envanteri (`sourceId`, lisans, tarih aralığı, güven derecesi).

| Kaynak | Kapsam | Lisans (depo sahibi doğrular) | Kullanım |
|---|---|---|---|
| Natural Earth | Modern sınır, kıyı, nehir, göl | Kamu malı | Fizikî coğrafya; modern senaryolar. 1914'e kopyalanamaz (REG-021/028) |
| historical-basemaps (aourednik) | Dünya, birçok yıl (1914, 1938 vb.) | GPL-3.0 | Tarihsel ülke sınırı adayı; doğruluk "estimated" |
| CShapes 2.0 | 1886–2019 devlet sınırları | CC BY-NC-SA 4.0 (ticari olmayan) | Yüksek doğruluk; ticari yayını kısıtlar |

Tarihsel sınırlar kaynak lisansına bağlıdır; ticari yayın niyeti kaynak seçimini belirler.

## 5. Yeni adım planı (CLAUDE.md §5.1 devamı)

Şartnamenin sırası korunur: kurallar → bütünleşik sistemler → dünya verisi → Android kalitesi.
Her adım headless testleriyle gelir; altıgen kodu yalnız yerine geçen sistem testleri geçince silinir.

| Adım | Kapsam | Ana kabul kodları |
|---|---|---|
| 11 | Core.World coğrafi çekirdek: GeoPoint, sabit noktalı mesafe, Polygon/MultiPolygon, nokta-içinde-çokgen, BorderGraph, TransportGraph + A*, tekil şehir (sektörsüz), bölge kontrol aşamaları, TargetRef | MAP-G01, G02, G05; CITY-001/002/008; REG-015 |
| 12 | Kurallar göçü: Economy (şehir slotu), Logistics (graf üzerinde ikmal), Warfare (konumlu birlik, rota, TargetRef saldırı, kuşatma/garnizon, deniz bölgesi, hava menzili); altıgen dosyalarının silinmesi | WAR-L01..L12, CITY-004..006, MAP-G06..G08, REG-002/006/011/016/027/030 |
| 13 | Simulation, Intelligence (bölge/düğüm görünürlüğü), AI, Content, Persistence göçü; kayıt şeması 4 (geçersiz kılınan eski kayıt açık hatayla reddedilir) | MAP-G10, REG-012/017/018/029, 365 gün determinizm |
| 14 | ContentCompiler + 1914 MVP bölgesi (Balkanlar–Anadolu–Doğu Akdeniz): ülke/bölge çokgenleri, şehir koordinatları, yol/demiryolu/liman grafı, kaynak envanteri | §3.6, MAP-G09, REG-021/022/028 |
| 15 | Unity çokgen harita çizicisi: önceden üçgenlenmiş ülke/bölge doldurma, sınır çizgileri, LOD, şehir/birlik ikonları, mekânsal indeksli dokunma seçimi | MAP-G03/G04, REG-013/025/026 |
| 16 | Mobil akış: birlik seç → hedef → önizleme (rota, süre, ikmal, risk) → onay; şehir kartı, diplomasi/barış ekranı, +1/+7/+30 gün | §19.3, E2E-01 (MVP kapsamında) |
| 17 | 1914 MVP bütünleşik denge: üretim → ikmal → taarruz → barış; Android cihaz ölçümleri | §24.1 MVP, REG-020 |
| 18+ | 1936/1939, tam dünya, diğer dönemler (F4), ürünleştirme (F5) | §24 F4/F5 |

## 6. Kararlar (depo sahibi, 19.11.2026)

1. **Kullanım kişisel, ticari amaç yok.** Bu yüzden tarihsel sınırlar için birincil kaynak
   **CShapes 2.0** (CC BY-NC-SA 4.0: atıf zorunlu, ticari kullanım yok, türetilmiş veri aynı
   lisansla); eksik kalan dönemler için historical-basemaps (GPL-3.0); fizikî coğrafya (kıyı,
   nehir, göl) için Natural Earth (kamu malı). Atıf ve lisans envanteri `Docs/` altında tutulur;
   oyun ticari yayına çıkacaksa kaynaklar yeniden seçilmelidir.
2. v2.2 paketi depoya eklenmedi; içerik bu depodaki kataloglar ve §22 hattıyla üretilir.
3. MVP bölgesi önerildiği gibi: 1914 Balkanlar–Anadolu–Doğu Akdeniz.

## 7. İlerleme

| Adım | Durum |
|---|---|
| 11 coğrafi çekirdek | TESTED (headless) + Unity EditMode geçti |
| 12 coğrafi kurallar (ikmal, hareket, muharebe, kuşatma, bölge kontrolü, deniz, hava, konvoy, slot) | TESTED (headless) + Unity EditMode geçti |
| 13/1 coğrafi oyun durumu, günlük sistemler, emirler, kayıt şeması 4 | CODED + TESTED (headless 392/392) |
| 14 ContentCompiler + 1914 MVP (12 ülke, 129 bölge/şehir, tarihî düzeltmeler, senaryo kurulumu, açılış 1914) | TESTED (headless 322/322); Unity CI sonucu commit notunda |
| 13/2–4 altıgen silindi; ekonomi/diplomasi/istihbarat/YZ/senaryo/kayıt (şema 5) coğrafi; Unity ülke haritası (yalnız ülke sınırları + şehirler) | TESTED (headless 309/309); Unity CI sonucu commit notunda |
| 15 çokgen harita: sınır zincirleri + LOD sadeleştirme, mekânsal indeksli dokunma, liman simgesi, çakışmasız şehir adları, yığın rozetleri | Unity EditMode (MapGeometryTests, MapLabelTests); Unity CI sonucu commit notunda; başkent simgesi bekliyor (çekirdekte başkent verisi yok) |

Not (tarihî): Altıgen kural dosyaları Adım 12'de değil 13'te silindi; çünkü Simulation onlara bağlıdır ve her
commit derlenebilir kalmalıdır (§4 "sıfır eksik kod"). Adım 12 yeni kuralları `*.Geo` isim alanlarında
yan yana kurar ve testler; Adım 13 Simulation'ı geçirip eskileri siler.
