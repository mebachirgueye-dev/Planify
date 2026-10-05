using Planify.Models;

namespace Planify.Services;

/// <summary>Interface pour les exportateurs de données (CSV, Excel, PDF...).</summary>
public interface IExporter
{
    /// <summary>Nom affiché dans l'UI (ex. "CSV", "Excel").</summary>
    string DisplayName { get; }

    /// <summary>Extension de fichier (avec le point, ex. ".csv").</summary>
    string FileExtension { get; }

    /// <summary>Filtre pour la boîte de dialogue "Enregistrer sous".</summary>
    string FileFilter { get; }

    /// <summary>Exporte une liste d'objets vers un fichier.</summary>
    /// <typeparam name="T">Type des objets à exporter.</typeparam>
    /// <param name="items">Liste des objets.</param>
    /// <param name="filePath">Chemin du fichier de sortie.</param>
    /// <param name="cancellationToken">Token d'annulation.</param>
    Task ExportAsync<T>(IEnumerable<T> items, string filePath, CancellationToken cancellationToken = default)
        where T : class;
}

/// <summary>Exportateur CSV simple, sans dépendance externe.</summary>
public sealed class CsvExporter : IExporter
{
    public string DisplayName => "CSV";
    public string FileExtension => ".csv";
    public string FileFilter => "Fichiers CSV (*.csv)|*.csv";

    public async Task ExportAsync<T>(IEnumerable<T> items, string filePath, CancellationToken cancellationToken = default)
        where T : class
    {
        using var writer = new StreamWriter(filePath, false, new System.Text.UTF8Encoding(true)); // UTF-8 avec BOM pour Excel
        var properties = typeof(T).GetProperties()
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();

        // En-têtes
        var headers = properties.Select(p => EscapeCsv(p.Name));
        await writer.WriteLineAsync(string.Join(";", headers));

        // Données
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);
                return EscapeCsv(value?.ToString() ?? string.Empty);
            });
            await writer.WriteLineAsync(string.Join(";", values));
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        return value;
    }
}

/// <summary>Exportateur Excel (placeholder - nécessite une bibliothèque comme EPPlus ou ClosedXML).</summary>
public sealed class ExcelExporter : IExporter
{
    public string DisplayName => "Excel";
    public string FileExtension => ".xlsx";
    public string FileFilter => "Fichiers Excel (*.xlsx)|*.xlsx";

    public Task ExportAsync<T>(IEnumerable<T> items, string filePath, CancellationToken cancellationToken = default)
        where T : class
    {
        throw new NotImplementedException("L'export Excel nécessite l'ajout d'une bibliothèque (ex. EPPlus). Utilisez le CSV pour l'instant.");
    }
}

/// <summary>Exportateur PDF (placeholder - nécessite une bibliothèque comme PdfSharp ou QuestPDF).</summary>
public sealed class PdfExporter : IExporter
{
    public string DisplayName => "PDF";
    public string FileExtension => ".pdf";
    public string FileFilter => "Fichiers PDF (*.pdf)|*.pdf";

    public Task ExportAsync<T>(IEnumerable<T> items, string filePath, CancellationToken cancellationToken = default)
        where T : class
    {
        throw new NotImplementedException("L'export PDF nécessite l'ajout d'une bibliothèque (ex. QuestPDF). Utilisez le CSV pour l'instant.");
    }
}

/// <summary>Factory pour obtenir les exportateurs disponibles.</summary>
public static class ExporterFactory
{
    private static readonly IExporter[] _exporters = new IExporter[]
    {
        new CsvExporter(),
        new ExcelExporter(),
        new PdfExporter()
    };

    public static IReadOnlyList<IExporter> GetAll() => _exporters;

    public static IExporter? GetByName(string name) => _exporters.FirstOrDefault(e => e.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static IExporter GetDefault() => _exporters[0]; // CSV
}