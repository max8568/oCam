using FrameCam.Core;

namespace FrameCam.UI;

/// <summary>
/// The custom sizes dialog (spec 7.5): the saved sizes, which can be deleted, and a row to add one. Changes
/// count right away; however the dialog is closed, the caller keeps <see cref="Sizes"/>.
/// </summary>
internal sealed class CustomSizesForm : LightDialog
{
    private const string NoneYet = "還沒有自訂尺寸";

    private readonly List<CustomSize> _sizes;
    private readonly ListBox _list = new()
    {
        BorderStyle = BorderStyle.None,
        DrawMode = DrawMode.OwnerDrawFixed,
        IntegralHeight = false,
        ItemHeight = 28,
        BackColor = Palette.Field,
        AccessibleName = "已儲存的尺寸",
    };
    private readonly Label _empty = new() { Text = NoneYet, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Palette.Muted };
    private readonly RoundButton _delete = new() { Text = "刪除", Bounds = new Rectangle(298, 238, 100, 30), AccessibleName = "刪除選取的尺寸" };
    private readonly TextBox _label = new() { BorderStyle = BorderStyle.None, AccessibleName = "標籤", MaxLength = CustomSize.MaxLabelLength };
    private readonly TextBox _width = new() { BorderStyle = BorderStyle.None, AccessibleName = "寬" };
    private readonly TextBox _height = new() { BorderStyle = BorderStyle.None, AccessibleName = "高" };
    private readonly Label _message = new() { AutoSize = true, Location = new Point(34, 358), ForeColor = Palette.ErrorText, Visible = false };

    /// <param name="current">The region's size now, filled in as the size to add.</param>
    public CustomSizesForm(IReadOnlyList<CustomSize> sizes, PixelSize current)
        : base("自訂尺寸", "FrameCam 自訂尺寸", new Size(420, 460))
    {
        _sizes = [.. sizes];
        SuspendLayout();

        var list = new FieldBox { Bounds = new Rectangle(22, 80, 376, 150), CornerRadius = 16 };
        _list.Bounds = new Rectangle(10, 10, 356, 130);
        list.Controls.Add(_empty);
        list.Controls.Add(_list);
        _list.DrawItem += DrawSize;
        _list.SelectedIndexChanged += (_, _) => _delete.Enabled = _list.SelectedIndex >= 0;
        _delete.SetColors(Palette.Field, Palette.Pressed, Palette.FieldButtonHover, Palette.Ink);
        _delete.Click += (_, _) => DeleteSelected();

        _width.Text = current.Width.ToString();
        _height.Text = current.Height.ToString();
        var add = new RoundButton { Text = "新增", Bounds = new Rectangle(298, 350, 100, 32) };
        add.SetColors(Palette.Accent, Palette.AccentHover, Palette.AccentPressed, Color.White);
        add.Click += (_, _) => AddTyped();
        var done = new RoundButton { Text = "完成", DialogResult = DialogResult.OK, Bounds = new Rectangle(314, 404, 84, 36) };
        done.SetColors(Palette.Field, Palette.Pressed, Palette.FieldButtonHover, Palette.Ink);
        AcceptButton = add; // Enter in the boxes adds what was typed
        CancelButton = done;

        Controls.AddRange(new Control[]
        {
            Caption("已儲存的尺寸", 34, 58), list, _delete,
            Caption("標籤（可以不填）", 34, 282), FieldBox.Around(new Rectangle(22, 304, 196, 38), _label),
            Caption("寬", 238, 282), FieldBox.Around(new Rectangle(226, 304, 80, 38), _width),
            Caption("高", 330, 282), FieldBox.Around(new Rectangle(318, 304, 80, 38), _height),
            _message, add, done, CloseButton(420, DialogResult.Cancel),
        });
        ShowSizes(selected: -1);
        ResumeLayout(false);
        PerformLayout();
    }

    /// <summary>The saved sizes, with this dialog's additions and deletions.</summary>
    public IReadOnlyList<CustomSize> Sizes => _sizes.ToArray();

    private void AddTyped()
    {
        if (_sizes.Count >= CustomSize.MaxCount)
        {
            Say($"最多存 {CustomSize.MaxCount} 個自訂尺寸，請先刪掉一些");
            return;
        }
        var (size, error) = CustomSize.Parse(_label.Text, _width.Text, _height.Text);
        if (size is null)
        {
            Say(error!);
            return;
        }
        if (_sizes.Contains(size))
        {
            Say("已經有一樣的自訂尺寸");
            return;
        }
        _sizes.Add(size);
        ShowSizes(selected: _sizes.Count - 1);
        Say(null);
        _label.Text = "";
        _label.Focus();
    }

    private void DeleteSelected()
    {
        int index = _list.SelectedIndex;
        if (index < 0)
            return;
        _sizes.RemoveAt(index);
        ShowSizes(selected: Math.Min(index, _sizes.Count - 1));
        Say(null);
    }

    private void ShowSizes(int selected)
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        _list.Items.AddRange(_sizes.Cast<object>().ToArray());
        _list.EndUpdate();
        _list.SelectedIndex = selected;
        _list.Visible = _sizes.Count > 0;
        _empty.Visible = _sizes.Count == 0;
        _delete.Enabled = _list.SelectedIndex >= 0;
    }

    private void Say(string? message)
    {
        _message.Text = message ?? "";
        _message.Visible = message is not null;
    }

    private void DrawSize(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
            return;
        var g = e.Graphics;
        using (var back = new SolidBrush(Palette.Field))
            g.FillRectangle(back, e.Bounds);
        if ((e.State & DrawItemState.Selected) != 0)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pick = new SolidBrush(Palette.FieldButton);
            using var shape = Shapes.Rounded(RectangleF.Inflate(e.Bounds, 0, -2), LogicalToDeviceUnits(8));
            g.FillPath(pick, shape);
        }
        var text = Rectangle.FromLTRB(e.Bounds.Left + LogicalToDeviceUnits(10), e.Bounds.Top, e.Bounds.Right, e.Bounds.Bottom);
        TextRenderer.DrawText(g, ((CustomSize)_list.Items[e.Index]).MenuText, Font, text, Palette.Strong,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }
}
