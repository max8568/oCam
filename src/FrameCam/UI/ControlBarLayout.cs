namespace FrameCam.UI;

/// <summary>The control bar's buttons, left to right.</summary>
internal enum BarItem { Record, Screenshot, Size, Pick, Folder, Settings, Help, Close }

/// <summary>
/// Where everything on the control bar goes, in pixels of its window. The bar is a pill 48 pixels high
/// (at 100 % scale) inside a see-through margin that holds its shadow.
/// </summary>
internal sealed class ControlBarLayout
{
    /// <summary>The see-through edge around the bar at 100 % scale.</summary>
    public const int Margin = 8;

    private const int Height = 48;
    private const int Padding = 6;
    private const int Gap = 4;
    private const int GripWidth = 18;
    private const int ButtonSize = 36;
    private const int SeparatorWidth = 5;

    private readonly Dictionary<BarItem, RectangleF> _items = new();
    private readonly List<float> _separators = new();

    private ControlBarLayout(float scale, float recordWidth)
    {
        float margin = Margin * scale;
        float top = margin + Padding * scale;
        float x = margin + Padding * scale;
        Grip = new RectangleF(x, top, GripWidth * scale, ButtonSize * scale);
        x = Grip.Right + Gap * scale;

        void Add(BarItem item, float width)
        {
            _items[item] = new RectangleF(x, top, width, ButtonSize * scale);
            x += width + Gap * scale;
        }

        void Separator()
        {
            _separators.Add(x + SeparatorWidth * scale / 2);
            x += (SeparatorWidth + Gap) * scale;
        }

        Add(BarItem.Record, recordWidth);
        Add(BarItem.Screenshot, ButtonSize * scale);
        Separator();
        Add(BarItem.Size, ButtonSize * scale);
        Add(BarItem.Pick, ButtonSize * scale);
        Add(BarItem.Folder, ButtonSize * scale);
        Add(BarItem.Settings, ButtonSize * scale);
        Add(BarItem.Help, ButtonSize * scale);
        Separator();
        Add(BarItem.Close, ButtonSize * scale);

        float right = _items[BarItem.Close].Right + Padding * scale;
        Bar = new RectangleF(margin, margin, right - margin, Height * scale);
        WindowSize = new Size((int)Math.Ceiling(Bar.Right + margin), (int)Math.Ceiling(Bar.Bottom + margin));
    }

    /// <param name="scale">Display scale, 1 at 100 %.</param>
    /// <param name="recordWidth">Width of the record button in pixels, from <see cref="ControlBarPainter.RecordWidth"/>.</param>
    public static ControlBarLayout Compute(float scale, float recordWidth) => new(scale, recordWidth);

    public Size WindowSize { get; }

    /// <summary>The pill itself, inside the shadow margin.</summary>
    public RectangleF Bar { get; }

    /// <summary>The dots at the left end. Dragging there, or anywhere on the bar that is not a button, moves the bar.</summary>
    public RectangleF Grip { get; }

    /// <summary>Horizontal centers of the thin lines between button groups.</summary>
    public IReadOnlyList<float> Separators => _separators;

    public RectangleF this[BarItem item] => _items[item];

    /// <summary>The button under a point of the window, or null.</summary>
    public BarItem? ItemAt(Point p)
    {
        foreach (var (item, rect) in _items)
            if (rect.Contains(p))
                return item;
        return null;
    }
}
