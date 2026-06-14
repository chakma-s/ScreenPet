using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ScreenPet;

/// <summary>
/// Renders a Ghibli-style dog character using real illustrated sprites as the body,
/// with procedurally drawn expression overlays (eyes, brows, mouth, extras) on top.
///
/// Sprites: dog_idle.png, dog_walk.png, dog_sleep.png, dog_react.png
/// If sprites are missing, falls back to a procedural spirit creature.
///
/// Expression overlays adapt color to remain visible on any background.
/// Dog art is always drawn in its natural illustrated colors.
/// </summary>
public sealed class GhibliRenderer : IDisposable
{
    public int Size { get; }

    // ── Dinosaur sprite structures (pre-cached left/right black/white) ───────
    private DinoSprite? _dinoIdle;
    private DinoSprite? _dinoWalk;
    private DinoSprite? _dinoSleep;
    private DinoSprite? _dinoReact;
    private DinoSprite? _dinoType;
    private DinoSprite? _dinoClick;
    private bool        _spritesLoaded;

    public bool FacingRight { get; set; } = false;

    // ── Expression overlay face layout (as fraction of Size) ─────────────────
    // These are tuned for the generated Ghibli dog sprites (face in upper-center)
    private const float FaceXFrac   = 0.50f;  // face center X
    private const float FaceYFrac   = 0.36f;  // face center Y
    private const float EyeSpreadFx = 0.18f;  // half-distance between eyes
    private const float EyeYFrac    = 0.33f;  // eye center Y
    private const float EyeRFrac    = 0.085f; // eye radius
    private const float MouthYFrac  = 0.46f;  // mouth center Y
    private const float MouthWFrac  = 0.16f;  // mouth half-width
    private const float BrowOffFrac = 0.085f; // how far brows are above eyes

    // ── Cached resources ──────────────────────────────────────────────────────
    private readonly Font _zzFont;
    private readonly Font _symbolFont;
    private readonly Font _markFont;

    // ── Animation phases ──────────────────────────────────────────────────────
    private float _breathPhase;
    private float _extraPhase;
    private float _wagPhase;      // tail wag (if applicable)
    private int   _frameIdx;
    private int   _frameTick;

    // ── Current state (for sprite selection) ──────────────────────────────────
    public PetState CurrentState { get; set; } = PetState.Idle;

    // ─────────────────────────────────────────────────────────────────────────

    public GhibliRenderer(int size = 80)
    {
        Size        = size;
        _zzFont     = new Font("Arial",           7f,  FontStyle.Bold,    GraphicsUnit.Point);
        _symbolFont = new Font("Segoe UI Symbol", 11f, FontStyle.Regular, GraphicsUnit.Point);
        _markFont   = new Font("Arial",           12f, FontStyle.Bold,    GraphicsUnit.Point);

        LoadSprites();
    }

    // ── Sprite loading ────────────────────────────────────────────────────────

    private void LoadSprites()
    {
        var idleLeft  = TryLoadEmbeddedSprite("dino_idle.png");
        var walkLeft  = TryLoadEmbeddedSprite("dino_walk.png");
        var sleepLeft = TryLoadEmbeddedSprite("dino_sleep.png");
        var reactLeft = TryLoadEmbeddedSprite("dino_react.png");
        var typeLeft  = TryLoadEmbeddedSprite("dino_type.png");
        var clickLeft = TryLoadEmbeddedSprite("dino_click.png");

        if (idleLeft != null)  _dinoIdle  = new DinoSprite(idleLeft);
        if (walkLeft != null)  _dinoWalk  = new DinoSprite(walkLeft);
        if (sleepLeft != null) _dinoSleep = new DinoSprite(sleepLeft);
        if (reactLeft != null) _dinoReact = new DinoSprite(reactLeft);
        if (typeLeft != null)  _dinoType  = new DinoSprite(typeLeft);
        if (clickLeft != null) _dinoClick = new DinoSprite(clickLeft);

        _spritesLoaded = _dinoIdle != null;
    }

