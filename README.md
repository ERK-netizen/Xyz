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

Unity Personal'ı kullanma koşulu Unity'nin şartlarındadır (bugün: son 12 ayda 200 bin USD'nin
altında gelir/fon); bu koşulu depo sahibi doğrular.

`.github/workflows/unity-tests.yml` iki yoldan birini seçer. Sır yoksa iş atlanır.

**Yol A — önerilen, dosyasız (yalnız hesap bilgisi):**

1. https://id.unity.com adresinde ücretsiz bir Unity hesabı açın (varsa onu kullanın). CI için
   ayrı bir hesap önerilir; hesapta iki adımlı doğrulama açıksa otomatik giriş başarısız olabilir.
2. GitHub → depo → Settings → Secrets and variables → Actions altında iki sır ekleyin:
   `UNITY_EMAIL` ve `UNITY_PASSWORD`. **Parolayı sohbete veya dosyaya yazmayın; yalnız sır olarak girin.**
3. Actions → "Unity EditMode testleri" → Run workflow. İş, 6000.3.28f1 editörünü
   `buildalon/unity-setup` ile kurar, Personal lisansı `buildalon/activate-unity-license` ile
   hesabınızla etkinleştirir ve EditMode testlerini çalıştırır (Unity Hub ve `.ulf` gerekmez).

**Yol B — game-ci imajı (`.ulf` dosyasıyla):** Unity Hub ile kişisel lisansı bir kez
etkinleştirin; oluşan `Unity_lic.ulf` (Windows `C:\ProgramData\Unity\Unity_lic.ulf`, macOS
`/Library/Application Support/Unity/Unity_lic.ulf`, Linux `~/.local/share/unity3d/Unity/Unity_lic.ulf`)
içeriğini `UNITY_LICENSE` sırrına, ayrıca `UNITY_EMAIL` ve `UNITY_PASSWORD` sırlarını ekleyin.
`UNITY_LICENSE` tanımlıysa bu yol çalışır.

Yerelde de ücretsizdir: Unity Hub → Personal lisans → projeyi 6000.3.28f1 ile açın.

**Her iki yolda:** iş bitince `unity-uretilen-dosyalar` yapıtını indirin. İçindeki `.meta` dosyalarını ve
   `Packages/packages-lock.json` dosyasını (varsa Unity'nin düzelttiği `manifest.json` ile)
   aynı yollara kopyalayıp **bir kez** commit edin. Bundan sonra GUID'ler sabittir.

## APK (Android telefona kurulum)

1. Yukarıdaki Yol A sırları (`UNITY_EMAIL`, `UNITY_PASSWORD`) tanımlı olmalıdır.
2. Actions → "Android derlemesi" iş akışı; geliştirme dalına oyun dosyası gönderildiğinde (push)
   kendiliğinden, varsayılan dalda ise "Run workflow" ile çalışır (Play Store için `aab` seçeneği).
3. İş bitince `tarih-strateji-android` yapıtını indirin; içindeki `TarihStrateji.apk` dosyasını
   telefona kopyalayıp açın ("bilinmeyen kaynaklardan yükleme" izni istenir). Android 7.1+ ve
   64 bit (ARM64) cihaz gerekir. APK hata ayıklama anahtarıyla imzalıdır; mağaza yayını için
   ayrı bir yayın anahtarı (keystore) gerekir.

Unity Editor'de: menü **Tarih Strateji → Android APK derle** aynı ayarlarla `Build/Android/`
altına derler.

## Yapı

| Klasör | İçerik |
|--------|--------|
| `Assets/TarihStrateji/Core/` | 16 saf C# modülü, her biri `noEngineReferences` `.asmdef` ile |
| `Assets/TarihStrateji/Unity/` | Bootstrap (oturum), sunum (harita, dokunmatik, UI Toolkit; yalnız Domain sözleşmeleri), Editor (Android derleme) |
| `Tools/HeadlessTests/` | .NET 8 NUnit testleri ve `.asmdef` aynası projeler |
| `.claude/skills/` | Ajan becerileri (router, Unity, YZ, kayıt, arayüz, performans, denge) |
| `.github/workflows/` | CI: headless testler, `.meta` denetimi, Unity EditMode, Android APK |

## Lisanslar

`.claude/skills/` altındaki beceriler (yerel `game-balance` hariç)
[awesome-gamedev-agent-skills](https://github.com/ERK-netizen/awesome-gamedev-agent-skills)
deposundan Apache-2.0 lisansıyla alınmıştır; bkz. `.claude/skills/LICENSE` ve `NOTICE`.
