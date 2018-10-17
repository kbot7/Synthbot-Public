param(
	[string]$username,
	[string]$sqlUser,
	[string]$sqlPassword,
	[string]$webAppName,
	[string]$storageConnectionString
)

$DbName ="discordbot-ci-$username-db"
$resourceGroupName = "synthbot-dev-ci-$username"

$webApp = Get-AzureRmWebApp -ResourceGroupName $resourceGroupName -Name $webAppName
$appSettings = $webapp.SiteConfig.AppSettings
$newAppSettings = @{}
ForEach ($item in $appSettings) {
	$newAppSettings[$item.Name] = $item.Value
}
$newAppSettings['synthbot.webapp.host'] = $webApp.HostNames[0]
$newAppSettings['synthbot.webapp.protocol'] = "https"
$newAppSettings['synthbot.storage.connectionstring'] = $storageConnectionString
$newAppSettings['synthbot.storage.log.web.tablename'] = "DevAzure" + "$username" + "WebLog"
$newAppSettings['synthbot.storage.log.bot.tablename'] = "DevAzure" + "$username" + "BotLog"

Set-AzureRMWebApp -ConnectionStrings @{ DefaultConnection = @{ Type="SQLAzure"; Value ="Server=tcp:synthbot-dev-db.database.windows.net;Database=$DbName;User ID=$sqlUser@synthbot-dev-db;Password=$sqlPassword;Trusted_Connection=False;Encrypt=True;" } } -Name $webAppName -ResourceGroupName $resourceGroupName -AppSettings $newAppSettings