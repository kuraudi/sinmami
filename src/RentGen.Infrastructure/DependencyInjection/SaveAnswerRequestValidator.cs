using FluentValidation;
using RentGen.Application.Drafts.DTOs;

namespace RentGen.Infrastructure.DependencyInjection;

public sealed class SaveAnswerRequestValidator : AbstractValidator<SaveAnswerRequest>
{
    public SaveAnswerRequestValidator()
    {
        RuleFor(x => x.StepKey).NotEmpty().MaximumLength(128);
    }
}
