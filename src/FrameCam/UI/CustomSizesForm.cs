using FrameCam.Core;

namespace FrameCam.UI;

/// <summary>
/// The custom sizes dialog (spec 7.5): a row to add a size at the top, then the saved sizes, each with an X
/// at its end that deletes it, and a gray bar at the bottom with the count and 完成. Clicking a saved size puts
/// it in the top row to change, where 新增 becomes 儲存 next to a round 取消 arrow. Changes count right away; however the
/// dialog is closed, the caller keeps <see cref="Sizes"/>.
/// </summary>
internal sealed class CustomSizesForm : LightDialog
{
    private const string NoneYet = "還沒有自訂尺寸";
    private const int RowHeight = 40;
    private const int SizeColumn = 104; // "8192 × 8192" fits
    private const int RemoveCenter = 16; // the X's center, from the row's right end
    private const int RemoveRadius = 12;
    private const int CancelRoom = 44; // the boxes move left this much to make room for 取消

    private readonly List<CustomSize> _sizes;
    private readonly PixelSize _current;
    private readonly ListBox _list = new()
    {
        BorderStyle = BorderStyle.None,
        DrawMode = DrawMode.OwnerDrawFixed,
        IntegralHeight = false,
        ItemHeight = RowHeight,
        BackColor = Palette.Surface,
        AccessibleName = "已儲存的尺寸",
    };
    private readonly Label _empty = new() { Text = NoneYet, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Palette.Muted, BackColor = Palette.Surface };
    private readonly Label _count = new() { AutoSize = true, Location = new Point(22, 24), ForeColor = Palette.Muted };
    private readonly TextBox _label = new() { BorderStyle = BorderStyle.None, AccessibleName = "標籤" }; // no length limit: too long gets a message instead
    private readonly TextBox _width = new() { BorderStyle = BorderStyle.None, AccessibleName = "寬" };
    private readonly TextBox _height = new() { BorderStyle = BorderStyle.None, AccessibleName = "高" };
    private readonly Label _message = new() { AutoSize = true, Location = new Point(34, 124), ForeColor = Palette.ErrorText, Visible = false };
    private readonly RoundButton _add = new() { Text = "新增", Bounds = new Rectangle(378, 80, 60, 38) };
    private readonly RoundButton _cancel = new() { Glyph = Glyph.Undo, AccessibleName = "取消", Bounds = new Rectangle(400, 80, 38, 38), Visible = false };
    private readonly FieldBox _labelField;
    private readonly Control[] _afterLabel; // move left with the add button while a size is being changed
    private int _editing = -1; // the row in the top row to change, or -1 while adding
    private int _hoverRow = -1;
    private bool _hoverRemove;
    private int _pressedRemove = -1;

