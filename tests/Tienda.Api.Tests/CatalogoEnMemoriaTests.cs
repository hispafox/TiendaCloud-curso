namespace Tienda.Api.Tests;

public class CatalogoEnMemoriaTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    [Fact]
    public async Task Listar_devuelve_los_seis_productos_de_ejemplo()
    {
        var productos = await new CatalogoEnMemoria().ListarProductosAsync(Ct);

        Assert.Equal(6, productos.Count);
        Assert.Contains(productos, p => p.Nombre == "Camiseta");
    }

    [Fact]
    public async Task Pedir_resta_las_existencias()
    {
        var catalogo = new CatalogoEnMemoria();
        var antes = (await catalogo.ListarProductosAsync(Ct)).Single(p => p.Id == 1).Existencias;

        var resultado = await catalogo.PedirAsync(1, 3, Ct);

        Assert.Equal(EstadoPedido.Hecho, resultado.Estado);
        var despues = (await catalogo.ListarProductosAsync(Ct)).Single(p => p.Id == 1).Existencias;
        Assert.Equal(antes - 3, despues);
    }

    [Fact]
    public async Task Pedir_mas_de_lo_que_queda_no_resta_nada()
    {
        var catalogo = new CatalogoEnMemoria();
        var antes = (await catalogo.ListarProductosAsync(Ct)).Single(p => p.Id == 3).Existencias;

        var resultado = await catalogo.PedirAsync(3, antes + 1, Ct);

        Assert.Equal(EstadoPedido.SinExistencias, resultado.Estado);
        var despues = (await catalogo.ListarProductosAsync(Ct)).Single(p => p.Id == 3).Existencias;
        Assert.Equal(antes, despues);
    }

    [Fact]
    public async Task Pedir_un_producto_que_no_existe_lo_dice()
    {
        var resultado = await new CatalogoEnMemoria().PedirAsync(999, 1, Ct);

        Assert.Equal(EstadoPedido.ProductoNoExiste, resultado.Estado);
    }

    [Fact]
    public async Task UltimosPedidos_empieza_por_el_mas_reciente_y_lleva_el_nombre_del_producto()
    {
        var catalogo = new CatalogoEnMemoria();
        await catalogo.PedirAsync(1, 1, Ct);
        await catalogo.PedirAsync(2, 1, Ct);

        var pedidos = await catalogo.UltimosPedidosAsync(10, Ct);

        Assert.Equal(2, pedidos.Count);
        Assert.Equal("Taza", pedidos[0].Producto);
        Assert.Equal("Camiseta", pedidos[1].Producto);
    }

    [Fact]
    public async Task Listar_devuelve_copias_que_no_cambian_el_catalogo()
    {
        var catalogo = new CatalogoEnMemoria();
        var primera = await catalogo.ListarProductosAsync(Ct);
        primera[0].Existencias = 0;

        var segunda = await catalogo.ListarProductosAsync(Ct);

        Assert.NotEqual(0, segunda[0].Existencias);
    }
}
