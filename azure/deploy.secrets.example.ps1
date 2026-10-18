# =============================================================================
# deploy.secrets.example.ps1 - Plantilla de los valores de desplegar.ps1.
#
# Copiala a azure\deploy.secrets.ps1 y rellenala. Ese fichero esta en .gitignore:
# no se sube nunca al repositorio.
# =============================================================================

# Suscripcion de Azure donde se monta la tienda (az account list -o table).
$Suscripcion = "00000000-0000-0000-0000-000000000000"

# Region europea con zonas de disponibilidad (modulo 1 del curso).
$Ubicacion = "westeurope"

# Grupo de recursos: dice que es y de que entorno (modulo 1).
$GrupoRecursos = "rg-tienda-pruebas"

# Quien responde de los recursos: va en la etiqueta «responsable» (modulo 5).
$Responsable = "tu.nombre@tuempresa.com"

# Contrasena del administrador de SQL (modulo 3): 8 caracteres o mas, con
# mayuscula, minuscula, numero y simbolo.
$SqlPassword = "CambiaEsto-2026!"

# Importe mensual del presupuesto del grupo (modulo 5), en la moneda de tu
# suscripcion: la cifra de tu estimacion del mes con la calculadora de Azure.
# Los avisos llegan a $Responsable al 50, 90 y 100 % del gasto previsto.
$PresupuestoMensual = 50
