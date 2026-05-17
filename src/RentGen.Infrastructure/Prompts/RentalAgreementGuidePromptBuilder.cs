using RentGen.Application.Common.Interfaces;
using RentGen.Application.Common.Models;
using RentGen.Application.Prompts;

namespace RentGen.Infrastructure.Prompts;

public sealed class RentalAgreementGuidePromptBuilder : IPromptBuilder<RentalAgreementGuidePromptContext>
{
    public PromptBuildResult Build(RentalAgreementGuidePromptContext context)
    {
        return new PromptBuildResult(
            """
            You are a legal helper preparing a short practical post-generation guide.

            Output rules:
            1. Write in Russian.
            2. Return 5 to 7 concise action steps.
            3. Every step should be practical, concrete, and easy to follow.
            4. Personalize the guide using the actual agreement context such as term, deposit, pets, handover, house rules, and other visible conditions.
            5. Do not repeat the agreement text verbatim.
            6. Do not present the answer as a definitive legal opinion or personal legal advice.
            7. Do not use markdown, asterisks, bold markers, fenced blocks, or decorative symbols.
            8. Return clean plain text only.
            """,
            $"""
            Generate a personalized mini-guide in Russian with 5 to 7 action steps for the user after the rental agreement draft has been created.

            Agreement draft:
            {context.DocumentContent}

            Draft data:
            {context.AnswersJson}
            """);
    }
}
