using System.Drawing.Drawing2D;

namespace FrameCam.UI;

internal enum Glyph { Record, Stop, Pause, Play, Camera, Size, PickWindow, Folder, Sliders, Help, Minimize, Close, Grip }

/// <summary>
/// The button icons, drawn as lines on a 24 × 24 grid and scaled to the box, so they stay sharp at any
/// display scale. They match the icons of design 21.
/// </summary>
internal static class Glyphs
{
    private const float Stroke = 1.75f;

    public static void Draw(Graphics g, Glyph glyph, RectangleF box, Color color)
    {
        var saved = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform(box.X, box.Y);
        g.ScaleTransform(box.Width / 24f, box.Height / 24f);
        using var pen = new Pen(color, Stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        using var brush = new SolidBrush(color);
        switch (glyph)
        {
            case Glyph.Record:
                g.FillEllipse(brush, 5.5f, 5.5f, 13, 13);
                break;
            case Glyph.Stop:
                using (var square = Shapes.Rounded(new RectangleF(6, 6, 12, 12), 2))
                    g.FillPath(brush, square);
                break;
            case Glyph.Pause:
                using (var left = Shapes.Rounded(new RectangleF(7, 6, 3.5f, 12), 1))
                    g.FillPath(brush, left);
                using (var right = Shapes.Rounded(new RectangleF(13.5f, 6, 3.5f, 12), 1))
                    g.FillPath(brush, right);
                break;
            case Glyph.Play:
                g.FillPolygon(brush, Points(8.5f, 5.5f, 18.5f, 12, 8.5f, 18.5f));
                g.DrawPolygon(pen, Points(8.5f, 5.5f, 18.5f, 12, 8.5f, 18.5f)); // rounds the corners
                break;
            case Glyph.Camera:
                g.DrawPolygon(pen, Points(3.5f, 8.5f, 6.7f, 8.5f, 8.3f, 6, 15.7f, 6, 17.3f, 8.5f, 20.5f, 8.5f, 20.5f, 18.5f, 3.5f, 18.5f));
                g.DrawEllipse(pen, 8.6f, 9.8f, 6.8f, 6.8f);
                break;
            case Glyph.Size:
                using (var frame = Shapes.Rounded(new RectangleF(3.5f, 5.5f, 17, 13), 1.5f))
                    g.DrawPath(pen, frame);
                g.DrawLines(pen, Points(7, 11, 7, 8.8f, 10, 8.8f));
                g.DrawLines(pen, Points(17, 13, 17, 15.2f, 14, 15.2f));
                break;
            case Glyph.PickWindow:
                using (var window = Shapes.Rounded(new RectangleF(3.5f, 4.5f, 14, 11), 1.5f))
                    g.DrawPath(pen, window);
                g.DrawLine(pen, 3.5f, 8, 17.5f, 8);
                var arrow = Points(13.5f, 12.5f, 20.5f, 15.3f, 17.4f, 16.4f, 16.3f, 19.5f);
                g.FillPolygon(brush, arrow);
                g.DrawPolygon(pen, arrow);
                break;
            case Glyph.Folder:
                using (var folder = new GraphicsPath())
                {
                    folder.AddArc(3.5f, 6, 3, 3, 180, 90);
                    folder.AddLines(Points(5, 6, 9.4f, 6, 11.4f, 8, 19, 8));
                    folder.AddArc(17.5f, 8, 3, 3, 270, 90);
                    folder.AddArc(17.5f, 16, 3, 3, 0, 90);
                    folder.AddArc(3.5f, 16, 3, 3, 90, 90);
                    folder.CloseFigure();
                    g.DrawPath(pen, folder);
                }
                break;
            case Glyph.Sliders:
                g.DrawLine(pen, 4, 7.5f, 13, 7.5f);
                g.DrawLine(pen, 17, 7.5f, 20, 7.5f);
                g.DrawLine(pen, 4, 16.5f, 7, 16.5f);
                g.DrawLine(pen, 11, 16.5f, 20, 16.5f);
                g.DrawEllipse(pen, 13, 5.5f, 4, 4);
                g.DrawEllipse(pen, 7, 14.5f, 4, 4);
                break;
            case Glyph.Help:
                g.DrawEllipse(pen, 3, 3, 18, 18);
                using (var hook = new GraphicsPath())
                {
                    hook.AddArc(9.4f, 6.6f, 5.2f, 5.2f, 180, 225);
                    hook.AddLine(13.84f, 11.04f, 12, 12.4f);
                    hook.AddLine(12, 12.4f, 12, 13.6f);
                    g.DrawPath(pen, hook);
                }
                g.FillEllipse(brush, 10.9f, 15.6f, 2.2f, 2.2f);
                break;
            case Glyph.Minimize:
                g.DrawLine(pen, 6.5f, 12, 17.5f, 12);
                break;
            case Glyph.Close:
                g.DrawLine(pen, 6.5f, 6.5f, 17.5f, 17.5f);
                g.DrawLine(pen, 17.5f, 6.5f, 6.5f, 17.5f);
                break;
            case Glyph.Grip:
                foreach (float x in new[] { 9f, 15f })
                    foreach (float y in new[] { 6f, 12f, 18f })
                        g.FillEllipse(brush, x - 1.6f, y - 1.6f, 3.2f, 3.2f);
                break;
        }
        g.Restore(saved);
    }

    private static PointF[] Points(params float[] xy)
    {
        var points = new PointF[xy.Length / 2];
        for (int i = 0; i < points.Length; i++)
            points[i] = new PointF(xy[2 * i], xy[2 * i + 1]);
        return points;
    }
}
