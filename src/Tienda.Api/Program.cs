using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Tienda.Api;

var builder = WebApplication.CreateBuilder(args);

var opciones = builder.Configuration.GetSection("Tienda").Get<OpcionesTienda>() ?? new OpcionesTienda();
var cadenaPedidos = builder.Configuration.GetConnectionString("Pedidos");
var cadenaFotos = builder.Configuration.GetConnectionString("Fotos");

// Modulo 3: con su cadena de conexion, el catalogo va a Azure SQL Database.
// Sin ella (en local), sigue en memoria, como en el modulo 2.
if (!string.IsNullOrWhiteSpace(cadenaPedidos))
{
    builder.Services.AddDbContext<TiendaDb>(o => o.UseSqlServer(cadenaPedidos));
    builder.Services.AddSingleton<PreparadorBase>();
    builder.Services.AddScoped<ICatalogo, CatalogoSql>();
}
else
{
    builder.Services.AddSingleton<ICatalogo, CatalogoEnMemoria>();
}

// Modulo 3: las fotos, en Blob Storage. Sin cadena (en local), salen de la propia web.
if (!string.IsNullOrWhiteSpace(cadenaFotos))
    builder.Services.AddSingleton<IAlmacenFotos>(new AlmacenFotosBlob(new BlobContainerClient(cadenaFotos, opciones.Contenedor)));
else
    builder.Services.AddSingleton<IAlmacenFotos, AlmacenFotosLocal>();

builder.Services.AddSingleton<IComprobacion>(sp => new ComprobacionBaseDatos(cadenaPedidos, sp.GetRequiredService<IServiceScopeFactory>()));
builder.Services.AddSingleton<IComprobacion>(new ComprobacionAlmacenamiento(cadenaFotos, opciones.Contenedor));

var app = builder.Build();

async Task<List<ResultadoComprobacion>> ComprobarTodo(IEnumerable<IComprobacion> comprobaciones, CancellationToken ct)
{
    var resultados = new List<ResultadoComprobacion>();
    foreach (var c in comprobaciones)
        resultados.Add(await EjecutorComprobaciones.EjecutarSeguroAsync(c, ct));
    return resultados;
}

// El front React, compilado en wwwroot, lo sirve la misma web.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/productos", async (ICatalogo catalogo, CancellationToken ct) =>
    Results.Ok(await catalogo.ListarProductosAsync(ct)));

// La foto de un producto: la web la lee del almacenamiento y la sirve.
app.MapGet("/api/productos/{id:int}/foto", async (int id, ICatalogo catalogo, IAlmacenFotos fotos, CancellationToken ct) =>
{
    var producto = (await catalogo.ListarProductosAsync(ct)).FirstOrDefault(p => p.Id == id);
    if (producto?.Foto is null)
        return Results.NotFound();
    var foto = await fotos.AbrirAsync(producto.Foto, ct);
    return foto is null ? Results.NotFound() : Results.Stream(foto.Contenido, foto.Tipo);
});

app.MapGet("/api/pedidos", async (ICatalogo catalogo, CancellationToken ct) =>
    Results.Ok(await catalogo.UltimosPedidosAsync(10, ct)));

app.MapPost("/api/pedidos", async (PeticionPedido peticion, ICatalogo catalogo, CancellationToken ct) =>
{
    if (peticion.Cantidad < 1)
        return Results.BadRequest(new { error = "La cantidad tiene que ser 1 o más." });

    var resultado = await catalogo.PedirAsync(peticion.ProductoId, peticion.Cantidad, ct);
    return resultado.Estado switch
    {
        EstadoPedido.Hecho => Results.Created("/api/pedidos", resultado.Pedido),
        EstadoPedido.ProductoNoExiste => Results.NotFound(new { error = "Ese producto no existe." }),
        _ => Results.Conflict(new { error = "No quedan existencias suficientes." }),
    };
});

// La que mira desplegar.ps1 al terminar: 200 si todo va bien, 503 si algo falla.
// El front la ensena en su panel de estado.
app.MapGet("/salud", async (IEnumerable<IComprobacion> comprobaciones, CancellationToken ct) =>
{
    var resultados = await ComprobarTodo(comprobaciones, ct);
    return PaginaEstado.TodoCorrecto(resultados)
        ? Results.Ok(resultados)
        : Results.Json(resultados, statusCode: StatusCodes.Status503ServiceUnavailable);
});

// Cualquier otra ruta es del front.
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
