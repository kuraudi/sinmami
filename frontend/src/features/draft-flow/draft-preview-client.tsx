"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { motion } from "framer-motion";
import {
  CheckCircle2, ChevronLeft, Sparkles, AlertTriangle,
} from "lucide-react";
import { AppShell } from "@/components/app-shell";
import { LoadingPanel } from "@/components/ui/loading-panel";
import { documentTypeLabel } from "@/lib/presenters";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import { generateDraftDocument, validateDraftFlow } from "@/store/slices/draft-flow-slice";
import { rentgenApi } from "@/lib/api/rentgen-api";
import { addLocalDocumentId } from "@/lib/local-history";
import type { DraftDetailsResponse, DraftStepItemResponse } from "@/types/api";

function groupSteps(steps: DraftStepItemResponse[]) {
  const sections: Array<{ section: string; steps: DraftStepItemResponse[] }> = [];
  for (const step of steps) {
    const existing = sections.find((s) => s.section === step.section);
    if (existing) { existing.steps.push(step); continue; }
    sections.push({ section: step.section, steps: [step] });
  }
  return sections;
}

function formatValue(value: unknown): string {
  if (value === null || value === undefined || value === "") return "—";
  if (typeof value === "boolean") return value ? "Да" : "Нет";
  return String(value);
}

