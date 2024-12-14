# Play.Inventory
Inventory Microservice

## Create and publish package
```powershell
$version="1.0.3"
$owner="Dot-Net-Micro-Services"
$gh_pat="[PAT HERE]"

dotnet pack src\Play.Inventory.Contracts\ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/Play.Inventory -o ..\packages

dotnet nuget push ..\packages\Play.Inventory.Contracts.$version.nupkg --api-key $gh_pat --source "github"
```

## Build the docker image
```powershell
$env:GH_OWNER="Dot-Net-Micro-Services"
$env:GH_PAT="[PAT HERE]"
docker build --secret id=GH_OWNER --secret id=GH_PAT -t play.inventory:$version .
```

## Run the docker image
```powershell
$cosmosDbConnectionString="[CONNECTION STRING HERE]"
$serviceBusConnectionString="[CONNECTION STRING HERE]"
docker run -it -rm -p 5004:5004 --name inventory 
-e MongoDbSettings__ConnectionString=$cosmosDbConnectionString
-e ServiceBusSettings__ConnectionString=$serviceBusConnectionString
-e ServiceSettings__MessageBroker="SERVICEBUS"
play.inventory:$version
```