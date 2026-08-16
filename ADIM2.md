# Adım 2 — TMDB Film Entegrasyonu

## Amaç

TMDB API üzerinden Türkiye'de vizyondaki ve yakında vizyona girecek filmleri çekmek ve ASP.NET Core Web API üzerinden JSON olarak sunmak.

Bu adım sonunda aşağıdaki endpoint'ler çalışacaktır:

```http
GET /api/movies/now-playing
GET /api/movies/upcoming
```

## Kullanılacak TMDB endpoint'leri

### Vizyondaki filmler

```http
GET https://api.themoviedb.org/3/movie/now_playing
```

### Gelecek filmler

```http
GET https://api.themoviedb.org/3/movie/upcoming
```

İki istekte de şu parametreler kullanılacaktır:

```text
language=tr-TR
region=TR
page=1
```

Kimlik doğrulaması HTTP başlığında yapılacaktır:

```http
Authorization: Bearer TMDB_ACCESS_TOKEN
```

## Adım 2.1 — TMDB erişim anahtarını hazırlama

**Durum: Tamamlandı.** Access Token, .NET User Secrets içinde saklanıyor.

1. [TMDB](https://www.themoviedb.org/) hesabı oluştur.
2. Hesap ayarlarından API bölümünü aç.
3. **API Read Access Token** değerini al.
4. Token'ı kaynak koda veya `appsettings.json` dosyasına yazma.
5. Yerel geliştirme için .NET User Secrets kullan:

```powershell
dotnet user-secrets init `
  --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj

dotnet user-secrets set "Tmdb:AccessToken" "TMDB_TOKEN_BURAYA" `
  --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj
```

Kontrol etmek için:

```powershell
dotnet user-secrets list `
  --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj
```

Token terminal çıktısında veya ekran görüntüsünde paylaşılmamalıdır.

## Adım 2.2 — TMDB cevap modellerini oluşturma

**Durum: Tamamlandı.** Ham TMDB modelleri ve dışarı açılacak sade film modeli oluşturuldu.

Oluşturulacak dosyalar:

```text
src/TelegramMovieBot.Api/
  Models/
    Movie.cs
    TmdbMovie.cs
    TmdbMovieResponse.cs
```

### `TmdbMovie`

TMDB'den gelen ham film alanlarını temsil eder:

```text
id
title
original_title
overview
poster_path
release_date
vote_average
popularity
adult
```

JSON alanları `JsonPropertyName` ile açıkça eşleştirilmelidir.

### `TmdbMovieResponse`

Sayfalı TMDB cevabını temsil eder:

```text
page
results
total_pages
total_results
```

### `Movie`

Kendi API'mizin dışarı döndüreceği sade modeldir:

```text
Id
Title
OriginalTitle
Overview
PosterUrl
ReleaseDate
VoteAverage
Popularity
TmdbUrl
```

TMDB'nin ham cevabı doğrudan kullanıcıya döndürülmeyecektir. Böylece dış servisteki değişiklikler kendi API sözleşmemizi daha az etkiler.

## Adım 2.3 — `TmdbClient` metotlarını yazma

**Durum: Tamamlandı.** Vizyondaki ve gelecek filmler için istemci metotları ve temel istemci testleri eklendi.

`TmdbClient` içine iki metot eklenecektir:

```csharp
Task<TmdbMovieResponse> GetNowPlayingAsync(
    int page,
    CancellationToken cancellationToken);

Task<TmdbMovieResponse> GetUpcomingAsync(
    int page,
    CancellationToken cancellationToken);
```

Kurallar:

- Sayfa değeri `1` değerinden küçük olamaz.
- Dil ve bölge ayarları `TmdbOptions` üzerinden okunmalıdır.
- Her çağrıda `CancellationToken` kullanılmalıdır.
- Başarısız HTTP cevapları sessizce boş listeye çevrilmemelidir.
- TMDB hata cevabı loglanmalı fakat erişim token'ı loglanmamalıdır.
- JSON cevabı boş veya geçersizse anlaşılır bir uygulama hatası üretilmelidir.

## Adım 2.4 — `MovieService` oluşturma

**Durum: Tamamlandı.** Film filtreleme, sıralama ve sade API modeline dönüştürme servisi eklendi.

Oluşturulacak dosya:

```text
src/TelegramMovieBot.Api/Services/MovieService.cs
```

Servisin görevleri:

- `TmdbClient` üzerinden ham veriyi almak.
- Yetişkin içerikleri filtrelemek.
- Ham TMDB modelini kendi `Movie` modelimize dönüştürmek.
- Afiş adresini oluşturmak.
- TMDB film detay bağlantısını oluşturmak.
- Vizyondaki filmleri popülerlik değerine göre sıralamak.
- Gelecek filmleri yayın tarihine göre sıralamak.

Eksik veriler şu şekilde ele alınmalıdır:

- Başlık yoksa `original_title` kullanılmalı.
- Açıklama yoksa boş metin dönmeli.
- Afiş yoksa `PosterUrl` değeri `null` olmalı.
- Yayın tarihi geçersiz veya yoksa `ReleaseDate` değeri `null` olmalı.

## Adım 2.5 — Movies Controller oluşturma

**Durum: Tamamlandı.** Vizyondaki ve gelecek filmler endpoint'leri ile sayfa doğrulaması eklendi.

Oluşturulacak dosya:

```text
src/TelegramMovieBot.Api/Controllers/MoviesController.cs
```

### Vizyondakiler

```http
GET /api/movies/now-playing?page=1
```

Başarılı cevap:

```json
[
  {
    "id": 123,
    "title": "Örnek Film",
    "originalTitle": "Example Movie",
    "overview": "Film açıklaması",
    "posterUrl": "https://image.tmdb.org/t/p/w500/example.jpg",
    "releaseDate": "2026-07-24",
    "voteAverage": 7.5,
    "popularity": 120.4,
    "tmdbUrl": "https://www.themoviedb.org/movie/123?language=tr-TR"
  }
]
```

### Gelecek filmler

```http
GET /api/movies/upcoming?page=1
```

`page` değeri verilmezse varsayılan olarak `1` kullanılmalıdır.

Geçersiz örnek:

```http
GET /api/movies/upcoming?page=0
```

Beklenen cevap:

```text
HTTP 400 Bad Request
```

## Adım 2.6 — Hata yönetimi

**Durum: Tamamlandı.** Merkezi hata yakalama ve güvenli `ProblemDetails` cevapları eklendi.

Temel hata senaryoları:

| Durum | API cevabı |
|---|---|
| Geçersiz sayfa | `400 Bad Request` |
| TMDB token eksik | `503 Service Unavailable` |
| TMDB yetkilendirme hatası | `502 Bad Gateway` |
| TMDB zaman aşımı | `504 Gateway Timeout` |
| TMDB geçici olarak kapalı | `502 Bad Gateway` |
| Beklenmeyen uygulama hatası | `500 Internal Server Error` |

Hata cevapları ASP.NET Core `ProblemDetails` biçiminde olmalıdır.

Canlı ortam hata cevabında şunlar bulunmamalıdır:

- TMDB access token
- Stack trace
- Sunucu dosya yolları
- Hassas yapılandırma değerleri

## Adım 2.7 — Testler

En az şu testler yazılmalıdır:

- [x] TMDB film cevabı doğru modele çevriliyor.
- [x] Yetişkin içerik filtreleniyor.
- [x] Başlık yoksa orijinal başlık kullanılıyor.
- [x] Afiş yolu yoksa `PosterUrl` null oluyor.
- [x] Gelecek filmler tarihe göre sıralanıyor.
- [x] Vizyondaki filmler popülerliğe göre sıralanıyor.
- [x] `page=0` isteği `400` döndürüyor.
- [x] TMDB başarısız cevabı başarılı boş liste gibi gösterilmiyor.
- [x] İstek iptal edildiğinde `CancellationToken` çalışıyor.

Testlerde gerçek TMDB API çağrısı yapılmamalıdır. Sahte bir `HttpMessageHandler` veya istemci arayüzü kullanılmalıdır.

## Adım 2.8 — Elle doğrulama

**Durum: Tamamlandı.** Sağlık, OpenAPI, doğrulama ve gerçek TMDB endpoint'leri yerel API üzerinden kontrol edildi.

Uygulamayı başlat:

```powershell
dotnet run `
  --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj
```

Swagger arayüzünü aç:

```text
http://localhost:PORT/swagger
```

Kontrol edilecek istekler:

```http
GET /api/movies/now-playing
GET /api/movies/now-playing?page=2
GET /api/movies/upcoming
GET /api/movies/upcoming?page=0
GET /health
```

## Güvenlik kuralları

- TMDB token Git'e eklenmeyecek.
- Token URL query parametresi olarak gönderilmeyecek.
- Token loglara yazılmayacak.
- Endpoint'lerden token veya iç yapılandırma dönülmeyecek.
- Dış HTTP çağrılarına zaman aşımı uygulanacak.
- Gelen `page` değeri doğrulanacak.

## Tamamlanma kriterleri

**Adım 2 tamamlandı.** Aşağıdaki kriterlerin tamamı doğrulandı.

Adım 2 aşağıdaki koşullar sağlandığında tamamlanmış sayılır:

1. TMDB erişim token'ı User Secrets üzerinden okunur.
2. Vizyondaki filmler endpoint'i gerçek TMDB verisi döndürür.
3. Gelecek filmler endpoint'i gerçek TMDB verisi döndürür.
4. Sonuçlar Türkçe ve Türkiye bölgesine göre istenir.
5. API yalnızca kendi sade `Movie` modelini dışarı açar.
6. Hatalar `ProblemDetails` biçiminde güvenli şekilde döndürülür.
7. Otomatik testler başarıyla çalışır.
8. Solution hatasız build olur.

## Sonraki adım

Adım 3'te film listeleri Telegram mesajına dönüştürülecek ve hedef sohbet veya kanala test mesajı gönderilecektir.

## Resmî kaynaklar

- [TMDB uygulama kimlik doğrulaması](https://developer.themoviedb.org/docs/authentication-application)
- [TMDB Now Playing](https://developer.themoviedb.org/reference/movie-now-playing-list)
- [TMDB Upcoming](https://developer.themoviedb.org/reference/movie-upcoming-list)
