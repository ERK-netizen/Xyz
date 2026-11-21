# Android imza anahtarları

`gelistirme.keystore` **gizli değildir**: parolası (`tarihstrateji-gelistirme`, takma ad `tarihstrateji`) bu depoda ve
`.github/workflows/android-build.yml`'de açıkça yazılıdır. Tek amacı, yayın anahtarı tanımlanmamışken her CI derlemesinin
**aynı** imzayı taşımasıdır; böylece yeni APK eskisinin üzerine kurulur ve cihazdaki kayıtlar silinmez (Unity'nin hata
ayıklama anahtarı her çalıştırıcıda farklıdır).

Bu anahtar yalnız kişisel, mağaza dışı kurulum içindir. Play Store ya da herkese dağıtım için kendi yayın anahtarınızı
depo sırlarına ekleyin (`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASS`);
sırlar varsa iş akışı her zaman onları kullanır. Anahtar değiştiğinde (geliştirme → yayın) uygulama bir kez kaldırılıp
yeniden kurulmalıdır.
