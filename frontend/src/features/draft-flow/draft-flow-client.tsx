"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { motion, AnimatePresence } from "framer-motion";
import {
  CheckCircle2, ChevronLeft, ChevronRight,
  FileText, Sparkles, ArrowRight, ShieldCheck,
  ArrowLeft, Menu, X,
} from "lucide-react";
import { AppShell } from "@/components/app-shell";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingPanel } from "@/components/ui/loading-panel";
import { StatusPill } from "@/components/ui/status-pill";
import { StepInput } from "@/features/draft-flow/step-input";
import { documentTypeLabel, featureCodeLabel, normalizePlainText } from "@/lib/presenters";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import {
  askDraftHelp,
  bootstrapDraftFlow,
  clearDraftFlow,
  saveDraftAnswer,
  validateDraftFlow,
} from "@/store/slices/draft-flow-slice";
import type { DraftStepItemResponse, StepValue } from "@/types/api";

function normalizeStepValue(value: unknown): StepValue {
  if (typeof value === "string") return value;
  if (typeof value === "number") return String(value);
  if (typeof value === "boolean") return value ? "Да" : "Нет";
  return null;
}

function toApiValue(_step: DraftStepItemResponse, value: StepValue): string | number | boolean | null {
  if (typeof value === "string") { const trimmed = value.trim(); return trimmed || null; }
  if (typeof value === "number") return String(value);
  if (typeof value === "boolean") return value;
  return null;
}

function groupSteps(steps: DraftStepItemResponse[]) {
  const sections: Array<{ section: string; steps: DraftStepItemResponse[] }> = [];
  for (const step of steps) {
    const existing = sections.find((item) => item.section === step.section);
    if (existing) { existing.steps.push(step); continue; }
    sections.push({ section: step.section, steps: [step] });
  }
  return sections;
}

