namespace YardMasterSuite.Core;

/// <summary>
/// Convert a board's arc <c>span</c> on a track into distance-from-entry along our travel
/// (**1.16** path-ahead). DV <c>EquiPointSet.Point.span</c> grows from the curve's in-end (0) to
/// out-end (<c>curve.length</c>); when we walk the track the other way, flip.
/// </summary>
public static class TrackPathSpan
{
    /// <summary>
    /// Meters from the path's entry of this track to the board, along the rail (not chord).
    /// </summary>
    public static float WithinTrackMeters(float spanMeters, float trackLengthMeters, bool travelIncreasingSpan)
    {
        var length = trackLengthMeters < 0f ? 0f : trackLengthMeters;
        var span = spanMeters;
        if (span < 0f)
        {
            span = 0f;
        }

        if (length > 0f && span > length)
        {
            span = length;
        }

        return travelIncreasingSpan ? span : length - span;
    }
}
