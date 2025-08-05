using Eduva.Application.Common.Specifications;
using Eduva.Domain.Entities;
using Eduva.Domain.Enums;
using System.Linq.Expressions;

namespace Eduva.Application.Features.Jobs.Specifications
{
    public class GetJobsSpecification : ISpecification<Job>
    {
        public Expression<Func<Job, bool>> Criteria { get; private set; }
        public Func<IQueryable<Job>, IOrderedQueryable<Job>>? OrderBy { get; private set; }
        public List<Expression<Func<Job, object>>> Includes { get; private set; } = [];
        public Func<IQueryable<Job>, IQueryable<Job>>? Selector { get; private set; }
        public int Skip { get; private set; }
        public int Take { get; private set; }

        public GetJobsSpecification(JobSpecParam param, Guid userId)
        {
            Criteria = job =>
                (job.UserId == userId) &&
                (string.IsNullOrEmpty(param.SearchTerm) || job.Topic.ToLower().Contains(param.SearchTerm.ToLower())) &&
                (job.JobStatus == JobStatus.Completed || job.AudioOutputBlobName != null || job.VideoOutputBlobName != null);

            if (!string.IsNullOrEmpty(param.SortBy))
            {
                bool isDescending = param.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
                OrderBy = param.SortBy.ToLower() switch
                {
                    "topic" => isDescending
                        ? q => q.OrderByDescending(j => j.Topic)
                        : q => q.OrderBy(j => j.Topic),
                    "createdat" => isDescending
                        ? q => q.OrderByDescending(j => j.CreatedAt)
                        : q => q.OrderBy(j => j.CreatedAt),
                    "lastmodifiedat" => isDescending
                        ? q => q.OrderByDescending(j => j.LastModifiedAt)
                        : q => q.OrderBy(j => j.LastModifiedAt),
                    _ => isDescending
                        ? q => q.OrderByDescending(j => j.CreatedAt)
                        : q => q.OrderBy(j => j.CreatedAt)
                };
            }
            else
            {
                OrderBy = q => q.OrderByDescending(j => j.CreatedAt);
            }


            if (param.IsPagingEnabled)
            {
                Skip = (param.PageIndex - 1) * param.PageSize;
                Take = param.PageSize;
            }
            else
            {
                Skip = 0;
                Take = int.MaxValue;
            }
        }
    }
}
