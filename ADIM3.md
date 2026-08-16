# Adım 3 — Telegram Entegrasyonu ve Test Mesajı

## Amaç

TMDB'den alınan vizyon ve gelecek film listelerini okunabilir Telegram mesajlarına dönüştürmek ve seçilen kullanıcıya, gruba veya kanala göndermek.

Bu adım sonunda geliştirme ortamında aşağıdaki endpoint ile gerçek test bildirimi gönderilebilecektir:

```http
POST /api/notifications/test
```

## Uygulama sırası

- [x] 3.1 Telegram botunu oluştur ve token'ı güvenli sakla.
- [x] 3.2 Mesajın gönderileceği hedefin `chat_id` değerini belirle.
- [x] 3.3 Telegram istek ve cevap modellerini oluştur.
- [x] 3.4 `TelegramClient` ile `sendMessage` entegrasyonunu yaz.
- [x] 3.5 Film listesini Telegram mesajına dönüştüren formatter'ı yaz.
- [x] 3.6 Bildirim servisini oluştur.
- [x] 3.7 Yalnızca geliştirme ortamında çalışan test endpoint'ini ekle.
- [x] 3.8 Otomatik testleri tamamla.
- [x] 3.9 Gerçek Telegram test mesajını doğrula.

## Adım 3.1 — Telegram botunu oluşturma

**Durum: Tamamlandı.** Bot oluşturuldu ve token .NET User Secrets içinde saklandı.

Telegram'da resmî `@BotFather` hesabını aç:

1. `/newbot` komutunu gönder.
2. Bot için görünen bir ad belirle.
3. `bot` ile biten benzersiz bir kullanıcı adı belirle.
4. BotFather'ın verdiği bot token'ını güvenli bir yerde tut.

Bot token'ı bir parola gibidir. Token'ı:

- Kaynak koda yazma.
- `appsettings.json` dosyasına yazma.
- Git'e ekleme.
- Ekran görüntüsünde veya mesajlarda paylaşma.

Token açığa çıkarsa BotFather üzerinden yenisi oluşturulmalıdır.

User Secrets'a kaydet:

```powershell
dotnet user-secrets set "Telegram:BotToken" "BOT_TOKEN_BURAYA" `
  --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj
```

## Adım 3.2 — Hedef `chat_id` değerini bulma

**Durum: Tamamlandı.** Kişisel sohbet kimliği bulundu ve .NET User Secrets içinde saklandı.

### Kişisel sohbet

Botlar kullanıcıyla konuşmayı kendiliğinden başlatamaz. Önce:

1. Telegram'da botu aç.
2. **Başlat** düğmesine bas veya `/start` mesajı gönder.
3. Botun aldığı güncellemelerden kişisel `chat_id` değerini öğren.

### Grup

1. Botu gruba ekle.
2. Grupta bota hitap eden bir mesaj gönder.
3. Güncelleme verisindeki `message.chat.id` değerini al.

Grup ve süper grup kimlikleri negatif sayı olabilir. Bu normaldir.

### Kanal

1. Botu kanala ekle.
2. Mesaj gönderebilmesi için yönetici yetkisi ver.
3. Herkese açık kanalda `@kanalkullaniciadi` değeri doğrudan `chat_id` olarak kullanılabilir.

`chat_id` belirlendikten sonra User Secrets'a kaydet:

```powershell
dotnet user-secrets set "Telegram:ChatId" "CHAT_ID_BURAYA" `
  --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj
```

Güncellemeleri okumak için token'ı tarayıcı geçmişine veya kaynak koda yazmak yerine, geliştirme sırasında güvenli ve geçici bir yardımcı metot kullanılacaktır.

## Adım 3.3 — Telegram modelleri

**Durum: Tamamlandı.** Mesaj isteği, standart API cevabı ve gönderilen mesaj modelleri oluşturuldu.

Oluşturulacak dosyalar:

```text
src/TelegramMovieBot.Api/
  Models/Telegram/
    TelegramSendMessageRequest.cs
    TelegramApiResponse.cs
    TelegramMessage.cs
```

Gönderilecek temel alanlar:

```json
{
  "chat_id": "HEDEF",
  "text": "MESAJ",
  "parse_mode": "HTML",
  "disable_web_page_preview": true
}
```

Telegram cevabındaki `ok`, `result`, `error_code` ve `description` alanları güvenli şekilde ele alınmalıdır.

