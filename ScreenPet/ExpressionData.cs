namespace ScreenPet;

// ═════════════════════════════════════════════════════════════════════════════
// Enum definitions for expression components
// ═════════════════════════════════════════════════════════════════════════════

public enum EyeShape
{
    Circle,        // Open round eye with pupil + shine
    Wide,          // Larger shocked open eye
    Happy,         // Upward '^' squint
    Sad,           // Teardrop-tilted
    ClosedFlat,    // Horizontal line (—)
    ClosedCurve,   // Gentle downward arc (⌣) — peaceful
    Heart,         // ♥ filled heart
    Star,          // ★ 5-point star
    Dot,           // Tiny filled dot
    Spiral,        // Concentric arcs — dizzy
    X,             // × crossed lines
    Wink,          // Curved single line (winking)
    Sleepy,        // Half-closed drooping semicircle
    Teary,         // Open eye + tear drop starting
}

public enum BrowShape
{
    None,
    Normal,        // Flat neutral arc
    Raised,        // Slightly arched up
    HighRaised,    // Very high (shocked)
    Furrowed,      // Angled inward/down — worried
    Angry,         // Sharply angled inward — fierce
    Sad,           // Angled inward/up — sad
}

public enum MouthShape
{
    Smile,         // Gentle U curve
    BigSmile,      // Wide U
    GrinTeeth,     // Wide arc + teeth block
    Frown,         // ∩ inverted arc
    SmallO,        // Small oval
    BigO,          // Large oval
    Flat,          // Straight horizontal line
    Smirk,         // One-sided bezier smile
    CatMouth,      // ω double-arc cat style
    Tongue,        // Smile + tongue ellipse
    Tremble,       // Wavy scared line
    Zigzag,        // W goofy line
    TinySmile,     // Micro smile
}

public enum ExtraEffect
{
    None,
    Blush,          // Soft pink cheek circles
    BlushStrong,    // Larger blush marks
    Sweat,          // Small teardrop sweat
    SweatBig,       // Large sweat drop
    Sparkles,       // 5 rotating star sparkles
    Hearts,         // 3 floating hearts
    Zzz,            // Three floating Zs
    QuestionMark,   // ? above head
    ExclamationMark,// ! above head
    MusicNote,      // Floating ♪
    Anger,          // # mark vein
    Dizzy,          // Rotating small circles
    TearDrop,       // Single cheek tear
    TearStream,     // Streaming tears
    StarCircle,     // 6 stars rotating (triumphant)
}

// ═════════════════════════════════════════════════════════════════════════════
// Expression record
// ═════════════════════════════════════════════════════════════════════════════

public readonly record struct Expression(
    EyeShape   LeftEye,
    EyeShape   RightEye,
    BrowShape  LeftBrow,
    BrowShape  RightBrow,
    MouthShape Mouth,
    ExtraEffect Extra,
    string     Name
);

// ═════════════════════════════════════════════════════════════════════════════
// Library of 50 expressions
// ═════════════════════════════════════════════════════════════════════════════

