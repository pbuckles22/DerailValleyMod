using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class CarsDisplayTests
{
    [Fact]
    public void Format_shows_placeholder_when_missing()
    {
        Assert.Equal("— Cars", CarsDisplay.Format(null));
    }

    [Theory]
    [InlineData(0, "Cars 0")]
    [InlineData(1, "Cars 1")]
    [InlineData(12, "Cars 12")]
    public void Format_shows_count(int count, string expected)
    {
        Assert.Equal(expected, CarsDisplay.Format(count));
    }
}