    /// <param name="current">The region's size now, filled in as the size to add.</param>
    public CustomSizesForm(IReadOnlyList<CustomSize> sizes, PixelSize current)
        : base("自訂尺寸", "FrameCam 自訂尺寸", new Size(460, 470))
    {
        _sizes = [.. sizes];
        _current = current;
        SuspendLayout();

        _width.Text = current.Width.ToString();
        _height.Text = current.Height.ToString();
        var times = new Label { Text = "×", TextAlign = ContentAlignment.MiddleCenter, ForeColor = Palette.Muted, Bounds = new Rectangle(274, 80, 18, 38) };
        _add.SetColors(Palette.Accent, Palette.AccentHover, Palette.AccentPressed, Color.White);
        _add.Click += (_, _) => SaveTyped();
        _cancel.SetColors(Palette.Field, Palette.Pressed, Palette.FieldButtonHover, Palette.Ink);
        _cancel.Click += (_, _) => StopEditing();
        _labelField = FieldBox.Around(new Rectangle(22, 80, 168, 38), _label);
        var widthCaption = Caption("寬", 210, 58);
        var widthField = FieldBox.Around(new Rectangle(198, 80, 76, 38), _width);
        var heightCaption = Caption("高", 304, 58);
        var heightField = FieldBox.Around(new Rectangle(292, 80, 76, 38), _height);
        _afterLabel = [widthCaption, widthField, times, heightCaption, heightField, _add];

        var list = new FieldBox { Bounds = new Rectangle(22, 174, 416, 212), CornerRadius = 12, Bordered = true, BackColor = Palette.Surface };
        _list.Bounds = new Rectangle(5, 5, 406, 202); // inside the round corners
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
            if (e.Button != MouseButtons.Left)
                return;
            if (row >= 0)
            {
                if (RemoveAt(e.Location) == row)
                    Delete(row, selected: -1);
            }
            else if (_list.IndexFromPoint(e.Location) is var clicked and >= 0 && clicked < _sizes.Count && clicked != _editing)
                StartEditing(clicked); // the row already being changed keeps what was typed
        };
        _list.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete && _list.SelectedIndex >= 0)
                Delete(_list.SelectedIndex, selected: Math.Min(_list.SelectedIndex, _sizes.Count - 2)); // the next one, so pressing again keeps deleting
        };
        _list.GotFocus += (_, _) => _list.Invalidate(); // the selected row shows only while the list has the focus
        _list.LostFocus += (_, _) => _list.Invalidate();

        var done = new RoundButton { Text = "完成", DialogResult = DialogResult.OK, Bounds = new Rectangle(354, 14, 84, 36) };
        done.SetColors(Palette.Accent, Palette.AccentHover, Palette.AccentPressed, Color.White);
        var bar = BottomBar(460, 406, _count, done);
        AcceptButton = _add; // Enter in the boxes adds or saves what was typed
        CancelButton = done;

        Controls.AddRange(new Control[]
        {
            Caption("標籤（可以不填）", 34, 58), _labelField,
            widthCaption, widthField,
            times,
            heightCaption, heightField,
            _add, _cancel, _message,
            Caption("已儲存的尺寸", 34, 152), list, bar, CloseButton(460, DialogResult.Cancel),
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

    /// <summary>Esc while a size is being changed stops changing it, rather than closing the dialog.</summary>
    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData == Keys.Escape && _editing >= 0)
        {
            StopEditing();
            return true;
        }
        return base.ProcessDialogKey(keyData);
    }

    /// <summary>The saved sizes, with this dialog's additions, changes and deletions.</summary>
    public IReadOnlyList<CustomSize> Sizes => _sizes.ToArray();

    /// <summary>新增, or 儲存 while a size is being changed.</summary>
    private void SaveTyped()
    {
        if (_editing < 0 && _sizes.Count >= CustomSize.MaxCount)
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
        if (_sizes.Where((_, i) => i != _editing).Contains(size))
        {
            Say("已經有一樣的自訂尺寸");
            return;
        }
        if (_editing >= 0)
        {
            _sizes[_editing] = size;
            _list.Items[_editing] = size;
            StopEditing();
            return;
        }
        _sizes.Add(size);
        ShowSizes(selected: -1);
        _list.TopIndex = _sizes.Count - 1; // the new one is in view
        Say(null);
        _label.Text = "";
        _label.Focus();
    }

    private void StartEditing(int row)
    {
        if (_editing < 0)
            ShowSaveAndCancel(true);
        _editing = row;
        var size = _sizes[row];
        _label.Text = size.Label;
        _width.Text = size.Width.ToString();
        _height.Text = size.Height.ToString();
        Say(null);
        _list.Invalidate();
    }

    /// <summary>Back to adding, with the boxes as the dialog opened.</summary>
    private void StopEditing()
    {
        _editing = -1;
        ShowSaveAndCancel(false);
        _label.Text = "";
        _width.Text = _current.Width.ToString();
        _height.Text = _current.Height.ToString();
        Say(null);
        _list.Invalidate();
    }

    /// <summary>新增 becomes 儲存 and 取消 shows; the label box narrows, and the boxes after it move left, to make room.</summary>
    private void ShowSaveAndCancel(bool show)
    {
        int room = LogicalToDeviceUnits(CancelRoom) * (show ? 1 : -1);
        _labelField.Width -= room;
        _labelField.Invalidate(); // a panel does not repaint by itself when it narrows, and would keep the square cut of its old shape
        _label.Width -= room;
        foreach (var control in _afterLabel)
            control.Left -= room;
        _add.Text = show ? "儲存" : "新增";
        _cancel.Visible = show;
    }

    private void Delete(int index, int selected)
    {
        if (index == _editing)
            StopEditing();
        else if (index < _editing)
            _editing--;
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
        _count.Text = $"已存 {_sizes.Count} 個，最多 {CustomSize.MaxCount} 個";
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
        using (var back = new SolidBrush(Palette.Surface))
            g.FillRectangle(back, e.Bounds);
        if (e.Index < _list.Items.Count - 1)
        {
            using var divider = new Pen(Palette.Border, 1);
            int y = e.Bounds.Bottom - 1;
            g.DrawLine(divider, e.Bounds.Left + LogicalToDeviceUnits(8), y, e.Bounds.Right - LogicalToDeviceUnits(8), y);
        }
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        bool selected = (e.State & DrawItemState.Selected) != 0 && _list.Focused;
        if (e.Index == _hoverRow || selected || e.Index == _editing)
        {
            using var row = new SolidBrush(Palette.Hover);
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
