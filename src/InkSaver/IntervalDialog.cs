using InkSaver.Core;

namespace InkSaver;

/// <summary>Small dialog to enter a custom number of days between prints.</summary>
internal sealed class IntervalDialog : Form
{
    private readonly NumericUpDown _days;

    public IntervalDialog(int currentDays, Icon icon)
    {
        Text = Strings.IntervalDialogTitle;
        Icon = icon;
        Font = SystemFonts.MessageBoxFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        TopMost = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        _days = new NumericUpDown
        {
            Minimum = AppSettings.MinIntervalDays,
            Maximum = AppSettings.MaxIntervalDays,
            Value = Math.Clamp(currentDays, AppSettings.MinIntervalDays, AppSettings.MaxIntervalDays),
            Width = 70,
            TextAlign = HorizontalAlignment.Right,
            Anchor = AnchorStyles.Left,
        };

        var ok = new Button { Text = Strings.Ok, DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
        AcceptButton = ok;
        CancelButton = cancel;

        var input = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 12) };
        input.Controls.Add(new Label { Text = Strings.IntervalDialogPrompt, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 6, 0) });
        input.Controls.Add(_days);
        input.Controls.Add(new Label { Text = Strings.DaysUnit, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(6, 6, 0, 0) });

        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, WrapContents = false };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };
        layout.Controls.Add(input);
        layout.Controls.Add(buttons);
        Controls.Add(layout);

        Shown += (_, _) =>
        {
            _days.Select(0, _days.Text.Length);
            _days.Focus();
        };
    }

    public int Days => (int)_days.Value;
}
