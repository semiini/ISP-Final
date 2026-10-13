using System.Text.RegularExpressions;
using ClipboardGuard.Core.Models;

namespace ClipboardGuard.UI;

/// <summary>Selects the detected categories to cover for one paste.</summary>
internal sealed class PasteSelectionForm : Form
{
    private readonly CheckedListBox _types = new()
    {
        Dock = DockStyle.Fill, CheckOnClick = true, IntegralHeight = false,
    };
    private readonly SensitiveDataType[] _detectedTypes;
    public PasteChoice Choice { get; private set; } = PasteChoice.Cancel;
    public HashSet<SensitiveDataType> SelectedTypes =>
        _types.CheckedIndices.Cast<int>().Select(index => _detectedTypes[index]).ToHashSet();

    public PasteSelectionForm(SourceInfo source, DetectionResult detection,
        DestinationInfo destination, PolicyDecision recommended)
    {
        Text = "ClipboardGuard — Choose what to cover";
        ClientSize = new Size(560, 440);
        MinimumSize = new Size(540, 400);
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        MinimizeBox = false;
        MaximizeBox = false;
        Icon = SystemIcons.Shield;
        AutoScaleMode = AutoScaleMode.Dpi;

        _detectedTypes = detection.Matches.Select(match => match.DataType).Distinct().ToArray();
        foreach (var type in _detectedTypes)
        {
            string label = type switch
            {
                SensitiveDataType.CreditCardNumber => "Card number",
                SensitiveDataType.ApiKey => "API key",
                _ => Regex.Replace(type.ToString(), "([a-z])([A-Z])", "$1 $2"),
            };
            _types.Items.Add(label, true);
        }

        var heading = new Label
        {
            Text = $"From {source.ProcessName} ({source.Category}) into " +
                $"{destination.ProcessName} ({destination.Category}, {destination.RiskLevel} risk).\n\n" +
                "You have these sensitive data types. Check the ones to cover.\n" +
                "Unchecked types will be pasted with their original values.",
            Dock = DockStyle.Fill, AutoSize = true, MaximumSize = new Size(510, 0),
        };
        var selectAll = new Button { Text = "Select all", AutoSize = true };
        var deselectAll = new Button { Text = "Deselect all", AutoSize = true };
        selectAll.Click += (_, _) => SetAll(true);
        deselectAll.Click += (_, _) => SetAll(false);
        var selectionButtons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        selectionButtons.Controls.AddRange([selectAll, deselectAll]);

        var original = CreateChoiceButton("Paste original", PasteChoice.Original);
        var covered = CreateChoiceButton("Paste selected", PasteChoice.Redacted);
        var cancel = CreateChoiceButton("Cancel", PasteChoice.Cancel);
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        actions.Controls.AddRange([covered, original, cancel]);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 4,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(selectionButtons, 0, 1);
        layout.Controls.Add(_types, 0, 2);
        layout.Controls.Add(actions, 0, 3);
        Controls.Add(layout);
        AcceptButton = recommended switch
        {
            PolicyDecision.Allow => original,
            PolicyDecision.Block => cancel,
            _ => covered,
        };
        CancelButton = cancel;
        Shown += (_, _) => ((Button)AcceptButton!).Focus();
    }

    private Button CreateChoiceButton(string text, PasteChoice choice)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += (_, _) => { Choice = choice; DialogResult = DialogResult.OK; Close(); };
        return button;
    }

    private void SetAll(bool selected)
    {
        for (int index = 0; index < _types.Items.Count; index++)
            _types.SetItemChecked(index, selected);
    }
}
