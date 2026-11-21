# ContentCompiler

Tarihsel GeoJSON'dan (1914 MVP: CShapes 2.0, `format: cshapes`) oyunun coğrafi senaryo paketini (`.bytes`, TSGP sürüm 1) üretir. Yalnız geliştirme
aracıdır; çıktı depoya girer, oyun çalışırken Python gerekmez. Ayrıntı: `compile_geo_package.py` başlığı,
kaynak ve lisanslar: `Docs/KAYNAK_ENVANTERI.md`.

```bash
python3 -m venv .venv && .venv/bin/pip install -r Tools/ContentCompiler/requirements.txt
```

```bash
.venv/bin/python Tools/ContentCompiler/compile_geo_package.py Tools/ContentCompiler/data/1914_mvp.json --out Assets/TarihStrateji/Unity/Bootstrap/Resources/Senaryolar/1914_mvp.bytes --tr Assets/TarihStrateji/Unity/Presentation/Resources/Localization/tr.txt
```

Paket değişirse headless testler (`GeoPackageTests`, `Scenario1914Tests`, `Campaign1914Tests`) ve
`GoldenValues.Scenario1914Day180` yeniden doğrulanır; altın değer yalnız gerekçesi commit'te yazılarak güncellenir.

Dünya paketi (Adım 21) aynı komutla `data/dunya1936.json`'dan derlenir (masaüstünde ≈ 2–3 dk). Yalnız bu yapılandırmanın açtığı
ayarlar: `gridPrecision`, `maxRoadKm`, `portLinkDegrees`, `portLandCrossing`; 360° kapsamda deniz ızgarası ±180°'de sarmalanır.
