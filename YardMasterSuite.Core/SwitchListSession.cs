using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>Active Switch List binding (3.6) — selected job + current step for Align.</summary>
public static class SwitchListSession
{
    private static IReadOnlyList<SwitchListStep>? _steps;
    private static string? _jobId;
    private static int _index;

    public static bool HasActive => _steps != null && _steps.Count > 0 && !string.IsNullOrEmpty(_jobId);

    public static string? JobId => _jobId;

    public static IReadOnlyList<SwitchListStep>? Steps => _steps;

    public static int CurrentIndex => _index;

    /// <summary>True after advancing past the last step.</summary>
    public static bool IsComplete => HasActive && _index >= _steps!.Count;

    public static SwitchListStep? CurrentStep =>
        HasActive && _index >= 0 && _index < _steps!.Count ? _steps[_index] : null;

    public static string? CurrentAlignTrackId => CurrentStep?.DestTrackId;

    public static void Bind(string jobId, IReadOnlyList<SwitchListStep> steps)
    {
        var id = jobId?.Trim();
        if (string.IsNullOrEmpty(id) || steps == null || steps.Count == 0)
        {
            Clear();
            return;
        }

        _jobId = id;
        _steps = steps;
        _index = 0;
    }

    public static bool TryAdvance()
    {
        if (!HasActive || _steps == null)
        {
            return false;
        }

        if (_index >= _steps.Count - 1)
        {
            _index = _steps.Count;
            return false;
        }

        _index++;
        return true;
    }

    public static void Clear()
    {
        _jobId = null;
        _steps = null;
        _index = 0;
    }
}
