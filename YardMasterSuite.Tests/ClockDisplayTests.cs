using System;
using YardMasterSuite.Core;
using Xunit;

namespace YardMasterSuite.Tests;

public class ClockDisplayTests
{
    [Fact]
    public void Format_pads_hour_and_minute()
    {
        Assert.Equal("Clock 09:05", ClockDisplay.Format(9, 5));
        Assert.Equal("Clock 14:30", ClockDisplay.Format(14, 30));
    }

    [Fact]
    public void Format_from_DateTime()
    {
        Assert.Equal("Clock 06:00", ClockDisplay.Format(new DateTime(1, 1, 1, 6, 0, 0)));
        Assert.Equal("— Clock", ClockDisplay.Format((DateTime?)null));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(24, 0)]
    [InlineData(12, 60)]
    public void Format_rejects_out_of_range(int hour, int minute)
    {
        Assert.Equal("— Clock", ClockDisplay.Format(hour, minute));
    }
}
