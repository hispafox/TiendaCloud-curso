# =============================================================================
# desplegar.ps1 - Monta la tienda en Azure con una sola orden.
#
# Ejecuta desde la RAIZ del repositorio:
#     pwsh -ExecutionPolicy Bypass -File .\azure\desplegar.ps1
#
# Cada rama del curso monta un paso mas. En esta (modulo 5), el grupo de
# recursos, la web en App Service con la API .NET y el front React, la base
# de datos SQL, el almacenamiento de las fotos, el Key Vault con la identidad
# de la web y sus dos permisos, y el presupuesto del grupo.
#
# REQUISITOS:
#   1. Azure CLI (az).        Comprobar: az version
#   2. SDK de .NET 10.        Comprobar: dotnet --version
#   3. Node.js con npm.       Comprobar: node --version
#   4. azure\deploy.secrets.ps1, copia de deploy.secrets.example.ps1 con tus valores.
#   5. Permisos para crear recursos y asignar roles en la suscripcion.
#
# No pide nada por pantalla: todo sale de deploy.secrets.ps1.
#
# IDEMPOTENTE: puedes lanzarlo las veces que quieras. El Bicep es incremental y
# solo cambia lo que haya cambiado en el fichero.
#
# Para no pagar de mas al acabar: .\azure\borrar.ps1
# =============================================================================

$ErrorActionPreference = "Stop"

function Assert-Ok([string] $que) {
    if ($LASTEXITCODE -ne 0) {
        throw "Fallo en: $que (codigo $LASTEXITCODE). Se aborta: no se ha ejecutado nada mas."
    }
}

# ===== Comprobacion de requisitos =====
if (-not (Get-Command az -ErrorAction SilentlyContinue)) { throw "Falta Azure CLI (az). Instalalo: https://aka.ms/installazurecli" }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw "Falta el SDK de .NET. Instalalo: https://dot.net" }
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) { throw "Falta Node.js (npm). Instalalo: https://nodejs.org" }

# ===== Valores: desde deploy.secrets.ps1 =====
$secretsFile = Join-Path $PSScriptRoot "deploy.secrets.ps1"
if (-not (Test-Path $secretsFile)) {
    throw "Falta azure\deploy.secrets.ps1. Copia deploy.secrets.example.ps1 con ese nombre y rellenalo."
}
. $secretsFile
foreach ($v in "Suscripcion", "Ubicacion", "GrupoRecursos", "Responsable", "SqlPassword", "PresupuestoMensual") {
    if (-not (Get-Variable $v -ValueOnly -ErrorAction SilentlyContinue)) { throw "deploy.secrets.ps1 no define `$$v." }
}
# Opcional: la region de la base, si no es la del resto (modulo 3).
if (-not (Get-Variable UbicacionSql -ValueOnly -ErrorAction SilentlyContinue)) { $UbicacionSql = $Ubicacion }
Write-Host "    Valores cargados de deploy.secrets.ps1" -ForegroundColor Green

# Las cuatro etiquetas de la tienda (modulo 5), tambien en el grupo.
$etiquetas = @(
    "aplicacion=tienda"
    "entorno=pruebas"
    "responsable=$Responsable"
    "centro-de-coste=curso-arquitectura-cloud"
)

$bicep    = Join-Path $PSScriptRoot "main.bicep"
$repoRoot = Split-Path $PSScriptRoot -Parent
$proyecto = Join-Path $repoRoot "src\Tienda.Api\Tienda.Api.csproj"
$frontend = Join-Path $repoRoot "frontend"
$salida   = Join-Path $repoRoot "publicar"
$zip      = Join-Path $repoRoot "publicar.zip"

# ===== 1. Login y suscripcion =====
Write-Host "==> [1/5] Azure login" -ForegroundColor Cyan
az account show 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { az login | Out-Null; Assert-Ok "az login" }
az account set --subscription $Suscripcion
Assert-Ok "az account set"

# ===== 2. Grupo de recursos, con sus etiquetas (modulo 1) =====
Write-Host "==> [2/5] Grupo de recursos $GrupoRecursos en $Ubicacion" -ForegroundColor Cyan
az group create -n $GrupoRecursos -l $Ubicacion --tags $etiquetas | Out-Null
Assert-Ok "az group create"

# ===== 3. Infraestructura (Bicep) =====
Write-Host "==> [3/5] Desplegando la infraestructura (unos minutos)..." -ForegroundColor Cyan
$json = az deployment group create -g $GrupoRecursos -f $bicep `
    -p "responsable=$Responsable" "sqlAdminPassword=$SqlPassword" "presupuestoMensual=$PresupuestoMensual" "ubicacionSql=$UbicacionSql" `
    --query "properties.outputs" -o json
Assert-Ok "az deployment group create"
$outputs = $json | ConvertFrom-Json
$appName = $outputs.appName.value
$appUrl  = $outputs.appUrl.value
if (-not $appName -or -not $appUrl) { throw "El despliegue no devolvio appName/appUrl. Se aborta." }
Write-Host "    Web: $appName  ($appUrl)" -ForegroundColor Green

# ===== 4. Publicar la aplicacion: la API .NET con el front en wwwroot =====
Write-Host "==> [4/5] Publicando la API .NET y el front React" -ForegroundColor Cyan
if (Test-Path $salida) { Remove-Item $salida -Recurse -Force }
if (Test-Path $zip) { Remove-Item $zip -Force }
npm ci --prefix $frontend --no-audit --no-fund
Assert-Ok "npm ci"
npm run build --prefix $frontend
Assert-Ok "npm run build"
dotnet publish $proyecto -c Release -o $salida
Assert-Ok "dotnet publish"
$wwwroot = Join-Path $salida "wwwroot"
New-Item -ItemType Directory -Force -Path $wwwroot | Out-Null
Copy-Item -Path (Join-Path $frontend "dist\*") -Destination $wwwroot -Recurse -Force
Compress-Archive -Path (Join-Path $salida "*") -DestinationPath $zip
az webapp deploy -g $GrupoRecursos -n $appName --src-path $zip --type zip | Out-Null
Assert-Ok "az webapp deploy"

# ===== 5. Comprobar que la tienda responde =====
# La web recien publicada tarda un poco en arrancar, la primera vez crea las
# tablas de la base, y los permisos de la identidad pueden tardar unos minutos
# en aplicarse: se reintenta.
Write-Host "==> [5/5] Comprobando $appUrl/salud" -ForegroundColor Cyan
$correcto = $false
for ($i = 1; $i -le 10 -and -not $correcto; $i++) {
    try {
        $r = Invoke-WebRequest -Uri "$appUrl/salud" -UseBasicParsing -TimeoutSec 60
        $correcto = $r.StatusCode -eq 200
    } catch {
        Write-Host "    Intento $i de 10: aun no. Espero 30 segundos..." -ForegroundColor Yellow
        Start-Sleep -Seconds 30
    }
}

Write-Host "`n=============================================" -ForegroundColor Green
if ($correcto) {
    Write-Host " LISTO. La tienda esta en marcha." -ForegroundColor Green
} else {
    Write-Host " Desplegado, pero /salud aun no responde bien. Abre la URL y mira que pieza falla." -ForegroundColor Yellow
}
Write-Host " URL:     $appUrl" -ForegroundColor Green
Write-Host " Borrar:  .\azure\borrar.ps1" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
