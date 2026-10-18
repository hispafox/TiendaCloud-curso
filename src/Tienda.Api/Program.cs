using Tienda.Api;

var builder = WebApplication.CreateBuilder(args);

// Modulo 2: el catalogo, en la memoria de la propia web.
builder.Services.AddSingleton<ICatalogo, CatalogoEnMemoria>();

var app = builder.Build();

// El front React, compilado en wwwroot, lo sirve la misma web.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/productos", async (ICatalogo catalogo, CancellationToken ct) =>
    Results.Ok(await catalogo.ListarProductosAsync(ct)));

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

// Cualquier otra ruta es del front.
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
