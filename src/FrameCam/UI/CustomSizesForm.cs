using FrameCam.Core;

namespace FrameCam.UI;

/// <summary>
/// The custom sizes dialog (spec 7.5): a row to add a size at the top, then the saved sizes, each with an X
/// at its end that deletes it. Changes count right away; however the dialog is closed, the caller keeps
/// <see cref="Sizes"/>.
/// </summary>
internal sealed class CustomSizesForm : LightDialog
{
    private const string NoneYet = "還沒有自訂尺寸";
    private const int RowHeight = 34;
    private const int SizeColumn = 104; // "8192 × 8192" fits
    private const int RemoveCenter = 16; // the X's center, from the row's right end
    private const int RemoveRadius = 12;

    private readonly List<CustomSize> _sizes;
    private readonly ListBox _list = new()
    {
        BorderStyle = BorderStyle.None,
        DrawMode = DrawMode.OwnerDrawFixed,
        IntegralHeight = false,
        ItemHeight = RowHeight,
        BackColor = Palette.Field,
        AccessibleName = "已儲存的尺寸",
    };
    private readonly Label _empty = new() { Text = NoneYet, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Palette.Muted };
    private readonly TextBox _label = new() { BorderStyle = BorderStyle.None, AccessibleName = "標籤" }; // no length limit: too long gets a message instead
    private readonly TextBox _width = new() { BorderStyle = BorderStyle.None, AccessibleName = "寬" };
    private readonly TextBox _height = new() { BorderStyle = BorderStyle.None, AccessibleName = "高" };
    private readonly Label _message = new() { AutoSize = true, Location = new Point(34, 124), ForeColor = Palette.ErrorText, Visible = false };
    private int _hoverRow = -1;
    private bool _hoverRemove;
    private int _pressedRemove = -1;

