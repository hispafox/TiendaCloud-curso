# =============================================================================
# ver-cambios.ps1 - Ensena que cambiaria desplegar.ps1 en Azure, sin desplegar.
#
# Ejecuta desde la RAIZ del repositorio:
#     pwsh -ExecutionPolicy Bypass -File .\azure\ver-cambios.ps1
#
# Es el plan antes de aplicar del modulo 5: lanza el what-if de Bicep con los
# mismos valores que desplegar.ps1, sacados de deploy.secrets.ps1, y ensena
# recurso a recurso que se crearia, que se modificaria y que quedaria igual.
#
# Lo unico que crea, si no existe, es el grupo de recursos vacio: el what-if
# necesita un grupo con el que comparar. Un grupo vacio no cuesta nada, y
# borrar.ps1 lo quita como siempre.
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
foreach ($v in "Suscripcion", "Ubicacion", "GrupoRecursos", "Responsable", "SqlPassword", "PresupuestoMensual") {
    if (-not (Get-Variable $v -ValueOnly -ErrorAction SilentlyContinue)) { throw "deploy.secrets.ps1 no define `$$v." }
}
# Opcional: la region de la base, si no es la del resto (modulo 3).
if (-not (Get-Variable UbicacionSql -ValueOnly -ErrorAction SilentlyContinue)) { $UbicacionSql = $Ubicacion }

$bicep = Join-Path $PSScriptRoot "main.bicep"

# ===== 1. Login y suscripcion =====
Write-Host "==> [1/3] Azure login" -ForegroundColor Cyan
az account show 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { az login | Out-Null; Assert-Ok "az login" }
az account set --subscription $Suscripcion
Assert-Ok "az account set"

# ===== 2. El grupo, vacio si no existe =====
Write-Host "==> [2/3] Grupo de recursos $GrupoRecursos" -ForegroundColor Cyan
$existe = az group exists -n $GrupoRecursos
Assert-Ok "az group exists"
if ($existe -ne "true") {
    Write-Host "    No existe: lo creo vacio, con sus etiquetas, para poder comparar." -ForegroundColor Yellow
    $etiquetas = @(
        "aplicacion=tienda"
        "entorno=pruebas"
        "responsable=$Responsable"
        "centro-de-coste=curso-arquitectura-cloud"
    )
    az group create -n $GrupoRecursos -l $Ubicacion --tags $etiquetas | Out-Null
    Assert-Ok "az group create"
}

# ===== 3. El what-if =====
Write-Host "==> [3/3] Lo que cambiaria el despliegue (what-if)" -ForegroundColor Cyan
az deployment group what-if -g $GrupoRecursos -f $bicep `
    -p "responsable=$Responsable" "sqlAdminPassword=$SqlPassword" "presupuestoMensual=$PresupuestoMensual" "ubicacionSql=$UbicacionSql"
Assert-Ok "az deployment group what-if"

Write-Host "`n=============================================" -ForegroundColor Green
Write-Host " Esto es lo que haria desplegar.ps1. No se ha desplegado nada." -ForegroundColor Green
Write-Host " Create = se crearia, Modify = se cambiaria, NoChange = igual." -ForegroundColor Green
Write-Host " Mira con cuidado lo que se borraria o se reemplazaria." -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
