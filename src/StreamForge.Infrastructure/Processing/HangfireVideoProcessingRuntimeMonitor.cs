using Hangfire;
using Hangfire.Common;
using Hangfire.Storage.Monitoring;
using StreamForge.Application.Interfaces;
using StreamForge.Application.UseCases.Processing;

namespace StreamForge.Infrastructure.Processing;

public sealed class HangfireVideoProcessingRuntimeMonitor : IVideoProcessingRuntimeMonitor
{
    private const int _maxScan = 1000;
    private readonly JobStorage _jobStorage;

    public HangfireVideoProcessingRuntimeMonitor(JobStorage jobStorage)
    {
        _jobStorage = jobStorage;
    }

    public Task<bool> HasActiveExecutionAsync(Guid processingJobId, CancellationToken cancellationToken = default)
    {
        var monitoring = _jobStorage.GetMonitoringApi();

        if (ContainsMatchingProcessingJob(
            monitoring.ProcessingJobs(0, ClampCount(monitoring.ProcessingCount())),
            processingJobId))
        {
            return Task.FromResult(true);
        }

        foreach (var queue in monitoring.Queues())
        {
            if (ContainsMatchingEnqueuedJob(
                monitoring.EnqueuedJobs(queue.Name, 0, ClampCount(monitoring.EnqueuedCount(queue.Name))),
                processingJobId))
            {
                return Task.FromResult(true);
            }
        }

        if (ContainsMatchingScheduledJob(
            monitoring.ScheduledJobs(0, ClampCount(monitoring.ScheduledCount())),
            processingJobId))
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    private static int ClampCount(long count)
    {
        if (count <= 0)
        {
            return 0;
        }

        return (int)Math.Min(count, _maxScan);
    }

    private static bool ContainsMatchingProcessingJob(
        JobList<ProcessingJobDto> jobs,
        Guid processingJobId)
    {
        return jobs.Any(entry => IsTargetJob(entry.Value.Job, processingJobId));
    }

    private static bool ContainsMatchingEnqueuedJob(
        JobList<EnqueuedJobDto> jobs,
        Guid processingJobId)
    {
        return jobs.Any(entry => IsTargetJob(entry.Value.Job, processingJobId));
    }

    private static bool ContainsMatchingScheduledJob(
        JobList<ScheduledJobDto> jobs,
        Guid processingJobId)
    {
        return jobs.Any(entry => IsTargetJob(entry.Value.Job, processingJobId));
    }

    private static bool IsTargetJob(Job? job, Guid processingJobId)
    {
        if (job is null)
        {
            return false;
        }

        if (job.Type != typeof(ProcessVideoJobService) || job.Method.Name != nameof(ProcessVideoJobService.Handle))
        {
            return false;
        }

        if (job.Args.Count == 0)
        {
            return false;
        }

        var arg = job.Args[0];
        return arg switch
        {
            Guid guid => guid == processingJobId,
            string text when Guid.TryParse(text, out var parsed) => parsed == processingJobId,
            _ => false
        };
    }
}
