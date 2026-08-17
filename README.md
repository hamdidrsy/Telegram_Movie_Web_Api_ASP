# Telegram Movie Bot API

Türkiye'de vizyondaki ve yakında vizyona girecek filmleri TMDB üzerinden alan, düzenli bir mesaj biçimine dönüştüren ve Telegram'a gönderen ASP.NET Core projesidir.

Projenin mevcut amacı, bilgisayarın açık kalmasına ihtiyaç duymadan her gün bir kez çalışıp güncel film listesini Telegram sohbetine göndermektir.

## Mevcut özellikler

- Türkiye'de vizyondaki filmleri listeler.
- Yakında vizyona girecek filmleri listeler.
- TMDB sonuçlarını Türkçe ve Türkiye bölgesine göre alır.
- Yetişkin içerikleri filtreler.
- Filmleri tarih ve popülerliğe göre sıralar.
- Film adı, yayın tarihi, puan ve TMDB bağlantısı içeren Telegram mesajları oluşturur.
- Telegram HTML karakterlerini güvenli biçimde işler.
- Uzun mesajları Telegram sınırının altında tutar.
- Her gün GitHub Actions üzerinden otomatik çalışır.
- İstenirse GitHub Actions ekranından elle çalıştırılabilir.
- API, Docker container veya tek seferlik job olarak çalışabilir.
- Merkezi `ProblemDetails` hata cevapları üretir.
- Aynı gün tekrarlı otomatik gönderime karşı uygulama içi koruma içerir.

## Günlük çalışma

Canlı bildirim görevi GitHub Actions üzerinde çalışır:

```text
Her gün 10:00 — Europe/Istanbul
```

Workflow dosyası:

```text
.github/workflows/daily-movie-notification.yml
```

Workflow şu işlemleri gerçekleştirir:

1. Projeyi indirir.
2. .NET 8 ortamını hazırlar.
3. Bağımlılıkları geri yükler.
4. Uygulamayı `--run-once` modunda çalıştırır.
5. TMDB'den iki film listesi alır.
6. Telegram'a iki mesaj gönderir.
7. Başarıyla kapanır.

GitHub repository secret kasasında şu anahtarlar bulunmalıdır:

```text
TMDB_ACCESS_TOKEN
TELEGRAM_BOT_TOKEN
TELEGRAM_CHAT_ID
```

Secret değerleri kaynak kodda, workflow dosyasında veya bu README içinde tutulmaz.

## Telegram komutları

Bot şu anda gelen Telegram mesajlarını veya komutlarını dinlemez. Yalnızca zamanlanmış veya elle başlatılan backend görevi üzerinden bildirim gönderir.

İlk kurulumda kullanıcı, botla özel sohbet açabilmek için Telegram'da bir kez şu komutu gönderir:

```text
/start
```

`/start` mevcut uygulama tarafından cevaplanan bir komut değildir; Telegram'ın bot ile kullanıcı arasında sohbet başlatmasını sağlar.

Şu komutlar henüz uygulanmamıştır:

```text
/vizyondakiler
/yakinda
/bildirim
```

Bu komutların çalışabilmesi için Telegram webhook veya long polling desteğinin ayrıca geliştirilmesi ve sürekli erişilebilir bir backend üzerinde çalıştırılması gerekir.

## API endpoint'leri

### Sağlık kontrolü

```http
GET /health
```

### Vizyondaki filmler

```http
GET /api/movies/now-playing?page=1
```

### Gelecek filmler

```http
GET /api/movies/upcoming?page=1
```

### Test bildirimi

```http
POST /api/notifications/test
```

Test bildirimi endpoint'i yalnızca `Development` ortamında çalışır. `Production` ortamında `404 Not Found` döndürür.

Development ortamında Swagger arayüzü:

```text
/swagger
```

## Kullanılan teknolojiler

- .NET 8 LTS
- ASP.NET Core Web API
- C#
- Typed `HttpClient`
- ASP.NET Core `BackgroundService`
- ASP.NET Core Options ve validation
- Swagger / OpenAPI
- xUnit
- Docker
- GitHub Actions
- TMDB API v3
- Telegram Bot API

## Proje yapısı

