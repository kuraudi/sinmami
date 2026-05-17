using RentGen.Application.Common.Models;

namespace RentGen.Application.Common.Exceptions;

public sealed class BusinessValidationException : Exception
{
    public BusinessValidationException(string message, IReadOnlyCollection<ValidationIssue> errors)
        : base(message)
    {
        Errors = errors;
    }

    public IReadOnlyCollection<ValidationIssue> Errors { get; }
}
