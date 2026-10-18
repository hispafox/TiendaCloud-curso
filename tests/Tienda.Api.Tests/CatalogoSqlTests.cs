using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tienda.Api.Tests;

// El catalogo de la base, contra un SQLite en memoria: el mismo EF Core que en Azure SQL.
public sealed class CatalogoSqlTests : IDisposable
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private readonly SqliteConnection conexion = new("DataSource=:memory:");
    private readonly PreparadorBase preparador = new();

    public CatalogoSqlTests() => conexion.Open();

    public void Dispose() => conexion.Dispose();

    private CatalogoSql NuevoCatalogo(out TiendaDb db)
    {
        db = new TiendaDb(new DbContextOptionsBuilder<TiendaDb>().UseSqlite(conexion).Options);
        return new CatalogoSql(db, preparador);
    }

    [Fact]
    public async Task La_primera_vez_crea_las_tablas_y_carga_los_seis_productos()
    {
        var catalogo = NuevoCatalogo(out _);

        var productos = await catalogo.ListarProductosAsync(Ct);

        Assert.Equal(6, productos.Count);
        Assert.All(productos, p => Assert.NotNull(p.Foto));
    }

    [Fact]
    public async Task Preparar_dos_veces_no_duplica_los_productos()
    {
        await NuevoCatalogo(out _).ListarProductosAsync(Ct);

        var otraWeb = new CatalogoSql(new TiendaDb(new DbContextOptionsBuilder<TiendaDb>().UseSqlite(conexion).Options), new PreparadorBase());
        var productos = await otraWeb.ListarProductosAsync(Ct);

        Assert.Equal(6, productos.Count);
    }

    [Fact]
    public async Task Pedir_resta_las_existencias_y_guarda_el_pedido()
    {
        var catalogo = NuevoCatalogo(out _);
        var antes = (await catalogo.ListarProductosAsync(Ct)).Single(p => p.Id == 2).Existencias;

        var resultado = await catalogo.PedirAsync(2, 3, Ct);

        Assert.Equal(EstadoPedido.Hecho, resultado.Estado);
        Assert.Equal("Taza", resultado.Pedido!.Producto);
        var despues = (await NuevoCatalogo(out _).ListarProductosAsync(Ct)).Single(p => p.Id == 2).Existencias;
        Assert.Equal(antes - 3, despues);
        Assert.Single(await catalogo.UltimosPedidosAsync(10, Ct));
    }

    [Fact]
    public async Task Pedir_mas_de_lo_que_queda_no_resta_ni_guarda_nada()
    {
        var catalogo = NuevoCatalogo(out _);
        var antes = (await catalogo.ListarProductosAsync(Ct)).Single(p => p.Id == 3).Existencias;

        var resultado = await catalogo.PedirAsync(3, antes + 1, Ct);

        Assert.Equal(EstadoPedido.SinExistencias, resultado.Estado);
        var despues = (await NuevoCatalogo(out _).ListarProductosAsync(Ct)).Single(p => p.Id == 3).Existencias;
        Assert.Equal(antes, despues);
        Assert.Empty(await catalogo.UltimosPedidosAsync(10, Ct));
    }

    [Fact]
    public async Task Pedir_un_producto_que_no_existe_lo_dice()
    {
        var resultado = await NuevoCatalogo(out _).PedirAsync(999, 1, Ct);

        Assert.Equal(EstadoPedido.ProductoNoExiste, resultado.Estado);
    }

    [Fact]
    public async Task UltimosPedidos_empieza_por_el_mas_reciente()
    {
        var catalogo = NuevoCatalogo(out _);
        await catalogo.PedirAsync(1, 1, Ct);
        await catalogo.PedirAsync(5, 2, Ct);

        var pedidos = await catalogo.UltimosPedidosAsync(10, Ct);

        Assert.Equal(["Cuaderno", "Camiseta"], pedidos.Select(p => p.Producto));
    }
}
