using Tienda.Web;

var builder = WebApplication.CreateBuilder(args);

var opciones = builder.Configuration.GetSection("Tienda").Get<OpcionesTienda>() ?? new OpcionesTienda();
var cadenaPedidos = builder.Configuration.GetConnectionString("Pedidos");

builder.Services.AddSingleton<IComprobacion>(new ComprobacionKeyVault(opciones));
builder.Services.AddSingleton<IComprobacion>(new ComprobacionAlmacenamiento(opciones));
builder.Services.AddSingleton<IComprobacion>(new ComprobacionBaseDatos(cadenaPedidos));

var app = builder.Build();

async Task<List<ResultadoComprobacion>> ComprobarTodo(IEnumerable<IComprobacion> comprobaciones, CancellationToken ct)
{
    var resultados = new List<ResultadoComprobacion>();
    foreach (var c in comprobaciones)
        resultados.Add(await EjecutorComprobaciones.EjecutarSeguroAsync(c, ct));
    return resultados;
}

// La pagina que se ensena en clase.
app.MapGet("/", async (IEnumerable<IComprobacion> comprobaciones, CancellationToken ct) =>
    Results.Content(PaginaEstado.Generar(await ComprobarTodo(comprobaciones, ct)), "text/html; charset=utf-8"));

// La que mira desplegar.ps1 al terminar: 200 si todo va bien, 503 si algo falla.
app.MapGet("/salud", async (IEnumerable<IComprobacion> comprobaciones, CancellationToken ct) =>
{
    var resultados = await ComprobarTodo(comprobaciones, ct);
    return PaginaEstado.TodoCorrecto(resultados)
        ? Results.Ok(resultados)
        : Results.Json(resultados, statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.Run();

public partial class Program;
