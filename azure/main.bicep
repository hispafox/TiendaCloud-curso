// =============================================================================
// main.bicep - La tienda del curso, en un grupo de recursos.
//
// Cada rama del curso le anade los bloques de su modulo. En esta:
//   modulo 1  etiquetas en todo, para saber de quien es cada recurso
//   modulo 2  plan de App Service y la web .NET, que sirve la API y el front
//   modulo 3  base de datos SQL para el catalogo y los pedidos, y almacenamiento
//             para las fotos, con su ciclo de vida
//   modulo 4  identidad de la web y Key Vault con un unico secreto (un mensaje);
//             la web entra al almacenamiento y al Key Vault sin clave
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

@description('El mensaje que se guarda en el Key Vault y que la web ensena.')
param mensaje string = 'Hola desde el Key Vault de la tienda'

// ----- Nombres -----
var sufijo = uniqueString(resourceGroup().id)
var planName = 'plan-tienda-${entorno}'
var appName = 'app-tienda-${sufijo}'
var storageName = 'sttienda${take(sufijo, 13)}'
var contenedorFotos = 'fotos'
var sqlServerName = 'sql-tienda-${sufijo}'
var sqlDbName = 'pedidos'
var keyVaultName = 'kv-tienda-${take(sufijo, 13)}'

// ----- Roles (identificadores fijos de Azure) -----
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

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
    name: 'Basic'
    tier: 'Basic'
  }
}

// ----- Modulo 3: el almacenamiento de las fotos -----
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

// ----- Modulo 4: el Key Vault, con un unico secreto inofensivo -----
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: etiquetas
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
  }
}

resource secretoMensaje 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'mensaje'
  properties: {
    value: mensaje
  }
}

// ----- Modulo 2: el plan y la web -----
resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: etiquetas
  kind: 'linux'
  sku: {
    name: 'B1'
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
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      minTlsVersion: '1.2'
      appSettings: [
        { name: 'WEBSITE_RUN_FROM_PACKAGE', value: '1' }
        { name: 'Tienda__KeyVaultUri', value: keyVault.properties.vaultUri }
        { name: 'Tienda__BlobUri', value: storageAccount.properties.primaryEndpoints.blob }
        { name: 'Tienda__Contenedor', value: contenedorFotos }
        // La base sigue con usuario y contrasena: darle acceso a la identidad pide
        // una orden dentro de la base que el Bicep no puede declarar (modulo 6).
        { name: 'ConnectionStrings__Pedidos', value: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Database=${sqlDbName};User ID=${sqlAdminLogin};Password=${sqlAdminPassword};Encrypt=true;TrustServerCertificate=false;' }
      ]
    }
  }
  dependsOn: [
    sqlDatabase
    fotos
    secretoMensaje
  ]
}

// ----- Modulo 4: la web entra con su identidad, sin clave -----
resource storageRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, app.id, storageBlobDataContributorRoleId)
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalId: app.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource keyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, app.id, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalId: app.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output appName string = app.name
output appUrl string = 'https://${app.properties.defaultHostName}'
output keyVaultName string = keyVault.name
output storageAccountName string = storageAccount.name
