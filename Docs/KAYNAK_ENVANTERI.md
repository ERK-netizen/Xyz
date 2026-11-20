# Kaynak envanteri ve atıflar

Bu belge oyunun coğrafi verisinin kaynaklarını, lisanslarını ve uygulanan düzeltmeleri kaydeder
(TS-GDD-TDD-3.1 §22; `Docs/V31_GECIS_PLANI.md` §4 ve §6). Paketler `Tools/ContentCompiler` ile üretilir;
her paket kaynak dosyanın SHA-256'sını taşır.

## 1914 MVP (`Assets/TarihStrateji/Unity/Bootstrap/Resources/Senaryolar/1914_mvp.bytes`)

| Alan | Değer |
|---|---|
| Kaynak | historical-basemaps, `geojson/world_1914.geojson` — André Ourednik ve katkıcılar |
| Adres | https://github.com/aourednik/historical-basemaps |
| Lisans | GPL-3.0 (türetilmiş paket de GPL-3.0 kapsamındadır) |
| Doğruluk | "estimated" (kaynağın `BORDERPRECISION` alanı); sınırlar oyun ölçeğinde sadeleştirilmiş kabul edilir |
| Kaynak SHA-256 | `Tools/ContentCompiler/data/1914_mvp.json` içinde sabitlenmiştir |
| Kapsam | 12,0°–46,5° D, 29,5°–48,0° K (Balkanlar, Anadolu, Doğu Akdeniz, Kafkasya'nın batısı) |

Planlanan birincil kaynak CShapes 2.0 (CC BY-NC-SA 4.0) bu ortamdan indirilemediği için ikinci seçenek
historical-basemaps kullanıldı (depo sahibinin 19.11.2026 kararındaki yedek). Kullanım kişiseldir, ticari
değildir. Oyun ticari yayına çıkacaksa kaynaklar yeniden seçilmelidir.

### Tarihî düzeltmeler (derleyicide `corrections`)

| Düzeltme | Gerekçe |
|---|---|
| Doğu Trakya ve İstanbul Bulgaristan'dan Osmanlı'ya | Kaynak hatası. 29.09.1913 İstanbul Antlaşması: Enez–Meriç–Edirne'nin kuzeyi–Rezve hattının doğusu Osmanlı. |
| Kars–Ardahan Osmanlı'dan Rusya'ya | Kaynak hatası. 1878 Berlin Antlaşması'ndan 1918'e kadar Rus Kars ve Batum vilayetleri. |
| Besarabya Romanya'dan Rusya'ya | Kaynak hatası. 1812–1918 arası Rus İmparatorluğu. |
| Ermenistan, Gürcistan, Azerbaycan → Rusya | Kaynakta ayrı ülke; 1914'te Rus İmparatorluğu'nun parçası. |
| Adsız adalar | En yakın ada kaydına göre: Ege ve İyon adaları, Girit → Yunanistan; Onikiada → İtalya (1912); Kıbrıs → Birleşik Krallık. |

Mısır (Hidivlik) ve Kıbrıs hukuken Osmanlı'ya bağlı olsa da 1914'te fiilen Britanya yönetiminde olduğundan
"Birleşik Krallık" olarak modellenir.

### Türetilmiş veri (kaynakta yok, derleyici üretir)

- İdari bölgeler: her ülke kendi şehirlerinin Voronoi hücreleriyle bölünür (tarihî vilayet sınırı değildir).
- Şehir koordinatları ve kademeleri elle girilmiştir (`data/1914_mvp.json`); kıyıda kalan şehirler kaba kıyı
  verisinde sınır içine kaydırılır (derleyici günlüğe yazar).
- Kara yolları Delaunay üçgenlemesinden (denizi geçmeyen, komşu bölgeler arası); 1914 demiryolları elle
  listelenmiş çiftlerdir. Boğaz geçişleri ve dar deniz geçitleri elle tanımlıdır; açık deniz 0,75° ızgaradır.
- Nüfus: ülke toplamı (1914 tahmini, bin kişi) × 1/5 ölçek, bölgelere kademe² × √alan ağırlığıyla dağıtılır.
