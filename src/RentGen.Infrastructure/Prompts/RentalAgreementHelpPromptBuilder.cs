using RentGen.Application.Common.Interfaces;
using RentGen.Application.Common.Models;
using RentGen.Application.Prompts;

namespace RentGen.Infrastructure.Prompts;

public sealed class RentalAgreementHelpPromptBuilder : IPromptBuilder<RentalAgreementHelpPromptContext>
{
    public PromptBuildResult Build(RentalAgreementHelpPromptContext context)
    {
        var stepTitle = string.IsNullOrWhiteSpace(context.StepTitle) ? "не указан" : context.StepTitle;
        var stepQuestion = string.IsNullOrWhiteSpace(context.StepQuestionText) ? "не указан" : context.StepQuestionText;
        var stepHelp = string.IsNullOrWhiteSpace(context.StepHelpText) ? "нет" : context.StepHelpText;
        var inputType = string.IsNullOrWhiteSpace(context.StepInputType) ? "не указан" : context.StepInputType;
        var options = string.IsNullOrWhiteSpace(context.StepOptionsText) ? "нет" : context.StepOptionsText;
        var collectedFacts = string.IsNullOrWhiteSpace(context.CollectedFactsText)
            ? "Пока нет сохраненных данных."
            : context.CollectedFactsText;
        var missingSteps = string.IsNullOrWhiteSpace(context.MissingRequiredStepsText)
            ? "Все обязательные шаги уже заполнены."
            : context.MissingRequiredStepsText;
        var answerMode = DetectAnswerMode(context.UserQuestion);

        return new PromptBuildResult(
            """
            Ты помогаешь пользователю заполнить договор аренды и связанные документы.

            Отвечай по правилам:
            1. Отвечай только на последний вопрос пользователя.
            2. Отвечай по-русски, простыми словами и без канцелярита.
            3. Сначала дай прямой ответ по сути вопроса одной короткой фразой.
            4. Затем кратко поясни только то, что относится к текущему шагу.
            5. Не уходи в соседние шаги, если это не нужно для ответа.
            6. Если пользователь спрашивает "зачем", объясни именно зачем это поле нужно в договоре и какие последствия у него для структуры документа.
            7. Если пользователь спрашивает "обязательно ли", ответь прямо: обязательно, необязательно или зависит от типа стороны/условия.
            8. Если пользователь спрашивает "что писать" или "как заполнить", дай точный формат и 1 короткий пример.
            9. Если есть варианты ответа, перечисляй их человеческими словами, а не внутренними ключами.
            10. Не придумывай факты за пользователя.
            11. Не используй markdown, звездочки, JSON, XML, code block, таблицы и служебные пометки.
            12. Не показывай reasoning, think, chain-of-thought и внутренние рассуждения модели.
            13. Не пиши дисклеймеры и не упоминай, что ты ИИ.
            14. Ответ должен быть коротким и полезным: обычно 2-4 абзаца.
            """,
            $"""
            Режим ответа: {answerMode}
            Тариф пользователя: {context.Plan}
            Ключ шага: {context.StepKey ?? "не указан"}
            Название шага: {stepTitle}
            Вопрос шага: {stepQuestion}
            Подсказка шага: {stepHelp}
            Тип ответа: {inputType}
            Допустимые варианты: {options}

            Уже собранные факты:
            {collectedFacts}

            Какие обязательные шаги еще не заполнены:
            {missingSteps}

            Последний вопрос пользователя:
            {context.UserQuestion}
            """);
    }

    private static string DetectAnswerMode(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return "объяснить, как заполнить текущий шаг";
        }

        if (ContainsAny(question, "зачем", "для чего", "почему это нужно", "почему нужно"))
        {
            return "объяснить, зачем нужен текущий шаг";
        }

        if (ContainsAny(question, "обязательно", "можно пропустить", "нужно ли"))
        {
            return "объяснить, обязательно ли заполнять текущий шаг";
        }

        if (ContainsAny(question, "можно ли другое", "можно по-другому", "иначе", "свой вариант"))
        {
            return "объяснить, какие варианты допустимы на текущем шаге";
        }

        return "объяснить, как заполнить текущий шаг";
    }

    private static bool ContainsAny(string source, params string[] fragments)
    {
        return fragments.Any(fragment => source.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}
