using FluentAssertions;
using StreamForge.Domain.Entities;

namespace StreamForge.Domain.Tests;

public sealed class TagTests
{
    [Fact]
    public void Create_ShouldNormalizeName()
    {
        var tag = Tag.Create("  ASP.NET  ");

        tag.Name.Should().Be("asp.net");
        tag.UsageCount.Should().Be(0);
    }

    [Fact]
    public void UsageCount_ShouldNotGoBelowZero()
    {
        var tag = Tag.Create("backend");

        tag.DecrementUsageCount();
        tag.IncrementUsageCount();
        tag.DecrementUsageCount();
        tag.DecrementUsageCount();

        tag.UsageCount.Should().Be(0);
    }

    [Fact]
    public void UpdateName_ShouldNormalizeName()
    {
        var tag = Tag.Create("backend");

        tag.UpdateName("  DotNet  ");

        tag.Name.Should().Be("dotnet");
    }
}
