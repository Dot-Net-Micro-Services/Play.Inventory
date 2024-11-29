# Play.Inventory
Inventory Microservice

## Create and publish package
```powershell
$version="1.0.2"
$owner="Dot-Net-Micro-Services"
$gh_pat="[PAT HERE]"

dotnet pack src\Play.Inventory.Contracts\ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/Play.Inventory -o ..\packages

dotnet nuget push ..\packages\Play.Inventory.Contracts.$version.nupkg --api-key $gh_pat --source "github"
```