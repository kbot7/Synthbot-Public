param(
    [string]$username,
    [string]$sqlUser,
    [string]$sqlPassword,
    [string]$webAppName
)

$DbName ="discordbot-ci-$username-db"
$ResourceGroup = "synthbot-dev-ci-$username"
$Username = $sqlUser
$password = $sqlPassword

Set-AzureRMWebApp -ConnectionStrings @{ DefaultConnection = @{ Type="SQLAzure"; Value ="Server=tcp:synthbot-dev-db.database.windows.net;Database=$DbName;User ID=$Username@synthbot-dev-db;Password=$password;Trusted_Connection=False;Encrypt=True;" } } -Name $webAppName -ResourceGroupName $ResourceGroup