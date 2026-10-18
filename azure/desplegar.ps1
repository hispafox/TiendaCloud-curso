# =============================================================================
# desplegar.ps1 - Monta la tienda en Azure con una sola orden.
#
# Ejecuta desde la RAIZ del repositorio:
#     pwsh -ExecutionPolicy Bypass -File .\azure\desplegar.ps1
#
# Cada rama del curso monta un paso mas. En esta (modulo 1), el sitio de la
# tienda: el grupo de recursos con sus etiquetas.
#
# REQUISITOS:
#   1. Azure CLI (az).        Comprobar: az version
#   2. azure\deploy.secrets.ps1, copia de deploy.secrets.example.ps1 con tus valores.
#   3. Permisos para crear grupos de recursos en la suscripcion.
#
# No pide nada por pantalla: todo sale de deploy.secrets.ps1.
#
# IDEMPOTENTE: puedes lanzarlo las veces que quieras sin romper nada.
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

# ===== Valores: desde deploy.secrets.ps1 =====
$secretsFile = Join-Path $PSScriptRoot "deploy.secrets.ps1"
if (-not (Test-Path $secretsFile)) {
    throw "Falta azure\deploy.secrets.ps1. Copia deploy.secrets.example.ps1 con ese nombre y rellenalo."
}
. $secretsFile
foreach ($v in "Suscripcion", "Ubicacion", "GrupoRecursos", "Responsable") {
    if (-not (Get-Variable $v -ValueOnly -ErrorAction SilentlyContinue)) { throw "deploy.secrets.ps1 no define `$$v." }
}
Write-Host "    Valores cargados de deploy.secrets.ps1" -ForegroundColor Green

# Las cuatro etiquetas de la tienda (modulo 5), tambien en el grupo.
$etiquetas = @(
    "aplicacion=tienda"
    "entorno=pruebas"
    "responsable=$Responsable"
    "centro-de-coste=curso-arquitectura-cloud"
)

# ===== 1. Login y suscripcion =====
Write-Host "==> [1/2] Azure login" -ForegroundColor Cyan
az account show 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { az login | Out-Null; Assert-Ok "az login" }
az account set --subscription $Suscripcion
Assert-Ok "az account set"

# ===== 2. Grupo de recursos, con sus etiquetas (modulo 1) =====
Write-Host "==> [2/2] Grupo de recursos $GrupoRecursos en $Ubicacion" -ForegroundColor Cyan
az group create -n $GrupoRecursos -l $Ubicacion --tags $etiquetas | Out-Null
Assert-Ok "az group create"

Write-Host "`n=============================================" -ForegroundColor Green
Write-Host " LISTO. El grupo $GrupoRecursos esta creado en $Ubicacion." -ForegroundColor Green
Write-Host " Miralo en el Portal: Resource groups > $GrupoRecursos > Tags." -ForegroundColor Green
Write-Host " Borrar:  .\azure\borrar.ps1" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
