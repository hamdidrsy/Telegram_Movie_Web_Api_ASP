param(
    [string]$ResourceGroup = "telegram-movie-bot-rg",
    [string]$Location = "northeurope",
    [string]$EnvironmentName = "telegram-movie-bot-env",
    [Parameter(Mandatory = $true)]
    [string]$RegistryName,
    [string]$JobName = "telegram-movie-bot-job",
    [string]$CronExpression = "0 7 * * *"
)

$ErrorActionPreference = "Stop"

$requiredVariables = @(
    "TMDB_ACCESS_TOKEN",
    "TELEGRAM_BOT_TOKEN",
    "TELEGRAM_CHAT_ID"
)

foreach ($variableName in $requiredVariables) {
    $value = [Environment]::GetEnvironmentVariable($variableName)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "$variableName ortam değişkeni tanımlanmalıdır."
    }
}

az group create `
    --name $ResourceGroup `
    --location $Location `
    --output none

az acr create `
    --name $RegistryName `
    --resource-group $ResourceGroup `
    --sku Basic `
    --admin-enabled true `
    --output none

az acr build `
    --registry $RegistryName `
    --image "telegram-movie-bot:latest" `
    .

az containerapp env create `
    --name $EnvironmentName `
    --resource-group $ResourceGroup `
    --location $Location `
    --output none

$registryCredentials = az acr credential show `
    --name $RegistryName `
    --resource-group $ResourceGroup | ConvertFrom-Json
$registryServer = "$RegistryName.azurecr.io"
$registryUsername = $registryCredentials.username
$registryPassword = $registryCredentials.passwords[0].value

az containerapp job create `
    --name $JobName `
    --resource-group $ResourceGroup `
    --environment $EnvironmentName `
    --trigger-type Schedule `
    --cron-expression $CronExpression `
    --replica-timeout 300 `
    --replica-retry-limit 2 `
    --replica-completion-count 1 `
    --parallelism 1 `
    --image "$registryServer/telegram-movie-bot:latest" `
    --cpu 0.25 `
    --memory 0.5Gi `
    --registry-server $registryServer `
    --registry-username $registryUsername `
    --registry-password $registryPassword `
    --secrets `
        "tmdb-access-token=$env:TMDB_ACCESS_TOKEN" `
        "telegram-bot-token=$env:TELEGRAM_BOT_TOKEN" `
        "telegram-chat-id=$env:TELEGRAM_CHAT_ID" `
    --env-vars `
        "Tmdb__AccessToken=secretref:tmdb-access-token" `
        "Telegram__BotToken=secretref:telegram-bot-token" `
        "Telegram__ChatId=secretref:telegram-chat-id" `
        "Notification__Enabled=false" `
        "Notification__MaxMoviesPerList=8" `
    --output none

Write-Output "Azure Container Apps Job oluşturuldu: $JobName"
Write-Output "Günlük cron (UTC): $CronExpression"
