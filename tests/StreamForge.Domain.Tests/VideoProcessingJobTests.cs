using FluentAssertions;
using StreamForge.Domain.Entities;
using StreamForge.Domain.Enums;

namespace StreamForge.Domain.Tests;

public sealed class VideoProcessingJobTests
{
    [Fact]
    public void StartAndComplete_ShouldTransitionJob()
    {
        var job = VideoProcessingJob.Create(Guid.NewGuid(), ProcessingJobType.Transcode);

        job.Start();
        job.UpdateProgress(45);
        job.Complete();

        job.Status.Should().Be(ProcessingJobStatus.Completed);
        job.Progress.Should().Be(100);
        job.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateProgress_ShouldRejectWhenNotProcessing()
    {
        var job = VideoProcessingJob.Create(Guid.NewGuid(), ProcessingJobType.Transcode);

        var act = () => job.UpdateProgress(50);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Progress can only be updated for processing jobs");
    }

    [Fact]
    public void Reset_ShouldClearFailureState()
    {
        var job = VideoProcessingJob.Create(Guid.NewGuid(), ProcessingJobType.Transcode);

        job.Fail("ffmpeg failed");
        job.Reset();

        job.Status.Should().Be(ProcessingJobStatus.Pending);
        job.Progress.Should().Be(0);
        job.ErrorMessage.Should().BeNull();
        job.StartedAt.Should().BeNull();
        job.CompletedAt.Should().BeNull();
    }
}