public static class ExpressionLibrary
{
    public static readonly Expression[] All =
    [
        //  #0  Neutral
        new(EyeShape.Circle,     EyeShape.Circle,     BrowShape.Normal,     BrowShape.Normal,     MouthShape.Smile,     ExtraEffect.None,          "Neutral"),
        //  #1  Happy
        new(EyeShape.Happy,      EyeShape.Happy,      BrowShape.Raised,     BrowShape.Raised,     MouthShape.Smile,     ExtraEffect.None,          "Happy"),
        //  #2  Very Happy
        new(EyeShape.Happy,      EyeShape.Happy,      BrowShape.Raised,     BrowShape.Raised,     MouthShape.BigSmile,  ExtraEffect.Blush,         "Very Happy"),
        //  #3  Ecstatic
        new(EyeShape.Star,       EyeShape.Star,       BrowShape.HighRaised, BrowShape.HighRaised, MouthShape.BigSmile,  ExtraEffect.Sparkles,      "Ecstatic"),
        //  #4  In Love
        new(EyeShape.Heart,      EyeShape.Heart,      BrowShape.Normal,     BrowShape.Normal,     MouthShape.Smile,     ExtraEffect.Hearts,        "In Love"),
        //  #5  Winking
        new(EyeShape.Happy,      EyeShape.Wink,       BrowShape.Raised,     BrowShape.Normal,     MouthShape.GrinTeeth, ExtraEffect.None,          "Winking"),
        //  #6  Excited
        new(EyeShape.Wide,       EyeShape.Wide,       BrowShape.HighRaised, BrowShape.HighRaised, MouthShape.BigSmile,  ExtraEffect.Sparkles,      "Excited"),
        //  #7  Content
        new(EyeShape.Happy,      EyeShape.Happy,      BrowShape.Normal,     BrowShape.Normal,     MouthShape.CatMouth,  ExtraEffect.Blush,         "Content"),
        //  #8  Playful
        new(EyeShape.Happy,      EyeShape.Happy,      BrowShape.Raised,     BrowShape.Raised,     MouthShape.Tongue,    ExtraEffect.None,          "Playful"),
        //  #9  Musical
        new(EyeShape.ClosedCurve,EyeShape.ClosedCurve,BrowShape.Normal,    BrowShape.Normal,     MouthShape.TinySmile, ExtraEffect.MusicNote,     "Musical"),
        //  #10 Triumphant
        new(EyeShape.Happy,      EyeShape.Happy,      BrowShape.Raised,     BrowShape.Raised,     MouthShape.GrinTeeth, ExtraEffect.StarCircle,    "Triumphant"),
        //  #11 Shy
        new(EyeShape.Circle,     EyeShape.Circle,     BrowShape.Normal,     BrowShape.Normal,     MouthShape.TinySmile, ExtraEffect.BlushStrong,   "Shy"),
        //  #12 Sad
        new(EyeShape.Sad,        EyeShape.Sad,        BrowShape.Sad,        BrowShape.Sad,        MouthShape.Frown,     ExtraEffect.None,          "Sad"),
        //  #13 Very Sad
        new(EyeShape.Sad,        EyeShape.Sad,        BrowShape.Sad,        BrowShape.Sad,        MouthShape.Tremble,   ExtraEffect.TearDrop,      "Very Sad"),
        //  #14 Crying
        new(EyeShape.Teary,      EyeShape.Teary,      BrowShape.Furrowed,   BrowShape.Furrowed,   MouthShape.Tremble,   ExtraEffect.TearStream,    "Crying"),
        //  #15 Worried
        new(EyeShape.Circle,     EyeShape.Circle,     BrowShape.Furrowed,   BrowShape.Furrowed,   MouthShape.Frown,     ExtraEffect.Sweat,         "Worried"),
        //  #16 Anxious
        new(EyeShape.Wide,       EyeShape.Wide,       BrowShape.Raised,     BrowShape.Raised,     MouthShape.Tremble,   ExtraEffect.SweatBig,      "Anxious"),
        //  #17 Disappointed
        new(EyeShape.Sad,        EyeShape.Sad,        BrowShape.Sad,        BrowShape.Sad,        MouthShape.Flat,      ExtraEffect.None,          "Disappointed"),
        //  #18 Surprised
        new(EyeShape.Wide,       EyeShape.Wide,       BrowShape.HighRaised, BrowShape.HighRaised, MouthShape.SmallO,    ExtraEffect.None,          "Surprised"),
        //  #19 Shocked
        new(EyeShape.Wide,       EyeShape.Wide,       BrowShape.HighRaised, BrowShape.HighRaised, MouthShape.BigO,      ExtraEffect.ExclamationMark,"Shocked"),
        //  #20 Scared
        new(EyeShape.Wide,       EyeShape.Wide,       BrowShape.HighRaised, BrowShape.HighRaised, MouthShape.Tremble,   ExtraEffect.Sweat,         "Scared"),
        //  #21 Confused
        new(EyeShape.Wide,       EyeShape.Circle,     BrowShape.Raised,     BrowShape.Furrowed,   MouthShape.SmallO,    ExtraEffect.QuestionMark,  "Confused"),
        //  #22 Puzzled
        new(EyeShape.Circle,     EyeShape.Circle,     BrowShape.Furrowed,   BrowShape.Raised,     MouthShape.Smirk,     ExtraEffect.QuestionMark,  "Puzzled"),
        //  #23 Angry
        new(EyeShape.X,          EyeShape.X,          BrowShape.Angry,      BrowShape.Angry,      MouthShape.Frown,     ExtraEffect.Anger,         "Angry"),
        //  #24 Furious
        new(EyeShape.Wide,       EyeShape.Wide,       BrowShape.Angry,      BrowShape.Angry,      MouthShape.GrinTeeth, ExtraEffect.Anger,         "Furious"),
        //  #25 Grumpy
        new(EyeShape.Circle,     EyeShape.Circle,     BrowShape.Angry,      BrowShape.Angry,      MouthShape.Frown,     ExtraEffect.None,          "Grumpy"),
        //  #26 Mischievous
        new(EyeShape.Happy,      EyeShape.Wink,       BrowShape.Angry,      BrowShape.Angry,      MouthShape.Smirk,     ExtraEffect.None,          "Mischievous"),
        //  #27 Determined
        new(EyeShape.Circle,     EyeShape.Circle,     BrowShape.Furrowed,   BrowShape.Furrowed,   MouthShape.Flat,      ExtraEffect.None,          "Determined"),
        //  #28 Sleepy
        new(EyeShape.Sleepy,     EyeShape.Sleepy,     BrowShape.Normal,     BrowShape.Normal,     MouthShape.Flat,      ExtraEffect.None,          "Sleepy"),
        //  #29 Asleep
        new(EyeShape.ClosedCurve,EyeShape.ClosedCurve,BrowShape.Normal,    BrowShape.Normal,     MouthShape.TinySmile, ExtraEffect.Zzz,           "Asleep"),
        //  #30 Just Woke Up
        new(EyeShape.ClosedFlat, EyeShape.Sleepy,     BrowShape.Normal,     BrowShape.Normal,     MouthShape.SmallO,    ExtraEffect.None,          "Just Woke Up"),
        //  #31 Exhausted
        new(EyeShape.Sleepy,     EyeShape.Sleepy,     BrowShape.Sad,        BrowShape.Sad,        MouthShape.Frown,     ExtraEffect.None,          "Exhausted"),
        //  #32 Goofy
        new(EyeShape.Happy,      EyeShape.Happy,      BrowShape.Raised,     BrowShape.Raised,     MouthShape.Zigzag,    ExtraEffect.None,          "Goofy"),
        //  #33 Cheeky
        new(EyeShape.Dot,        EyeShape.Dot,        BrowShape.Normal,     BrowShape.Normal,     MouthShape.GrinTeeth, ExtraEffect.None,          "Cheeky"),
        //  #34 Awestruck
        new(EyeShape.Wide,       EyeShape.Wide,       BrowShape.HighRaised, BrowShape.HighRaised, MouthShape.BigSmile,  ExtraEffect.Hearts,        "Awestruck"),
        //  #35 Cheeky Wink
        new(EyeShape.Happy,      EyeShape.Wink,       BrowShape.Raised,     BrowShape.Raised,     MouthShape.GrinTeeth, ExtraEffect.Blush,         "Cheeky Wink"),
        //  #36 Dizzy
        new(EyeShape.Spiral,     EyeShape.Spiral,     BrowShape.Raised,     BrowShape.Raised,     MouthShape.SmallO,    ExtraEffect.Dizzy,         "Dizzy"),
        //  #37 Singing
        new(EyeShape.Happy,      EyeShape.Happy,      BrowShape.Normal,     BrowShape.Normal,     MouthShape.SmallO,    ExtraEffect.MusicNote,     "Singing"),
        //  #38 Peaceful
        new(EyeShape.ClosedCurve,EyeShape.ClosedCurve,BrowShape.Normal,    BrowShape.Normal,     MouthShape.TinySmile, ExtraEffect.None,          "Peaceful"),
        //  #39 Curious
        new(EyeShape.Circle,     EyeShape.Wide,       BrowShape.Raised,     BrowShape.Normal,     MouthShape.Smile,     ExtraEffect.QuestionMark,  "Curious"),
        //  #40 Thinking
        new(EyeShape.Dot,        EyeShape.Dot,        BrowShape.Furrowed,   BrowShape.Furrowed,   MouthShape.Flat,      ExtraEffect.None,          "Thinking"),
        //  #41 Humming
        new(EyeShape.ClosedCurve,EyeShape.ClosedCurve,BrowShape.Normal,    BrowShape.Normal,     MouthShape.Smile,     ExtraEffect.MusicNote,     "Humming"),
        //  #42 Hopeful
        new(EyeShape.Circle,     EyeShape.Circle,     BrowShape.Raised,     BrowShape.Raised,     MouthShape.TinySmile, ExtraEffect.None,          "Hopeful"),
        //  #43 Calm
        new(EyeShape.Happy,      EyeShape.Happy,      BrowShape.Normal,     BrowShape.Normal,     MouthShape.CatMouth,  ExtraEffect.None,          "Calm"),
        //  #44 Moved/Emotional
        new(EyeShape.Teary,      EyeShape.Happy,      BrowShape.Furrowed,   BrowShape.Raised,     MouthShape.BigSmile,  ExtraEffect.TearDrop,      "Moved"),
        //  #45 Embarrassed
        new(EyeShape.Circle,     EyeShape.Circle,     BrowShape.Raised,     BrowShape.Raised,     MouthShape.Smile,     ExtraEffect.BlushStrong,   "Embarrassed"),
        //  #46 Amazed
        new(EyeShape.Wide,       EyeShape.Wide,       BrowShape.HighRaised, BrowShape.HighRaised, MouthShape.BigO,      ExtraEffect.Sparkles,      "Amazed"),
        //  #47 Proud
        new(EyeShape.Happy,      EyeShape.Happy,      BrowShape.Raised,     BrowShape.Raised,     MouthShape.BigSmile,  ExtraEffect.BlushStrong,   "Proud"),
        //  #48 Nervous
        new(EyeShape.Sad,        EyeShape.Sad,        BrowShape.Sad,        BrowShape.Sad,        MouthShape.Frown,     ExtraEffect.Sweat,         "Nervous"),
        //  #49 Legendary
        new(EyeShape.Star,       EyeShape.Star,       BrowShape.HighRaised, BrowShape.HighRaised, MouthShape.GrinTeeth, ExtraEffect.StarCircle,    "Legendary"),
    ];

    // ── State-specific expression pools (0-based indices into All[]) ──────────

    /// Calm, curious, happy moods for Idle state
    public static readonly int[] IdlePool    = [ 0, 1, 7, 9, 11, 38, 39, 40, 41, 42, 43 ];

    /// Active / excited moods for Following state
    public static readonly int[] FollowPool  = [ 1, 5, 6, 8, 10, 27, 43, 1, 2, 8 ];

    /// Surprised / energetic for Reacting state
    public static readonly int[] ReactPool   = [ 3, 6, 18, 19, 20, 21, 34, 35, 44, 46, 49 ];

    /// Playful / happy for Playing state
    public static readonly int[] PlayPool    = [ 3, 5, 8, 9, 10, 32, 33, 34, 35, 37, 47 ];

    /// Sleepy / asleep for Sleeping state
    public static readonly int[] SleepPool   = [ 28, 29, 29, 31, 38 ];
}