export function DraftPreviewClient({ draftId }: { draftId: string }) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const dispatch = useAppDispatch();
  const plan = useAppSelector((s) => s.session.plan);
  const { isGenerating } = useAppSelector((s) => s.draftFlow);

  const [draft, setDraft] = useState<DraftDetailsResponse | null>(null);
  const [steps, setSteps] = useState<DraftStepItemResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function load() {
      try {
        const [draftData, stepsData] = await Promise.all([
          rentgenApi.getDraft(draftId, plan),
          rentgenApi.getDraftSteps(draftId, plan),
        ]);
        setDraft(draftData);
        setSteps(stepsData);
      } catch {
        setError("Не удалось загрузить данные черновика.");
      } finally {
        setIsLoading(false);
      }
    }
    void load();
  }, [draftId, plan, searchParams.get("t")]);

  async function handleGenerate() {
    try {
      setError(null);
      const validation = await dispatch(validateDraftFlow(draftId)).unwrap();
      if (!validation.isValid) {
        router.push(`/drafts/${draftId}`);
        return;
      }
      const generated = await dispatch(generateDraftDocument(draftId)).unwrap();
      addLocalDocumentId(generated.documentId);
      router.push(`/documents/${generated.documentId}`);
    } catch {
      setError("Не удалось сгенерировать документ.");
    }
  }

  const groupedSections = groupSteps(steps);
  const answeredSteps = steps.filter((s) => s.isAnswered);
  const totalRequired = steps.filter((s) => s.required);
  const answeredRequired = totalRequired.filter((s) => s.isAnswered);

  return (
    <AppShell eyebrow="Предпросмотр данных" title="Проверьте введённые данные">
      <div className="flex flex-col gap-5">

        {/* Шапка */}
        <div className="flex items-center justify-between gap-3 rounded-3xl border border-(--line) bg-white px-4 py-3 sm:px-6 sm:py-4">
          <Link
            href={`/drafts/${draftId}`}
            className="inline-flex items-center gap-1.5 text-sm font-semibold text-(--muted) transition hover:text-foreground"
          >
            <ChevronLeft size={15} strokeWidth={2} />
            <span className="hidden sm:inline">Вернуться к заполнению</span>
            <span className="sm:hidden">Назад</span>
          </Link>
          <div className="flex items-center gap-2 text-xs text-(--muted) sm:gap-4 sm:text-sm">
            <span>
              <span className="font-bold text-foreground">{answeredSteps.length}</span>/{steps.length}
            </span>
            <span className="hidden sm:inline">
              <span className="font-bold text-foreground">{answeredRequired.length}</span>/{totalRequired.length} обязательных
            </span>
            {draft && (
              <span className="hidden rounded-full bg-indigo-50 px-3 py-1 text-xs font-semibold text-indigo-600 sm:inline">
                {documentTypeLabel(draft.documentType)}
              </span>
            )}
          </div>
        </div>

        {isLoading ? (
          <div className="rounded-3xl border border-(--line) bg-white p-10">
            <LoadingPanel title="Загружаем данные" description="Подтягиваем все заполненные шаги." lines={5} />
          </div>
        ) : error ? (
          <div className="rounded-3xl border border-rose-200 bg-rose-50 px-6 py-5 text-sm text-rose-900">{error}</div>
        ) : (
          <>
            {/* Секции с данными */}
            <div className="flex flex-col gap-4">
              {groupedSections.map((section, sectionIdx) => (
                <motion.div
                  key={section.section}
                  initial={{ opacity: 0, y: 12 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.2, delay: sectionIdx * 0.04 }}
                  className="rounded-3xl border border-(--line) bg-white overflow-hidden"
                >
                  <div className="border-b border-(--line) px-6 py-4"
                    style={{ background: "linear-gradient(135deg, #f8f7ff 0%, #f3f0ff 100%)" }}>
                    <p className="text-[10px] font-bold uppercase tracking-[0.22em] text-indigo-500">{section.section}</p>
                  </div>

                  <div className="divide-y divide-(--line)">
                    {section.steps.map((step) => {
                      const val = formatValue(step.value);
                      const isEmpty = val === "—";
                      return (
                        <div key={step.stepKey} className="flex items-start gap-3 px-4 py-3 sm:gap-4 sm:px-6 sm:py-4">
                          <div className="mt-0.5 shrink-0">
                            {step.isAnswered
                              ? <CheckCircle2 size={15} strokeWidth={2} className="text-emerald-500" />
                              : <span className={`flex h-3.5 w-3.5 items-center justify-center rounded-full border-2 ${step.required ? "border-amber-400" : "border-(--line)"}`} />
                            }
                          </div>
                          <div className="min-w-0 flex-1">
                            <div className="flex items-center gap-2 flex-wrap">
                              <p className="text-xs font-semibold text-(--muted)">{step.title}</p>
                              {step.required && !step.isAnswered && (
                                <span className="rounded-full bg-amber-50 px-2 py-0.5 text-[10px] font-bold text-amber-600">Обязательно</span>
                              )}
                            </div>
                            <p className={`mt-1 text-sm ${isEmpty ? "text-(--muted) italic" : "text-foreground font-medium"}`}>
                              {val}
                            </p>
                          </div>
                          <Link
                            href={`/drafts/${draftId}?step=${step.stepKey}&from=preview`}
                            className="shrink-0 rounded-xl border border-(--line) px-3 py-1.5 text-xs font-medium text-(--muted) transition hover:bg-stone-50 hover:text-foreground"
                          >
                            Изменить
                          </Link>
                        </div>
                      );
                    })}
                  </div>
                </motion.div>
              ))}
            </div>

            {/* Нижняя панель */}
            <div className="sticky bottom-3 rounded-3xl border border-(--line) bg-white px-4 py-3 shadow-lg shadow-black/5 sm:bottom-4 sm:px-6 sm:py-4">
              <div className="flex items-center justify-between gap-3 sm:gap-4">
                <div className="text-sm">
                  {answeredRequired.length < totalRequired.length ? (
                    <span className="flex items-center gap-2 text-amber-700">
                      <AlertTriangle size={14} strokeWidth={2} />
                      Не заполнено {totalRequired.length - answeredRequired.length} обязательных полей
                    </span>
                  ) : (
                    <span className="flex items-center gap-2 text-emerald-700">
                      <CheckCircle2 size={14} strokeWidth={2} />
                      Все обязательные поля заполнены
                    </span>
                  )}
                </div>
                <button
                  type="button"
                  onClick={handleGenerate}
                  disabled={isGenerating}
                  className="inline-flex items-center gap-2 rounded-2xl px-6 py-3 text-sm font-bold text-white transition hover:opacity-90 disabled:opacity-50"
                  style={{ background: "var(--gradient)" }}
                >
                  {isGenerating ? (
                    <><span className="h-4 w-4 animate-spin rounded-full border-2 border-white/30 border-t-white" />Формирую...</>
                  ) : (
                    <><Sparkles size={15} strokeWidth={1.75} />Сгенерировать документ</>
                  )}
                </button>
              </div>
            </div>
          </>
        )}
      </div>
    </AppShell>
  );
}
