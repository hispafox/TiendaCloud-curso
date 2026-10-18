namespace Tienda.Api.Tests;

public class FotosTests
{
    [Fact]
    public void Cada_producto_de_ejemplo_tiene_su_foto_dentro_de_la_web()
    {
        foreach (var nombre in FotosEjemplo.Nombres)
        {
            using var foto = FotosEjemplo.Abrir(nombre);
            Assert.NotNull(foto);
        }
    }

    [Fact]
    public async Task El_almacen_local_devuelve_la_foto_como_svg()
    {
        var foto = await new AlmacenFotosLocal().AbrirAsync("taza.svg", CancellationToken.None);

        Assert.NotNull(foto);
        Assert.Equal("image/svg+xml", foto.Tipo);
    }

    [Fact]
    public async Task El_almacen_local_no_inventa_fotos_que_no_tiene()
    {
        Assert.Null(await new AlmacenFotosLocal().AbrirAsync("no-existe.svg", CancellationToken.None));
    }
}
