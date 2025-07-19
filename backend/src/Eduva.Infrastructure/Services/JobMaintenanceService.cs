using Eduva.Application.Interfaces;
using Eduva.Application.Interfaces.Repositories;
using Eduva.Application.Interfaces.Services;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eduva.Infrastructure.Services;

public class JobMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<JobMaintenanceService> _logger;
    private readonly TimeSpan _maintenanceInterval = TimeSpan.FromHours(1); // Run every hour
    private readonly TimeSpan _jobExpirationThreshold = TimeSpan.FromHours(24); // 24 hours

    public JobMaintenanceService(IServiceProvider serviceProvider, ILogger<JobMaintenanceService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Maintenance Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformMaintenanceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during job maintenance");
            }

            await Task.Delay(_maintenanceInterval, stoppingToken);
        }

        _logger.LogInformation("Job Maintenance Service stopped");
    }

    private async Task PerformMaintenanceAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

        try
        {
            _logger.LogInformation("Starting job maintenance cycle");

            // Find expired jobs
            var jobRepository = unitOfWork.GetCustomRepository<IJobRepository>();
            var expiredJobs = await jobRepository.GetExpiredJobsAsync(_jobExpirationThreshold, cancellationToken);
            var expiredJobsList = expiredJobs.ToList();

            if (expiredJobsList.Count != 0)
            {
                _logger.LogInformation("Found {Count} expired jobs", expiredJobsList.Count);

                foreach (var job in expiredJobsList)
                {
                    try
                    {
                        // Update job status to expired
                        job.JobStatus = JobStatus.Expired;
                        job.LastModifiedAt = DateTimeOffset.UtcNow;
                        unitOfWork.GetRepository<Job, Guid>().Update(job);

                        // Delete temporary blobs
                        var blobsToDelete = new List<string>();

                        // Add all source blob names to deletion list
                        if (job.SourceBlobNames != null && job.SourceBlobNames.Count != 0)
                            blobsToDelete.AddRange(job.SourceBlobNames);

                        if (!string.IsNullOrEmpty(job.ContentBlobName))
                            blobsToDelete.Add(job.ContentBlobName);

                        if (blobsToDelete.Count != 0)
                        {
                            await storageService.DeleteRangeTempFileAsync(blobsToDelete);
                            _logger.LogInformation("Deleted {Count} blobs for expired job {JobId}", blobsToDelete.Count, job.Id);
                        }

                        _logger.LogInformation("Expired job {JobId} updated and cleaned up", job.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to clean up expired job {JobId}", job.Id);
                    }
                }

                await unitOfWork.CommitAsync();
                _logger.LogInformation("Maintenance cycle completed. Processed {Count} expired jobs", expiredJobsList.Count);
            }
            else
            {
                _logger.LogDebug("No expired jobs found during maintenance cycle");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform job maintenance");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job Maintenance Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}
