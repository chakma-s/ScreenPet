using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenPet;

/// <summary>
/// Transparent, always-on-top, click-through overlay window — v2.
///
/// Changes vs v1:
///  • OrbitalBody   — pet orbits the cursor in a 62–118 px ring
///  • ScreenSampler — detects dark/light background, switches pet color
///  • GhibliRenderer — fully procedural Ghibli spirit art + 50 expressions
/// </summary>
public sealed class PetWindow : Form
{
    // ── Win32 constants ───────────────────────────────────────────────────────
    private const int WS_EX_LAYERED     = 0x0008_0000;
    private const int WS_EX_TRANSPARENT = 0x0000_0020;
    private const int WS_EX_NOACTIVATE  = 0x0800_0000;
    private const int WS_EX_TOOLWINDOW  = 0x0000_0080;
    private const int ULW_ALPHA         = 0x0000_0002;
    private const byte AC_SRC_OVER      = 0x00;
    private const byte AC_SRC_ALPHA     = 0x01;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT  { public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE   { public int cx, cy; }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BLENDFUNCTION
    {
        public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public int bmiColors;
    }

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst,
        ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc,
        int crKey, ref BLENDFUNCTION pblend, int dwFlags);

    [DllImport("user32.dll",  ExactSpelling = true, SetLastError = true)] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll",  ExactSpelling = true)]                      private static extern int    ReleaseDC(IntPtr hWnd, IntPtr hdc);
    [DllImport("gdi32.dll",   ExactSpelling = true, SetLastError = true)] private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
    [DllImport("gdi32.dll",   ExactSpelling = true)]                      private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
    [DllImport("gdi32.dll",   ExactSpelling = true, SetLastError = true)] private static extern bool   DeleteDC(IntPtr hdc);
    [DllImport("gdi32.dll",   ExactSpelling = true, SetLastError = true)] private static extern bool   DeleteObject(IntPtr hgdiobj);
    [DllImport("gdi32.dll",   SetLastError = true)]                       private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

    // ── Pet dimensions ────────────────────────────────────────────────────────
    private const int PetSize = 100;

    // ── Subsystems ────────────────────────────────────────────────────────────
    private readonly CursorTracker   _cursor;
    private readonly OrbitalBody     _orbital;
    private readonly PetBrain        _brain;
    private readonly GhibliRenderer  _renderer;
    private readonly ScreenSampler   _sampler;
    private readonly StartupManager  _startup;
    private readonly KeyboardTracker _keyboard;

    private readonly System.Windows.Forms.Timer _ticker;
    private NotifyIcon?        _tray;
    private ContextMenuStrip?  _menu;
    private ToolStripMenuItem? _pauseItem;

    // ── DIB Section persistent variables ──────────────────────────────────────
    private IntPtr _screenDc = IntPtr.Zero;
    private IntPtr _memDc = IntPtr.Zero;
    private IntPtr _hDib = IntPtr.Zero;
    private IntPtr _hOldBmp = IntPtr.Zero;
    private Bitmap? _dibBitmap;
    private Graphics? _dibGraphics;

    // ── Constructor ───────────────────────────────────────────────────────────

    public PetWindow()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar   = false;
        TopMost         = true;
        Width           = PetSize;
        Height          = PetSize;
        StartPosition   = FormStartPosition.Manual;

        var cur  = Cursor.Position;
        Location = new Point(cur.X - PetSize / 2, cur.Y - PetSize - 20);

        _cursor   = new CursorTracker();
        _orbital  = new OrbitalBody();
        _brain    = new PetBrain();
        _renderer = new GhibliRenderer(PetSize);
        _sampler  = new ScreenSampler();
        _startup  = new StartupManager("ScreenPet", Application.ExecutablePath);
        _keyboard = new KeyboardTracker();

        _keyboard.KeyPressed += () => _brain.OnKeyPress();

        BuildTray();
        InitDIB();

