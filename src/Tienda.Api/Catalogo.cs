namespace Tienda.Api;

// Un producto del catalogo. Lleva existencias y no precio: la tienda del curso no cobra.
public sealed class Producto
{
    public int Id { get; set; }
    public required string Nombre { get; set; }
    public required string Descripcion { get; set; }
    public int Existencias { get; set; }
}

// Un pedido guardado.
public sealed class Pedido
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; }
}

// Lo que manda el front para pedir.
public sealed record PeticionPedido(int ProductoId, int Cantidad);

// Lo que devuelve la API al listar pedidos: con el nombre del producto.
public sealed record PedidoVista(int Id, string Producto, int Cantidad, DateTime Fecha);

public enum EstadoPedido
{
    Hecho,
    ProductoNoExiste,
    SinExistencias
}

public sealed record ResultadoPedido(EstadoPedido Estado, PedidoVista? Pedido = null);

// Lo que la API sabe hacer con productos y pedidos, este donde este guardado.
public interface ICatalogo
{
    Task<IReadOnlyList<Producto>> ListarProductosAsync(CancellationToken ct);
    Task<IReadOnlyList<PedidoVista>> UltimosPedidosAsync(int cuantos, CancellationToken ct);
    Task<ResultadoPedido> PedirAsync(int productoId, int cantidad, CancellationToken ct);
}

// Modulo 2: el catalogo vive en la memoria de la web. Se pierde al reiniciar,
// y cada instancia tendria el suyo: por eso el modulo 3 lo lleva a una base de datos.
public sealed class CatalogoEnMemoria : ICatalogo
{
    private readonly Lock cerrojo = new();
    private readonly List<Producto> productos = DatosEjemplo.Productos();
    private readonly List<Pedido> pedidos = [];

    public Task<IReadOnlyList<Producto>> ListarProductosAsync(CancellationToken ct)
    {
        lock (cerrojo)
            return Task.FromResult<IReadOnlyList<Producto>>(productos.Select(Copiar).ToList());
    }

    public Task<IReadOnlyList<PedidoVista>> UltimosPedidosAsync(int cuantos, CancellationToken ct)
    {
        lock (cerrojo)
        {
            var vistas = pedidos
                .OrderByDescending(p => p.Id)
                .Take(cuantos)
                .Select(p => new PedidoVista(p.Id, productos.First(x => x.Id == p.ProductoId).Nombre, p.Cantidad, p.Fecha))
                .ToList();
            return Task.FromResult<IReadOnlyList<PedidoVista>>(vistas);
        }
    }

    public Task<ResultadoPedido> PedirAsync(int productoId, int cantidad, CancellationToken ct)
    {
        lock (cerrojo)
        {
            var producto = productos.FirstOrDefault(p => p.Id == productoId);
            if (producto is null)
                return Task.FromResult(new ResultadoPedido(EstadoPedido.ProductoNoExiste));
            if (producto.Existencias < cantidad)
                return Task.FromResult(new ResultadoPedido(EstadoPedido.SinExistencias));

            producto.Existencias -= cantidad;
            var pedido = new Pedido { Id = pedidos.Count + 1, ProductoId = productoId, Cantidad = cantidad, Fecha = DateTime.UtcNow };
            pedidos.Add(pedido);
            var vista = new PedidoVista(pedido.Id, producto.Nombre, pedido.Cantidad, pedido.Fecha);
            return Task.FromResult(new ResultadoPedido(EstadoPedido.Hecho, vista));
        }
    }

    private static Producto Copiar(Producto p) =>
        new() { Id = p.Id, Nombre = p.Nombre, Descripcion = p.Descripcion, Existencias = p.Existencias };
}