    private static unsafe Bitmap? TryLoadEmbeddedSprite(string resourceName)
    {
        try
        {
            var assembly = typeof(GhibliRenderer).Assembly;
            string fullName = $"ScreenPet.Assets.sprites.{resourceName}";
            using var stream = assembly.GetManifestResourceStream(fullName);
            if (stream == null) return null;

            using var src = new Bitmap(stream);

            // Convert to 32bpp ARGB so we can do per-pixel alpha
            var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
                g.DrawImage(src, 0, 0);

            // Dynamic luminance-to-alpha conversion: Crops the paper background and makes the edges anti-aliased and transparent
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int*  ptr    = (int*)data.Scan0;
            int   total  = bmp.Width * bmp.Height;

            for (int i = 0; i < total; i++)
            {
                int px = ptr[i];
                int r  = (px >> 16) & 0xFF;
                int g2 = (px >>  8) & 0xFF;
                int b  =  px        & 0xFF;

                float luma = 0.299f * r + 0.587f * g2 + 0.114f * b;

                // Threshold: anything brighter than 215 is paper background and becomes transparent
                if (luma >= 215f)
                {
                    ptr[i] = 0; // transparent
                }
                else
                {
                    // Map dark pencil lines to opaque (alpha = 255), and interpolate using a power curve (gamma) to keep lines thin and sharp
                    float t = (215f - luma) / (215f - 70f);
                    t = Math.Clamp(t, 0f, 1f);
                    byte alpha = (byte)(MathF.Pow(t, 2.5f) * 255);
                    
                    ptr[i] = (alpha << 24) | (r << 16) | (g2 << 8) | b;
                }
            }
            bmp.UnlockBits(data);
            return bmp;
        }
        catch { return null; }
    }

    // ── Animation step ────────────────────────────────────────────────────────

    public void Step()
    {
        _breathPhase += 0.046f;
        _extraPhase  += 0.075f;
        _wagPhase    += 0.12f;

        // Frame cycling for walk animation (8 ticks per frame)
        if (++_frameTick >= 8) { _frameTick = 0; _frameIdx = (_frameIdx + 1) % 4; }
    }

    // ── Main render ───────────────────────────────────────────────────────────

    /// <summary>Draws the dino directly onto the provided Graphics context — no frame allocation.</summary>
    public void Render(Graphics g, in Expression expr, Color petColor)
    {
        g.Clear(Color.Transparent);
        g.SmoothingMode      = SmoothingMode.AntiAlias;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.InterpolationMode  = InterpolationMode.HighQualityBicubic;

        if (_spritesLoaded)
            RenderDino(g, expr, petColor);
        else
            RenderFallbackSpirit(g, expr, petColor);
    }

    // ── Dinosaur rendering (sprite only, outline adaptive inversion) ─────────