        _ticker = new System.Windows.Forms.Timer { Interval = 33 };
        _ticker.Tick += OnTick;
        _ticker.Start();
    }

    private void InitDIB()
    {
        _screenDc = GetDC(IntPtr.Zero);
        _memDc    = CreateCompatibleDC(_screenDc);

        BITMAPINFO bmi = new BITMAPINFO();
        bmi.bmiHeader.biSize        = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
        bmi.bmiHeader.biWidth       = PetSize;
        bmi.bmiHeader.biHeight      = -PetSize; // Negative for top-down bitmap
        bmi.bmiHeader.biPlanes      = 1;
        bmi.bmiHeader.biBitCount    = 32;
        bmi.bmiHeader.biCompression = 0; // BI_RGB

        _hDib = CreateDIBSection(_screenDc, ref bmi, 0, out IntPtr pBits, IntPtr.Zero, 0);
        if (_hDib != IntPtr.Zero)
        {
            _hOldBmp     = SelectObject(_memDc, _hDib);
            _dibBitmap   = new Bitmap(PetSize, PetSize, PetSize * 4, PixelFormat.Format32bppArgb, pBits);
            _dibGraphics = Graphics.FromImage(_dibBitmap);
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
            return cp;
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e) { }
    protected override void OnPaint(PaintEventArgs e)           { }

    // ── Game tick (≈33 fps) ───────────────────────────────────────────────────

    private void OnTick(object? sender, EventArgs e)
    {
        // 1. Sample cursor
        _cursor.Update();

        // 2. Brain state + expression
        _brain.Update(_cursor);

        if (_brain.IsPaused) return;

        // 3. Sample background color (throttled internally)
        _sampler.Update(_cursor.Position);

        // 4. Orbital movement (pass isTyping as true if in Playing state)
        _orbital.Update(_cursor.Position, _brain.State == PetState.Playing);

        // 5. Determine facing direction (face right if cursor is to the right of the pet window center)
        _renderer.FacingRight = (_cursor.Position.X > Location.X + PetSize / 2);

        // 6. Advance renderer animation + sync state for sprite selection
        _renderer.CurrentState = _brain.State;
        _renderer.Step();

        // 6. Determine pet color from background
        Color petColor = _sampler.IsDarkBackground ? Color.White : Color.Black;

        // 7. Render Ghibli frame directly onto DIB graphics
        if (_dibGraphics != null)
        {
            _renderer.Render(_dibGraphics, _brain.CurrentExpression, petColor);
        }

        // 8. Position window (center the pet on orbital position)
        var pos = _orbital.Position;
        var sc  = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
        int wx  = Math.Clamp((int)(pos.X - PetSize / 2f), 0, sc.Width  - PetSize);
        int wy  = Math.Clamp((int)(pos.Y - PetSize / 2f), 0, sc.Height - PetSize);
        Location = new Point(wx, wy);

        // 9. Blit via UpdateLayeredWindow
        BlitFrame();
    }

    // ── UpdateLayeredWindow rendering ─────────────────────────────────────────

    private void BlitFrame()
    {
        if (!IsHandleCreated || _screenDc == IntPtr.Zero || _memDc == IntPtr.Zero) return;

        var pos    = new POINT { x = Location.X, y = Location.Y };
        var size   = new SIZE  { cx = PetSize,    cy = PetSize    };
        var srcPos = new POINT { x = 0,           y = 0           };
        var blend  = new BLENDFUNCTION
        {
            BlendOp             = AC_SRC_OVER,
            BlendFlags          = 0,
            SourceConstantAlpha = 255,
            AlphaFormat         = AC_SRC_ALPHA,
        };

        UpdateLayeredWindow(Handle, _screenDc, ref pos, ref size,
            _memDc, ref srcPos, 0, ref blend, ULW_ALPHA);
    }

    // ── System tray ──────────────────────────────────────────────────────────

    private void BuildTray()
    {
        _menu = new ContextMenuStrip();

        _pauseItem = new ToolStripMenuItem("⏸  Pause Pet");
        _pauseItem.Click += (_, _) =>
        {
            _brain.IsPaused = !_brain.IsPaused;
            Visible          = !_brain.IsPaused;
            _pauseItem.Text  = _brain.IsPaused ? "▶  Resume Pet" : "⏸  Pause Pet";
        };

        var startupItem = new ToolStripMenuItem("🚀  Start with Windows")
        {
            Checked      = _startup.IsEnabled,
            CheckOnClick = true,
        };
        startupItem.CheckedChanged += (_, _) =>
        {
            if (startupItem.Checked) _startup.Enable();
            else                     _startup.Disable();
        };

        var exitItem = new ToolStripMenuItem("❌  Exit ScreenPet");
        exitItem.Click += (_, _) => { _tray?.Dispose(); Application.Exit(); };

        _menu.Items.Add(_pauseItem);
        _menu.Items.Add(startupItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(exitItem);

        _tray = new NotifyIcon
        {
            Text             = "ScreenPet 🐾",
            Icon             = BuildTrayIcon(),
            ContextMenuStrip = _menu,
            Visible          = true,
        };
        _tray.DoubleClick += (_, _) => _pauseItem.PerformClick();
    }

    private static Icon BuildTrayIcon()
    {
        using var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var amber = Color.FromArgb(255, 220, 130, 40);
            var dark  = Color.FromArgb(255,  80,  40,  0);
            using var fa = new SolidBrush(amber);
            using var fd = new SolidBrush(dark);
            g.FillEllipse(fa, 8, 12, 16, 16);
            g.FillEllipse(fd, 10, 14, 12, 12);
            g.FillEllipse(fa, 5,  6, 7, 7);
            g.FillEllipse(fa, 14, 4, 7, 7);
            g.FillEllipse(fa, 21, 7, 7, 7);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _ticker.Dispose();
            _renderer.Dispose();
            _tray?.Dispose();
            _menu?.Dispose();
            _keyboard.Dispose();

            _dibGraphics?.Dispose();
            _dibBitmap?.Dispose();
        }

        if (_memDc != IntPtr.Zero)
        {
            if (_hOldBmp != IntPtr.Zero)
            {
                SelectObject(_memDc, _hOldBmp);
                _hOldBmp = IntPtr.Zero;
            }
            DeleteDC(_memDc);
            _memDc = IntPtr.Zero;
        }

        if (_hDib != IntPtr.Zero)
        {
            DeleteObject(_hDib);
            _hDib = IntPtr.Zero;
        }

        if (_screenDc != IntPtr.Zero)
        {
            ReleaseDC(IntPtr.Zero, _screenDc);
            _screenDc = IntPtr.Zero;
        }

        base.Dispose(disposing);
    }
}
