# Sinema Telegram Botu — En Hızlı Canlıya Çıkış Planı

## 1. Hedef

Türkiye'de vizyondaki ve yakında vizyona girecek filmleri düzenli olarak çekip Telegram kanalına, grubuna veya kullanıcıya otomatik mesaj gönderen bir ASP.NET Core Web API geliştirmek.

İlk sürümün hedefi:

- TMDB üzerinden vizyondaki filmleri almak.
- TMDB üzerinden gelecek filmleri almak.
- Türkçe başlık, tarih, puan ve film bağlantısını hazırlamak.
- Her gün belirlenen saatte Telegram'a özet mesaj göndermek.
- Uygulamayı Docker ile canlıya almak.
- Anahtarları kaynak koda yazmadan ortam değişkenlerinde saklamak.

## 2. En hızlı teknik seçim

İlk sürümde gereksiz servisler eklemeyelim:

| Konu | İlk sürüm kararı |
|---|---|
| Platform | ASP.NET Core Web API, .NET 10 LTS |
| Film kaynağı | TMDB API v3 |
| Telegram | Telegram Bot API'ye `HttpClient` ile doğrudan istek |
| Zamanlama | API içinde çalışan `BackgroundService` |
| Veri tabanı | İlk sürümde yok |
| Tekrar gönderim kontrolü | Günlük tek özet mesaj; bellek içi son çalışma kontrolü |
| API dokümantasyonu | OpenAPI |
| Paketleme | Docker |
| İlk hosting tercihi | Azure Container Apps, minimum replika `1` |
| Alternatif hosting | Render üzerinde Docker Web Service/Background Worker |

> Önemli: `BackgroundService` kullandığımız ilk sürüm tek replika ile çalışmalıdır. Birden fazla replika aynı bildirimi birden fazla kez gönderebilir. Uygulama uykuya alınır veya sıfıra ölçeklenirse zamanlanmış bildirim de çalışmaz. Bu nedenle canlı ortamda minimum replika `1` olmalıdır.

## 3. Basit mimari

```text
TMDB API
   |
   v
MovieService -----> MessageFormatter
                         |
                         v
                  TelegramService -----> Telegram
                         ^
                         |
NotificationWorker ------+

ASP.NET Core API:
  GET  /api/movies/now-playing
  GET  /api/movies/upcoming
  POST /api/notifications/test
  GET  /health
```

İlk sürüm tek uygulamadır. Web API ve zamanlanmış görev aynı container içinde çalışır.

## 4. Kullanılacak dış servisler

### TMDB

