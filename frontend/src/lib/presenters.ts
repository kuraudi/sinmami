import { AppendixType, DocumentType, FeatureCode } from "@/types/api";

export function documentTypeLabel(type: DocumentType) {
  switch (type) {
    case DocumentType.RentalAgreement:
      return "Договор аренды";
    default:
      return "Документ";
  }
}

export function appendixTypeLabel(type: AppendixType) {
  switch (type) {
    case AppendixType.HandoverAct:
      return "Акт приема-передачи";
    case AppendixType.InventoryList:
      return "Опись имущества";
    case AppendixType.PetAddendum:
      return "Приложение о проживании с животными";
    case AppendixType.PaymentSchedule:
      return "График арендных платежей";
    case AppendixType.DepositAgreement:
      return "Соглашение об обеспечительном платеже";
    case AppendixType.HouseRules:
      return "Правила проживания";
    default:
      return "Приложение";
  }
}

export function featureCodeLabel(featureCode: FeatureCode | null) {
  switch (featureCode) {
    case FeatureCode.ExtendedRentalSections:
      return "Premium-условие";
    case FeatureCode.PersonalizedGuide:
      return "AI-guide";
    case FeatureCode.Appendices:
      return "Premium-приложение";
    default:
      return null;
  }
}

export function formatDate(value?: string | null) {
  if (!value) {
    return "—";
  }

  const date = new Date(value);
  const formatter = new Intl.DateTimeFormat("ru-RU", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    timeZone: "Europe/Moscow",
  });

  return formatter.format(date).replace(",", "");
}

export function normalizePlainText(value?: string | null) {
  if (!value) {
    return "";
  }

  return value
    .replace(/<think>[\s\S]*?<\/think>/gi, "")
    .replace(/^\s*(thought|thinking|reasoning)\s*:\s*.*$/gim, "")
    .replace(/^\s*(размышление|ход рассуждений|мысли)\s*:\s*.*$/gim, "")
    .replace(/\[([^\]]+)\]\(([^)]+)\)/g, "$1 ($2)")
    .replace(/(\*\*|__)(.*?)\1/g, "$2")
    .replace(/(^|[^\*])\*(?!\*)([^*]+)\*(?!\*)/g, "$1$2")
    .replace(/`/g, "")
    .replace(/^\s*[-*+]\s+/gm, "- ")
    .replace(/^\s*(\d+)\)\s+/gm, "$1. ")
    .replace(/\n{3,}/g, "\n\n")
    .trim();
}

export function splitParagraphs(value?: string | null) {
  const normalized = normalizePlainText(value);
  if (!normalized) {
    return [];
  }

  return normalized
    .split(/\n{2,}/)
    .map((item) => item.trim())
    .filter(Boolean);
}
