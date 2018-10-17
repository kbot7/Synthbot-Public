dotnet msbuild "Synthbot.WebApp/Synthbot.WebApp.csproj" ^
/p:PublishUrl=..\publish ^
/p:DeployOnBuild=true ^
/p:Configuration=Release ^
/p:WebPublishMethod=FileSystem ^
/p:DeployTarget=WebPublish ^
/p:AutoParameterizationWebConfigConnectionStrings=false ^
/p:RuntimeIdentifier=win10-x64 ^
/p:SelfContained=true ^
/p:SolutionDir="."

dotnet msbuild "Synthbot.DiscordBot/Synthbot.DiscordBot.csproj" ^
/p:PublishUrl=..\publish ^
/p:DeployOnBuild=true ^
/p:Configuration=Release ^
/p:WebPublishMethod=FileSystem ^
/p:DeployTarget=WebPublish ^
/p:AutoParameterizationWebConfigConnectionStrings=false ^
/p:RuntimeIdentifier=win10-x64 ^
/p:SelfContained=true ^
/p:SolutionDir="."