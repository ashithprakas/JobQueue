using System.Text;
using JobQueue.Core.Constants;

namespace JobQueue.API.DTOs;

using FluentValidation;

public class CreateJobRequest
{
    public Guid Id { get; set; }
    public string Payload { get; set; }
}

public class CreateJobRequestValidator : AbstractValidator<CreateJobRequest>
{
    public CreateJobRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Job id cannot be empty");
        RuleFor(x => x.Payload)
            .Cascade(CascadeMode.Stop) // Prevents next rules from running if NotNull/NotEmpty fails
            .NotNull().NotEmpty().WithMessage("Payload cannot be empty")
            .Must(payload => Encoding.UTF8.GetByteCount(payload) <= JobConstants.MaxPayloadLength).WithMessage("Payload cannot exceed 256KB");

    }
}