    private void RenderDino(Graphics g, in Expression expr, Color petColor)
    {
        // Select sprite structure based on state
        DinoSprite? dinoSprite = CurrentState switch
        {
            PetState.Sleeping  => _dinoSleep ?? _dinoIdle,
            PetState.Reacting  => _dinoReact ?? _dinoIdle,
            PetState.Playing   => _dinoType  ?? _dinoIdle,
            PetState.Following => _dinoWalk  ?? _dinoIdle,
            _                  => _dinoIdle,
        };

        if (dinoSprite == null) return;

        // Select the pre-cached left/right black/white bitmap
        bool useWhite = (petColor == Color.White);
        Bitmap? sprite = FacingRight
            ? (useWhite ? dinoSprite.RightWhite : dinoSprite.RightBlack)
            : (useWhite ? dinoSprite.LeftWhite  : dinoSprite.LeftBlack);

        if (sprite == null) return;

        // Save Graphics state before transformations
        var state = g.Save();

        // ── Breathing scale ───────────────────────────────────────────────
        float breath = 1f + MathF.Sin(_breathPhase) * 0.015f;
        int   drawW  = (int)(Size * breath);
        int   drawH  = (int)(Size * breath);
        int   drawX  = (Size - drawW) / 2;
        int   drawY  = (Size - drawH) / 2;

        // ── Apply dynamic walking/movement transformations ────────────────
        if (CurrentState == PetState.Following)
        {
            // Waddling walking animation: bob up and down, tilt side to side
            float walkBob = MathF.Abs(MathF.Sin(_breathPhase * 2.5f)) * 3.5f;
            float walkTilt = MathF.Sin(_breathPhase * 2.5f) * 5f;
            g.TranslateTransform(Size / 2f, Size / 2f);
            g.TranslateTransform(0f, -walkBob);
            g.RotateTransform(walkTilt);
            g.TranslateTransform(-Size / 2f, -Size / 2f);
        }
        else if (CurrentState == PetState.Reacting)
        {
            // Rapid clicking mouse animation: rapid vertical bobbing
            float clickBob = MathF.Abs(MathF.Sin(_breathPhase * 18f)) * 3.5f;
            g.TranslateTransform(Size / 2f, Size / 2f);
            g.TranslateTransform(0f, -clickBob);
            g.TranslateTransform(-Size / 2f, -Size / 2f);
        }
        else if (CurrentState == PetState.Playing)
        {
            // Keyboard typing animation: rapid high-frequency hand/body jitter
            float typeJitterX = MathF.Sin(_breathPhase * 16f) * 1.5f;
            float typeJitterY = MathF.Cos(_breathPhase * 16f) * 1.0f;
            g.TranslateTransform(Size / 2f, Size / 2f);
            g.TranslateTransform(typeJitterX, typeJitterY);
            g.TranslateTransform(-Size / 2f, -Size / 2f);
        }
        else if (CurrentState == PetState.Sleeping)
        {
            // Gentle breathing bob
            float sleepBob = MathF.Sin(_breathPhase * 0.7f) * 1.2f;
            g.TranslateTransform(Size / 2f, Size / 2f);
            g.TranslateTransform(0f, sleepBob);
            g.TranslateTransform(-Size / 2f, -Size / 2f);
        }

        // ── Draw dino sprite directly (100% hardware blit, zero runtime CPU overhead) ──
        g.DrawImage(sprite, new Rectangle(drawX, drawY, drawW, drawH));

        // Restore Graphics state so extra overlays are not rotated/bobbed
        g.Restore(state);

        // ── Non-facial extra effects overlay ──────────────────────────────
        // Zzz sleeping animation bubbles
        if (CurrentState == PetState.Sleeping)
        {
            DrawExtras(g, ExtraEffect.Zzz, petColor, Size * 0.5f, Size * 0.3f, Size * 0.35f, _extraPhase);
        }
        else
        {
            // Other extras drawn outside the face
            DrawExtras(g, expr.Extra, petColor, Size * 0.5f, Size * 0.35f, Size * 0.36f, _extraPhase);
        }
    }

    private static Color GetShine(Color petColor) =>
        petColor.GetBrightness() > 0.5f
            ? Color.FromArgb(130, 15, 15, 15)
            : Color.FromArgb(150, 240, 240, 240);

    // ═════════════════════════════════════════════════════════════════════════
    // Fallback: procedural spirit creature (when no sprite files found)
    // ═════════════════════════════════════════════════════════════════════════

