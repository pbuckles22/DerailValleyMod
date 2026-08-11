using System;
using System.Globalization;

namespace YardMasterSuite.Core;

/// <summary>
/// Keeps Arrived · Next visible for a short grace after a Cleared sample so a
/// one-frame past/travel flip (or rolling through the pin) does not hide HERE.
/// </summary>
public sealed class SwitchListArriveHold
{
    public const float DefaultHoldSeconds = 2.5f;

    private readonly float _holdSeconds;
    private string? _pinKey;
    private float _clearedUntil = float.NegativeInfinity;

    public SwitchListArriveHold(float holdSeconds = DefaultHoldSeconds)
    {
        _holdSeconds = holdSeconds > 0f ? holdSeconds : DefaultHoldSeconds;
    }

    public bool IsHolding(float now) => now <= _clearedUntil;

    public ConsistClearanceStatus Apply(
        float t,
        ConsistClearanceStatus raw,
        string? pinKey)
    {
        var key = string.IsNullOrWhiteSpace(pinKey) ? "" : pinKey!.Trim();
        if (!string.Equals(_pinKey, key, StringComparison.Ordinal))
        {
            _pinKey = key;
            _clearedUntil = float.NegativeInfinity;
        }

        if (raw == ConsistClearanceStatus.Cleared)
        {
            _clearedUntil = t + _holdSeconds;
            return ConsistClearanceStatus.Cleared;
        }

        if (t <= _clearedUntil)
        {
            return ConsistClearanceStatus.Cleared;
        }

        return raw;
    }

    public void Clear()
    {
        _pinKey = null;
        _clearedUntil = float.NegativeInfinity;
    }

    public static string FormatDiag(
        string? pinId,
        ConsistClearanceStatus raw,
        ConsistClearanceStatus shown,
        ConsistClearanceStatus past,
        float nearMeters,
        float nearRadius,
        bool holding,
        float consistLengthMeters = -1f,
        float offsetMeters = -1f)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("T2 arrive: pin=");
        sb.Append(string.IsNullOrEmpty(pinId) ? "—" : pinId);
        sb.Append(" past=");
        sb.Append(past);
        sb.Append(" near=");
        sb.Append(nearMeters.ToString("0.0", CultureInfo.InvariantCulture));
        sb.Append("m/");
        sb.Append(nearRadius.ToString("0", CultureInfo.InvariantCulture));
        sb.Append("m raw=");
        sb.Append(raw);
        sb.Append(" shown=");
        sb.Append(shown);
        if (consistLengthMeters >= 0f)
        {
            sb.Append(" len=");
            sb.Append(consistLengthMeters.ToString("0.0", CultureInfo.InvariantCulture));
            sb.Append('m');
        }

        if (offsetMeters >= 0f)
        {
            sb.Append(" offset=");
            sb.Append(offsetMeters.ToString("0.0", CultureInfo.InvariantCulture));
            sb.Append('m');
        }

        if (holding)
        {
            sb.Append(" hold");
        }

        return sb.ToString();
    }
}
