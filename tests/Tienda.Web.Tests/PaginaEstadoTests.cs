using Tienda.Web;

namespace Tienda.Web.Tests;

public class PaginaEstadoTests
{
    [Fact]
    public void TodoCorrecto_con_todas_las_comprobaciones_bien_devuelve_true()
    {
        var resultados = new[]
        {
            new ResultadoComprobacion("Key Vault", true, "Hola"),
            new ResultadoComprobacion("Base de datos", true, "Pedido guardado"),
        };

        Assert.True(PaginaEstado.TodoCorrecto(resultados));
    }

    [Fact]
    public void TodoCorrecto_con_una_comprobacion_mal_devuelve_false()
    {
        var resultados = new[]
        {
            new ResultadoComprobacion("Key Vault", false, "Sin permiso"),
            new ResultadoComprobacion("Base de datos", true, "Pedido guardado"),
        };

        Assert.False(PaginaEstado.TodoCorrecto(resultados));
    }

    [Fact]
    public void TodoCorrecto_sin_comprobaciones_devuelve_false()
    {
        Assert.False(PaginaEstado.TodoCorrecto(Array.Empty<ResultadoComprobacion>()));
    }

    [Fact]
    public void Generar_escapa_el_html_del_detalle()
    {
        var html = PaginaEstado.Generar(new[] { new ResultadoComprobacion("Key Vault", true, "<script>x</script>") });

        Assert.DoesNotContain("<script>x</script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Generar_marca_cada_fila_como_correcta_o_como_fallo()
    {
        var html = PaginaEstado.Generar(new[]
        {
            new ResultadoComprobacion("Key Vault", true, "Hola"),
            new ResultadoComprobacion("Almacenamiento", false, "Sin permiso"),
        });

        Assert.Contains("Correcto", html);
        Assert.Contains("Falla", html);
        Assert.Contains("Hay piezas que fallan.", html);
    }
}
