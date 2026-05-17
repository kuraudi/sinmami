using RentGen.Application.Common.Models;

namespace RentGen.Infrastructure.Drafts;

internal static class DraftHelpFallbackComposer
{
    public static string Compose(ScenarioStep? step, string userQuestion)
    {
        if (step is null)
        {
            return "Уточните, пожалуйста, по какому именно шагу вопрос. Тогда я подскажу, что сюда писать, зачем это поле нужно и в каком формате лучше ответить.";
        }

        var intent = DetectIntent(userQuestion);

        return step.Key switch
        {
            "landlord_type" or "tenant_type" => ComposePartyTypeAnswer(step, intent),
            "landlord_rf_citizenship_confirmed" or "tenant_rf_citizenship_confirmed" => ComposeCitizenshipConfirmationAnswer(intent),
            "landlord_passport_number" or "tenant_passport_number" => ComposePassportNumberAnswer(step, intent),
            "landlord_passport_issued_by" or "tenant_passport_issued_by" => ComposeIssuedByAnswer(step, intent),
            "landlord_passport_unit_code" or "tenant_passport_unit_code" => ComposeUnitCodeAnswer(step, intent),
            "landlord_passport_issue_date" or "tenant_passport_issue_date" => ComposePassportDateAnswer(step, intent),
            "landlord_registration_address" or "tenant_registration_address" => ComposeRegistrationAddressAnswer(step, intent),
            "landlord_entrepreneur_ogrnip" or "tenant_entrepreneur_ogrnip" => ComposeOgrnipAnswer(intent),
            "landlord_entrepreneur_inn" or "tenant_entrepreneur_inn" => ComposeEntrepreneurInnAnswer(intent),
            "landlord_entrepreneur_registration_address" or "tenant_entrepreneur_registration_address" => ComposeEntrepreneurAddressAnswer(intent),
            "landlord_phone" or "tenant_phone" => ComposePhoneAnswer(intent),
            "payment_method" => ComposePaymentMethodAnswer(intent),
            "agreement_date" or "lease_start_date" or "lease_end_date" => ComposeDateAnswer(step, intent),
            "deposit_required" => ComposeDepositToggleAnswer(intent),
            "has_pets_clause" => ComposePetsToggleAnswer(intent),
            "sublease_allowed" => ComposeSubleaseAnswer(intent),
            _ => ComposeGenericAnswer(step, intent)
        };
    }

