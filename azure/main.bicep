// =============================================================================
// main.bicep - La tienda del curso, en un grupo de recursos.
//
// Cada rama del curso le anade los bloques de su modulo. En esta:
//   modulo 1  etiquetas en todo, para saber de quien es cada recurso
//   modulo 2  plan de App Service y la web .NET, que sirve la API y el front
//   modulo 3  base de datos SQL para el catalogo y los pedidos, y almacenamiento
//             para las fotos, con su ciclo de vida
//
// Lo despliega azure/desplegar.ps1. Lo borra azure/borrar.ps1.
// =============================================================================

@description('Region. La pone desplegar.ps1 desde deploy.secrets.ps1.')
param location string = resourceGroup().location

@description('Entorno: va en los nombres y en las etiquetas.')
param entorno string = 'pruebas'

@description('Quien responde de estos recursos (etiqueta responsable).')
param responsable string

@description('Usuario administrador de SQL.')
param sqlAdminLogin string = 'tiendaadmin'

@secure()
@description('Contrasena del administrador de SQL. Sale de deploy.secrets.ps1.')
param sqlAdminPassword string

// ----- Nombres -----
var sufijo = uniqueString(resourceGroup().id)
var planName = 'plan-tienda-${entorno}'
var appName = 'app-tienda-${sufijo}'
var storageName = 'sttienda${take(sufijo, 13)}'
var contenedorFotos = 'fotos'
var sqlServerName = 'sql-tienda-${sufijo}'
var sqlDbName = 'pedidos'

// ----- Modulo 1: las etiquetas, en todos los recursos -----
var etiquetas = {
  aplicacion: 'tienda'
  entorno: entorno
  responsable: responsable
  'centro-de-coste': 'curso-arquitectura-cloud'
}

// ----- Modulo 3: la base de datos del catalogo y los pedidos -----
resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: etiquetas
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
  }
}

// Deja entrar a los servicios de Azure (la web). Nada de Internet.
resource sqlFirewallAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDbName
  location: location
  tags: etiquetas
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 2
  }
  properties: {
    // Oferta gratuita de Azure SQL Database: cada mes trae su cupo de computo y
    // 32 GB. Si se acaba, la base se pausa hasta el mes siguiente y no cobra.
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
  }
}

// ----- Modulo 3: el almacenamiento de las fotos -----
// No tiene plan gratuito: se paga por lo que se guarda y por las operaciones.
// Redundancia local, la mas barata, y seis fotos pequenas.
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  tags: etiquetas
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource fotos 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: contenedorFotos
  properties: {
    publicAccess: 'None'
  }
}

// Las fotos que nadie cambia se enfrian solas: a Cool a los 30 dias y a Cold a los 90.
resource cicloDeVida 'Microsoft.Storage/storageAccounts/managementPolicies@2023-05-01' = {
  parent: storageAccount
  name: 'default'
  properties: {
    policy: {
      rules: [
        {
          enabled: true
          name: 'enfriar-fotos'
          type: 'Lifecycle'
          definition: {
            filters: {
              blobTypes: [ 'blockBlob' ]
              prefixMatch: [ '${contenedorFotos}/' ]
            }
            actions: {
              baseBlob: {
                tierToCool: { daysAfterModificationGreaterThan: 30 }
                tierToCold: { daysAfterModificationGreaterThan: 90 }
              }
            }
          }
        }
      ]
    }
  }
}

// ----- Modulo 2: el plan y la web -----
// Plan gratuito (F1): no se paga. A cambio, 60 minutos de CPU al dia, sin
// Always On y sin deployment slots: basta para una clase.
resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: etiquetas
  kind: 'linux'
  sku: {
    name: 'F1'
  }
  properties: {
    reserved: true
  }
}

resource app 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  tags: etiquetas
  kind: 'app,linux'
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      minTlsVersion: '1.2'
      appSettings: [
        { name: 'WEBSITE_RUN_FROM_PACKAGE', value: '1' }
        { name: 'Tienda__Contenedor', value: contenedorFotos }
        // Modulo 3: la web entra a la base con usuario y contrasena, y al
        // almacenamiento con la clave de la cuenta. El modulo 4 quita la clave.
        { name: 'ConnectionStrings__Pedidos', value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDbName};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=true;TrustServerCertificate=false;' }
        { name: 'ConnectionStrings__Fotos', value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}' }
      ]
    }
  }
  dependsOn: [
    sqlDatabase
    fotos
  ]
}

output appName string = app.name
output appUrl string = 'https://${app.properties.defaultHostName}'
output storageAccountName string = storageAccount.name