1. [TMDB](https://www.themoviedb.org/) hesabı aç.
2. API ayarlarından bir **API Read Access Token** oluştur.
3. Token'ı `TMDB__AccessToken` ortam değişkenine koy.
4. İsteklerde `Authorization: Bearer TOKEN` kullan.

Kullanılacak adresler:

- Vizyondakiler: `GET https://api.themoviedb.org/3/movie/now_playing?language=tr-TR&region=TR&page=1`
- Gelecek filmler: `GET https://api.themoviedb.org/3/movie/upcoming?language=tr-TR&region=TR&page=1`
- Afiş: `https://image.tmdb.org/t/p/w500/{poster_path}`
- Film sayfası: `https://www.themoviedb.org/movie/{id}?language=tr-TR`

Not: `region=TR`, sonuçları Türkiye bölgesine göre daraltır. TMDB'nin “upcoming” listesi bir “discover” sorgusu gibi çalıştığı için yayın tarihlerini uygulama tarafında da kontrol etmek faydalıdır.

### Telegram

1. Telegram'da `@BotFather` ile `/newbot` komutunu kullan.
2. Alınan token'ı `TELEGRAM__BotToken` ortam değişkenine koy.
3. Botu hedef gruba veya kanala ekle.
4. Kanala mesaj atacaksa botu yönetici yap.
5. Hedefin `chat_id` değerini öğren ve `TELEGRAM__ChatId` olarak tanımla.

Mesaj gönderme adresi:

```text
POST https://api.telegram.org/bot{BOT_TOKEN}/sendMessage
```

Telegram mesajı çok uzamasın diye ilk sürümde her listeden en fazla 5–10 film gönderelim. Gerekirse mesajı parçalara bölelim.

## 5. Önerilen proje yapısı

```text
TelegramMovieBot.sln
src/
  TelegramMovieBot.Api/
    Controllers/
      MoviesController.cs
      NotificationsController.cs
    Clients/
      TmdbClient.cs
      TelegramClient.cs
    Models/
      Movie.cs
      TmdbMovieResponse.cs
    Options/
      TmdbOptions.cs
      TelegramOptions.cs
      NotificationOptions.cs
    Services/
      MovieService.cs
      MessageFormatter.cs
    Workers/
      NotificationWorker.cs
    Program.cs
    appsettings.json
    Dockerfile
tests/
  TelegramMovieBot.Tests/
.dockerignore
.gitignore
README.md
```

İlk aşamada katmanlı mimariyi fazla büyütmeye gerek yok. Arayüzler yalnızca test veya dış servis değişimi gerektiren sınıflarda kullanılmalı.

## 6. Ortam değişkenleri

Canlı ortamda aşağıdaki değerler tanımlanmalı:

```env
TMDB__BaseUrl=https://api.themoviedb.org/3/
TMDB__AccessToken=...
TMDB__Language=tr-TR
TMDB__Region=TR

TELEGRAM__BotToken=...
TELEGRAM__ChatId=...

NOTIFICATION__Enabled=true
NOTIFICATION__Hour=10
NOTIFICATION__Minute=0
NOTIFICATION__TimeZone=Europe/Istanbul
NOTIFICATION__MaxMoviesPerList=8
```

Kurallar:

- Gerçek token'lar `appsettings.json`, README veya Git içine yazılmamalı.
- Yerelde `.NET user-secrets`, canlıda hosting servisinin secret/secret environment özelliği kullanılmalı.
- Uygulama açılırken zorunlu ayarlar doğrulanmalı; eksik token varsa açık bir hata ile başlamamalı veya bildirim özelliği kapalı olmalı.
- Loglarda bot token'ı, TMDB token'ı ve tam Telegram isteği yazılmamalı.

## 7. Uygulama adımları

### Aşama 1 — Çalışan iskelet

- .NET 10 Web API projesini ve test projesini oluştur.
- Options sınıflarını ve yapılandırma doğrulamasını ekle.
- Typed `HttpClient` ile `TmdbClient` ve `TelegramClient` oluştur.
- OpenAPI ve `/health` endpoint'ini ekle.

Tamamlanma ölçütü: Uygulama yerelde açılır ve `/health` başarılı cevap verir.

### Aşama 2 — Film verileri

- TMDB `now_playing` endpoint'ini bağla.
- TMDB `upcoming` endpoint'ini bağla.
- Sonuçları tarih ve popülerliğe göre düzenle.
- Yetişkin içeriğini filtrele.
- Boş afiş, boş açıklama ve eksik tarih durumlarını güvenli şekilde işle.

Tamamlanma ölçütü: İki film endpoint'i Türkçe JSON döndürür.

### Aşama 3 — Telegram mesajı

- Film listesini okunabilir HTML mesajına dönüştür.
- Telegram `sendMessage` çağrısını ekle.
- `/api/notifications/test` endpoint'i ile elle test mesajı gönder.
- Bu endpoint'i yalnızca geliştirme ortamında aç veya basit bir admin API anahtarıyla koru.

Örnek mesaj:

```text
🎬 Bu Hafta Vizyonda

1. Film Adı
⭐ 7.8/10 · 📅 26 Temmuz 2026
🔗 Detay

🚀 Yakında
...
```

Tamamlanma ölçütü: Yerelden hedef Telegram sohbetine test mesajı gelir.

### Aşama 4 — Otomatik çalışma

- `BackgroundService` içinde bir sonraki çalışma zamanını `Europe/Istanbul` saat dilimine göre hesapla.
- Uygulama açılır açılmaz mesaj göndermek yerine planlanan saati bekle.
- Hata durumunda uygulamayı çökertme; logla ve sınırlı tekrar dene.
- `CancellationToken` kullanarak uygulamanın düzgün kapanmasını sağla.
- Aynı gün ikinci kez gönderimi engelle.

Tamamlanma ölçütü: Test saati verilince mesaj tam o saatte bir kez gelir.

### Aşama 5 — Docker ve canlı ortam

- Multi-stage Dockerfile ekle.
- Container'ın .NET 8 ve sonrası varsayılan portu olan `8080` üzerinde dinlemesini sağla.
- Container'ı yerelde çalıştır ve `/health` ile kontrol et.
- Azure'a gönder, secret'ları tanımla ve minimum replika sayısını `1` yap.

Tamamlanma ölçütü: Canlı `/health` endpoint'i çalışır ve canlı container Telegram mesajı gönderir.

## 8. En hızlı deployment yolu: Azure Container Apps

Azure CLI kurulu ve Azure hesabına giriş yapılmışsa proje kökünden temel akış:

```powershell
az login
az extension add --name containerapp --upgrade
az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.OperationalInsights

az containerapp up `
  --name telegram-movie-bot `
  --resource-group telegram-movie-bot-rg `
  --location northeurope `
  --source . `
  --ingress external `
  --target-port 8080
```

`az containerapp up`, kaynak kodu build edip container registry ve Container App kaynaklarını oluşturabilen hızlı yoldur. İlk dağıtımdan sonra:

1. Azure Portal'da secret'ları oluştur.
2. Secret'ları yukarıdaki ortam değişkenlerine bağla.
3. Minimum replika sayısını `1`, ilk sürümde maksimum replika sayısını da `1` yap.
4. `/health` adresini kontrol et.
5. Test bildirimini gönder.
6. Container loglarını kontrol et.

> Maliyet oluşabilir. Kaynakları açmadan önce Azure fiyatlandırmasını kontrol et. Kullanılmayan test resource group'larını sil.

## 9. Test listesi

Canlıya çıkmadan önce:

- [ ] TMDB token geçerli.
- [ ] `language=tr-TR` ve `region=TR` kullanılıyor.
- [ ] Vizyondaki filmler endpoint'i çalışıyor.
- [ ] Gelecek filmler endpoint'i çalışıyor.
- [ ] Boş sonuç geldiğinde uygulama hata vermiyor.
- [ ] Telegram bot hedef kanalda/grupta mesaj yetkisine sahip.
- [ ] Test mesajı doğru sohbete gidiyor.
- [ ] Uzun mesajlar Telegram limitine takılmadan parçalanıyor.
- [ ] Token'lar Git geçmişinde bulunmuyor.
- [ ] `/api/notifications/test` canlıda korunuyor veya kapalı.
- [ ] Saat dilimi `Europe/Istanbul`.
- [ ] Aynı bildirim iki kez gitmiyor.
- [ ] Container portu `8080`.
- [ ] Canlı ortam minimum ve maksimum replika sayısı `1`.
- [ ] `/health` başarılı.
- [ ] Uygulama yeniden başladığında loglarda hata yok.

## 10. İlk sürümden sonra yapılacaklar

MVP çalıştıktan sonra aşağıdaki sırayla geliştirebiliriz:

1. PostgreSQL ekleyip gönderilen film ve bildirim geçmişini saklamak.
2. `BackgroundService` yerine ayrı worker, Azure Container Apps Job veya Quartz/Hangfire kullanmak.
3. Kullanıcının `/vizyondakiler`, `/yakinda`, `/aboneol` komutlarını desteklemek.
4. Birden fazla kanal ve kullanıcıya bildirim göndermek.
5. Kullanıcının tür, dil ve bildirim saati tercihini saklamak.
6. Mesajlara afiş görseli eklemek.
7. Retry, timeout, rate limiting ve circuit breaker politikalarını geliştirmek.
8. GitHub Actions ile otomatik test ve deployment eklemek.
9. OpenTelemetry/Application Insights ile izleme eklemek.

Veri tabanı eklenince bildirim kaydı için önerilen benzersiz anahtar:

```text
(chat_id, notification_type, notification_date)
```

Bu anahtar, birden fazla replika veya yeniden başlatma durumunda çift mesajı kalıcı olarak engeller.

## 11. Tahmini çalışma sırası

En hızlı uygulanabilir sıra:

1. Proje iskeleti ve ayarlar
2. TMDB entegrasyonu
3. Telegram test mesajı
4. Mesaj biçimlendirme
5. Zamanlayıcı
6. Testler
7. Docker
8. Azure deployment
9. Canlı bildirim testi

MVP kapsamını korursak ilk sürümde kullanıcı kaydı, yönetim paneli, kapsamlı veri tabanı modeli, mikroservis ve mesaj kuyruğu gerekli değildir.

## 12. Resmî kaynaklar

- [TMDB uygulama kimlik doğrulaması](https://developer.themoviedb.org/docs/authentication-application)
- [TMDB Upcoming endpoint'i](https://developer.themoviedb.org/reference/movie-upcoming-list)
- [TMDB Now Playing endpoint'i](https://developer.themoviedb.org/reference/movie-now-playing-list)
- [Telegram Bot API ve sendMessage](https://core.telegram.org/bots/api#sendmessage)
- [Azure Container Apps ile .NET](https://learn.microsoft.com/en-us/azure/container-apps/dotnet-overview)
- [az containerapp up ile hızlı deployment](https://learn.microsoft.com/en-us/azure/container-apps/containerapp-up)
- [Render Docker deployment](https://render.com/docs/docker)

