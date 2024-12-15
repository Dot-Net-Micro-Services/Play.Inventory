# Play.Inventory
Inventory Microservice

## Create and publish package
```powershell
$version="1.0.4"
$owner="Dot-Net-Micro-Services"
$gh_pat="[PAT HERE]"

dotnet pack src\Play.Inventory.Contracts\ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/Play.Inventory -o ..\packages

dotnet nuget push ..\packages\Play.Inventory.Contracts.$version.nupkg --api-key $gh_pat --source "github"
```

## Build the docker image
```powershell
$env:GH_OWNER="Dot-Net-Micro-Services"
$env:GH_PAT="[PAT HERE]"
$acrname="playeconomyacrdev"
docker build --secret id=GH_OWNER --secret id=GH_PAT -t "$acrname.azurecr.io/play.inventory:$version" .
```

## Run the docker image
```powershell
$cosmosDbConnectionString="[CONNECTION STRING HERE]"
$serviceBusConnectionString="[CONNECTION STRING HERE]"
docker run -it --rm -p 5004:5004 --name inventory 
-e MongoDbSettings__ConnectionString=$cosmosDbConnectionString
-e ServiceBusSettings__ConnectionString=$serviceBusConnectionString
-e ServiceSettings__MessageBroker="SERVICEBUS"
play.inventory:$version
```
## Publish the docker image
```powershell
az acr login --name $acrname
docker push "$acrname.azurecr.io/play.inventory:$version"
```

## Create the Kubernets namespace
```powershell
$namespace="inventory"
kubectl create namespace $namespace
```

## Create the Kubernets pod
```powershell
kubectl apply -f .\kubernetes\inventory.yaml -n $namespace
```

## Creating the Workload Identity and grant key vault access
```powershell
$appname="playeconomy"
$keyvaultname="playeconomy-vault-dev"
az identity create --resource-group $appname --name $namespace
$IDENTITY_CLIENT_ID=az identity show --resource-group $appname --name $namespace --query clientId -otsv
$SUBSCRIPTION_ID=az account show --query id -otsv
az role assignment create --assignee $IDENTITY_CLIENT_ID --role "Key Vault Secrets User" --scope "/subscriptions/$SUBSCRIPTION_ID/resourcegroups/$appname/providers/Microsoft.KeyVault/vaults/$keyvaultname"
```

## Establish the Federated Identity Credential
```powershell
$aksname="playeconomy-aks-dev"
$AKS_OIDC_ISSUER=az aks show --name $aksname --resource-group $appname --query "oidcIssuerProfile.issuerUrl" -otsv
az identity federated-credential create --name $namespace --identity-name $namespace --resource-group $appname --issuer $AKS_OIDC_ISSUER --subject "system:serviceaccount:${namespace}:${namespace}-serviceaccount"
```

## Install the helm chart
```powershell
$helmUser=[guid]::Empty.Guid
$helmPassword=az acr login --name $acrname --expose-token --output tsv --query accessToken

helm registry login "$acrname.azurecr.io" --username $helmUser --password $helmPassword

$chartVersion="0.1.0"
helm upgrade inventory-service oci://$acrname.azurecr.io/helm/microservice --version $chartVersion -f .\helm\values.yaml -n $namespace --install
```