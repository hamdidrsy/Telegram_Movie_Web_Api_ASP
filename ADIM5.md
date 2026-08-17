# Adım 5 — Backend'i Günde Bir Kez Canlıda Çalıştırma

## Seçilen canlı ortam: GitHub Actions

Azure hesabında aktif abonelik bulunmadığı için günlük çalışma GitHub Actions üzerinde hazırlanmıştır. Bu yöntemde bilgisayarın açık kalmasına gerek yoktur ve sürekli çalışan sunucu bulunmaz.

Workflow:

```text
.github/workflows/daily-movie-notification.yml
```

Çalışma planı:

```text
Her gün 10:00 — Europe/Istanbul
```

GitHub Actions, planlı workflow'u varsayılan branch'in son commit'i üzerinden çalıştırır. Workflow ayrıca GitHub Actions ekranından elle başlatılabilir.

Gerekli GitHub repository secret'ları:

```text
TMDB_ACCESS_TOKEN
TELEGRAM_BOT_TOKEN
TELEGRAM_CHAT_ID
```

Durum:

- [x] Günlük workflow oluşturuldu.
- [x] `Europe/Istanbul` saat dilimi tanımlandı.
- [x] Elle çalıştırma desteği eklendi.
- [x] Secret tabanlı yapılandırma eklendi.
- [x] Eşzamanlı çift çalışma engellendi.
- [x] On dakikalık job zaman aşımı eklendi.
- [ ] GitHub CLI oturumu açıldı.
- [ ] Repository secret'ları kaydedildi.
- [ ] Workflow varsayılan branch'e gönderildi.
- [ ] İlk manuel GitHub Actions çalışması doğrulandı.

> Public repository'lerde standart GitHub-hosted runner kullanımı ücretsizdir. Private repository'lerde GitHub Free hesabı aylık 2.000 dakika içerir. Bu workflow günde yalnızca kısa bir job çalıştırır.

> GitHub, public repository 60 gün boyunca etkinlik almazsa zamanlanmış workflow'ları otomatik kapatabilir. Böyle bir durumda Actions ekranından workflow tekrar etkinleştirilmelidir.

## Alternatif mimari: Azure Container Apps Job

Uygulama sürekli açık bir web sunucusu olarak değil, **Azure Container Apps Scheduled Job** olarak çalışacaktır:

```text
Her gün 07:00 UTC / 10:00 Türkiye saati
                  |
                  v
Azure Container Apps Job container'ı başlatır
                  |
                  v
TMDB -> iki film listesi -> Telegram'a iki mesaj
                  |
                  v
Container başarıyla kapanır
```

Bu yaklaşımda bilgisayarın açık kalmasına gerek yoktur. Container yalnızca görev çalışırken kaynak tüketir.

## Hazırlananlar

- [x] `--run-once` tek seferlik çalışma modu
- [x] Multi-stage Dockerfile
- [x] Güvenli `.dockerignore`
- [x] Varsayılan container komutu: `--run-once`
- [x] Azure kaynaklarını oluşturan deployment betiği
- [x] Secret reference tabanlı ortam değişkenleri
- [x] Tek paralel çalışma
- [x] Beş dakikalık görev zaman aşımı
- [x] Azure seviyesinde iki tekrar denemesi
- [x] Günlük `07:00 UTC` cron planı
- [x] Azure CLI kurulumu
- [ ] Azure hesabına giriş
- [ ] Azure kaynaklarının oluşturulması
- [ ] Canlı manuel job testi
- [ ] İlk zamanlanmış çalışmanın doğrulanması

## Neden 07:00 UTC?

Azure Container Apps Job cron ifadelerini UTC olarak değerlendirir. Türkiye yıl boyunca UTC+3 kullandığı için:

```text
07:00 UTC = 10:00 Europe/Istanbul
```

Kullanılan cron:

```text
0 7 * * *
```

## Yerel Docker doğrulaması

İmajı oluştur:

```powershell
docker build -t telegram-movie-bot:local .
```

Container varsayılan olarak tek seferlik job modunda çalışır. Gerçek token'lar Dockerfile veya imaj içine yazılmamalıdır.

## Azure ön koşulları

- Aktif Azure aboneliği
- Azure CLI
- Container Apps CLI uzantısı
- Azure hesabında kaynak oluşturma yetkisi

Kurulumdan sonra:

```powershell
az login
az extension add --name containerapp --upgrade
az provider register --namespace Microsoft.App
az provider register --namespace Microsoft.OperationalInsights
az provider register --namespace Microsoft.ContainerRegistry
```

## Deployment

Secret değerlerini yalnızca mevcut PowerShell oturumuna koy:

```powershell
$env:TMDB_ACCESS_TOKEN="..."
$env:TELEGRAM_BOT_TOKEN="..."
$env:TELEGRAM_CHAT_ID="..."
```

Registry adı Azure genelinde benzersiz, yalnızca küçük harf ve rakamlardan oluşmalıdır:

```powershell
.\deploy\azure-container-app-job.ps1 `
  -RegistryName "benzersizregistryadi"
```

Betik şunları oluşturur:

1. Resource Group
2. Azure Container Registry
3. Container imajı
4. Container Apps Environment
5. Günlük zamanlanmış Container Apps Job
6. TMDB ve Telegram secret referansları

## Canlı test

Job'ı planlanan saati beklemeden bir kez başlat:

```powershell
az containerapp job start `
  --name telegram-movie-bot-job `
  --resource-group telegram-movie-bot-rg
```

Son çalışmaları listele:

```powershell
az containerapp job execution list `
  --name telegram-movie-bot-job `
  --resource-group telegram-movie-bot-rg `
  --output table
```

## Güvenlik

- Token'lar Dockerfile'a veya imaja eklenmez.
- Token'lar Git'e eklenmez.
- Azure Job içinde secret olarak saklanır.
- Container'a yalnızca secret reference ile aktarılır.
- Job tek paralel replica ile çalışır.
- Telegram istemcisinin hassas URL loglaması kapalıdır.

## Tamamlanma kriterleri

Adım 5 şu koşullarda tamamlanır:

1. Docker imajı hatasız oluşturulur.
2. Azure Container Apps Job oluşturulur.
3. Manuel job çalışması başarılı olur.
4. Telegram'a iki canlı mesaj ulaşır.
5. Job başarı koduyla kapanır.
6. Günlük cron planı `0 7 * * *` olarak görünür.
7. İlk otomatik günlük çalıştırma başarılı olur.

## Resmî kaynaklar

- [Azure Container Apps Jobs](https://learn.microsoft.com/en-us/azure/container-apps/jobs)
- [Azure CLI ile Container Apps Job oluşturma](https://learn.microsoft.com/en-us/azure/container-apps/jobs-get-started-cli)
- [Azure Container Apps secret yönetimi](https://learn.microsoft.com/en-us/azure/container-apps/manage-secrets)
