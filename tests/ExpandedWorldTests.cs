using DawnOfBlade.World;
using Xunit;

namespace DawnOfBlade.Tests;

public class ExpandedWorldTests
{
    [Fact]
    public void ExpandedWorld_TakesMoreThanTenMinutesToWalkEdgeToEdge()
    {
        const float normalWalkMetersPerSecond = 5.0f;
        var minutes = OpenWorldBuilder.WorldSizeMeters / normalWalkMetersPerSecond / 60.0f;

        Assert.True(minutes > 10.0f);
    }

    [Fact]
    public void ExpandedWorld_DefinesLandmarksAndContextualPopulation()
    {
        Assert.Equal(10, OpenWorldBuilder.LandmarkBuildingCount);
        Assert.Equal(60, OpenWorldBuilder.ContextualNpcCount);
    }
}
