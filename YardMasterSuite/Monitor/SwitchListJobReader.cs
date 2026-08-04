using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DV.Logic.Job;
using YardMasterSuite.Core;

namespace YardMasterSuite.Monitor;

/// <summary>
/// Maps live DV jobs → <see cref="JobSummary"/> for Switch List (3.6).
/// Fail closed when start/dest tracks cannot be read.
/// </summary>
internal static class SwitchListJobReader
{
    private const BindingFlags InstanceAll =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static IReadOnlyList<Job> ListCandidateJobs()
    {
        var list = new List<Job>();
        var seen = new HashSet<Job>();
        try
        {
            void Add(Job? job)
            {
                if (job == null || !seen.Add(job))
                {
                    return;
                }

                list.Add(job);
            }

            var current = JobsManager.Instance?.currentJobs;
            if (current != null)
            {
                for (var i = 0; i < current.Count; i++)
                {
                    Add(current[i]);
                }
            }

            if (TelemetryReader.TryPeekInventoryJobs(out var held))
            {
                for (var i = 0; i < held.Count; i++)
                {
                    Add(held[i]);
                }
            }
        }
        catch
        {
            // fail closed — empty list
        }

        return list;
    }

    public static bool TryBuildSummary(Job? job, out JobSummary? summary, out string? error)
    {
        summary = null;
        error = null;
        if (job == null)
        {
            error = "no job";
            return false;
        }

        try
        {
            if (!TryExtractTrackIds(job, out var originTrack, out var destTrack, out var detail))
            {
                error = "no start/dest tracks";
                Main.Log("T2 switch-list: " + error + (detail != null ? " · " + detail : ""));
                return false;
            }

            var originYard = job.chainData?.chainOriginYardId?.Trim();
            var destYard = job.chainData?.chainDestinationYardId?.Trim();
            if (string.IsNullOrEmpty(originYard))
            {
                originYard = PathGraphBuilder.YardIdFromTrackKey(originTrack);
            }

            if (string.IsNullOrEmpty(destYard))
            {
                destYard = PathGraphBuilder.YardIdFromTrackKey(destTrack);
            }

            var typeLabel = "";
            try
            {
                typeLabel = job.jobType.ToString();
            }
            catch
            {
                typeLabel = "";
            }

            summary = new JobSummary
            {
                JobId = job.ID?.Trim() ?? "",
                JobTypeLabel = string.IsNullOrEmpty(typeLabel) ? null : typeLabel,
                OriginYardId = string.IsNullOrEmpty(originYard) ? null : originYard,
                DestYardId = string.IsNullOrEmpty(destYard) ? null : destYard,
                OriginTrackId = originTrack,
                DestTrackId = destTrack,
            };

            if (string.IsNullOrEmpty(summary.JobId))
            {
                error = "no job id";
                summary = null;
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = "job read failed";
            Main.Log($"T2 switch-list: job read fail · {ex.GetType().Name}");
            return false;
        }
    }

    private static bool TryExtractTrackIds(
        Job job,
        out string? originTrack,
        out string? destTrack,
        out string? detail)
    {
        originTrack = null;
        destTrack = null;
        detail = null;
        var starts = new List<string>();
        var dests = new List<string>();
        var types = new List<string>();

        // Job.tasks is public List<Task>; nested TransportTask tracks are private fields.
        WalkTasks(job.tasks, starts, dests, types, depth: 0);

        if (starts.Count == 0 || dests.Count == 0)
        {
            detail = types.Count == 0
                ? "tasks empty"
                : ("saw " + string.Join(",", types) + $" · starts={starts.Count} dests={dests.Count}");
            return false;
        }

        originTrack = starts[0];
        destTrack = dests[dests.Count - 1];
        return true;
    }

    private static void WalkTasks(
        object? tasksObj,
        List<string> starts,
        List<string> dests,
        List<string> types,
        int depth)
    {
        if (tasksObj == null || depth > 12)
        {
            return;
        }

        if (tasksObj is TransportTask transport)
        {
            NoteType(types, "TransportTask");
            // Private fields: startingTrack / destinationTrack
            TryAddTrackMember(transport, "startingTrack", starts);
            TryAddTrackMember(transport, "destinationTrack", dests);
            return;
        }

        if (tasksObj is SequentialTasks sequential)
        {
            NoteType(types, "SequentialTasks");
            WalkTasks(GetMember(sequential, "tasks"), starts, dests, types, depth + 1);
            return;
        }

        if (tasksObj is ParallelTasks parallel)
        {
            NoteType(types, "ParallelTasks");
            WalkTasks(GetMember(parallel, "tasks"), starts, dests, types, depth + 1);
            return;
        }

        if (tasksObj is WarehouseTask)
        {
            NoteType(types, "WarehouseTask");
            return;
        }

        if (tasksObj is Task leaf)
        {
            NoteType(types, leaf.GetType().Name);
            TryAddTrackMember(leaf, "startingTrack", starts);
            TryAddTrackMember(leaf, "startTrack", starts);
            TryAddTrackMember(leaf, "destinationTrack", dests);
            var nested = GetMember(leaf, "tasks");
            if (nested != null)
            {
                WalkTasks(nested, starts, dests, types, depth + 1);
            }

            return;
        }

        if (tasksObj is IEnumerable enumerable && tasksObj is not string)
        {
            foreach (var item in enumerable)
            {
                WalkTasks(item, starts, dests, types, depth + 1);
            }
        }
    }

    private static object? GetMember(object obj, string name)
    {
        try
        {
            var type = obj.GetType();
            return type.GetField(name, InstanceAll)?.GetValue(obj)
                ?? type.GetProperty(name, InstanceAll)?.GetValue(obj);
        }
        catch
        {
            return null;
        }
    }

    private static void NoteType(List<string> types, string name)
    {
        if (types.Count < 12)
        {
            types.Add(name);
        }
    }

    private static void TryAddTrackMember(object task, string member, List<string> sink)
    {
        try
        {
            var val = GetMember(task, member);
            if (val is Track track)
            {
                var id = TrackDisplayId(track);
                if (id != null
                    && (sink.Count == 0 || !string.Equals(sink[sink.Count - 1], id, StringComparison.Ordinal)))
                {
                    sink.Add(id);
                }
            }
        }
        catch
        {
            // fail closed for this member
        }
    }

    private static string? TrackDisplayId(Track? track)
    {
        if (track == null)
        {
            return null;
        }

        try
        {
            var trackId = track.ID;
            if (trackId == null)
            {
                return null;
            }

            var display = trackId.FullDisplayID?.Trim();
            if (!string.IsNullOrEmpty(display))
            {
                return display;
            }

            return trackId.FullID?.Trim();
        }
        catch
        {
            return null;
        }
    }
}
