# =============================================================================
# borrar.ps1 - Borra la tienda entera para que no siga costando dinero.
#
# Ejecuta desde la RAIZ del repositorio:
#     pwsh -ExecutionPolicy Bypass -File .\azure\borrar.ps1
#
# Borra el grupo de recursos completo, con todo lo que tenga dentro. Si el
# grupo no existe, lo dice y termina sin error.
# =============================================================================

$ErrorActionPreference = "Stop"

function Assert-Ok([string] $que) {
    if ($LASTEXITCODE -ne 0) {
        throw "Fallo en: $que (codigo $LASTEXITCODE). Se aborta."
    }
}

$secretsFile = Join-Path $PSScriptRoot "deploy.secrets.ps1"
if (-not (Test-Path $secretsFile)) {
    throw "Falta azure\deploy.secrets.ps1: de ahi salen la suscripcion y el grupo de recursos."
}
. $secretsFile

az account show 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { az login | Out-Null; Assert-Ok "az login" }
az account set --subscription $Suscripcion
Assert-Ok "az account set"

$existe = az group exists -n $GrupoRecursos
Assert-Ok "az group exists"
if ($existe -ne "true") {
    Write-Host "El grupo $GrupoRecursos no existe. No hay nada que borrar." -ForegroundColor Green
    exit 0
}

Write-Host "==> [1/1] Borrando el grupo $GrupoRecursos (unos minutos)..." -ForegroundColor Cyan
az group delete -n $GrupoRecursos --yes
Assert-Ok "az group delete"

Write-Host "`nLISTO. La tienda esta borrada y ya no cuesta nada." -ForegroundColor Green
