param(
    [string] $fullBranch
)

$branchPaths = $fullBranch.split('/')

if ($branchPaths.Count -eq 5 -and $branchPaths[2] -eq "users")
{
    $username = $branchPaths[3]
    Write-Host "##vso[task.setvariable variable=WebAppName]discordbot-ci-$username"
    Write-Host "##vso[task.setvariable variable=Branch.Username]$username"
    Write-Host "Branch User: $username"

} else {
    Write-Host "No user found"
}
