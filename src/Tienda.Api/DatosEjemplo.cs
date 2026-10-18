namespace Tienda.Api;

// Los productos con los que arranca la tienda, cada uno con su foto.
public static class DatosEjemplo
{
    public static List<Producto> Productos() =>
    [
        new() { Id = 1, Nombre = "Camiseta", Descripcion = "Camiseta de algodón con el logo de la tienda.", Existencias = 40, Foto = "camiseta.svg" },
        new() { Id = 2, Nombre = "Taza", Descripcion = "Taza de cerámica para el café de la mañana.", Existencias = 30, Foto = "taza.svg" },
        new() { Id = 3, Nombre = "Mochila", Descripcion = "Mochila resistente para el día a día.", Existencias = 15, Foto = "mochila.svg" },
        new() { Id = 4, Nombre = "Gorra", Descripcion = "Gorra ajustable, de las que se llevan en campaña.", Existencias = 25, Foto = "gorra.svg" },
        new() { Id = 5, Nombre = "Cuaderno", Descripcion = "Cuaderno de tapa dura para apuntar las decisiones.", Existencias = 50, Foto = "cuaderno.svg" },
        new() { Id = 6, Nombre = "Botella", Descripcion = "Botella de acero que mantiene el agua fría.", Existencias = 20, Foto = "botella.svg" },
    ];
}