export function DraftFlowClient({ draftId }: { draftId: string }) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const dispatch = useAppDispatch();
  const plan = useAppSelector((state) => state.session.plan);
  const {
    aiHelp, currentQuestion, draft, error,
    isAsking, isGenerating, isLoading, isSaving, isValidating,
    lastGeneratedDocumentId, steps, validation,
  } = useAppSelector((state) => state.draftFlow);

  const fromPreview = searchParams.get("from") === "preview";
  const [selectedStepKey, setSelectedStepKey] = useState<string | null>(searchParams.get("step"));
  const [draftValues, setDraftValues] = useState<Record<string, StepValue>>({});
  const [aiQuestion, setAiQuestion] = useState("");
  const [localNotice, setLocalNotice] = useState<string | null>(null);
  const [navOpen, setNavOpen] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [agreedToTerms, setAgreedToTerms] = useState(false);

  useEffect(() => {
    void dispatch(bootstrapDraftFlow(draftId));
    return () => { dispatch(clearDraftFlow()); };
  }, [dispatch, draftId, plan]);

  const resolvedSelectedStepKey =
    selectedStepKey && steps.some((item) => item.stepKey === selectedStepKey)
      ? selectedStepKey
      : currentQuestion?.stepKey ?? steps.find((item) => item.isCurrent)?.stepKey ?? steps[0]?.stepKey ?? null;

  const selectedStep = steps.find((item) => item.stepKey === resolvedSelectedStepKey) ?? null;
  const groupedSteps = groupSteps(steps);
  const editorValue =
    selectedStep && resolvedSelectedStepKey
      ? draftValues[resolvedSelectedStepKey] ?? normalizeStepValue(selectedStep.value)
      : null;
  const currentStepIndex = selectedStep ? steps.findIndex((item) => item.stepKey === selectedStep.stepKey) : -1;
  const previousStep = currentStepIndex > 0 ? steps[currentStepIndex - 1] : null;
  const nextStep = currentStepIndex >= 0 && currentStepIndex < steps.length - 1 ? steps[currentStepIndex + 1] : null;
  const isLastStep = !!selectedStep && !nextStep;
  const requiredSteps = steps.filter((item) => item.required);
  const answeredRequiredSteps = requiredSteps.filter((item) => item.isAnswered).length;
  const totalAnsweredSteps = steps.filter((item) => item.isAnswered).length;
  const visibleAiHelp =
    selectedStep && aiHelp && (!aiHelp.relatedStepKey || aiHelp.relatedStepKey === selectedStep.stepKey)
      ? aiHelp : null;
  const hasUnsavedChanges =
    !!selectedStep &&
    toApiValue(selectedStep, editorValue) !== toApiValue(selectedStep, normalizeStepValue(selectedStep.value));

  const aiAnswer = normalizePlainText(visibleAiHelp?.answer);
  const aiAnswerParagraphs = aiAnswer.split(/\n{2,}/).map((item) => item.trim()).filter(Boolean);
  const progress = steps.length > 0 ? Math.round((totalAnsweredSteps / steps.length) * 100) : 0;

  async function persistCurrentStep() {
    if (!selectedStep) return null;
    const apiValue = toApiValue(selectedStep, editorValue);
    if (apiValue === null) return null;
    try {
      setLocalNotice(null);
      const bundle = await dispatch(saveDraftAnswer({ draftId, stepKey: selectedStep.stepKey, value: apiValue })).unwrap();
      setDraftValues((current) => { const next = { ...current }; delete next[selectedStep.stepKey]; return next; });
      return bundle.nextQuestion?.stepKey ?? null;
    } catch { return null; }
  }

  function findNextProblem(currentKey: string): string | null {
    if (!validation || validation.isValid) return null;
    const problemKeys = [
      ...validation.missingSteps,
      ...validation.errors.map((e) => e.stepKey).filter(Boolean) as string[],
    ];
    // следующий проблемный после текущего
    const currentIdx = steps.findIndex((s) => s.stepKey === currentKey);
    const after = problemKeys.filter((k) => {
      const idx = steps.findIndex((s) => s.stepKey === k);
      return idx > currentIdx;
    });
    if (after.length > 0) return after[0];
    // если нет после — первый из проблемных (цикл)
    return problemKeys[0] ?? null;
  }

  function findNextUnanswered(fromIndex: number): string | null {
    for (let i = fromIndex + 1; i < steps.length; i++) {
      if (!steps[i].isAnswered) return steps[i].stepKey;
    }
    return null;
  }

  async function handleAdvance() {
    if (!selectedStep) return;
    if (fromPreview) {
      if (hasUnsavedChanges) await persistCurrentStep();
      router.push(`/drafts/${draftId}/preview?t=${Date.now()}`);
      return;
    }
    let savedNextKey: string | null = null;
    if (hasUnsavedChanges) savedNextKey = await persistCurrentStep();
    const nextProblem = findNextProblem(selectedStep.stepKey);
    const nextUnanswered = findNextUnanswered(currentStepIndex);
    const target = savedNextKey ?? nextProblem ?? nextUnanswered ?? nextStep?.stepKey ?? null;
    if (target) setSelectedStepKey(target);
  }

  async function handleAskAi() {
    if (!selectedStep || !aiQuestion.trim()) return;
    try {
      setLocalNotice(null);
      await dispatch(askDraftHelp({ draftId, stepKey: selectedStep.stepKey, question: aiQuestion.trim() })).unwrap();
      setAiQuestion("");
    } catch { /* error in redux */ }
  }

  async function handleValidate() {
    try {
      setLocalNotice(null);
      if (hasUnsavedChanges) await persistCurrentStep();
      await dispatch(validateDraftFlow(draftId)).unwrap();
    } catch { /* error in redux */ }
  }

  async function handleRequestPreview() {
    try {
      setLocalNotice(null);
      if (hasUnsavedChanges) await persistCurrentStep();
      const validationResult = await dispatch(validateDraftFlow(draftId)).unwrap();
      if (!validationResult.isValid) {
        const targetStepKey = validationResult.missingSteps[0] ?? validationResult.errors.find((item) => item.stepKey)?.stepKey ?? null;
        if (targetStepKey) setSelectedStepKey(targetStepKey);
        setLocalNotice("Сначала заполните обязательные поля, потом откройте предпросмотр документа.");
        return;
      }
      setAgreedToTerms(false);
      setShowConfirm(true);
    } catch { /* error in redux */ }
  }

  return (
    <AppShell eyebrow="Договор аренды" title="Заполнение черновика">
      <div className="flex flex-col gap-4 md:gap-5">

        {/* ── Прогресс-бар сверху ── */}
        <div className="rounded-3xl border border-(--line) bg-white p-4 sm:p-5">
          <div className="flex items-center justify-between gap-3">
            {/* Левая часть: название + статус */}
            <div className="flex items-center gap-2 min-w-0 sm:gap-3">
              <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl sm:h-9 sm:w-9"
                style={{ background: "var(--gradient)" }}>
                <FileText size={15} strokeWidth={1.75} className="text-white" />
              </div>
              <div className="min-w-0">
                <p className="text-xs font-semibold text-foreground truncate sm:text-sm">
                  {draft ? documentTypeLabel(draft.documentType) : "Черновик"}
                </p>
                <p className="text-[10px] text-(--muted) sm:text-xs">
                  Шаг {currentStepIndex + 1} из {steps.length} · {answeredRequiredSteps}/{requiredSteps.length} обяз.
                </p>
              </div>
              <div className="hidden sm:block">{draft && <StatusPill status={draft.status} />}</div>
            </div>

            {/* Правая: процент + кнопка навигации */}
            <div className="flex items-center gap-2 shrink-0 sm:gap-3">
              <span className="text-sm font-bold text-indigo-600">{progress}%</span>
              <button
                type="button"
                onClick={() => setNavOpen(!navOpen)}
                className="flex items-center gap-1.5 rounded-xl border border-(--line) px-2.5 py-1.5 text-xs font-medium text-(--muted) transition hover:bg-stone-50 sm:px-3"
              >
                {navOpen ? <X size={13} strokeWidth={2} /> : <Menu size={13} strokeWidth={2} />}
                <span className="hidden sm:inline">Шаги</span>
              </button>
            </div>
          </div>

          {/* Прогресс-линия */}
          <div className="mt-4 h-1.5 w-full rounded-full bg-indigo-100">
            <div
              className="h-1.5 rounded-full transition-all duration-500 ease-out"
              style={{ width: `${progress}%`, background: "var(--gradient)" }}
            />
          </div>


          {/* Выдвижная навигация */}
          <AnimatePresence>
            {navOpen && (
              <motion.div
                initial={{ height: 0, opacity: 0 }}
                animate={{ height: "auto", opacity: 1 }}
                exit={{ height: 0, opacity: 0 }}
                transition={{ duration: 0.2 }}
                className="overflow-hidden"
              >
                <div className="mt-4 border-t border-(--line) pt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3 max-h-64 overflow-y-auto">
                  {groupedSteps.map((section) => (
                    <div key={section.section}>
                      <p className="mb-2 text-[10px] font-bold uppercase tracking-[0.2em] text-(--muted)">{section.section}</p>
                      <div className="flex flex-col gap-1">
                        {section.steps.map((step) => {
                          const isActive = step.stepKey === resolvedSelectedStepKey;
                          return (
                            <button
                              key={step.stepKey}
                              type="button"
                              onClick={() => { setSelectedStepKey(step.stepKey); setNavOpen(false); }}
                              className={`flex items-center gap-2 rounded-xl px-3 py-2 text-left text-sm transition ${
                                isActive ? "bg-indigo-50 font-semibold text-indigo-700" : "text-foreground hover:bg-stone-50"
                              }`}
                            >
                              {step.isAnswered
                                ? <CheckCircle2 size={13} strokeWidth={2} className="shrink-0 text-emerald-500" />
                                : <span className={`h-3 w-3 shrink-0 rounded-full border-2 ${isActive ? "border-indigo-400" : "border-(--line)"}`} />
                              }
                              <span className="truncate">{step.title}</span>
                              {featureCodeLabel(step.featureCode) && (
                                <span className="ml-auto shrink-0 rounded-full bg-indigo-50 px-1.5 py-0.5 text-[10px] font-bold text-indigo-500">Pro</span>
                              )}
                            </button>
                          );
                        })}
                      </div>
                    </div>
                  ))}
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>

        {/* ── Основная область: форма + помощник ── */}
        <div className={`grid gap-4 md:gap-5 ${plan === "Premium" ? "lg:grid-cols-[minmax(0,1fr)_320px]" : ""}`}>

          {/* Центр: текущий шаг */}
          <div className="min-w-0">
            {isLoading && !draft ? (
              <div className="rounded-3xl border border-(--line) bg-white p-8">
                <LoadingPanel title="Загрузка черновика" description="Подтягиваем шаги сценария и ответы." lines={4} />
              </div>
            ) : selectedStep ? (
              <AnimatePresence mode="wait">
                <motion.div
                  key={selectedStep.stepKey}
                  initial={{ opacity: 0, y: 12 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: -8 }}
                  transition={{ duration: 0.22, ease: "easeOut" }}
                  className="rounded-3xl border border-(--line) bg-white p-5 sm:p-6 md:p-8"
                >
                  {/* Статус-строка */}
                  <div className="flex items-center justify-between gap-3 mb-6">
                    <div className="flex items-center gap-2 text-xs text-(--muted)">
                      <span>Шаг {currentStepIndex + 1} из {steps.length}</span>
                      {!selectedStep.required && (
                        <span className="rounded-full bg-stone-100 px-2.5 py-0.5 text-[11px] font-medium">Необязательный</span>
                      )}
                    </div>
                    <div className="flex items-center gap-2">
                      {selectedStep.isAnswered && !hasUnsavedChanges && (
                        <span className="inline-flex items-center gap-1.5 rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700">
                          <CheckCircle2 size={11} strokeWidth={2.5} />
                          Сохранено
                        </span>
                      )}
                      {hasUnsavedChanges && (
                        <span className="rounded-full bg-amber-50 px-3 py-1 text-xs font-semibold text-amber-700">Не сохранено</span>
                      )}
                    </div>
                  </div>

                  <StepInput
                    step={selectedStep}
                    value={editorValue}
                    onChange={(value) => {
                      setDraftValues((current) => ({ ...current, [selectedStep.stepKey]: value }));
                    }}
                  />

                  {/* Навигация */}
                  <div className="mt-6 flex flex-col gap-3 border-t border-(--line) pt-5 sm:mt-8 sm:gap-4 sm:pt-6">

                    <div className="flex items-center justify-between gap-2 sm:gap-3">
                    <button
                      type="button"
                      onClick={() => previousStep && setSelectedStepKey(previousStep.stepKey)}
                      disabled={!previousStep}
                      className="inline-flex items-center gap-1.5 rounded-2xl border border-(--line) bg-white px-3 py-2.5 text-sm font-semibold text-foreground transition hover:bg-stone-50 disabled:cursor-not-allowed disabled:opacity-40 sm:gap-2 sm:px-4"
                    >
                      <ChevronLeft size={15} strokeWidth={2} />
                      <span className="hidden sm:inline">Назад</span>
                    </button>

                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        onClick={handleValidate}
                        disabled={isValidating || isSaving}
                        className="rounded-2xl border border-(--line) bg-white px-3 py-2.5 text-sm font-semibold text-foreground transition hover:bg-stone-50 disabled:opacity-50 sm:px-4"
                      >
                        {isValidating ? "..." : "Проверить"}
                      </button>

                      {/* Из предпросмотра — только Сохранить */}
                      {fromPreview ? (
                        <button
                          type="button"
                          onClick={handleAdvance}
                          disabled={isSaving}
                          className="inline-flex items-center gap-2 rounded-2xl px-5 py-2.5 text-sm font-semibold text-white transition hover:opacity-90 disabled:opacity-50"
                          style={{ background: "var(--gradient)" }}
                        >
                          {isSaving ? "Сохраняю..." : "Сохранить и вернуться"}
                          <ArrowRight size={15} strokeWidth={2} />
                        </button>
                      ) : (
                        <>
                          {/* Дальше — на всех шагах кроме последнего, пока валидация не прошла */}
                          {!isLastStep && !validation?.isValid && (
                            <button
                              type="button"
                              onClick={handleAdvance}
                              disabled={isSaving}
                              className="inline-flex items-center gap-2 rounded-2xl px-5 py-2.5 text-sm font-semibold text-white transition hover:opacity-90 disabled:opacity-50"
                              style={{ background: "var(--gradient)" }}
                            >
                              {isSaving ? "Сохраняю..." : "Дальше"}
                              <ChevronRight size={16} strokeWidth={2} />
                            </button>
                          )}

                          {/* Предпросмотр — на последнем шаге или когда валидация прошла */}
                          {(isLastStep || validation?.isValid) && (
                            <button
                              type="button"
                              onClick={handleRequestPreview}
                              disabled={isValidating || isSaving}
                              className="inline-flex items-center gap-2 rounded-2xl px-5 py-2.5 text-sm font-semibold text-white transition hover:opacity-90 disabled:opacity-40 disabled:cursor-not-allowed"
                              style={{ background: "var(--gradient)" }}
                            >
                              {isValidating ? "Проверяю..." : "Предпросмотр"}
                              <ArrowRight size={15} strokeWidth={2} />
                            </button>
                          )}
                        </>
                      )}
                    </div>
                    </div>
                  </div>

                  {/* Валидация */}
                  {validation && !validation.isValid && (
                    <div className="mt-5 rounded-2xl border border-amber-200 bg-amber-50 p-4">
                      <p className="text-sm font-semibold text-amber-900 mb-3">Нужно заполнить</p>
                      <div className="flex flex-wrap gap-2">
                        {validation.missingSteps.map((stepKey) => (
                          <button key={stepKey} type="button" onClick={() => setSelectedStepKey(stepKey)}
                            className="rounded-xl border border-amber-200 bg-white px-3 py-1.5 text-xs font-semibold text-amber-900 transition hover:bg-amber-50">
                            {steps.find((item) => item.stepKey === stepKey)?.title ?? stepKey}
                          </button>
                        ))}
                        {validation.errors.map((item, index) => (
                          <button key={`${item.code}-${index}`} type="button" onClick={() => item.stepKey && setSelectedStepKey(item.stepKey)}
                            className="rounded-xl border border-amber-200 bg-white px-3 py-1.5 text-xs text-amber-900 text-left transition hover:bg-amber-50">
                            {item.message}
                          </button>
                        ))}
                      </div>
                    </div>
                  )}

                  {validation?.isValid && (
                    <div className="mt-5 flex items-center gap-2 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
                      <CheckCircle2 size={15} strokeWidth={2} className="shrink-0" />
                      Все ключевые данные заполнены. Можно открыть предпросмотр.
                    </div>
                  )}

                  {lastGeneratedDocumentId && (
                    <div className="mt-3 flex items-center justify-between gap-3 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
                      <span>Предпросмотр уже сформирован</span>
                      <Link href={`/documents/${lastGeneratedDocumentId}`} className="font-semibold underline">Открыть →</Link>
                    </div>
                  )}

                  {error && (
                    <div className="mt-4 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-900 wrap-break-word">{error}</div>
                  )}
                  {localNotice && (
                    <div className="mt-4 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900 wrap-break-word">{localNotice}</div>
                  )}

                  <div className="mt-6 flex items-center justify-between border-t border-(--line) pt-4 text-xs text-(--muted)">
                    <Link href="/" className="flex items-center gap-1.5 transition hover:text-foreground">
                      <ArrowLeft size={12} strokeWidth={2} />
                      На главную
                    </Link>
                  </div>
                </motion.div>
              </AnimatePresence>
            ) : (
              <div className="rounded-3xl border border-(--line) bg-white p-8">
                <EmptyState title="Нет доступных шагов" description="Проверьте выбранный тариф или вернитесь на главную." />
              </div>
            )}
          </div>

          {/* Правая: помощник (только Premium) */}
          {plan === "Premium" && <aside className="lg:sticky lg:top-6 lg:self-start">
            <div className="rounded-3xl border border-(--line) bg-white overflow-hidden">
              <div className="flex items-center gap-3 border-b border-(--line) px-5 py-4"
                style={{ background: "linear-gradient(135deg, #f0f0ff 0%, #f5f3ff 100%)" }}>
                <div className="flex h-9 w-9 items-center justify-center rounded-xl" style={{ background: "var(--gradient)" }}>
                  <Sparkles size={16} strokeWidth={1.75} className="text-white" />
                </div>
                <div>
                  <p className="text-sm font-semibold text-foreground">Помощник</p>
                  <p className="text-xs text-(--muted)">Задайте вопрос по текущему шагу</p>
                </div>
              </div>

              <div className="p-5 flex flex-col gap-4">
                {selectedStep && (
                  <div className="rounded-2xl bg-indigo-50 px-3 py-2.5 text-xs text-indigo-700">
                    <span className="font-semibold">Шаг:</span> {selectedStep.title}
                  </div>
                )}

                <textarea
                  value={aiQuestion}
                  onChange={(event) => setAiQuestion(event.target.value)}
                  rows={4}
                  placeholder="Например: зачем эта информация нужна в договоре?"
                  className="w-full rounded-2xl border border-(--line) bg-stone-50 px-4 py-3 text-sm text-foreground outline-none transition focus:border-indigo-400 focus:bg-white resize-none"
                />

                <button
                  type="button"
                  onClick={handleAskAi}
                  disabled={isAsking || !selectedStep || !aiQuestion.trim()}
                  className="inline-flex items-center justify-center gap-2 rounded-2xl px-4 py-2.5 text-sm font-semibold text-white transition hover:opacity-90 disabled:opacity-50"
                  style={{ background: "var(--gradient)" }}
                >
                  {isAsking ? (
                    <><span className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-white/30 border-t-white" />Получаю ответ...</>
                  ) : (
                    <><Sparkles size={14} strokeWidth={1.75} />Спросить</>
                  )}
                </button>

                <AnimatePresence>
                  {visibleAiHelp ? (
                    <motion.div key="answer" initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }}
                      transition={{ duration: 0.25 }} className="rounded-2xl border border-(--line) bg-stone-50 p-4">
                      <div className="flex items-center justify-between gap-2 mb-3">
                        <p className="text-xs font-semibold text-(--muted) uppercase tracking-[0.16em]">Ответ</p>
                        <span className={`rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-wide ${
                          visibleAiHelp.source === "deepseek" ? "bg-emerald-100 text-emerald-800" : "bg-amber-100 text-amber-800"
                        }`}>
                          {visibleAiHelp.source === "deepseek" ? "live" : "fallback"}
                        </span>
                      </div>
                      <div className="max-h-[50vh] overflow-y-auto space-y-3 text-sm leading-7 text-foreground pr-1">
                        {aiAnswerParagraphs.map((paragraph, index) => (
                          <p key={`${index}-${paragraph.slice(0, 32)}`} className="whitespace-pre-wrap wrap-break-word">{paragraph}</p>
                        ))}
                      </div>
                    </motion.div>
                  ) : selectedStep ? (
                    <motion.div key="placeholder" initial={{ opacity: 0 }} animate={{ opacity: 1 }}
                      className="flex flex-col items-center gap-2 rounded-2xl border border-dashed border-(--line) bg-stone-50/60 px-4 py-6 text-center">
                      <FileText size={20} strokeWidth={1.5} className="text-(--muted)" />
                      <p className="text-xs leading-5 text-(--muted)">Ответ по шагу «{selectedStep.title}» появится здесь</p>
                    </motion.div>
                  ) : null}
                </AnimatePresence>
              </div>
            </div>
          </aside>}
        </div>
      </div>

      {/* Модалка подтверждения перед предпросмотром */}
      <AnimatePresence>
        {showConfirm && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.2 }}
            className="fixed inset-0 z-50 flex items-end justify-center sm:items-center p-4"
            style={{ background: "rgba(15, 10, 40, 0.55)", backdropFilter: "blur(4px)" }}
            onClick={(e) => { if (e.target === e.currentTarget) setShowConfirm(false); }}
          >
            <motion.div
              initial={{ opacity: 0, y: 24, scale: 0.97 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: 16, scale: 0.97 }}
              transition={{ duration: 0.25, ease: "easeOut" }}
              className="w-full max-w-md rounded-3xl bg-white p-6 shadow-2xl sm:p-8"
            >
              <div className="mx-auto mb-5 flex h-12 w-12 items-center justify-center rounded-2xl" style={{ background: "var(--gradient)" }}>
                <ShieldCheck size={22} strokeWidth={1.75} className="text-white" />
              </div>
              <h2 className="text-center text-xl font-bold text-foreground mb-1.5">Почти готово</h2>
              <p className="text-center text-sm text-(--muted) leading-6 mb-6">
                Перед просмотром документа подтвердите согласие с условиями
              </p>
              <label className="flex items-start gap-3 cursor-pointer rounded-2xl border border-indigo-100 bg-indigo-50/60 px-4 py-4 mb-6 transition hover:bg-indigo-50">
                <input
                  type="checkbox"
                  checked={agreedToTerms}
                  onChange={(e) => setAgreedToTerms(e.target.checked)}
                  className="mt-0.5 h-4 w-4 shrink-0 accent-indigo-600"
                />
                <span className="text-sm leading-6 text-foreground">
                  Я ознакомился и соглашаюсь с{" "}
                  <Link href="/legal/offer" target="_blank" onClick={(e) => e.stopPropagation()} className="font-semibold text-indigo-600 underline underline-offset-2 hover:text-indigo-800">
                    публичной офертой
                  </Link>{" "}
                  и{" "}
                  <Link href="/legal/privacy" target="_blank" onClick={(e) => e.stopPropagation()} className="font-semibold text-indigo-600 underline underline-offset-2 hover:text-indigo-800">
                    политикой конфиденциальности
                  </Link>
                </span>
              </label>
              <div className="flex flex-col gap-3">
                <button
                  type="button"
                  onClick={() => { setShowConfirm(false); router.push(`/drafts/${draftId}/preview`); }}
                  disabled={!agreedToTerms}
                  className="inline-flex w-full items-center justify-center gap-2 rounded-2xl px-5 py-3 text-sm font-bold text-white transition hover:opacity-90 disabled:opacity-40 disabled:cursor-not-allowed"
                  style={{ background: "var(--gradient)" }}
                >
                  <ArrowRight size={15} strokeWidth={2} />
                  Перейти к предпросмотру
                </button>
                <button
                  type="button"
                  onClick={() => setShowConfirm(false)}
                  className="w-full rounded-2xl border border-(--line) bg-white px-5 py-3 text-sm font-semibold text-foreground transition hover:bg-stone-50"
                >
                  Отмена
                </button>
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>

    </AppShell>
  );
}
