using System.Text.RegularExpressions;

namespace Tienda.Api.Tests;

// El Bicep y los scripts de azure/ no se prueban contra Azure: estos tests leen
// los ficheros y comprueban reglas que Azure solo hace cumplir al desplegar.
public class InfraestructuraTests
{
    private static readonly string Azure = Path.Combine(RaizDelRepositorio(), "azure");

    private static string Bicep => File.ReadAllText(Path.Combine(Azure, "main.bicep"));

    private static string RaizDelRepositorio()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Tienda.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("No encuentro Tienda.slnx");
    }

    // El bloque de un recurso del Bicep: de "resource <nombre> " a su llave de cierre.
    private static string Recurso(string nombre)
    {
        var bloque = Regex.Match(Bicep, $@"^resource {nombre} .*?^\}}", RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(bloque.Success, $"main.bicep no declara el recurso {nombre}");
        return bloque.Value;
    }

    // Los nombres que un script pasa al Bicep con -p "nombre=valor".
    private static IEnumerable<string> ParametrosDe(string script) =>
        Regex.Matches(File.ReadAllText(script), @"-p((?:\s+""\w+=[^""]*"")+)")
            .SelectMany(p => Regex.Matches(p.Groups[1].Value, @"""(\w+)=").Select(n => n.Groups[1].Value));

    [Fact]
    public void Base_gratuita_con_AutoPause_pide_copias_locales()
    {
        var baseDatos = Recurso("sqlDatabase");

        Assert.Contains("useFreeLimit: true", baseDatos);
        Assert.Contains("freeLimitExhaustionBehavior: 'AutoPause'", baseDatos);
        Assert.Contains("requestedBackupStorageRedundancy: 'Local'", baseDatos);
    }

    [Fact]
    public void Servidor_y_base_SQL_van_en_la_region_que_pasa_desplegar()
    {
        Assert.Contains("location: ubicacionSql", Recurso("sqlServer"));
        Assert.Contains("location: ubicacionSql", Recurso("sqlDatabase"));
        Assert.Contains("ubicacionSql", ParametrosDe(Path.Combine(Azure, "desplegar.ps1")));
    }

    [Fact]
    public void Los_scripts_solo_pasan_parametros_que_el_Bicep_declara()
    {
        var scripts = Directory.GetFiles(Azure, "*.ps1").Where(s => ParametrosDe(s).Any()).ToList();

        Assert.Contains(scripts, s => Path.GetFileName(s) == "desplegar.ps1");
        foreach (var script in scripts)
            foreach (var nombre in ParametrosDe(script))
                Assert.Matches($@"(?m)^param {nombre} ", Bicep);
    }
}
