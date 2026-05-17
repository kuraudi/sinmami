using FluentValidation;
using RentGen.Application.Drafts.DTOs;

namespace RentGen.Infrastructure.DependencyInjection;

public sealed class CreateDraftRequestValidator : AbstractValidator<CreateDraftRequest>
{
    public CreateDraftRequestValidator()
    {
        RuleFor(x => x.DocumentType).IsInEnum();
    }
}
