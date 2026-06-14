using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenPet;

/// <summary>
/// Samples a small region of the screen at the cursor center to determine
/// whether the underlying background is predominantly dark or light.
///
/// Sampling at cursor (not at pet) avoids self-sampling — the pet orbits
/// 60–118 px away, so the cursor area is always pet-free.
///
/// Uses unsafe LockBits for minimal CPU cost.
/// Runs every <see cref="TickInterval"/> ticks (~450 ms) to stay cheap.
/// </summary>
public sealed class ScreenSampler
{
    private const int   SamplePx       = 28;     // sample area in pixels (28×28)
    private const int   TickInterval   = 100;     // sample every N ticks
    private const float DarkThreshold  = 108f;  // avg luminance below this → dark bg

    private int  _ticks  = 0;
    private bool _isDark = false;   // default: light background → black pet

    /// <summary>True when the background under the cursor is predominantly dark.</summary>
    public bool IsDarkBackground => _isDark;

    /// <summary>Call once per game tick. <paramref name="cursor"/> is the center to sample.</summary>
    public void Update(Point cursor)
    {
        if (++_ticks < TickInterval) return;
        _ticks = 0;
        Sample(cursor);
    }

    private unsafe void Sample(Point center)
    {
        var screen = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);

        int x0 = Math.Clamp(center.X - SamplePx / 2, 0, screen.Width  - SamplePx);
        int y0 = Math.Clamp(center.Y - SamplePx / 2, 0, screen.Height - SamplePx);

        try
        {
            using var bmp = new Bitmap(SamplePx, SamplePx, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
                g.CopyFromScreen(x0, y0, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);

            var data = bmp.LockBits(
                new Rectangle(0, 0, SamplePx, SamplePx),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            float total = 0f;
            int   count = 0;
            int*  ptr   = (int*)data.Scan0;
            int   total_px = SamplePx * SamplePx;

            // Skip every 4th pixel → ~196 samples, negligible CPU cost
            for (int i = 0; i < total_px; i += 4)
            {
                int px = ptr[i];
                int r  = (px >> 16) & 0xFF;
                int gv = (px >>  8) & 0xFF;
                int b  =  px        & 0xFF;
                // Skip pixels that are likely the pet's own rendering (pure B/W)
                if ((r < 8 && gv < 8 && b < 8) || (r > 248 && gv > 248 && b > 248))
                    continue;
                total += 0.299f * r + 0.587f * gv + 0.114f * b;
                count++;
            }

            bmp.UnlockBits(data);

            if (count > 2) // enough data to be meaningful
                _isDark = (total / count) < DarkThreshold;
        }
        catch
        {
            // CopyFromScreen can fail on locked screens, UAC dialogs, etc.
            // Keep the previous value silently.
        }
    }
}
