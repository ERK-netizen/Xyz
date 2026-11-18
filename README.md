# Tarih Strateji

Android (dikey ekran), çevrimdışı, Türkçe tarihsel büyük strateji oyunu.
Motor: **Unity 6.3 LTS (`6000.3.x`)**. Oyun kuralları **%100 saf, deterministik C#**
(`TarihStrateji.Core.*`) ile yazılır ve Unity açılmadan test edilir.

Bağlayıcı mimari kurallar ve çalışma protokolü: **[CLAUDE.md](CLAUDE.md)**.

## Hızlı başlangıç

Gereksinimler: .NET 8 SDK, Python 3.10+. Unity Editor yalnız sunum/derleme için gerekir.

```bash
python3 Tools/HeadlessTests/sync_core_projects.py --check
```

```bash
python3 .claude/skills/unity-csharp-scripting/scripts/check_unity_project.py .
```

```bash
dotnet test Tools/HeadlessTests/TarihStrateji.Core.Tests -c Release
```

## Unity'de ilk açılış

1. Projeyi Unity Hub ile **6000.3.x** sürümünde açın (daha yeni bir sürümle açmak projeyi yükseltir).
2. Unity, `Assets/` altındaki her dosya ve klasör için `.meta` dosyalarını üretir. **Bu `.meta`
   dosyalarını hemen commit edin**; GUID'ler bir kez commit edildikten sonra asla yeniden üretilmez.
3. Window → General → Test Runner → EditMode altında `TarihStrateji.Tests.EditMode` testlerini çalıştırın.

## Unity CI: derleme, EditMode testleri ve `.meta` üretimi

**Lisans:** Unity Editor lisans etkinleştirmesi olmadan çalışmaz; lisanssız sürüm yoktur.
Ücretsiz yol **Unity Personal**'dır: ücret yoktur, ücretsiz bir Unity hesabıyla bir kez
etkinleştirilir. Oyun kurallarının tamamı Unity olmadan derlenir ve test edilir; Unity yalnız
sunum katmanı (Adım 10) ve APK için gerekir.

`.github/workflows/unity-tests.yml`, Unity 6000.3.28f1'i resmi game-ci imajında
(`unityci/editor:ubuntu-6000.3.28f1-base-3.2.2`) çalıştırır. Lisans sırları tanımlı değilse
iş atlanır. Tanımlamak için:

1. Unity Hub ile kişisel lisansı bir kez etkinleştirin. Oluşan `Unity_lic.ulf` dosyası:
   Windows `C:\ProgramData\Unity\Unity_lic.ulf`, macOS
   `/Library/Application Support/Unity/Unity_lic.ulf`, Linux `~/.local/share/unity3d/Unity/Unity_lic.ulf`.
2. GitHub → depo → Settings → Secrets and variables → Actions altında üç sır ekleyin:
   `UNITY_LICENSE` (dosyanın tam içeriği), `UNITY_EMAIL`, `UNITY_PASSWORD`.
3. Actions → "Unity EditMode testleri" → Run workflow.
4. İş bitince `unity-uretilen-dosyalar` yapıtını indirin. İçindeki `.meta` dosyalarını ve
   `Packages/packages-lock.json` dosyasını (varsa Unity'nin düzelttiği `manifest.json` ile)
   aynı yollara kopyalayıp **bir kez** commit edin. Bundan sonra GUID'ler sabittir.

## Yapı

| Klasör | İçerik |
|--------|--------|
| `Assets/TarihStrateji/Core/` | 16 saf C# modülü, her biri `noEngineReferences` `.asmdef` ile |
| `Assets/TarihStrateji/Unity/` | Sunum katmanı (yalnız `IGameCommandGateway` / `IGameReadModel`) |
| `Tools/HeadlessTests/` | .NET 8 NUnit testleri ve `.asmdef` aynası projeler |
| `.claude/skills/` | Ajan becerileri (router, Unity, YZ, kayıt, arayüz, performans, denge) |
| `.github/workflows/` | CI: headless testler ve `.meta` denetimi |

## Lisanslar

`.claude/skills/` altındaki beceriler (yerel `game-balance` hariç)
[awesome-gamedev-agent-skills](https://github.com/ERK-netizen/awesome-gamedev-agent-skills)
deposundan Apache-2.0 lisansıyla alınmıştır; bkz. `.claude/skills/LICENSE` ve `NOTICE`.
