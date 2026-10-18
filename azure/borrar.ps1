# =============================================================================
# borrar.ps1 - Borra la tienda entera para que no siga costando dinero.
#
# Ejecuta desde la RAIZ del repositorio:
#     pwsh -ExecutionPolicy Bypass -File .\azure\borrar.ps1
#
# Borra el grupo de recursos completo y despues purga el Key Vault: si no, se
# queda unos dias en la papelera con su nombre reservado y el siguiente
# desplegar.ps1 choca con el. Si el grupo no existe, lo dice y termina sin error.
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

# Antes de borrar, se apuntan los Key Vault del grupo para purgarlos despues:
# borrado el grupo, ya no aparecen en el.
$vaults = az keyvault list -g $GrupoRecursos --query "[].name" -o tsv
Assert-Ok "az keyvault list"

Write-Host "==> [1/2] Borrando el grupo $GrupoRecursos (unos minutos)..." -ForegroundColor Cyan
az group delete -n $GrupoRecursos --yes
Assert-Ok "az group delete"

Write-Host "==> [2/2] Purgando el Key Vault" -ForegroundColor Cyan
foreach ($kv in ($vaults -split "`n" | Where-Object { $_.Trim() })) {
    az keyvault purge --name $kv.Trim() | Out-Null
    Assert-Ok "az keyvault purge $kv"
    Write-Host "    Purgado: $kv" -ForegroundColor Green
}

Write-Host "`nLISTO. La tienda esta borrada y ya no cuesta nada." -ForegroundColor Green
