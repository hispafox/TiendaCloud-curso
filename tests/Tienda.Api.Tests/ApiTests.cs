using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tienda.Api.Tests;

// La API entera en memoria, con WebApplicationFactory: cada test arranca su propia tienda.
public class ApiTests
{
    [Fact]
    public async Task Get_productos_devuelve_el_catalogo()
    {
        await using var fabrica = new WebApplicationFactory<Program>();
        var cliente = fabrica.CreateClient();

        var productos = await cliente.GetFromJsonAsync<List<Producto>>("/api/productos");

        Assert.NotNull(productos);
        Assert.Equal(6, productos.Count);
    }

    [Fact]
    public async Task Post_pedido_devuelve_201_y_aparece_en_los_ultimos_pedidos()
    {
        await using var fabrica = new WebApplicationFactory<Program>();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/pedidos", new PeticionPedido(2, 1));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var pedidos = await cliente.GetFromJsonAsync<List<PedidoVista>>("/api/pedidos");
        Assert.NotNull(pedidos);
        Assert.Equal("Taza", Assert.Single(pedidos).Producto);
    }

    [Fact]
    public async Task Post_pedido_sin_existencias_devuelve_409()
    {
        await using var fabrica = new WebApplicationFactory<Program>();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/pedidos", new PeticionPedido(3, 1000));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    [Fact]
    public async Task Post_pedido_con_cantidad_cero_devuelve_400()
    {
        await using var fabrica = new WebApplicationFactory<Program>();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/pedidos", new PeticionPedido(1, 0));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Post_pedido_de_un_producto_que_no_existe_devuelve_404()
    {
        await using var fabrica = new WebApplicationFactory<Program>();
        var cliente = fabrica.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/pedidos", new PeticionPedido(999, 1));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
