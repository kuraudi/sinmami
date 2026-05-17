import type { AppendixStatus, DocumentStatus, DraftStatus } from "@/types/api";

type StatusValue = AppendixStatus | DocumentStatus | DraftStatus;

const statusMap: Record<number, { label: string; tone: string }> = {
  1: { label: "Черновик", tone: "bg-white text-[var(--muted)]" },
  2: { label: "В работе", tone: "bg-amber-100 text-amber-900" },
  3: { label: "Готов", tone: "bg-emerald-100 text-emerald-900" },
  4: { label: "Генерация", tone: "bg-sky-100 text-sky-900" },
  5: { label: "Сгенерирован", tone: "bg-[var(--accent-soft)] text-[var(--accent-strong)]" },
  6: { label: "Ошибка", tone: "bg-rose-100 text-rose-900" },
  7: { label: "Архив", tone: "bg-stone-200 text-stone-700" },
};

export function StatusPill({ status }: { status: StatusValue }) {
  const config = statusMap[status] ?? {
    label: "Неизвестно",
    tone: "bg-white text-[var(--muted)]",
  };

  return (
    <span className={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${config.tone}`}>
      {config.label}
    </span>
  );
}
