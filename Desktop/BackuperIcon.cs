using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Backuper.Desktop;

public static class BackuperIcon
{
    // A crisp vault with an upward arrow, drawn at tray resolution instead of
    // scaling a detailed illustration down to sixteen pixels.
    public static Icon Create()
    {
        using var bitmap = new Bitmap(64, 64);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var background = new SolidBrush(Color.FromArgb(26, 36, 58));
        using var accent = new Pen(Color.FromArgb(94, 234, 212), 5) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        g.FillEllipse(background, 1, 1, 62, 62);
        g.DrawLines(accent, [new(15, 34), new(15, 47), new(49, 47), new(49, 34)]);
        g.DrawLine(accent, 32, 37, 32, 17);
        g.DrawLines(accent, [new(22, 27), new(32, 17), new(42, 27)]);
        var handle = bitmap.GetHicon();
        try { using var temporary = Icon.FromHandle(handle); return (Icon)temporary.Clone(); }
        finally { DestroyIcon(handle); }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
