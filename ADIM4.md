# Adım 4 — Otomatik Günlük Telegram Bildirimi

## Amaç

Vizyondaki ve gelecek filmler bildirimini her gün yapılandırılan saatte, `Europe/Istanbul` saat dilimine göre otomatik göndermek.

Bu aşamada elle kullanılan test endpoint'i korunacak, ayrıca uygulama içinde sürekli çalışan bir `BackgroundService` eklenecektir.

## Uygulama sırası

- [x] 4.1 Bildirim zamanını hesaplayan test edilebilir zamanlama sınıfını oluştur.
- [x] 4.2 `MovieNotificationWorker` arka plan servisini oluştur.
- [x] 4.3 Worker'ı uygulama servislerine kaydet.
- [x] 4.4 Bildirim ayarlarını ve doğrulamasını tamamla.
- [x] 4.5 Aynı gün çift gönderimi engelle.
- [x] 4.6 Hata, iptal ve yeniden deneme davranışını ekle.
- [x] 4.7 Otomatik testleri tamamla.
- [x] 4.8 Kısa süreli gerçek zamanlama testi yap.
- [x] 4.9 Canlı ortam çalışma kurallarını doğrula.

## Adım 4.1 — Zaman hesaplama

Oluşturulacak dosya:

```text
src/TelegramMovieBot.Api/Services/NotificationSchedule.cs
```

Sınıfın görevi:

- UTC anını `Europe/Istanbul` yerel saatine çevirmek.
- Bugünkü planlanan saat henüz gelmediyse bugünü seçmek.
- Planlanan saat geçtiyse ertesi günü seçmek.
- Bir sonraki çalışmaya kalan süreyi hesaplamak.
- Yaz/kış saati ve saat dilimi dönüşümünü `TimeZoneInfo` ile yapmak.

Örnek:

```text
Şu an:             16 Ağustos 2026 09:00
Bildirim saati:    10:00
Sonraki çalışma:   16 Ağustos 2026 10:00
```

```text
Şu an:             16 Ağustos 2026 11:00
Bildirim saati:    10:00
Sonraki çalışma:   17 Ağustos 2026 10:00
```

Zaman hesabı doğrudan `DateTime.Now` kullanmamalıdır. Test edilebilirlik için UTC zamanı parametre veya `TimeProvider` üzerinden alınmalıdır.

## Adım 4.2 — BackgroundService

Oluşturulacak dosya:

```text
src/TelegramMovieBot.Api/Workers/MovieNotificationWorker.cs
```

Worker şu sırayla çalışacaktır:

1. `Notification:Enabled` ayarını kontrol et.
2. Bir sonraki bildirim zamanını hesapla.
3. O zamana kadar iptal edilebilir biçimde bekle.
4. Yeni bir dependency injection scope oluştur.
5. `IMovieNotificationService` üzerinden bildirimi gönder.
6. Başarılı gönderim tarihini bellekte sakla.
7. Bir sonraki günün zamanını yeniden hesapla.

Uygulama açılır açılmaz mesaj gönderilmemelidir. Her zaman ayarlanan saat beklenmelidir.

## Adım 4.3 — Servis kaydı

Worker, `Program.cs` içinde hosted service olarak kaydedilecektir:

```csharp
builder.Services.AddHostedService<MovieNotificationWorker>();
```

`IMovieNotificationService` scoped olduğu için worker onu doğrudan constructor üzerinden almamalıdır. Her çalışmada `IServiceScopeFactory` ile yeni scope oluşturulmalıdır.

## Adım 4.4 — Bildirim ayarları

Kullanılacak ayarlar:

```json
{
  "Notification": {
    "Enabled": false,
    "Hour": 10,
    "Minute": 0,
    "TimeZone": "Europe/Istanbul",
    "MaxMoviesPerList": 8
  }
}
```

Canlı ortam değişkenleri:

```env
NOTIFICATION__Enabled=true
NOTIFICATION__Hour=10
NOTIFICATION__Minute=0
NOTIFICATION__TimeZone=Europe/Istanbul
NOTIFICATION__MaxMoviesPerList=8
```

Doğrulama kuralları:

- `Hour`: `0–23`
- `Minute`: `0–59`
- `TimeZone`: sistemde bulunabilen geçerli bir saat dilimi
- `MaxMoviesPerList`: `1–20`

Yerel geliştirmede otomatik gönderim varsayılan olarak kapalı kalmalıdır. Böylece API her başlatıldığında istemeden mesaj üretilmez.

## Adım 4.5 — Çift gönderimi engelleme

İlk sürümde uygulama tek replika olarak çalışacaktır. Worker bellekte son başarılı gönderim tarihini tutacaktır.

Kurallar:

- Aynı yerel takvim gününde ikinci başarılı gönderim yapılmamalı.
- Başarısız deneme başarılı gönderim olarak kaydedilmemeli.
- Development test endpoint'i otomatik worker'ın durumunu değiştirmemeli.
- Uygulama yeniden başlarsa bellek bilgisi kaybolur.

