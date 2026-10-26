# =============================================================================
# provocar-fallo.ps1 - Quita a la web su permiso sobre el Key Vault, para ver
# el fallo en la tienda (paso 6 de la demo del modulo 6).
#
# Ejecuta desde la RAIZ del repositorio, con la tienda ya montada:
#     pwsh -ExecutionPolicy Bypass -File .\azure\provocar-fallo.ps1
#
# Borra la asignacion del rol Key Vault Secrets User de la identidad de la web
# sobre el Key Vault. Al cabo de unos minutos, la fila del Key Vault del panel
# de estado sale en rojo y el mensaje desaparece; la base y el almacenamiento
# siguen en verde.
#
# Para devolver el permiso: .\azure\desplegar.ps1. El Bicep repone la
# asignacion que falta.
# =============================================================================

$ErrorActionPreference = "Stop"

function Assert-Ok([string] $que) {
    if ($LASTEXITCODE -ne 0) {
        throw "Fallo en: $que (codigo $LASTEXITCODE). Se aborta."
    }
}

$rol = "Key Vault Secrets User"

if (-not (Get-Command az -ErrorAction SilentlyContinue)) { throw "Falta Azure CLI (az). Instalalo: https://aka.ms/installazurecli" }

$secretsFile = Join-Path $PSScriptRoot "deploy.secrets.ps1"
if (-not (Test-Path $secretsFile)) {
    throw "Falta azure\deploy.secrets.ps1: de ahi salen la suscripcion y el grupo de recursos."
}
. $secretsFile

az account show 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { az login | Out-Null; Assert-Ok "az login" }
az account set --subscription $Suscripcion
Assert-Ok "az account set"

# ===== 1. La web, su identidad y el Key Vault =====
Write-Host "==> [1/2] Buscando la web y el Key Vault en $GrupoRecursos" -ForegroundColor Cyan
$app = az webapp list -g $GrupoRecursos --query "[0].name" -o tsv
Assert-Ok "az webapp list"
$keyVault = az keyvault list -g $GrupoRecursos --query "[0].id" -o tsv
Assert-Ok "az keyvault list"
if (-not $app -or -not $keyVault) {
    throw "No encuentro la web o el Key Vault en $GrupoRecursos. Monta antes la tienda con .\azure\desplegar.ps1."
}
$identidad = az webapp identity show -g $GrupoRecursos -n $app --query principalId -o tsv
Assert-Ok "az webapp identity show"
if (-not $identidad) { throw "La web $app no tiene identidad. Monta antes la tienda con .\azure\desplegar.ps1." }

# ===== 2. Quitar el permiso =====
# La consulta va sin parentesis: az es un .cmd, y cmd.exe los toma como un
# bloque y aborta con "No se esperaba -o en este momento".
$asignaciones = az role assignment list --assignee $identidad --role $rol --scope $keyVault --query "[].id" -o tsv
Assert-Ok "az role assignment list"
if (-not $asignaciones) {
    Write-Host "`nLa web ya no tiene el rol $rol sobre el Key Vault. Para devolverlo: .\azure\desplegar.ps1" -ForegroundColor Yellow
    exit 0
}

Write-Host "==> [2/2] Quitando a la web el rol $rol sobre el Key Vault" -ForegroundColor Cyan
az role assignment delete --assignee $identidad --role $rol --scope $keyVault
Assert-Ok "az role assignment delete"

Write-Host "`n=============================================" -ForegroundColor Green
Write-Host " LISTO. La web ya no puede leer el secreto del Key Vault." -ForegroundColor Green
Write-Host " Recarga la tienda en unos minutos: la fila del Key Vault saldra en rojo" -ForegroundColor Green
Write-Host " y el mensaje desaparecera. La base y el almacenamiento siguen en verde." -ForegroundColor Green
Write-Host " Para devolver el permiso: .\azure\desplegar.ps1" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
