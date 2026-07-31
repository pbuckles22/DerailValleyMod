namespace YardMasterSuite.Core;

/// <summary>
/// Which posted boards apply to our travel direction (1.10).
/// <list type="bullet">
/// <item><b>Mainline</b> — right of travel, sitting on our track, board face turned toward us.</item>
/// <item><b>Switch</b> — dual text (<c>6/4</c>) near a junction; no right-hand rule
/// (DV places these at the points). Limit number still picked from throw/branch.</item>
/// </list>
/// <para>
/// <b>Facing polarity (0.5.43):</b> a board that governs our move faces oncoming traffic, so its
/// forward axis points <i>back</i> at us: <c>sign.forward · travel ≈ −1</c>. Player.log at 0.5.42
/// caught the board alongside the loco (<c>'6 +2' along=-0.4m lat=2m right=y fDot=-1</c>) being
/// rejected by a rule that demanded <c>fDot ≥ +0.5</c>. Boards reading <c>fDot ≈ +1</c> are the
/// opposite direction's boards, and they sit on our left.
/// </para>
/// <para>
/// <b>Track attribution:</b> "is this board on my track" is answered by the caller comparing the
/// board's nearest <c>RailTrack</c> with the loco bogie's track. Lateral distance cannot answer it:
/// it is measured off a straight line through the loco's heading, so a board 2 m from the rail
/// reads tens of meters away on a curve. The lateral corridor below is only a fallback for boards
/// whose track cannot be resolved.
/// </para>
/// </summary>
public static class SpeedLimitBoardFacing
{
    /// <summary>Board must be at least this far to the right of travel (side test, not distance).</summary>
    public const float MinRightLateralMeters = 0.75f;

    /// <summary>Required |sign.forward · travel|; governing boards are negative (facing us).</summary>
    public const float MinForwardAlign = 0.5f;

    /// <summary>Fallback corridor half-width at the board when track attribution is unavailable.</summary>
    public const float MaxRightLateralMeters = 20f;

    /// <summary>Fallback corridor growth per meter of |along| (curve tolerance).</summary>
    public const float LateralCorridorSlope = 0.12f;

    /// <summary>Fallback corridor never widens past this.</summary>
    public const float MaxLateralCeilingMeters = 60f;

    public const string KindMainline = "main";
    public const string KindSwitch = "switch";

    /// <summary>Fallback corridor half-width for a board <paramref name="alongMeters"/> away.</summary>
    public static float MaxLateralFor(float alongMeters)
    {
        var along = alongMeters < 0f ? -alongMeters : alongMeters;
        var widened = MaxRightLateralMeters + (LateralCorridorSlope * along);
        return widened > MaxLateralCeilingMeters ? MaxLateralCeilingMeters : widened;
    }

    public readonly struct Eval
    {
        public Eval(
            bool governs,
            float forwardDot,
            float rightDot,
            float lateralMeters,
            float maxLateralMeters,
            bool onRight,
            bool onOurTrack,
            bool trackKnown,
            string axis,
            float align,
            string kind)
        {
            Governs = governs;
            ForwardDot = forwardDot;
            RightDot = rightDot;
            LateralMeters = lateralMeters;
            MaxLateralMeters = maxLateralMeters;
            OnRight = onRight;
            OnOurTrack = onOurTrack;
            TrackKnown = trackKnown;
            Axis = axis;
            Align = align;
            Kind = kind;
        }

        public bool Governs { get; }

        /// <summary>sign.forward · travel — governing boards are ≈ −1 (turned toward us).</summary>
        public float ForwardDot { get; }

        public float RightDot { get; }
        public float LateralMeters { get; }

        /// <summary>Fallback corridor half-width applied at this board's distance.</summary>
        public float MaxLateralMeters { get; }

        public bool OnRight { get; }

        /// <summary>Board's nearest track is the loco's track (only meaningful with <see cref="TrackKnown"/>).</summary>
        public bool OnOurTrack { get; }

        /// <summary>Both board and loco resolved to a track; false means the corridor fallback ran.</summary>
        public bool TrackKnown { get; }

        public string Axis { get; }
        public float Align { get; }
        public string Kind { get; }
    }

    /// <summary>
    /// Mainline OR switch. Switch signs skip the right-hand gate when a junction is nearby.
    /// </summary>
    public static Eval Evaluate(
        float signForwardX,
        float signForwardZ,
        float signRightX,
        float signRightZ,
        float travelForwardX,
        float travelForwardZ,
        float deltaToSignX,
        float deltaToSignZ,
        bool isSwitchSign,
        bool junctionNearby,
        bool onOurTrack = false,
        bool trackKnown = false)
    {
        if (!TryNormalize(travelForwardX, travelForwardZ, out var tx, out var tz))
        {
            return Reject("none");
        }

        var rx = tz;
        var rz = -tx;
        var lateral = (deltaToSignX * rx) + (deltaToSignZ * rz);
        var along = (deltaToSignX * tx) + (deltaToSignZ * tz);
        var maxLateral = MaxLateralFor(along);
        var onRight = lateral >= MinRightLateralMeters;

        // Track identity when we have it; corridor only as a fallback.
        var ours = trackKnown
            ? onOurTrack
            : (lateral < 0f ? -lateral : lateral) <= maxLateral;

        var hasF = TryNormalize(signForwardX, signForwardZ, out var fx, out var fz);
        var hasR = TryNormalize(signRightX, signRightZ, out var srx, out var srz);
        var fDot = hasF ? (fx * tx) + (fz * tz) : 0f;
        var rDot = hasR ? (srx * tx) + (srz * tz) : 0f;
        var facesUs = hasF && fDot <= -MinForwardAlign;

        // Right-hand is only for the corridor fallback. Once track/route membership is known
        // (path-ahead or GetClosest), a left-of-heading reading on a curve must not drop an
        // on-path board — 0.5.51: skip '3'=30 at 29.6 m (right=n track=y) → Brake 30 at 26 m.
        var sideOk = trackKnown || onRight;

        // Switch dual board at the points: no right-hand rule, still must be our track and face us.
        if (isSwitchSign && junctionNearby)
        {
            return new Eval(
                governs: facesUs && ours,
                fDot,
                rDot,
                lateral,
                maxLateral,
                onRight,
                onOurTrack,
                trackKnown,
                "switch",
                fDot,
                KindSwitch);
        }

        return new Eval(
            governs: sideOk && facesUs && ours,
            fDot,
            rDot,
            lateral,
            maxLateral,
            onRight,
            onOurTrack,
            trackKnown,
            "fwd",
            fDot,
            KindMainline);
    }

    private static Eval Reject(string axis) =>
        new(false, 0f, 0f, 0f, 0f, false, false, false, axis, 0f, KindMainline);

    private static bool TryNormalize(float x, float z, out float nx, out float nz)
    {
        var len = (float)System.Math.Sqrt((x * x) + (z * z));
        if (len < 1e-4f)
        {
            nx = nz = 0f;
            return false;
        }

        nx = x / len;
        nz = z / len;
        return true;
    }
}
