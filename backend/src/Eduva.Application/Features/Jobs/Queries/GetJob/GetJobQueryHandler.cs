using AutoMapper;
using Eduva.Application.Features.Jobs.DTOs;
using Eduva.Application.Interfaces;
using Eduva.Domain.Entities;
using Eduva.Shared.Enums;
using MediatR;

namespace Eduva.Application.Features.Jobs.Queries.GetJob;

public class GetJobQuery : IRequest<(CustomCode, JobResponse?)>
{
    public Guid Id { get; set; }
}

public class GetJobQueryHandler : IRequestHandler<GetJobQuery, (CustomCode, JobResponse?)>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetJobQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<(CustomCode, JobResponse?)> Handle(GetJobQuery request, CancellationToken cancellationToken)
    {
        var jobRepository = _unitOfWork.GetRepository<Job, Guid>();
        var job = await jobRepository.GetByIdAsync(request.Id);
        
        if (job == null)
        {
            return (CustomCode.UserNotFound, null);
        }

        var response = _mapper.Map<JobResponse>(job);
        return (CustomCode.Success, response);
    }
}
