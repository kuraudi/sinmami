"use client";

import { featureCodeLabel } from "@/lib/presenters";
import type { QuestionStepModel, StepValue } from "@/types/api";

const optionLabels: Record<string, string> = {
  apartment: "Квартира",
  bank_transfer: "Банковский перевод",
  cash: "Наличные",
  commercial_space: "Коммерческое помещение",
  company: "Организация",
  entrepreneur: "ИП",
  house: "Дом",
  individual: "Физлицо",
  mixed: "Смешанный способ",
  room: "Комната",
  true: "Да",
  false: "Нет",
  monthly: "Ежемесячно",
  quarterly: "Ежеквартально",
  custom: "По отдельному графику",
};

type StepInputProps = {
  step: QuestionStepModel;
  value: StepValue;
  onChange: (value: StepValue) => void;
};

function toTextValue(value: StepValue) {
  if (typeof value === "boolean") return value ? "Да" : "Нет";
  if (typeof value === "number") return String(value);
  return typeof value === "string" ? value : "";
}

function buildExamples(step: QuestionStepModel) {
  if (step.inputType === "boolean") return ["Да, нужно", "Нет, не нужно"];
  if (step.inputType === "select") {
    if (step.stepKey === "payment_method") return ["нал", "безнал на счет", "наличка или перевод"];
    if (step.stepKey === "landlord_type" || step.stepKey === "tenant_type") return ["физовик", "юрик", "ипшник"];
    return step.options.map((option) => optionLabels[option] ?? option);
  }
  if (step.inputType === "date") return ["10.04.2026", "01.05.2026"];
  if (step.inputType === "number") return ["85000", "42", "5"];
  if (step.stepKey.endsWith("_passport_number")) return ["45 01 123456"];
  if (step.stepKey.endsWith("_passport_issued_by")) return ["ОВМД России по Тверскому району г. Москвы"];
  if (step.stepKey.endsWith("_passport_unit_code")) return ["770-001"];
  if (step.stepKey.endsWith("_passport_issue_date")) return ["15.07.2018"];
  if (step.stepKey.endsWith("_registration_address")) return ["г. Москва, ул. Лесная, д. 15, кв. 27"];
  if (step.stepKey.endsWith("_phone")) return ["+7 916 000-00-01"];
  if (step.stepKey.endsWith("_entrepreneur_ogrnip")) return ["318774600000001"];
  if (step.stepKey.endsWith("_entrepreneur_inn")) return ["770123456789"];
  if (step.stepKey.endsWith("_company_inn")) return ["7701234567"];
  if (step.stepKey.endsWith("_company_ogrn")) return ["1027700000000"];
  return step.placeholder ? [step.placeholder] : [];
}

export function StepInput({ step, value, onChange }: StepInputProps) {
  const featureLabel = featureCodeLabel(step.featureCode ?? null);
  const hasOptions = (step.inputType === "boolean" || step.inputType === "select") && step.options.length > 0;
  const examples = buildExamples(step);


  return (
    <div className="mt-5 flex flex-col gap-5">

      {/* Заголовок шага */}
      <div className="flex min-w-0 flex-wrap items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          <p className="text-[10px] font-bold uppercase tracking-[0.22em] text-indigo-500">{step.section}</p>
          <h3 className="font-editorial mt-1.5 wrap-break-word text-3xl leading-tight text-foreground">
            {step.title}
          </h3>
        </div>
        {featureLabel && (
          <span className="mt-1 rounded-full bg-indigo-50 px-3 py-1 text-xs font-semibold text-indigo-600">
            {featureLabel}
          </span>
        )}
      </div>

      {/* Вопрос + подсказка — компактно */}
      <div className="flex flex-col gap-1">
        <p className="wrap-break-word text-base leading-7 text-foreground">{step.questionText}</p>
        {step.helpText && (
          <p className="wrap-break-word text-sm leading-6 text-(--muted)">{step.helpText}</p>
        )}
      </div>

      {/* Варианты (select/boolean) */}
      {hasOptions && (
        <div className="flex flex-wrap gap-2">
          {step.options.map((option) => {
            const label = optionLabels[option] ?? option;
            const isSelected = toTextValue(value) === label;
            return (
              <button
                key={option}
                type="button"
                onClick={() => onChange(label)}
                className={`rounded-2xl border px-4 py-2 text-sm font-medium transition ${
                  isSelected
                    ? "border-indigo-400 bg-indigo-50 text-indigo-700"
                    : "border-(--line) bg-white text-foreground hover:border-indigo-300 hover:bg-indigo-50/40"
                }`}
              >
                {label}
              </button>
            );
          })}
        </div>
      )}

      {/* Поле ввода */}
      <div>
        <textarea
          value={toTextValue(value)}
          onChange={(event) => onChange(event.target.value)}
          placeholder={step.placeholder ?? "Напишите ответ свободно, своими словами"}
          rows={4}
          className="w-full rounded-2xl border border-(--line) bg-white px-4 py-3 text-sm text-foreground outline-none transition focus:border-indigo-400 resize-none"
        />
        {examples.length > 0 && (
          <div className="mt-2 flex flex-wrap gap-1.5">
            {examples.map((example) => (
              <button
                key={example}
                type="button"
                onClick={() => onChange(example)}
                className="rounded-full border border-(--line) bg-stone-50 px-3 py-1 text-xs text-(--muted) transition hover:bg-white hover:text-foreground"
              >
                {example}
              </button>
            ))}
          </div>
        )}
      </div>

    </div>
  );
}