    /// <param name="current">The region's size now, filled in as the size to add.</param>
    public CustomSizesForm(IReadOnlyList<CustomSize> sizes, PixelSize current)
        : base("自訂尺寸", "FrameCam 自訂尺寸", new Size(460, 470))
    {
        _sizes = [.. sizes];
        SuspendLayout();

        _width.Text = current.Width.ToString();
        _height.Text = current.Height.ToString();
        var times = new Label { Text = "×", TextAlign = ContentAlignment.MiddleCenter, ForeColor = Palette.Muted, Bounds = new Rectangle(274, 80, 18, 38) };
        var add = new RoundButton { Text = "新增", Bounds = new Rectangle(378, 80, 60, 38) };
        add.SetColors(Palette.Accent, Palette.AccentHover, Palette.AccentPressed, Color.White);
        add.Click += (_, _) => AddTyped();

        var list = new FieldBox { Bounds = new Rectangle(22, 178, 416, 216), CornerRadius = 16 };
        _list.Bounds = new Rectangle(6, 6, 404, 204); // inside the round corners
        _empty.Bounds = _list.Bounds;
        list.Controls.Add(_empty);
        list.Controls.Add(_list);
        _list.DrawItem += DrawSize;
        _list.MouseMove += (_, e) => Hover(e.Location);
        _list.MouseLeave += (_, _) => Hover(null);
        _list.MouseDown += (_, e) => _pressedRemove = e.Button == MouseButtons.Left ? RemoveAt(e.Location) : -1;
        _list.MouseUp += (_, e) =>
        {
            int row = _pressedRemove;
            _pressedRemove = -1;
            if (row >= 0 && e.Button == MouseButtons.Left && RemoveAt(e.Location) == row)
                Delete(row, selected: -1);
        };
        _list.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete && _list.SelectedIndex >= 0)
                Delete(_list.SelectedIndex, selected: Math.Min(_list.SelectedIndex, _sizes.Count - 2)); // the next one, so pressing again keeps deleting
        };
        _list.GotFocus += (_, _) => _list.Invalidate(); // the selected row shows only while the list has the focus
        _list.LostFocus += (_, _) => _list.Invalidate();

        var done = new RoundButton { Text = "完成", DialogResult = DialogResult.OK, Bounds = new Rectangle(354, 412, 84, 36) };
        done.SetColors(Palette.Field, Palette.Pressed, Palette.FieldButtonHover, Palette.Ink);
        AcceptButton = add; // Enter in the boxes adds what was typed
        CancelButton = done;

        Controls.AddRange(new Control[]
        {
            Caption("標籤（可以不填）", 34, 58), FieldBox.Around(new Rectangle(22, 80, 168, 38), _label),
            Caption("寬", 210, 58), FieldBox.Around(new Rectangle(198, 80, 76, 38), _width),
            times,
            Caption("高", 304, 58), FieldBox.Around(new Rectangle(292, 80, 76, 38), _height),
            add, _message,
            Caption("已儲存的尺寸", 34, 156), list,
            done, CloseButton(460, DialogResult.Cancel),
        });
        ShowSizes(selected: -1);
        ResumeLayout(false);
        PerformLayout();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _list.ItemHeight = LogicalToDeviceUnits(RowHeight); // owner-drawn rows do not grow with the display scale by themselves
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
        ShowSizes(selected: -1);
        _list.TopIndex = _sizes.Count - 1; // the new one is in view
        Say(null);
        _label.Text = "";
        _label.Focus();
    }

    private void Delete(int index, int selected)
    {
        _sizes.RemoveAt(index);
        ShowSizes(selected);
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
        _hoverRow = -1;
        _hoverRemove = false;
    }

    private void Say(string? message)
    {
        _message.Text = message ?? "";
        _message.Visible = message is not null;
    }

    /// <summary>The row whose X is under <paramref name="p"/>, or -1.</summary>
    private int RemoveAt(Point p)
    {
        int row = _list.IndexFromPoint(p);
        if (row < 0 || row >= _sizes.Count)
            return -1;
        var center = RemoveCircleCenter(_list.GetItemRectangle(row));
        int radius = LogicalToDeviceUnits(RemoveRadius);
        return Math.Abs(p.X - center.X) <= radius && Math.Abs(p.Y - center.Y) <= radius ? row : -1;
    }

    private Point RemoveCircleCenter(Rectangle row) => new(row.Right - LogicalToDeviceUnits(RemoveCenter), row.Top + row.Height / 2);

    private void Hover(Point? p)
    {
        int row = p is { } point ? _list.IndexFromPoint(point) : -1;
        bool remove = p is { } q && RemoveAt(q) >= 0;
        if (row == _hoverRow && remove == _hoverRemove)
            return;
        _hoverRow = row;
        _hoverRemove = remove;
        _list.Cursor = remove ? Cursors.Hand : Cursors.Default;
        _list.Invalidate();
    }

    private void DrawSize(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _list.Items.Count)
            return;
        var g = e.Graphics;
        using (var back = new SolidBrush(Palette.Field))
            g.FillRectangle(back, e.Bounds);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        bool selected = (e.State & DrawItemState.Selected) != 0 && _list.Focused;
        if (e.Index == _hoverRow || selected)
        {
            using var row = new SolidBrush(Palette.Surface);
            using var shape = Shapes.Rounded(RectangleF.Inflate(e.Bounds, 0, -2), LogicalToDeviceUnits(10));
            g.FillPath(row, shape);
        }

        var size = (CustomSize)_list.Items[e.Index];
        const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;
        int left = e.Bounds.Left + LogicalToDeviceUnits(12);
        int labelLeft = left + LogicalToDeviceUnits(SizeColumn);
        var center = RemoveCircleCenter(e.Bounds);
        TextRenderer.DrawText(g, $"{size.Width} × {size.Height}", Font, Rectangle.FromLTRB(left, e.Bounds.Top, labelLeft, e.Bounds.Bottom), Palette.Strong, flags);
        TextRenderer.DrawText(g, size.Label, Font, Rectangle.FromLTRB(labelLeft, e.Bounds.Top, center.X - LogicalToDeviceUnits(RemoveRadius + 4), e.Bounds.Bottom), Palette.Muted, flags);

        bool onRemove = e.Index == _hoverRow && _hoverRemove;
        int radius = LogicalToDeviceUnits(RemoveRadius);
        if (onRemove)
        {
            using var circle = new SolidBrush(Palette.RecordFill);
            g.FillEllipse(circle, center.X - radius, center.Y - radius, 2 * radius, 2 * radius);
        }
        float glyph = LogicalToDeviceUnits(14);
        Glyphs.Draw(g, Glyph.Close, new RectangleF(center.X - glyph / 2, center.Y - glyph / 2, glyph, glyph), onRemove ? Palette.RecordInk : Palette.Dots);
    }
}
