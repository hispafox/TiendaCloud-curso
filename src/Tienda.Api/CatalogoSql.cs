using Microsoft.EntityFrameworkCore;

namespace Tienda.Api;

// La base de la tienda: productos y pedidos.
public sealed class TiendaDb(DbContextOptions<TiendaDb> opciones) : DbContext(opciones)
{
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Producto>(p =>
        {
            // Los productos llegan con su Id de DatosEjemplo: la base no lo inventa.
            p.Property(x => x.Id).ValueGeneratedNever();
            p.Property(x => x.Nombre).HasMaxLength(100);
            p.Property(x => x.Descripcion).HasMaxLength(300);
            p.Property(x => x.Foto).HasMaxLength(100);
        });
        modelo.Entity<Pedido>(p => p.HasOne<Producto>().WithMany().HasForeignKey(x => x.ProductoId));
    }
}

// La primera vez que la web usa la base, crea las tablas y carga los productos de ejemplo.
public sealed class PreparadorBase
{
    private readonly SemaphoreSlim cerrojo = new(1, 1);
    private bool preparada;

    public async Task PrepararAsync(TiendaDb db, CancellationToken ct)
    {
        if (preparada)
            return;
        await cerrojo.WaitAsync(ct);
        try
        {
            if (preparada)
                return;
            await db.Database.EnsureCreatedAsync(ct);
            if (!await db.Productos.AnyAsync(ct))
            {
                db.Productos.AddRange(DatosEjemplo.Productos());
                await db.SaveChangesAsync(ct);
            }
            preparada = true;
        }
        finally
        {
            cerrojo.Release();
        }
    }
}

// Modulo 3: el catalogo y los pedidos en Azure SQL Database. Lo que hay en la
// base lo ven todas las instancias de la web, y sobrevive a los reinicios.
public sealed class CatalogoSql(TiendaDb db, PreparadorBase preparador) : ICatalogo
{
    public async Task<IReadOnlyList<Producto>> ListarProductosAsync(CancellationToken ct)
    {
        await preparador.PrepararAsync(db, ct);
        return await db.Productos.AsNoTracking().OrderBy(p => p.Id).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PedidoVista>> UltimosPedidosAsync(int cuantos, CancellationToken ct)
    {
        await preparador.PrepararAsync(db, ct);
        var consulta =
            from p in db.Pedidos
            join x in db.Productos on p.ProductoId equals x.Id
            orderby p.Id descending
            select new PedidoVista(p.Id, x.Nombre, p.Cantidad, p.Fecha);
        return await consulta.Take(cuantos).ToListAsync(ct);
    }

    // Restar las existencias y guardar el pedido van juntos, en una transaccion:
    // o pasan las dos cosas o ninguna. Es lo que obliga a llevar los pedidos a una relacional.
    public async Task<ResultadoPedido> PedirAsync(int productoId, int cantidad, CancellationToken ct)
    {
        await preparador.PrepararAsync(db, ct);
        await using var transaccion = await db.Database.BeginTransactionAsync(ct);

        var restadas = await db.Productos
            .Where(p => p.Id == productoId && p.Existencias >= cantidad)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Existencias, p => p.Existencias - cantidad), ct);
        if (restadas == 0)
        {
            var existe = await db.Productos.AnyAsync(p => p.Id == productoId, ct);
            return new ResultadoPedido(existe ? EstadoPedido.SinExistencias : EstadoPedido.ProductoNoExiste);
        }

        var pedido = new Pedido { ProductoId = productoId, Cantidad = cantidad, Fecha = DateTime.UtcNow };
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(ct);
        await transaccion.CommitAsync(ct);

        var nombre = await db.Productos.Where(p => p.Id == productoId).Select(p => p.Nombre).SingleAsync(ct);
        return new ResultadoPedido(EstadoPedido.Hecho, new PedidoVista(pedido.Id, nombre, pedido.Cantidad, pedido.Fecha));
    }
}
