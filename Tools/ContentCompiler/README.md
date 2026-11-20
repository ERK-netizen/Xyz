# ContentCompiler

Tarihsel GeoJSON'dan oyunun coğrafi senaryo paketini (`.bytes`, TSGP sürüm 1) üretir. Yalnız geliştirme
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
