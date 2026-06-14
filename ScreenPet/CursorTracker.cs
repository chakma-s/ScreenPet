using System;
using System.Drawing;

namespace ScreenPet;

/// <summary>
/// Tracks cursor position, velocity, and speed each game tick.
/// Runs on the UI thread — no heap allocation per tick.
/// </summary>
public sealed class CursorTracker
{
    private Point  _prev;
    private PointF _velocity;
    private float  _speed;

    public CursorTracker()
    {
        _prev = Cursor.Position;
    }

    /// <summary>Call once per game tick to update all readings.</summary>
    public void Update()
    {
        var now = Cursor.Position;
        float dx = now.X - _prev.X;
        float dy = now.Y - _prev.Y;

        _velocity = new PointF(dx, dy);
        _speed    = MathF.Sqrt(dx * dx + dy * dy);
        _prev     = now;
    }

    public Point  Position  => _prev;
    public PointF Velocity  => _velocity;
    public float  Speed     => _speed;

    /// <summary>True when the cursor has moved more than 1.5 px this tick.</summary>
    public bool IsMoving    => _speed > 1.5f;
}
