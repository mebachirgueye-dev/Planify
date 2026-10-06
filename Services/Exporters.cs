using Planify.Models;
using System.Text;

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

/// <summary>Exportateur PDF simple, sans dépendance externe.
/// Génère un PDF basique avec tableau de données (texte uniquement, pas d'images).</summary>
public sealed class PdfExporter : IExporter
{
    public string DisplayName => "PDF";
    public string FileExtension => ".pdf";
    public string FileFilter => "Fichiers PDF (*.pdf)|*.pdf";

    public async Task ExportAsync<T>(IEnumerable<T> items, string filePath, CancellationToken cancellationToken = default)
        where T : class
    {
        var itemList = items.ToList();
        if (itemList.Count == 0)
        {
            await File.WriteAllTextAsync(filePath, "%PDF-1.4\n%%EOF", cancellationToken);
            return;
        }

        var properties = typeof(T).GetProperties()
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();

        var pdf = new SimplePdfDocument();
        pdf.AddTable(typeof(T).Name, properties.Select(p => p.Name).ToArray(),
            itemList.Select(item => properties.Select(p => p.GetValue(item)?.ToString() ?? string.Empty).ToArray()).ToList());

        await File.WriteAllBytesAsync(filePath, pdf.Build(), cancellationToken);
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
        throw new NotImplementedException("L'export Excel nécessite l'ajout d'une bibliothèque (ex. EPPlus). Utilisez le CSV ou PDF pour l'instant.");
    }
}

/// <summary>Factory pour obtenir les exportateurs disponibles.</summary>
public static class ExporterFactory
{
    private static readonly IExporter[] _exporters = new IExporter[]
    {
        new CsvExporter(),
        new PdfExporter(),
        new ExcelExporter()
    };

    public static IReadOnlyList<IExporter> GetAll() => _exporters;

    public static IExporter? GetByName(string name) => _exporters.FirstOrDefault(e => e.DisplayName.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static IExporter GetDefault() => _exporters[0]; // CSV
}

/// <summary>Générateur PDF minimaliste pour tableaux de données simples.</summary>
internal sealed class SimplePdfDocument
{
    private readonly List<byte> _content = new();
    private readonly List<PdfObject> _objects = new();

    public void AddTable(string title, string[] headers, List<string[]> rows)
    {
        // Créer le contenu de la page
        var pageContent = new StringBuilder();
        pageContent.AppendLine("BT");
        pageContent.AppendLine("/F1 12 Tf");
        pageContent.AppendLine($"72 720 Td");
        pageContent.AppendLine($"({EscapePdfString(title)}) Tj");
        pageContent.AppendLine("ET");

        // Tableau simplifié - une ligne par enregistrement
        float y = 700;
        const float rowHeight = 18;
        const float xStart = 72;
        float colWidth = 450f / Math.Max(1, headers.Length);

        // En-têtes
        pageContent.AppendLine("BT");
        pageContent.AppendLine("/F1 10 Tf");
        for (int i = 0; i < headers.Length; i++)
        {
            float x = xStart + i * colWidth;
            pageContent.AppendLine($"{x} {y} Td ({EscapePdfString(headers[i])}) Tj");
        }
        pageContent.AppendLine("ET");

        y -= rowHeight;

        // Données
        foreach (var row in rows)
        {
            if (y < 72) break; // Nouvelle page nécessaire (simplifié: on s'arrête)
            pageContent.AppendLine("BT");
            pageContent.AppendLine("/F1 9 Tf");
            for (int i = 0; i < row.Length && i < headers.Length; i++)
            {
                float x = xStart + i * colWidth;
                var cellText = row[i].Length > 30 ? row[i].Substring(0, 30) + "…" : row[i];
                pageContent.AppendLine($"{x} {y} Td ({EscapePdfString(cellText)}) Tj");
            }
            pageContent.AppendLine("ET");
            y -= rowHeight;
        }

        var contentStream = pageContent.ToString();
        var contentBytes = Encoding.ASCII.GetBytes(contentStream);

        // Objets PDF
        AddObject(1, "<< /Type /Catalog /Pages 2 0 R >>"); // Catalog
        AddObject(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>"); // Pages
        AddObject(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>"); // Page
        AddObject(4, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"); // Font
        AddObject(5, $"<< /Length {contentBytes.Length} >>\nstream\n{contentStream}\nendstream"); // Content
    }

    private void AddObject(int id, string dict)
    {
        _objects.Add(new PdfObject(id, dict));
    }

    public byte[] Build()
    {
        var output = new StringBuilder();
        output.AppendLine("%PDF-1.4");

        long[] offsets = new long[_objects.Count + 1];
        int index = 0;

        foreach (var obj in _objects)
        {
            offsets[index] = output.Length;
            output.AppendLine($"{obj.Id} 0 obj");
            output.AppendLine(obj.Dictionary);
            output.AppendLine("endobj");
            index++;
        }

        long xrefStart = output.Length;
        output.AppendLine("xref");
        output.AppendLine($"0 {_objects.Count + 1}");
        output.AppendLine("0000000000 65535 f ");
        for (int i = 0; i < _objects.Count; i++)
        {
            output.AppendLine($"{offsets[i]:D10} 00000 n ");
        }
        output.AppendLine("trailer");
        output.AppendLine($"<< /Size {_objects.Count + 1} /Root 1 0 R >>");
        output.AppendLine("startxref");
        output.AppendLine(xrefStart.ToString());
        output.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(output.ToString());
    }

    private static string EscapePdfString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\r", "\\r").Replace("\n", "\\n");
    }

    private sealed class PdfObject
    {
        public int Id { get; }
        public string Dictionary { get; }
        public PdfObject(int id, string dict) { Id = id; Dictionary = dict; }
    }
}