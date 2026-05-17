using RentGen.Application.Common.Models;

namespace RentGen.Application.Common.Interfaces;

public interface IPromptBuilder<in TContext>
{
    PromptBuildResult Build(TContext context);
}
