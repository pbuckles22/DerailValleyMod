namespace YardMasterSuite.Core;

/// <summary>Taken-job consist check vs task cars (HUD GO / HOLD / RED).</summary>
public enum JobConsistStatus
{
    /// <summary>No job cars in the consist.</summary>
    Missing = 0,

    /// <summary>Foreign freight attached and/or job set incomplete.</summary>
    Hold = 1,

    /// <summary>Every job car attached; no non-job freight.</summary>
    Ready = 2,
}

/// <summary>Pure eval for job-consist GO/HOLD/RED.</summary>
public static class JobConsistStatusEval
{
    /// <summary>
    /// <paramref name="expectedJobCars"/> = task-car count;
    /// <paramref name="attachedJobCars"/> = those present in the consist;
    /// <paramref name="foreignFreightCars"/> = consist freight not on the job.
    /// </summary>
    public static JobConsistStatus Evaluate(
        int expectedJobCars,
        int attachedJobCars,
        int foreignFreightCars)
    {
        if (expectedJobCars < 0)
        {
            expectedJobCars = 0;
        }

        if (attachedJobCars < 0)
        {
            attachedJobCars = 0;
        }

        if (foreignFreightCars < 0)
        {
            foreignFreightCars = 0;
        }

        if (attachedJobCars > expectedJobCars)
        {
            attachedJobCars = expectedJobCars;
        }

        if (expectedJobCars == 0 || attachedJobCars == 0)
        {
            return JobConsistStatus.Missing;
        }

        if (foreignFreightCars > 0 || attachedJobCars < expectedJobCars)
        {
            return JobConsistStatus.Hold;
        }

        return JobConsistStatus.Ready;
    }
}

/// <summary>HUD chip: green GO · yellow HOLD · red RED.</summary>
public static class JobConsistStatusDisplay
{
    public const string GoColor = "#55FF55";
    public const string HoldColor = "#FFD400";
    public const string RedColor = "#FF5555";

    public static string Format(JobConsistStatus status) =>
        FormatCore(status, richText: false);

    public static string FormatHud(JobConsistStatus status) =>
        FormatCore(status, richText: true);

    private static string FormatCore(JobConsistStatus status, bool richText)
    {
        var (text, color) = status switch
        {
            JobConsistStatus.Ready => ("GO", GoColor),
            JobConsistStatus.Hold => ("HOLD", HoldColor),
            _ => ("RED", RedColor),
        };

        return richText ? $"<color={color}>{text}</color>" : text;
    }
}