    private static string ComposePartyTypeAnswer(ScenarioStep step, HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "Это поле нужно, чтобы система поняла, какие реквизиты стороны включать в договор. Для физлица дальше понадобятся подтверждение гражданства РФ, данные документа и адрес регистрации. Для ИП понадобятся ОГРНИП, ИНН и адрес регистрации. Для организации будут свои корпоративные реквизиты.",
            HelpIntent.Required => "Да, этот шаг лучше заполнить сразу. Без него система не поймет, какие данные дальше запрашивать у этой стороны.",
            HelpIntent.CanUseAlternative => "Здесь лучше выбрать один из трех статусов: обычный гражданин, ИП или организация. Если человек действует лично от себя, выбирайте «физлицо». Если он зарегистрирован как ИП, выбирайте «ИП». Если договор подписывает компания, выбирайте «организация».",
            _ => "Если участник договора действует лично как обычный гражданин, выбирайте «физлицо». Если он зарегистрирован как ИП, выбирайте «ИП». Если договор подписывает компания, выбирайте «организация». Можно отвечать коротко: «физовик», «ипшник», «юрик» — система это распознает."
        };
    }

    private static string ComposeCitizenshipConfirmationAnswer(HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "Для физлица в этой версии договора мы прямо фиксируем, что сторона является гражданином Российской Федерации. Это позволяет использовать стандартный набор реквизитов физлица без дополнительных оговорок.",
            HelpIntent.Required => "Да, для физлица этот шаг обязателен. Если сторона не подтверждает гражданство РФ, такой сценарий лучше оформлять отдельно, потому что типовой текст договора будет отличаться.",
            _ => "Если сторона является гражданином Российской Федерации, отвечайте «да». Если нет, лучше не продолжать типовой сценарий физлица и оформить договор отдельно с учетом статуса стороны."
        };
    }

    private static string ComposePassportNumberAnswer(ScenarioStep step, HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "Этот шаг нужен, чтобы в договоре были корректные идентификационные данные стороны. Для физлица это один из основных реквизитов, по которым можно точно определить участника договора.",
            HelpIntent.Required => "Если сторона указана как физлицо, это обязательное поле. Для организации или ИП такой шаг обычно не нужен.",
            _ => "Сюда нужно ввести только серию и номер документа, без слова «паспорт». Формат лучше такой: 45 01 123456. Если напишете слитно или с лишними символами, система попробует привести значение к нормальному виду."
        };
    }

    private static string ComposeIssuedByAnswer(ScenarioStep step, HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "Это поле уточняет реквизиты документа стороны. Вместе с номером, кодом подразделения и датой выдачи оно делает данные документа полными.",
            HelpIntent.Required => "Для физлица это поле лучше заполнить полностью. Иначе реквизиты документа в договоре будут неполными.",
            _ => "Сюда пишется только наименование органа, который выдал документ. Номер подразделения и дата выдачи заполняются в соседних шагах отдельно. Пишите так, как это указано в документе."
        };
    }

    private static string ComposeUnitCodeAnswer(ScenarioStep step, HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "Код подразделения — часть реквизитов документа. Он уточняет, каким подразделением выдан документ.",
            _ => "Сюда нужен код подразделения в формате 000-000. Например: 770-001. Если написать цифры без дефиса, система попробует привести их к нужному виду."
        };
    }

    private static string ComposePassportDateAnswer(ScenarioStep step, HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "Дата выдачи помогает полностью оформить реквизиты документа стороны. Она обычно указывается вместе с номером и органом выдачи.",
            _ => "Сюда введите дату выдачи документа в российском формате дд.мм.гггг. Например: 15.07.2018."
        };
    }

    private static string ComposeRegistrationAddressAnswer(ScenarioStep step, HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "Адрес регистрации нужен как часть реквизитов стороны в договоре. Он помогает точнее идентифицировать участника и делает документ более полным.",
            HelpIntent.Required => "Для физлица это поле желательно заполнить. Для договора аренды это стандартный блок реквизитов стороны.",
            _ => "Сюда пишется адрес регистрации обычной строкой: город, улица, дом, квартира. Например: г. Москва, ул. Лесная, д. 15, кв. 27."
        };
    }

    private static string ComposeOgrnipAnswer(HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "ОГРНИП нужен, чтобы корректно идентифицировать индивидуального предпринимателя в договоре. Это основной регистрационный номер ИП.",
            HelpIntent.Required => "Да, если сторона действует как ИП, это обязательный реквизит.",
            _ => "Сюда нужно ввести ОГРНИП без лишних слов. Обычно это 15 цифр. Если вы вставите номер с пробелами или пояснениями, система постарается оставить только сам номер."
        };
    }

    private static string ComposeEntrepreneurInnAnswer(HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "ИНН нужен как обязательный реквизит индивидуального предпринимателя для точной идентификации стороны договора.",
            HelpIntent.Required => "Да, для ИП это обязательное поле.",
            _ => "Сюда нужно ввести ИНН ИП. Обычно это 12 цифр без лишних слов и символов."
        };
    }

    private static string ComposeEntrepreneurAddressAnswer(HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "Адрес регистрации ИП нужен как часть реквизитов предпринимателя в договоре.",
            HelpIntent.Required => "Да, для ИП это обязательное поле.",
            _ => "Сюда пишется адрес регистрации ИП обычной строкой: город, улица, дом, помещение или квартира."
        };
    }

    private static string ComposePhoneAnswer(HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "Телефон добавляют как дополнительный контакт для связи между сторонами. Он полезен, но обычно не является критическим реквизитом самого договора.",
            HelpIntent.Required => "Это необязательное поле. Если не хотите указывать телефон в договоре, его можно пропустить.",
            _ => "Можно написать номер в свободном виде: +7 900 000-00-00, 89000000000 или 8 900 000 00 00. Система попробует привести его к одному формату."
        };
    }

    private static string ComposePaymentMethodAnswer(HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "Способ оплаты нужен, чтобы в договоре было ясно, как именно арендатор передает деньги: переводом, наличными или смешанно. Это снижает споры по расчетам.",
            HelpIntent.Required => "Да, этот шаг лучше заполнить, потому что он влияет на платежный раздел договора.",
            HelpIntent.CanUseAlternative => "Здесь лучше выбрать один из понятных вариантов: банковский перевод, наличные или смешанный способ. Если у вас нестандартная схема, обычно выбирают самый близкий вариант, а детали можно описать отдельно.",
            _ => "Можно отвечать коротко: «нал», «наличка», «перевод», «безнал», «нал и перевод». Система постарается распознать нужный вариант."
        };
    }

    private static string ComposeDateAnswer(ScenarioStep step, HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "Дата нужна, чтобы зафиксировать юридически важный момент в договоре: дату подписания или срок аренды.",
            _ => $"Сюда лучше вводить дату в формате дд.мм.гггг. Например: {step.Placeholder ?? "10.04.2026"}."
        };
    }

    private static string ComposeDepositToggleAnswer(HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "Этот шаг определяет, нужно ли отдельное соглашение об обеспечительном платеже. Если ответить «да», подробные условия залога будут оформляться уже отдельно, а не в основном договоре.",
            HelpIntent.Required => "Это не обязательный шаг. Если отдельное соглашение о залоге вам не нужно, можно ответить «нет».",
            _ => "Если хотите оформить отдельный документ по обеспечительному платежу, отвечайте «да». Если залог не нужен или отдельное соглашение не требуется, отвечайте «нет»."
        };
    }

    private static string ComposePetsToggleAnswer(HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "Этот шаг нужен только для решения, требуется ли отдельное приложение о проживании с животными. Если ответить «нет», основной договор на этом не ломается и детали про животных отдельно не собираются.",
            HelpIntent.Required => "Это необязательный шаг. Если животных нет или отдельное приложение не нужно, можно смело отвечать «нет».",
            _ => "Если хотите оформить отдельное приложение по животным, отвечайте «да». Если проживание с животными не планируется или отдельный документ не нужен, отвечайте «нет»."
        };
    }

    private static string ComposeSubleaseAnswer(HelpIntent intent)
    {
        return intent switch
        {
            HelpIntent.Why => "Этот шаг нужен, если вы хотите явно отразить в основном договоре условие о субаренде. Иначе можно оставить его выключенным.",
            _ => "Если хотите прямо указать в основном договоре, допускается ли субаренда, отвечайте «да» или «нет». Если это условие вам не нужно, можно оставить ответ «нет»."
        };
    }

    private static string ComposeGenericAnswer(ScenarioStep step, HelpIntent intent)
    {
        var requiredHint = step.Required
            ? "Это обязательный шаг для корректного черновика договора."
            : "Это необязательный шаг, но он может сделать договор понятнее и точнее.";

        return intent switch
        {
            HelpIntent.Why => $"{requiredHint} Сейчас система просит указать: {step.QuestionText}",
            HelpIntent.Required => requiredHint,
            _ => $"Сейчас нужно ответить на вопрос: {step.QuestionText} Если удобнее, можно писать свободным текстом. Система попробует извлечь нужное значение из ответа."
        };
    }

    private static HelpIntent DetectIntent(string userQuestion)
    {
        var question = userQuestion.Trim();

        if (ContainsAny(question, "зачем", "для чего", "почему это нужно", "почему нужно"))
        {
            return HelpIntent.Why;
        }

        if (ContainsAny(question, "обязательно", "обязател", "можно пропустить", "нужно ли"))
        {
            return HelpIntent.Required;
        }

        if (ContainsAny(question, "можно ли другое", "можно по-другому", "другое", "свой вариант", "иначе"))
        {
            return HelpIntent.CanUseAlternative;
        }

        return HelpIntent.HowToFill;
    }

    private static bool ContainsAny(string source, params string[] fragments)
    {
        return fragments.Any(fragment => source.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private enum HelpIntent
    {
        HowToFill,
        Why,
        Required,
        CanUseAlternative
    }
}
