using Planify.Controls;
using Planify.Helpers;
using Planify.Services;

namespace Planify.Forms;

/// <summary>
/// Boîte de dialogue générique pour exporter des données.
/// </summary>
public sealed class ExportDialog : Form
{
    private readonly IEnumerable<IExporter> _exporters;
    private readonly ComboBox _exporterCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _infoLabel = new();

    public ExportDialog(IEnumerable<IExporter> exporters)
    {
        _exporters = exporters.ToList();

        Text = "Exporter les données";
        Icon = AppResources.LoadIcon("planify.ico");
        Font = Theme.Body;
        BackColor = Theme.Surface;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
    }

    private void BuildLayout()
    {
        int margin = Theme.Px(24);
        int width = Theme.Px(460);
        int fieldWidth = width - 2 * margin;
        int y = margin;

        var title = new Label
        {
            Text = "Exporter les données",
            Font = Theme.Title,
            ForeColor = Theme.Navy,
            AutoSize = true,
            Location = new Point(margin, y)
        };
        Controls.Add(title);
        y += title.Height + Theme.Px(16);

        _exporterCombo.Width = fieldWidth;
        _exporterCombo.Font = Theme.Body;
        _exporterCombo.Items.AddRange(_exporters.Cast<object>().ToArray());
        _exporterCombo.SelectedIndex = 0;
        AddField("Format d'export", _exporterCombo, fieldWidth, margin, ref y);

        _infoLabel.AutoSize = true;
        _infoLabel.Font = Theme.Small;
        _infoLabel.ForeColor = Theme.TextMuted;
        _infoLabel.Location = new Point(margin, y);
        _infoLabel.MaximumSize = new Size(fieldWidth, 0);
        Controls.Add(_infoLabel);
        y += Theme.Px(40);

        var cancel = new ThemedButton
        {
            Text = "Annuler",
            Kind = ButtonKind.Secondary,
            Width = Theme.Px(110),
            DialogResult = DialogResult.Cancel
        };
        var export = new ThemedButton
        {
            Text = "Exporter",
            Kind = ButtonKind.Primary,
            Width = Theme.Px(130)
        };
        export.Click += OnExport;

        export.Location = new Point(width - margin - export.Width, y);
        cancel.Location = new Point(export.Left - Theme.Px(10) - cancel.Width, y);
        Controls.Add(cancel);
        Controls.Add(export);

        AcceptButton = export;
        CancelButton = cancel;

        ClientSize = new Size(width, y + export.Height + margin);
    }

    private void AddField(string label, Control input, int inputWidth, int x, ref int y)
    {
        var caption = new Label
        {
            Text = label,
            Font = Theme.SmallBold,
            ForeColor = Theme.Navy,
            AutoSize = true,
            Location = new Point(x, y)
        };
        Controls.Add(caption);
        y += Theme.Px(22);

        input.Location = new Point(x, y);
        input.Width = inputWidth;
        Controls.Add(input);
        y += input.Height + Theme.Px(16);
    }

    public IExporter SelectedExporter => _exporterCombo.SelectedItem as IExporter ?? ExporterFactory.GetDefault();

    private void OnExport(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.OK;
    }
}