## Adım 3.4 — `TelegramClient` yazma

**Durum: Tamamlandı.** Güvenli `sendMessage` çağrısı, yapılandırma kontrolleri ve Telegram hata yönetimi eklendi.

Mevcut `TelegramClient` içine şu metot eklenecektir:

```csharp
Task SendMessageAsync(
    string text,
    CancellationToken cancellationToken = default);
```

İstek adresi:

```text
POST https://api.telegram.org/bot{BOT_TOKEN}/sendMessage
```

Kurallar:

- Bot token ve `chat_id` eksikse Telegram'a istek gönderilmemeli.
- İstek gövdesi JSON olarak gönderilmeli.
- Her çağrıda `CancellationToken` kullanılmalı.
- HTTP ve Telegram API hataları boş veya başarılı cevap gibi gösterilmemeli.
- Token hiçbir log mesajına yazılmamalı.
- Tam istek adresi token içerdiği için loglanmamalı.
- HTTP zaman aşımı uygulanmalı.

Telegram URL'sinin token içermesi nedeniyle varsayılan `HttpClient` loglarının hassas bilgi yazmaması ayrıca kontrol edilmelidir.

## Adım 3.5 — Film mesajını hazırlama

**Durum: Tamamlandı.** Türkçe tarih ve puan biçimi, HTML güvenliği, film sınırı ve mesaj bölme desteği eklendi.

Oluşturulacak dosya:

```text
src/TelegramMovieBot.Api/Services/TelegramMessageFormatter.cs
```

Formatter'ın görevleri:

- Vizyondaki ve gelecek filmler için ayrı başlık oluşturmak.
- Film adı, yayın tarihi, puan ve TMDB bağlantısını göstermek.
- Eksik tarih ve puanı güvenli şekilde ele almak.
- Telegram HTML biçimlendirmesine özel karakterleri kaçırmak.
- Mesaj uzunluğunu Telegram sınırının altında tutmak.
- Ayarlardaki `MaxMoviesPerList` değerine uymak.

Örnek mesaj:

```text
🎬 <b>Vizyondaki Filmler</b>

1. <b>Film Adı</b>
📅 16 Ağustos 2026 · ⭐ 7.5/10
🔗 Film detayları

🚀 <b>Yakında</b>

1. <b>Gelecek Film</b>
📅 21 Ağustos 2026
🔗 Film detayları

Film verileri TMDB tarafından sağlanmaktadır.
```

Telegram `sendMessage` metni, entity işlemesinden sonra en fazla 4096 karakter olabilir. Güvenli pay bırakmak için uygulama mesajları yaklaşık 3500 karakter civarında bölecektir.

## Adım 3.6 — Bildirim servisi

**Durum: Tamamlandı.** İki film listesini hazırlayıp Telegram'a sıralı gönderen bildirim servisi eklendi.

Oluşturulacak dosya:

```text
src/TelegramMovieBot.Api/Services/MovieNotificationService.cs
```

Servis şu sırayla çalışacaktır:

1. `MovieService` üzerinden vizyondaki filmleri al.
2. `MovieService` üzerinden gelecek filmleri al.
3. Listeleri ayarlanan maksimum film sayısına indir.
4. `TelegramMessageFormatter` ile mesaj veya mesajlar oluştur.
5. `TelegramClient` ile sırayla gönder.
6. Gönderim sonucunu hassas veri içermeden logla.

İlk sürümde iki ayrı Telegram mesajı göndermek daha okunabilir olacaktır:

1. Vizyondaki filmler
2. Yakında çıkacak filmler

## Adım 3.7 — Test bildirimi endpoint'i

**Durum: Tamamlandı.** Development ortamında çalışan ve Production ortamında `404` dönen test endpoint'i eklendi.

Oluşturulacak dosya:

```text
src/TelegramMovieBot.Api/Controllers/NotificationsController.cs
```

Endpoint:

```http
POST /api/notifications/test
```

Güvenlik kuralları:

- Endpoint yalnızca `Development` ortamında çalışmalı.
- Canlı ortamda endpoint haritalanmamalı veya `404` dönmeli.
- Dışarıdan mesaj metni kabul etmemeli; mesajı sunucu kendi oluşturmalı.
- Her çağrı gerçek Telegram mesajı oluşturduğu için `GET` değil `POST` kullanılmalı.
- Başarılı cevap token, `chat_id` veya Telegram'ın ham cevabını içermemeli.