    private void RenderFallbackSpirit(Graphics g, in Expression expr, Color petColor)
    {
        float cx = Size * 0.5f;
        float cy = Size * 0.46f;
        float breath = 1f + MathF.Sin(_breathPhase) * 0.018f;
        float r = (Size * 0.36f) * breath;

        var bodyFill = Color.FromArgb(16,  petColor);
        var bodyLine = Color.FromArgb(195, petColor);
        Color shineCol = GetShine(petColor);

        using var fillBr  = new SolidBrush(bodyFill);
        using var linePen = new Pen(bodyLine, 2.4f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };

        DrawWisps(g, fillBr, linePen, cx, cy + r * 0.82f, r);
        float earR = r * 0.19f;
        DrawEarBump(g, fillBr, linePen, cx - r * 0.44f, cy - r * 0.90f, earR);
        DrawEarBump(g, fillBr, linePen, cx + r * 0.44f, cy - r * 0.90f, earR);

        using var bodyPath = MakeBodyPath(cx, cy, r);
        g.FillPath(fillBr, bodyPath);
        g.DrawPath(linePen, bodyPath);

        var bellyCol = Color.FromArgb(40, petColor);
        using var bellyBr = new SolidBrush(bellyCol);
        float dr = r * 0.092f;
        g.FillEllipse(bellyBr, cx - r*0.24f - dr, cy + r*0.22f - dr, dr*2, dr*2);
        g.FillEllipse(bellyBr, cx + r*0.24f - dr, cy + r*0.22f - dr, dr*2, dr*2);
        g.FillEllipse(bellyBr, cx - dr*0.85f,     cy + r*0.40f - dr*0.85f, dr*1.7f, dr*1.7f);

        float eyeX1 = cx - r * 0.33f, eyeX2 = cx + r * 0.33f;
        float eyeY = cy - r * 0.09f, eyeR = r * 0.215f;

        DrawEye(g, expr.LeftEye,  petColor, shineCol, eyeX1, eyeY, eyeR);
        DrawEye(g, expr.RightEye, petColor, shineCol, eyeX2, eyeY, eyeR);
        if (expr.LeftBrow  != BrowShape.None) DrawBrow(g, expr.LeftBrow,  petColor, eyeX1, eyeY - eyeR * 1.35f, eyeR, false);
        if (expr.RightBrow != BrowShape.None) DrawBrow(g, expr.RightBrow, petColor, eyeX2, eyeY - eyeR * 1.35f, eyeR, true);
        DrawMouth(g, expr.Mouth, petColor, cx, cy + r * 0.39f, r * 0.35f);
        DrawExtras(g, expr.Extra, petColor, cx, cy, r, _extraPhase);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Eye drawing (14 shapes)
    // ═════════════════════════════════════════════════════════════════════════

    private static void DrawEye(Graphics g, EyeShape shape, Color col, Color shine,
        float cx, float cy, float r)
    {
        var iris  = Color.FromArgb(22,  col);
        var pupil = Color.FromArgb(225, col);
        var eo    = Color.FromArgb(205, col);

        using var irisBr  = new SolidBrush(iris);
        using var pupilBr = new SolidBrush(pupil);
        using var shineBr = new SolidBrush(shine);
        using var ep      = new Pen(eo, 2.0f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        switch (shape)
        {
            case EyeShape.Circle:
                g.FillEllipse(irisBr, cx-r, cy-r, r*2, r*2);
                g.DrawEllipse(ep, cx-r, cy-r, r*2, r*2);
                float pr=r*.50f, sr=pr*.38f;
                g.FillEllipse(pupilBr, cx-pr, cy-pr, pr*2, pr*2);
                g.FillEllipse(shineBr, cx-pr*.25f, cy-pr*.5f, sr*2, sr*2);
                break;
            case EyeShape.Wide:
                float wr=r*1.28f, wp=wr*.52f, ws=wp*.36f;
                g.FillEllipse(irisBr, cx-wr, cy-wr, wr*2, wr*2);
                g.DrawEllipse(ep, cx-wr, cy-wr, wr*2, wr*2);
                g.FillEllipse(pupilBr, cx-wp, cy-wp, wp*2, wp*2);
                g.FillEllipse(shineBr, cx-wp*.22f, cy-wp*.5f, ws*2, ws*2);
                break;
            case EyeShape.Happy:
                g.DrawArc(ep, cx-r, cy-r*.28f, r*2, r*1.3f, 200, 140);
                break;
            case EyeShape.Sad:
                float sr2=r*.9f, sp=sr2*.45f;
                g.FillEllipse(irisBr, cx-sr2, cy-sr2*.85f, sr2*2, sr2*1.7f);
                g.DrawEllipse(ep, cx-sr2, cy-sr2*.85f, sr2*2, sr2*1.7f);
                g.FillEllipse(pupilBr, cx-sp, cy, sp*2, sp*2);
                break;
            case EyeShape.ClosedFlat:
                g.DrawLine(ep, cx-r, cy, cx+r, cy);
                break;
            case EyeShape.ClosedCurve:
                g.DrawArc(ep, cx-r, cy-r*.55f, r*2, r*1.3f, 10, 160);
                break;
            case EyeShape.Heart:
                FillHeart(g, Color.FromArgb(200, col), cx, cy, r);
                break;
            case EyeShape.Star:
                FillStar(g, Color.FromArgb(210, col), cx, cy, r, 5);
                break;
            case EyeShape.Dot:
                float dr=r*.38f;
                g.FillEllipse(pupilBr, cx-dr, cy-dr, dr*2, dr*2);
                break;
            case EyeShape.Spiral:
                for (float rs=r; rs>r*.18f; rs-=r*.26f)
                    g.DrawArc(ep, cx-rs, cy-rs, rs*2, rs*2, 0, 270);
                break;
            case EyeShape.X:
                float xd=r*.74f;
                g.DrawLine(ep, cx-xd, cy-xd, cx+xd, cy+xd);
                g.DrawLine(ep, cx+xd, cy-xd, cx-xd, cy+xd);
                break;
            case EyeShape.Wink:
                g.DrawArc(ep, cx-r, cy-r*.45f, r*2, r*1.1f, 15, 150);
                break;
            case EyeShape.Sleepy:
                var sv = g.Clip;
                g.SetClip(new RectangleF(cx-r*1.4f, cy-r*.1f, r*2.8f, r*1.6f));
                g.FillEllipse(irisBr, cx-r, cy-r, r*2, r*2);
                g.DrawEllipse(ep, cx-r, cy-r, r*2, r*2);
                g.Clip = sv;
                g.DrawLine(ep, cx-r*.88f, cy, cx+r*.88f, cy);
                break;
            case EyeShape.Teary:
                g.FillEllipse(irisBr, cx-r, cy-r, r*2, r*2);
                g.DrawEllipse(ep, cx-r, cy-r, r*2, r*2);
                float tp=r*.48f;
                g.FillEllipse(pupilBr, cx-tp, cy-tp, tp*2, tp*2);
                g.FillEllipse(shineBr, cx-tp*.2f, cy-tp*.45f, tp*.7f, tp*.7f);
                DrawTear(g, Color.FromArgb(160, 80, 140, 210), cx+r*.3f, cy+r*.7f, r*.22f);
                break;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Eyebrow drawing
    // ═════════════════════════════════════════════════════════════════════════

    private static void DrawBrow(Graphics g, BrowShape shape, Color col,
        float cx, float cy, float eyeR, bool isRight)
    {
        using var p = new Pen(Color.FromArgb(210, col), 2.0f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        float w      = eyeR * 1.45f;
        float innerX = isRight ? cx - w*.5f : cx + w*.5f;
        float outerX = isRight ? cx + w*.5f : cx - w*.5f;

        switch (shape)
        {
            case BrowShape.Normal:     g.DrawLine(p, cx-w*.5f, cy, cx+w*.5f, cy); break;
            case BrowShape.Raised:     g.DrawArc(p, cx-w*.65f, cy-eyeR*.55f, w*1.3f, eyeR*1.1f, 205, 130); break;
            case BrowShape.HighRaised: g.DrawLine(p, cx-w*.5f, cy-eyeR*.35f, cx+w*.5f, cy-eyeR*.35f); break;
            case BrowShape.Furrowed:   g.DrawLine(p, outerX, cy, innerX, cy+eyeR*.38f); break;
            case BrowShape.Angry:      g.DrawLine(p, outerX, cy, innerX, cy+eyeR*.60f); break;
            case BrowShape.Sad:        g.DrawLine(p, outerX, cy+eyeR*.38f, innerX, cy); break;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Mouth drawing (13 shapes)
    // ═════════════════════════════════════════════════════════════════════════

    private static void DrawMouth(Graphics g, MouthShape shape, Color col,
        float cx, float cy, float w)
    {
        using var p = new Pen(Color.FromArgb(210, col), 2.0f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        switch (shape)
        {
            case MouthShape.Smile:    g.DrawArc(p, cx-w, cy-w*.45f, w*2, w*.9f, 18, 144); break;
            case MouthShape.BigSmile: g.DrawArc(p, cx-w*1.2f, cy-w*.55f, w*2.4f, w*1.1f, 20, 140); break;
            case MouthShape.GrinTeeth:
            {
                using var ap = new GraphicsPath();
                ap.AddArc(cx-w*1.15f, cy-w*.45f, w*2.3f, w*.9f, 18, 144);
                g.DrawPath(p, ap);
                var tr = new RectangleF(cx-w*.75f, cy-w*.04f, w*1.5f, w*.38f);
                using var tb = new SolidBrush(Color.FromArgb(200, col));
                g.FillRectangle(tb, tr);
                using var tp2 = new Pen(Color.FromArgb(90, col), 0.8f);
                for (int i=1; i<4; i++) g.DrawLine(tp2, cx-w*.75f+w*.375f*i, cy-w*.04f, cx-w*.75f+w*.375f*i, cy+w*.34f);
                break;
            }
            case MouthShape.Frown:    g.DrawArc(p, cx-w, cy, w*2, w*.9f, 200, 140); break;
            case MouthShape.SmallO:
            {
                float r2=w*.28f;
                using var ob=new SolidBrush(Color.FromArgb(15, col));
                g.FillEllipse(ob, cx-r2, cy-r2*.8f, r2*2, r2*1.6f);
                g.DrawEllipse(p, cx-r2, cy-r2*.8f, r2*2, r2*1.6f); break;
            }
            case MouthShape.BigO:
            {
                float r2=w*.42f;
                using var ob=new SolidBrush(Color.FromArgb(15, col));
                g.FillEllipse(ob, cx-r2, cy-r2, r2*2, r2*2);
                g.DrawEllipse(p, cx-r2, cy-r2, r2*2, r2*2); break;
            }
            case MouthShape.Flat:    g.DrawLine(p, cx-w*.68f, cy, cx+w*.68f, cy); break;
            case MouthShape.Smirk:
                using (var sp=new GraphicsPath())
                { sp.AddBezier(cx-w*.5f,cy, cx,cy, cx+w*.38f,cy, cx+w*.5f,cy+w*.28f); g.DrawPath(p,sp); } break;
            case MouthShape.CatMouth:
            {
                float aw=w*.42f;
                g.DrawArc(p, cx-aw*2.15f, cy-aw*.65f, aw*1.95f, aw*1.3f, 25, 130);
                g.DrawArc(p, cx+aw*.2f,   cy-aw*.65f, aw*1.95f, aw*1.3f, 25, 130);
                g.DrawLine(p, cx-aw*.2f, cy, cx+aw*.2f, cy); break;
            }
            case MouthShape.Tongue:
            {
                g.DrawArc(p, cx-w, cy-w*.45f, w*2, w*.9f, 18, 144);
                float tr=w*.28f;
                using var tb=new SolidBrush(Color.FromArgb(155, 220, 80, 100));
                using var tp2=new Pen(Color.FromArgb(170, 190, 60, 80), 1.4f);
                g.FillEllipse(tb, cx-tr, cy+w*.08f, tr*2, tr*1.85f);
                g.DrawEllipse(tp2, cx-tr, cy+w*.08f, tr*2, tr*1.85f); break;
            }
            case MouthShape.Tremble:
            {
                var pts=new PointF[8];
                for (int i=0;i<=7;i++) pts[i]=new PointF(cx-w*.7f+w*1.4f*(i/7f), cy+MathF.Sin(i/7f*MathF.PI*4)*w*.1f);
                g.DrawLines(p, pts); break;
            }
            case MouthShape.Zigzag:
                g.DrawLines(p, new PointF[]{new(cx-w*.7f,cy),new(cx-w*.35f,cy+w*.3f),new(cx,cy),new(cx+w*.35f,cy+w*.3f),new(cx+w*.7f,cy)}); break;
            case MouthShape.TinySmile:
                g.DrawArc(p, cx-w*.48f, cy-w*.22f, w*.96f, w*.44f, 20, 140); break;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Extra effects (15 effects)
    // ═════════════════════════════════════════════════════════════════════════

    private void DrawExtras(Graphics g, ExtraEffect fx, Color col,
        float cx, float cy, float r, float phase)
    {
        switch (fx)
        {
            case ExtraEffect.Blush:
            case ExtraEffect.BlushStrong:
            {
                float a=fx==ExtraEffect.BlushStrong?72:46, br=r*(fx==ExtraEffect.BlushStrong?.30f:.23f);
                using var bb=new SolidBrush(Color.FromArgb((int)a, 225,80,105));
                g.FillEllipse(bb, cx-r*.70f-br, cy+r*.06f-br*.6f, br*2, br*1.2f);
                g.FillEllipse(bb, cx+r*.70f-br, cy+r*.06f-br*.6f, br*2, br*1.2f); break;
            }
            case ExtraEffect.Sweat:
            case ExtraEffect.SweatBig:
                DrawTear(g, Color.FromArgb(175,90,155,225), cx+r*.82f, cy-r*.52f, r*(fx==ExtraEffect.SweatBig?.20f:.13f)); break;
            case ExtraEffect.Sparkles:
            {
                var sc=Color.FromArgb(195, col);
                for (int i=0;i<5;i++) { float a=phase+i*MathF.Tau/5; FillStar(g,sc,cx+MathF.Cos(a)*r*1.18f,cy+MathF.Sin(a)*r*1.18f,r*.09f,4); } break;
            }
            case ExtraEffect.Hearts:
            {
                var hc=Color.FromArgb(185,215,75,108);
                for (int i=0;i<3;i++) { float a=phase*.65f+i*MathF.Tau/3; FillHeart(g,hc,cx+MathF.Cos(a)*r*1.12f,cy+MathF.Sin(a)*r*1.12f,r*.09f); } break;
            }
            case ExtraEffect.Zzz:
            {
                using var zb=new SolidBrush(Color.FromArgb(165, col));
                for (int i=0;i<3;i++)
                {
                    float sc=.6f+i*.2f;
                    using var zf=new Font("Arial", _zzFont.Size*sc, FontStyle.Bold, GraphicsUnit.Point);
                    g.DrawString("Z", zf, zb, cx+r*.5f+i*r*.18f, cy-r*.85f-i*r*.22f+MathF.Sin(phase+i)*r*.06f);
                } break;
            }
            case ExtraEffect.QuestionMark:
                using (var mb=new SolidBrush(Color.FromArgb(185, col))) g.DrawString("?", _markFont, mb, cx+r*.28f, cy-r*1.15f); break;
            case ExtraEffect.ExclamationMark:
                using (var mb=new SolidBrush(Color.FromArgb(185, col))) g.DrawString("!", _markFont, mb, cx+r*.22f, cy-r*1.15f); break;
            case ExtraEffect.MusicNote:
            {
                using var nb=new SolidBrush(Color.FromArgb(175, col));
                g.DrawString("♪", _symbolFont, nb, cx+r*.55f+MathF.Sin(phase)*r*.08f, cy-r*.88f-MathF.Cos(phase*.45f)*r*.1f); break;
            }
            case ExtraEffect.Anger:
            {
                using var ap=new Pen(Color.FromArgb(185, col), 2f);
                float ax=cx-r*.52f, ay=cy-r*.78f, as2=r*.16f;
                g.DrawLine(ap,ax-as2,ay-as2,ax+as2,ay+as2); g.DrawLine(ap,ax+as2,ay-as2,ax-as2,ay+as2);
                g.DrawLine(ap,ax,ay-as2,ax,ay+as2);          g.DrawLine(ap,ax-as2,ay,ax+as2,ay); break;
            }
            case ExtraEffect.Dizzy:
            {
                using var dp=new Pen(Color.FromArgb(155, col), 1.8f);
                for (int i=0;i<3;i++) { float a=phase+i*MathF.Tau/3, ds=r*.115f; g.DrawEllipse(dp,cx+MathF.Cos(a)*r*1.07f-ds,cy+MathF.Sin(a)*r*1.07f-ds,ds*2,ds*2); } break;
            }
            case ExtraEffect.TearDrop:
                DrawTear(g, Color.FromArgb(158,75,135,210), cx+r*.38f, cy+r*.52f, r*.105f); break;
            case ExtraEffect.TearStream:
            {
                var tc=Color.FromArgb(155,75,135,210);
                for (int i=0;i<3;i++) DrawTear(g,tc,cx+(i%2==0?-1:1)*r*.38f,cy+r*(.5f+i*.22f),r*.085f); break;
            }
            case ExtraEffect.StarCircle:
            {
                var sc=Color.FromArgb(200, col);
                for (int i=0;i<6;i++) { float a=phase+i*MathF.Tau/6; FillStar(g,sc,cx+MathF.Cos(a)*r*1.22f,cy+MathF.Sin(a)*r*1.22f,r*.08f,5); } break;
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Shape helpers (shared by dog and fallback modes)
    // ═════════════════════════════════════════════════════════════════════════

    private static void FillHeart(Graphics g, Color col, float cx, float cy, float r)
    {
        using var path = new GraphicsPath();
        float s = r * 1.1f;
        path.AddBezier(cx, cy+s*.4f, cx-s*2f, cy-s*1.0f, cx-s*2f, cy+s*.3f, cx, cy+s*1.5f);
        path.AddBezier(cx, cy+s*1.5f, cx+s*2f, cy+s*.3f, cx+s*2f, cy-s*1.0f, cx, cy+s*.4f);
        path.CloseFigure();
        using var b = new SolidBrush(col);
        g.FillPath(b, path);
    }

    private static void FillStar(Graphics g, Color col, float cx, float cy, float r, int pts)
    {
        var points = new PointF[pts * 2];
        float inner = r * 0.42f;
        for (int i = 0; i < pts*2; i++)
        {
            float a = MathF.PI * i / pts - MathF.PI / 2;
            float rad = i%2==0 ? r : inner;
            points[i] = new PointF(cx + MathF.Cos(a)*rad, cy + MathF.Sin(a)*rad);
        }
        using var b = new SolidBrush(col);
        g.FillPolygon(b, points);
    }

    private static void DrawTear(Graphics g, Color col, float cx, float ty, float r)
    {
        using var path = new GraphicsPath();
        path.AddArc(cx-r, ty, r*2, r*2, 180, 180);
        float br = r*.7f;
        path.AddBezier(cx+r, ty+r, cx+br, ty+r*3f, cx+br*.2f, ty+r*3.4f, cx, ty+r*3.5f);
        path.AddBezier(cx, ty+r*3.5f, cx-br*.2f, ty+r*3.4f, cx-br, ty+r*3f, cx-r, ty+r);
        path.CloseFigure();
        using var b = new SolidBrush(col);
        g.FillPath(b, path);
    }

    // ── Fallback body helpers ─────────────────────────────────────────────────

    private static GraphicsPath MakeBodyPath(float cx, float cy, float r)
    {
        float topW=r*.90f, botW=r, top=cy-r, bot=cy+r;
        var p = new GraphicsPath();
        p.AddBezier(cx,top, cx-topW,top, cx-botW,cy, cx-botW,cy+r*.28f);
        p.AddBezier(cx-botW,cy+r*.28f, cx-botW*.88f,bot, cx-botW*.38f,bot, cx,bot);
        p.AddBezier(cx,bot, cx+botW*.38f,bot, cx+botW*.88f,bot, cx+botW,cy+r*.28f);
        p.AddBezier(cx+botW,cy+r*.28f, cx+botW,cy, cx+topW,top, cx,top);
        p.CloseFigure();
        return p;
    }

    private static void DrawEarBump(Graphics g, Brush fill, Pen pen, float cx, float cy, float r)
    { g.FillEllipse(fill, cx-r, cy-r, r*2, r*2); g.DrawEllipse(pen, cx-r, cy-r, r*2, r*2); }

    private static void DrawWisps(Graphics g, Brush fill, Pen pen, float cx, float topY, float r)
    {
        for (int i=-1; i<=1; i++)
        {
            float wx=cx+i*r*.22f*1.4f, w=r*.22f*(i==0?1f:.75f), h=r*.38f*(i==0?1f:.72f);
            g.FillEllipse(fill, wx-w, topY, w*2, h*2);
            g.DrawEllipse(pen,  wx-w, topY, w*2, h*2);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // IDisposable
    // ═════════════════════════════════════════════════════════════════════════

    public void Dispose()
    {
        _zzFont.Dispose();
        _symbolFont.Dispose();
        _markFont.Dispose();
        _dinoIdle?.Dispose();
        _dinoWalk?.Dispose();
        _dinoSleep?.Dispose();
        _dinoReact?.Dispose();
        _dinoType?.Dispose();
        _dinoClick?.Dispose();
    }

    private class DinoSprite : IDisposable
    {
        public Bitmap? LeftBlack;
        public Bitmap? LeftWhite;
        public Bitmap? RightBlack;
        public Bitmap? RightWhite;

        public DinoSprite(Bitmap baseLeft)
        {
            LeftBlack = baseLeft;

            LeftWhite = new Bitmap(baseLeft);
            InvertOutlines(LeftWhite);

            RightBlack = new Bitmap(LeftBlack);
            RightBlack.RotateFlip(RotateFlipType.RotateNoneFlipX);

            RightWhite = new Bitmap(LeftWhite);
            RightWhite.RotateFlip(RotateFlipType.RotateNoneFlipX);
        }

        private static unsafe void InvertOutlines(Bitmap bmp)
        {
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int*  ptr    = (int*)data.Scan0;
            int   total  = bmp.Width * bmp.Height;

            for (int i = 0; i < total; i++)
            {
                int px = ptr[i];
                int a  = (px >> 24) & 0xFF;
                if (a == 0) continue; // transparent pixel, skip

                // Convert all non-transparent sketch line pixels directly to white, preserving their anti-aliased alpha transparency
                ptr[i] = (a << 24) | (255 << 16) | (255 << 8) | 255;
            }
            bmp.UnlockBits(data);
        }

        public void Dispose()
        {
            LeftBlack?.Dispose();
            LeftWhite?.Dispose();
            RightBlack?.Dispose();
            RightWhite?.Dispose();
        }
    }
}
