param(
    [string]$username
)

$secret = Get-AzureKeyVaultSecret -VaultName 'synthbot-dev-vault' -Name $username'-discord-bot-token'

Write-Host "key: $secret.SecretValueText"
$secretValue = $secret.SecretValueText

if ($secret.SecretValueText -ne $null) {
    Write-Host "##vso[task.setvariable variable=discordBotToken]$secretValue"
}