Başarılı örnek cevap:

```json
{
  "message": "Test bildirimi gönderildi.",
  "sentMessageCount": 2
}
```

## Adım 3.8 — Hata yönetimi

Merkezi `ProblemDetails` sistemine Telegram hataları eklenecektir:

| Durum | API cevabı |
|---|---|
| Bot token veya `chat_id` eksik | `503 Service Unavailable` |
| Telegram token geçersiz | `502 Bad Gateway` |
| Bot hedef sohbete erişemiyor | `502 Bad Gateway` |
| Telegram zaman aşımı | `504 Gateway Timeout` |
| Mesaj biçimi veya uzunluğu geçersiz | `502 Bad Gateway` |

Telegram'ın ham hata açıklaması loglanabilir fakat istemciye doğrudan verilmemelidir. Token hiçbir durumda loglanmamalıdır.

## Adım 3.9 — Otomatik testler

- [x] Telegram isteği doğru HTTP metodu ve JSON gövdesiyle gönderiliyor.
- [x] Bot token eksikse dış istek yapılmıyor.
- [x] `chat_id` eksikse dış istek yapılmıyor.
- [x] Telegram başarısız cevabı hata olarak ele alınıyor.
- [x] Film adındaki `<`, `>` ve `&` karakterleri HTML için kaçırılıyor.
- [x] Eksik yayın tarihi doğru gösteriliyor.
- [x] Film sayısı `MaxMoviesPerList` değerini aşmıyor.
- [x] Mesaj Telegram uzunluk sınırını aşmıyor.
- [x] İstek iptal edildiğinde `CancellationToken` çalışıyor.
- [x] Canlı ortamda test endpoint'i kullanılamıyor.

Testlerde gerçek Telegram mesajı gönderilmeyecektir. Gerçek gönderim yalnızca son elle doğrulama aşamasında yapılacaktır.

## Adım 3.10 — Elle doğrulama

**Durum: Tamamlandı.** Gerçek TMDB verileriyle iki mesaj kişisel Telegram sohbetine gönderildi ve hassas URL'nin loglanmadığı doğrulandı.

1. Bot token ve `chat_id` değerlerini User Secrets'a kaydet.
2. Botun hedef sohbet veya kanalda mesaj gönderme yetkisini doğrula.
3. API'yi Development ortamında başlat.
4. Swagger üzerinden `POST /api/notifications/test` çağrısını yap.
5. İki Telegram mesajının geldiğini kontrol et.
6. Başlık, tarih, puan ve bağlantıları kontrol et.
7. Uygulama loglarında token bulunmadığını kontrol et.

Uygulamayı çalıştırma:

```powershell
dotnet run `
  --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj
```

Swagger:

```text
http://localhost:PORT/swagger
```

## Tamamlanma kriterleri

**Adım 3 tamamlandı.** Telegram yapılandırması, mesaj üretimi, güvenli gönderim, otomatik testler ve gerçek sohbet doğrulaması başarıyla tamamlandı.

Adım 3 aşağıdaki koşullar sağlandığında tamamlanmış sayılır:

1. Bot token ve `chat_id`, User Secrets üzerinden okunur.
2. TMDB film listeleri okunabilir Telegram mesajlarına dönüşür.
3. HTML özel karakterleri güvenli şekilde işlenir.
4. Mesajlar Telegram uzunluk sınırına uyar.
5. Development test endpoint'i çalışır.
6. Canlı ortamda test endpoint'i kapalıdır.
7. Gerçek hedef sohbete test mesajları ulaşır.
8. Otomatik testlerin tamamı geçer.
9. Solution hatasız build olur.
10. Token kaynak kodda, Git geçmişinde veya loglarda bulunmaz.

## Sonraki adım

Adım 4'te `BackgroundService` oluşturularak film bildiriminin her gün `Europe/Istanbul` saat diliminde belirlenen saatte otomatik gönderilmesi sağlanacaktır.

## Resmî kaynaklar

- [Telegram bot oluşturma ve BotFather](https://core.telegram.org/bots/features#botfather)
- [Telegram botlara giriş](https://core.telegram.org/bots)
- [Telegram Bot API — sendMessage](https://core.telegram.org/bots/api#sendmessage)
- [Telegram bot başlangıç eğitimi](https://core.telegram.org/bots/tutorial)
