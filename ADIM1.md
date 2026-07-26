# Adım 1 — Çalışan Proje İskeleti ve Yapılandırma

## Amaç

ASP.NET Core Web API'nin çalışan temelini oluşturmak; TMDB ve Telegram entegrasyonlarında kullanılacak ayarları, HTTP istemcilerini, OpenAPI dokümantasyonunu ve sağlık kontrolünü hazırlamak.

Bu bilgisayarda .NET 10 SDK kurulu olmadığı için ilk sürüm **.NET 8 LTS** ile başlatılacaktır. .NET 10 kurulduğunda proje hedefi ayrıca yükseltilebilir.

## Bu adımda yapılacaklar

- [x] `TelegramMovieBot.sln` solution dosyasını oluştur.
- [x] `src/TelegramMovieBot.Api` altında ASP.NET Core Web API oluştur.
- [x] `tests/TelegramMovieBot.Tests` altında test projesi oluştur.
- [x] Test projesini API projesine bağla.
- [x] TMDB, Telegram ve bildirim ayar sınıflarını oluştur.
- [x] Uygulama açılırken ayarları doğrula.
- [x] TMDB ve Telegram için typed `HttpClient` sınıflarını oluştur.
- [x] OpenAPI desteğini etkinleştir.
- [x] `GET /health` sağlık kontrolünü ekle.
- [x] Development ortamı için güvenli örnek ayarları ekle.
- [x] Projeyi build et ve testleri çalıştır.

## Oluşturulacak yapı

```text
TelegramMovieBot.sln
src/
  TelegramMovieBot.Api/
    Clients/
      TmdbClient.cs
      TelegramClient.cs
    Options/
      TmdbOptions.cs
      TelegramOptions.cs
      NotificationOptions.cs
    Program.cs
    appsettings.json
tests/
  TelegramMovieBot.Tests/
```

## Yapılandırma alanları

```text
Tmdb:BaseUrl
Tmdb:AccessToken
Tmdb:Language
Tmdb:Region

Telegram:BotToken
Telegram:ChatId

Notification:Enabled
Notification:Hour
Notification:Minute
Notification:TimeZone
Notification:MaxMoviesPerList
```

Canlı ortam karşılıkları çift alt çizgi kullanılarak verilecektir:

```env
TMDB__AccessToken=...
TELEGRAM__BotToken=...
TELEGRAM__ChatId=...
```

Gerçek token değerleri hiçbir zaman Git'e eklenmeyecektir.

## Endpoint

```http
GET /health
```

Beklenen başarılı cevap:

```text
Healthy
```

Development ortamında OpenAPI belgesi:

```http
GET /swagger/v1/swagger.json
```

## Tamamlanma kriterleri

Adım 1 aşağıdaki koşullar sağlandığında tamamlanmış sayılır:

1. Solution içindeki API ve test projeleri hatasız build olur.
2. Testler başarıyla çalışır.
3. API yerelde başlatılabilir.
4. `/health` endpoint'i HTTP `200` döndürür.
5. Development ortamında OpenAPI belgesi üretilebilir.
6. Hiçbir gizli anahtar kaynak kodda veya Git'e eklenecek ayar dosyalarında bulunmaz.

## Sonraki adım

Adım 2'de TMDB'nin `now_playing` ve `upcoming` endpoint'leri bağlanacak, modeller oluşturulacak ve film verileri API üzerinden sunulacaktır.