```text
src/TelegramMovieBot.Api/
  Clients/          TMDB ve Telegram HTTP istemcileri
  Controllers/      Film ve test bildirimi endpoint'leri
  Exceptions/       Dış servis ve yapılandırma hataları
  Infrastructure/   Merkezi hata yönetimi ve çalışma modu
  Models/           API, TMDB ve Telegram modelleri
  Options/          Uygulama ayarları
  Services/         Film, mesaj ve bildirim servisleri
  Workers/          Günlük arka plan görevi

tests/TelegramMovieBot.Tests/
  Otomatik birim ve davranış testleri

.github/workflows/
  Günlük canlı bildirim workflow'u

deploy/
  İsteğe bağlı Azure Container Apps Job deployment betiği
```

## Yerel kurulum

Gereksinimler:

- .NET 8 SDK veya daha yeni uyumlu SDK
- TMDB API Read Access Token
- Telegram bot token
- Telegram hedef sohbet kimliği

Projeyi geri yükle ve test et:

```powershell
dotnet restore TelegramMovieBot.sln
dotnet test TelegramMovieBot.sln
```

User Secrets desteği proje içinde etkin durumdadır. Gizli değerleri yerelde kaydet:

```powershell
dotnet user-secrets set "Tmdb:AccessToken" "TMDB_TOKEN" `
  --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj

dotnet user-secrets set "Telegram:BotToken" "BOT_TOKEN" `
  --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj

dotnet user-secrets set "Telegram:ChatId" "CHAT_ID" `
  --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj
```

Örneklerdeki değerleri gerçek secret değerleriyle değiştirin. Gerçek değerleri Git'e eklemeyin.

API'yi çalıştır:

```powershell
dotnet run --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj
```

## Tek seferlik bildirim görevi

TMDB'den film verilerini alıp Telegram mesajlarını gönderdikten sonra uygulamayı kapatır:

```powershell
dotnet run `
  --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj `
  -- `
  --run-once
```

Bu mod GitHub Actions ve container job çalışmaları için kullanılır.

## Docker

İmajı oluştur:

```powershell
docker build -t telegram-movie-bot:local .
```

Docker imajının varsayılan komutu `--run-once` modudur. Secret değerleri imaj içine kopyalanmamalı, çalışma anında ortam değişkeni veya secret yöneticisi üzerinden verilmelidir.

## Yapılandırma

Hassas olmayan varsayılan ayarlar:

```json
{
  "Tmdb": {
    "BaseUrl": "https://api.themoviedb.org/3/",
    "Language": "tr-TR",
    "Region": "TR"
  },
  "Notification": {
    "Enabled": false,
    "Hour": 10,
    "Minute": 0,
    "TimeZone": "Europe/Istanbul",
    "MaxMoviesPerList": 8
  }
}
```

Ortam değişkenlerinde iç içe ayarlar çift alt çizgiyle yazılır:

```text
Tmdb__AccessToken
Telegram__BotToken
Telegram__ChatId
Notification__Enabled
Notification__Hour
Notification__Minute
Notification__TimeZone
Notification__MaxMoviesPerList
```

## Güvenlik

- Token ve sohbet kimliği kaynak kodda tutulmaz.
- `.env` ve yerel ayar dosyaları Git tarafından yok sayılır.
- GitHub Actions yalnızca repository secret referanslarını kullanır.
- Telegram token'ını içeren istek adresleri HTTP loglarından çıkarılmıştır.
- Production ortamında test bildirimi endpoint'i kapalıdır.
- API hata cevapları stack trace ve secret içermez.

Token yanlışlıkla paylaşılırsa BotFather üzerinden hemen yenilenmeli ve GitHub repository secret değeri güncellenmelidir.

## Testler

Tüm testleri çalıştır:

```powershell
dotnet test TelegramMovieBot.sln
```

Testler aşağıdaki alanları kapsar:

- TMDB istekleri ve veri dönüşümü
- Film filtreleme ve sıralama
- Telegram JSON istekleri
- Mesaj biçimlendirme ve HTML güvenliği
- Hata yönetimi
- Zaman hesaplama
- Tekrar gönderim kontrolü
- Retry ve iptal davranışı
- Development/Production endpoint davranışı
- Tek seferlik job modu

## Veri kaynağı

Film verileri [TMDB](https://www.themoviedb.org/) tarafından sağlanmaktadır.

Bu ürün TMDB API'sini kullanır ancak TMDB tarafından desteklenmemekte veya onaylanmamaktadır.
