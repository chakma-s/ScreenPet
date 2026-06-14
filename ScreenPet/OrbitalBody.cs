using System;
using System.Drawing;

namespace ScreenPet;

/// <summary>
/// Moves the pet in a continuous natural orbit within an annular ring
/// around the cursor.  Inner and outer radii define the "move in this range"
/// band shown in the reference diagram.
///
/// Coordinates: polar (angle + radius) relative to cursor.
/// Position smoothing (lerp) absorbs sudden cursor jumps.
/// Zero threads — runs on the UI message-pump timer.
/// </summary>
public sealed class OrbitalBody
{
    // ─── Ring boundaries ──────────────────────────────────────────────────────
    public const float InnerRadius = 62f;   // px — inner circle
    public const float OuterRadius = 118f;  // px — outer circle

    // ─── Orbital dynamics ─────────────────────────────────────────────────────
    private const float BaseAngSpeed = 0.006f;  // rad/tick  (~1 full orbit in ~1050 ticks ≈ 35s)
    private const float WobbleSpeed  = 0.006f;  // phase of angular-speed noise
    private const float WobbleAmp   = 0.009f;  // amplitude of angular-speed noise
    private const float RadiusBobSpd = 0.008f;  // sinusoidal radius oscillation speed
    private const float SmoothFactor = 0.18f;  // position lerp — lower = more lag (spring feel)

    private float  _angle;
    private float  _wobblePhase;
    private float  _radiusPhase;
    private PointF _smoothPos;
    private PointF _prevSmooth;

    public PointF Position  { get; private set; }
    public float  VelocityX { get; private set; }
    public float  VelocityY { get; private set; }

    public OrbitalBody()
    {
        var rng     = new Random();
        _angle       = (float)(rng.NextDouble() * MathF.Tau);
        _wobblePhase = (float)(rng.NextDouble() * MathF.Tau);
        _radiusPhase = (float)(rng.NextDouble() * MathF.Tau);
    }

    /// <summary>Advance one tick around <paramref name="cursor"/>.</summary>
    public void Update(Point cursor, bool isTyping)
    {
        float rawX, rawY;

        if (isTyping)
        {
            // Force the dinosaur strictly to the right side of the cursor (90px) so it does not block typed text
            rawX = cursor.X + 90f;
            rawY = cursor.Y;
        }
        else
        {
            // ── Angular velocity with gentle wobble ───────────────────────────
            _wobblePhase  += WobbleSpeed;
            float angSpeed = BaseAngSpeed + MathF.Sin(_wobblePhase) * WobbleAmp;
            _angle        += angSpeed;

            // ── Radius oscillates sinusoidally within the ring ────────────────
            _radiusPhase  += RadiusBobSpd;
            float mid      = (InnerRadius + OuterRadius) * 0.5f;
            float half     = (OuterRadius - InnerRadius) * 0.5f;
            float radius   = mid + MathF.Sin(_radiusPhase) * half * 0.88f;

            // ── Raw orbital position (center of pet) ──────────────────────────
            rawX = cursor.X + MathF.Cos(_angle) * radius;
            rawY = cursor.Y + MathF.Sin(_angle) * radius;
        }

        var raw = new PointF(rawX, rawY);

        // ── Smooth toward raw position (absorbs cursor teleports) ─────────
        if (_smoothPos == PointF.Empty) _smoothPos = raw;
        _prevSmooth = _smoothPos;
        _smoothPos  = new PointF(
            _smoothPos.X + (raw.X - _smoothPos.X) * SmoothFactor,
            _smoothPos.Y + (raw.Y - _smoothPos.Y) * SmoothFactor);

        VelocityX = _smoothPos.X - _prevSmooth.X;
        VelocityY = _smoothPos.Y - _prevSmooth.Y;
        Position  = _smoothPos;
    }
}