> Bellek içi kontrol ilk MVP için yeterlidir ancak yeniden başlatma durumunda kesin koruma sağlamaz. Kalıcı ve çok replikalı çözüm için PostgreSQL üzerinde benzersiz bildirim kaydı Adım 4 sonrasında eklenmelidir.

Kalıcı çözümde önerilen benzersiz anahtar:

```text
(chat_id, notification_type, notification_date)
```

## Adım 4.6 — Hata ve yeniden deneme

Worker bir dış servis hatasında uygulamayı çökertmemelidir.

İlk sürüm davranışı:

- Hata güvenli şekilde loglanır.
- Token ve `chat_id` loglanmaz.
- İlk başarısız denemeden sonra kısa bir gecikmeyle tekrar denenir.
- En fazla 3 deneme yapılır.
- Toplam üç deneme yapılır; başarısız denemeler arasında yaklaşık 1 ve 5 dakika beklenir.
- Uygulama kapanıyorsa bekleme ve istekler hemen iptal edilir.
- Üç deneme de başarısızsa ertesi gün normal programa dönülür.

Testlerde gerçek dakikalar boyunca beklenmeyecektir; `TimeProvider` veya gecikme soyutlaması kullanılacaktır.

## Adım 4.7 — Otomatik testler

- [x] Planlanan saat bugün gelmediyse bugünü seçiyor.
- [x] Planlanan saat geçtiyse ertesi günü seçiyor.
- [x] Saat tam planlanan zamandaysa çift çalışma oluşturmuyor.
- [x] `Europe/Istanbul` dönüşümü doğru yapılıyor.
- [x] `Enabled=false` iken bildirim gönderilmiyor.
- [x] Uygulama açılır açılmaz mesaj gönderilmiyor.
- [x] Planlanan zamanda bildirim servisi bir kez çağrılıyor.
- [x] Aynı gün ikinci bildirim engelleniyor.
- [x] Başarısız gönderim başarılı olarak işaretlenmiyor.
- [x] Geçici hata yeniden deneniyor.
- [x] `CancellationToken` beklemeyi ve gönderimi durduruyor.
- [x] Token veya `chat_id` loglanmıyor.

## Adım 4.8 — Kısa gerçek zamanlama testi

Gerçek günlük saati beklememek için yalnızca test sırasında:

1. Bildirim saati mevcut saatten 1–2 dakika sonrasına ayarlanır.
2. `Notification:Enabled=true` yapılır.
3. API başlatılır.
4. Başlangıçta hemen mesaj gelmediği kontrol edilir.
5. Planlanan dakikada iki film mesajının geldiği doğrulanır.
6. Aynı dakika içinde ikinci gönderim olmadığı kontrol edilir.
7. Test bittikten sonra `Enabled=false` yapılır.

Bu ayarlar Git'e yazılmadan User Secrets veya geçici ortam değişkenleri üzerinden verilmelidir.

Yerel test ayarları:

```powershell
dotnet user-secrets set "Notification:Enabled" "true" `
  --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj

dotnet user-secrets set "Notification:Hour" "SAAT" `
  --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj

dotnet user-secrets set "Notification:Minute" "DAKİKA" `
  --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj
```

Testten sonra:

```powershell
dotnet user-secrets set "Notification:Enabled" "false" `
  --project src/TelegramMovieBot.Api/TelegramMovieBot.Api.csproj
```

## Adım 4.9 — Canlı ortam kuralları

`BackgroundService` kullanıldığı sürece:

- Minimum replika: `1`
- Maksimum replika: `1`
- Uygulama uykuya alınmamalı.
- Container sürekli çalışmalı.
- Sunucunun fiziksel saat dilimine güvenilmemeli; `Europe/Istanbul` açıkça kullanılmalı.
- Sağlık kontrolü `/health` üzerinden yapılmalı.
- Uygulama yeniden başlatmaları takip edilmeli.

Birden fazla replika açılırsa aynı bildirimin birden fazla kez gönderilme riski vardır. Çoklu replika öncesinde kalıcı veri tabanı kilidi veya ayrı zamanlanmış job mimarisi kullanılmalıdır.

## Tamamlanma kriterleri

**Adım 4 tamamlandı.** Zaman hesaplama, worker, aynı gün tekrar koruması, sınırlı retry, otomatik testler ve gerçek zamanlanmış Telegram gönderimi doğrulandı.

Adım 4 aşağıdaki koşullar sağlandığında tamamlanmış sayılır:

1. Worker yalnızca `Notification:Enabled=true` olduğunda çalışır.
2. Bildirim Türkiye saatine göre doğru zamanda gönderilir.
3. Uygulama açılır açılmaz mesaj gönderilmez.
4. Aynı gün çift otomatik gönderim yapılmaz.
5. Geçici hatalarda sınırlı yeniden deneme uygulanır.
6. Uygulama kapanırken worker düzgün durur.
7. Gerçek kısa zamanlama testi başarılıdır.
8. Otomatik testlerin tamamı geçer.
9. Solution sıfır hata ve sıfır uyarıyla build olur.
10. Canlı ortam tek replika kuralı dokümante edilmiştir.

## Sonraki adım

Adım 5'te Dockerfile, container sağlık kontrolü ve Azure Container Apps deployment ayarları hazırlanacaktır.
