// =============================================================================
// main.bicep - La tienda del curso, en un grupo de recursos.
//
// Cada rama del curso le anade los bloques de su modulo. En esta:
//   modulo 1  etiquetas en todo, para saber de quien es cada recurso
//   modulo 2  plan de App Service y la web .NET, que sirve la API y el front
//
// Lo despliega azure/desplegar.ps1. Lo borra azure/borrar.ps1.
// =============================================================================

@description('Region. La pone desplegar.ps1 desde deploy.secrets.ps1.')
param location string = resourceGroup().location

@description('Entorno: va en los nombres y en las etiquetas.')
param entorno string = 'pruebas'

@description('Quien responde de estos recursos (etiqueta responsable).')
param responsable string

// ----- Nombres -----
var sufijo = uniqueString(resourceGroup().id)
var planName = 'plan-tienda-${entorno}'
var appName = 'app-tienda-${sufijo}'

// ----- Modulo 1: las etiquetas, en todos los recursos -----
var etiquetas = {
  aplicacion: 'tienda'
  entorno: entorno
  responsable: responsable
  'centro-de-coste': 'curso-arquitectura-cloud'
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
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      minTlsVersion: '1.2'
      appSettings: [
        { name: 'WEBSITE_RUN_FROM_PACKAGE', value: '1' }
      ]
    }
  }
}

output appName string = app.name
output appUrl string = 'https://${app.properties.defaultHostName}'
