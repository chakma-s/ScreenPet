using System;

namespace ScreenPet;

public enum PetState { Idle, Following, Reacting, Playing, Sleeping }

/// <summary>
/// 5-state FSM + expression rotation.
/// Runs synchronously on the UI thread — no allocations per tick.
/// </summary>
public sealed class PetBrain
{
    // ── State thresholds (ms) ─────────────────────────────────────────────────
    private const float IdleMs        = 3_000f;
    private const float SleepMs       = 30_000f;
    private const float ReactMs       = 950f;
    private const float PlayMs        = 22_000f;
    private const float PlayDurMs     = 2_200f;
    private const float ReactSpeedPx  = 13f;   // px/tick to trigger reaction

    // ── Expression rotation ───────────────────────────────────────────────────
    private const int   ExprHoldMin   = 160;   // ticks (~5 s)
    private const int   ExprHoldMax   = 280;   // ticks (~8 s)

    private static readonly Random Rng = new();

    // ── Timers ────────────────────────────────────────────────────────────────
    private float _idleTimer    = 0;
    private float _reactTimer   = 0;
    private float _playTimer    = 0;
    private float _playDurTimer = 0;
    private int   _exprTimer    = 0;
    private int   _exprHold     = 200;
    private const float Dt = 33f;  // ms per tick

    // ── Public state ──────────────────────────────────────────────────────────
    public PetState  State            { get; private set; } = PetState.Idle;
    public bool      IsPaused         { get; set; }
    public string?   ReactionEmoji    { get; private set; }
    public Expression CurrentExpression { get; private set; } = ExpressionLibrary.All[0];

    private static readonly string[] ReactionEmojis = ["✨", "💫", "⚡", "🌟", "😲", "🎉", "❤️"];

    // ─────────────────────────────────────────────────────────────────────────

    public void Update(CursorTracker cursor)
    {
        if (IsPaused) return;

        TickExpression();
        TickState(cursor);
    }

    // ── Expression rotation ───────────────────────────────────────────────────

    private void TickExpression()
    {
        _exprTimer++;
        if (_exprTimer < _exprHold) return;

        _exprTimer = 0;
        _exprHold  = Rng.Next(ExprHoldMin, ExprHoldMax);
        PickExpression();
    }

    private void PickExpression()
    {
        var pool = State switch
        {
            PetState.Idle      => ExpressionLibrary.IdlePool,
            PetState.Following => ExpressionLibrary.FollowPool,
            PetState.Reacting  => ExpressionLibrary.ReactPool,
            PetState.Playing   => ExpressionLibrary.PlayPool,
            PetState.Sleeping  => ExpressionLibrary.SleepPool,
            _                  => ExpressionLibrary.IdlePool,
        };
        CurrentExpression = ExpressionLibrary.All[pool[Rng.Next(pool.Length)]];
    }

    public void OnKeyPress()
    {
        if (IsPaused) return;

        // Wake up immediately if sleeping
        if (State == PetState.Sleeping)
        {
            _idleTimer = 0;
            TransitionTo(PetState.Following);
        }

        // Active typing duration (1.2 seconds of animation)
        _playDurTimer = 1200f;

        if (State != PetState.Playing && State != PetState.Reacting)
        {
            TransitionTo(PetState.Playing);
        }
    }

    private void TransitionTo(PetState next)
    {
        if (next == State) return;
        State = next;
        _exprTimer = _exprHold;  // force immediate expression refresh
    }

    // ── State machine tick ────────────────────────────────────────────────────

    private void TickState(CursorTracker cursor)
    {
        // ── Reaction: stays until timer expires ───────────────────────────
        if (_reactTimer > 0)
        {
            _reactTimer -= Dt;
            if (_reactTimer <= 0)
            {
                ReactionEmoji = null;
                TransitionTo(cursor.IsMoving ? PetState.Following : PetState.Idle);
            }
            return;
        }

        // ── Trigger reaction on fast cursor shake ─────────────────────────
        if (cursor.Speed > ReactSpeedPx && State != PetState.Sleeping)
        {
            ReactionEmoji = ReactionEmojis[Rng.Next(ReactionEmojis.Length)];
            _reactTimer   = ReactMs;
            TransitionTo(PetState.Reacting);
            _idleTimer = 0;
            return;
        }

        // ── Playing burst (typing animation) ──────────────────────────────
        if (State == PetState.Playing)
        {
            _playDurTimer -= Dt;
            if (_playDurTimer <= 0)
            {
                TransitionTo(cursor.IsMoving ? PetState.Following : PetState.Idle);
            }
            return;
        }

        // ── Sleeping: wake on any movement ───────────────────────────────
        if (State == PetState.Sleeping)
        {
            if (cursor.IsMoving) { _idleTimer = 0; TransitionTo(PetState.Following); }
            return;
        }

        // ── Normal flow ───────────────────────────────────────────────────
        if (cursor.IsMoving)
        {
            _idleTimer = 0;
            _playTimer += Dt;
            TransitionTo(PetState.Following);
        }
        else
        {
            _idleTimer += Dt;
            _playTimer += Dt;

            if (_idleTimer >= SleepMs)
            {
                TransitionTo(PetState.Sleeping);
            }
            else if (_idleTimer >= IdleMs)
            {
                TransitionTo(PetState.Idle);
            }
        }
    }
}
