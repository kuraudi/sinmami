"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { AppShell } from "@/components/app-shell";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingPanel } from "@/components/ui/loading-panel";
import { StepInput } from "@/features/draft-flow/step-input";
import { rentgenApi } from "@/lib/api/rentgen-api";
import { appendixTypeLabel, normalizePlainText } from "@/lib/presenters";
import { useAppSelector } from "@/store/hooks";
import type {
  AppendixDetailsResponse,
  AppendixFlowResponse,
  AppendixPreviewResponse,
  AppendixType,
  AskAiResponse,
  StepValue,
} from "@/types/api";

type AppendixFlowClientProps = {
  documentId: string;
  appendixType: AppendixType;
};

function triggerDownload(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

export function AppendixFlowClient({ documentId, appendixType }: AppendixFlowClientProps) {
  const plan = useAppSelector((state) => state.session.plan);

  const [flow, setFlow] = useState<AppendixFlowResponse | null>(null);
  const [values, setValues] = useState<Record<string, StepValue>>({});
  const [selectedStepKey, setSelectedStepKey] = useState<string | null>(null);
  const [preview, setPreview] = useState<AppendixPreviewResponse | null>(null);
  const [previewPdfUrl, setPreviewPdfUrl] = useState<string | null>(null);
  const [createdAppendix, setCreatedAppendix] = useState<AppendixDetailsResponse | null>(null);
  const [aiHelp, setAiHelp] = useState<AskAiResponse | null>(null);
  const [aiQuestion, setAiQuestion] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isPreviewing, setIsPreviewing] = useState(false);
  const [isLoadingPreviewPdf, setIsLoadingPreviewPdf] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const [isDownloadingPdf, setIsDownloadingPdf] = useState(false);
  const [isAsking, setIsAsking] = useState(false);
  const [previewMode, setPreviewMode] = useState<"text" | "pdf">("text");
  const [finalMode, setFinalMode] = useState<"text" | "pdf">("pdf");
  const [error, setError] = useState<string | null>(null);
  const [localNotice, setLocalNotice] = useState<string | null>(null);

  useEffect(() => {
    let isCancelled = false;

    async function loadFlow() {
      setIsLoading(true);
      setError(null);

      try {
        const [response, appendices] = await Promise.all([
          rentgenApi.getAppendixFlow(documentId, appendixType, plan),
          rentgenApi.getAppendices(documentId, plan),
        ]);

        if (isCancelled) {
          return;
        }

        setFlow(response);

        const initialValues = Object.fromEntries(
          response.steps
            .filter((step) => step.value !== undefined && step.value !== null)
            .map((step) => [step.stepKey, step.value ?? null]),
        );

        setValues(initialValues);
        setSelectedStepKey(response.steps[0]?.stepKey ?? null);

        const existing = appendices.find((item) => item.appendixType === appendixType);
        if (existing) {
          const details = await rentgenApi.getAppendix(existing.appendixId, plan);
          if (!isCancelled) {
            setCreatedAppendix(details);
          }
        }
      } catch (loadError) {
        if (!isCancelled) {
          setError(loadError instanceof Error ? loadError.message : "Не удалось загрузить сценарий приложения.");
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadFlow();

    return () => {
      isCancelled = true;
    };
  }, [appendixType, documentId, plan]);

  useEffect(() => {
    return () => {
      if (previewPdfUrl) {
        URL.revokeObjectURL(previewPdfUrl);
      }
    };
  }, [previewPdfUrl]);

  const selectedStep =
    flow?.steps.find((step) => step.stepKey === selectedStepKey) ?? flow?.steps[0] ?? null;
  const selectedIndex = selectedStep ? flow?.steps.findIndex((step) => step.stepKey === selectedStep.stepKey) ?? 0 : -1;
  const previousStep = selectedIndex > 0 ? flow?.steps[selectedIndex - 1] ?? null : null;
  const nextStep = selectedIndex >= 0 && flow && selectedIndex < flow.steps.length - 1 ? flow.steps[selectedIndex + 1] : null;
  const isLastStep = !!selectedStep && !nextStep;
  const finalAppendixPdfUrl = createdAppendix ? rentgenApi.getAppendixPdfInlineUrl(createdAppendix.appendixId) : null;
  const answeredCount = useMemo(
    () =>
      flow?.steps.filter((step) => {
        const value = values[step.stepKey];
        return value !== null && value !== undefined && `${value}`.trim() !== "";
      }).length ?? 0,
    [flow, values],
  );

  const visibleAiHelp =
    selectedStep && aiHelp && (!aiHelp.relatedStepKey || aiHelp.relatedStepKey === selectedStep.stepKey)
      ? aiHelp
      : null;
  const aiAnswerParagraphs = normalizePlainText(visibleAiHelp?.answer)
    .split(/\n{2,}/)
    .map((item) => item.trim())
    .filter(Boolean);

  async function loadPreviewPdf(nextValues: Record<string, StepValue>) {
    try {
      setIsLoadingPreviewPdf(true);
      const result = await rentgenApi.previewAppendixPdf(documentId, plan, {
        appendixType,
        answers: nextValues,
      });

      if (previewPdfUrl) {
        URL.revokeObjectURL(previewPdfUrl);
      }

      const url = URL.createObjectURL(result.blob);
      setPreviewPdfUrl(url);
    } finally {
      setIsLoadingPreviewPdf(false);
    }
  }

  async function handlePreview() {
    if (!isLastStep) {
      return;
    }

    try {
      setError(null);
      setLocalNotice(null);
      setIsPreviewing(true);

      const response = await rentgenApi.previewAppendix(documentId, plan, {
        appendixType,
        answers: values,
      });

      const nextValues = {
        ...values,
        ...response.normalizedAnswers,
      };

      setPreview(response);
      setValues(nextValues);
      setPreviewMode("text");

      const targetStep = response.missingSteps[0] ?? response.errors.find((item) => item.stepKey)?.stepKey ?? null;
      if (targetStep) {
        setSelectedStepKey(targetStep);
      }

      if (response.isReady) {
        await loadPreviewPdf(nextValues);
      } else {
        if (previewPdfUrl) {
          URL.revokeObjectURL(previewPdfUrl);
          setPreviewPdfUrl(null);
        }

        setLocalNotice("Сначала заполните обязательные поля этого документа, затем откройте юридический предпросмотр.");
      }
    } catch (previewError) {
      setError(previewError instanceof Error ? previewError.message : "Не удалось собрать предпросмотр приложения.");
    } finally {
      setIsPreviewing(false);
    }
  }

  async function handleCreate() {
    if (!isLastStep) {
      return;
    }

    try {
      setError(null);
      setLocalNotice(null);
      setIsCreating(true);

      const previewResponse = await rentgenApi.previewAppendix(documentId, plan, {
        appendixType,
        answers: values,
      });

      const nextValues = {
        ...values,
        ...previewResponse.normalizedAnswers,
      };

      setPreview(previewResponse);
      setValues(nextValues);

      if (!previewResponse.isReady) {
        const targetStep = previewResponse.missingSteps[0] ?? previewResponse.errors.find((item) => item.stepKey)?.stepKey ?? null;
        if (targetStep) {
          setSelectedStepKey(targetStep);
        }

        setLocalNotice("Сначала заполните все обязательные поля приложения.");
        return;
      }

      const created = await rentgenApi.createAppendix(documentId, appendixType, plan, nextValues);
      const details = await rentgenApi.getAppendix(created.appendixId, plan);
      setCreatedAppendix(details);
      setFinalMode("pdf");
      setLocalNotice(createdAppendix ? "Новая версия приложения сохранена." : "Приложение сохранено.");
    } catch (createError) {
      setError(createError instanceof Error ? createError.message : "Не удалось сохранить приложение.");
    } finally {
      setIsCreating(false);
    }
  }

  async function handleDownloadFinalPdf() {
    if (!createdAppendix) {
      return;
    }

    try {
      setError(null);
      setIsDownloadingPdf(true);
      const result = await rentgenApi.downloadAppendixPdf(createdAppendix.appendixId, plan);
      triggerDownload(result.blob, result.fileName);
    } catch (downloadError) {
      setError(downloadError instanceof Error ? downloadError.message : "Не удалось скачать итоговый PDF.");
    } finally {
      setIsDownloadingPdf(false);
    }
  }

  async function handleAskAi() {
    if (!selectedStep || !aiQuestion.trim()) {
      return;
    }

    try {
      setError(null);
      setIsAsking(true);
      setAiHelp(null);

      const response = await rentgenApi.askAppendixAi(documentId, plan, {
        appendixType,
        question: aiQuestion.trim(),
        stepKey: selectedStep.stepKey,
        answers: values,
      });

      setAiHelp(response);
      setAiQuestion("");
    } catch (askError) {
      setError(askError instanceof Error ? askError.message : "Не удалось получить ответ ИИ по приложению.");
    } finally {
      setIsAsking(false);
    }
  }

  return (
    <AppShell eyebrow="Приложение" title={flow?.title ?? appendixTypeLabel(appendixType)}>
      {isLoading ? (
        <LoadingPanel
          className="paper-card rounded-4xl"
          title="Загружаем сценарий приложения"
          description="Подтягиваем шаги мини-опроса и уже известные данные из основного договора."
          lines={4}
        />
      ) : flow && selectedStep ? (
        <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_420px]">
          <section className="paper-card min-w-0 overflow-hidden rounded-4xl p-6 md:p-8">
            <div className="rounded-3xl border border-[var(--line)] bg-[var(--accent-soft)]/70 p-5">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--accent-strong)]">
                Зачем нужен этот документ
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--foreground)]">{flow.description}</p>
            </div>

            <div className="mt-5 grid gap-3 md:grid-cols-3">
              <div className="rounded-[1.35rem] border border-[var(--line)] bg-white/70 p-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--accent-strong)]">
                  Текущий шаг
                </p>
                <p className="mt-2 text-lg font-semibold text-[var(--foreground)]">
                  {selectedIndex + 1} из {flow.steps.length}
                </p>
              </div>
              <div className="rounded-[1.35rem] border border-[var(--line)] bg-white/70 p-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--accent-strong)]">
                  Обязательность
                </p>
                <p className="mt-2 text-lg font-semibold text-[var(--foreground)]">
                  {selectedStep.required ? "Обязательный" : "Дополнительный"}
                </p>
              </div>
              <div className="rounded-[1.35rem] border border-[var(--line)] bg-white/70 p-4">
                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--accent-strong)]">
                  Прогресс
                </p>
                <p className="mt-2 text-lg font-semibold text-[var(--foreground)]">
                  {answeredCount} из {flow.steps.length}
                </p>
              </div>
            </div>

            <StepInput
              step={selectedStep}
              value={values[selectedStep.stepKey] ?? selectedStep.value ?? null}
              onChange={(value) =>
                setValues((current) => ({
                  ...current,
                  [selectedStep.stepKey]: value,
                }))
              }
            />

            <div className="mt-6 flex flex-wrap gap-3">
              <button
                type="button"
                onClick={() => previousStep && setSelectedStepKey(previousStep.stepKey)}
                disabled={!previousStep}
                className="rounded-full border border-[var(--line)] bg-white px-5 py-3 text-sm font-semibold text-[var(--foreground)] transition hover:bg-stone-50 disabled:cursor-not-allowed disabled:opacity-50"
              >
                Назад
              </button>
              <button
                type="button"
                onClick={() => nextStep && setSelectedStepKey(nextStep.stepKey)}
                disabled={!nextStep}
                className="rounded-full border border-[var(--line)] bg-white px-5 py-3 text-sm font-semibold text-[var(--foreground)] transition hover:bg-stone-50 disabled:cursor-not-allowed disabled:opacity-50"
              >
                Дальше
              </button>
              {isLastStep ? (
                <>
                  <button
                    type="button"
                    onClick={handlePreview}
                    disabled={isPreviewing}
                    className="rounded-full border border-[var(--line)] bg-white px-5 py-3 text-sm font-semibold text-[var(--foreground)] transition hover:bg-stone-50 disabled:cursor-not-allowed disabled:opacity-50"
                  >
                    {isPreviewing ? "Собираю..." : "Проверить и собрать предпросмотр"}
                  </button>
                  <button
                    type="button"
                    onClick={handleCreate}
                    disabled={isCreating}
                    className="rounded-full bg-[#162126] px-6 py-3 text-sm font-semibold text-white transition hover:bg-black disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    {isCreating
                      ? "Сохраняю..."
                      : createdAppendix
                        ? "Сохранить новую версию"
                        : "Сохранить приложение"}
                  </button>
                </>
              ) : null}
            </div>

            {localNotice ? (
              <div className="mt-4 rounded-[1.25rem] border border-emerald-200 bg-emerald-50 p-4 text-sm leading-6 text-emerald-950">
                {localNotice}
              </div>
            ) : null}

            {error ? (
              <div className="mt-4 rounded-[1.25rem] border border-rose-200 bg-rose-50 p-4 text-sm leading-6 text-rose-900">
                {error}
              </div>
            ) : null}

            {isLastStep && preview ? (
              <section className="mt-8 rounded-[1.6rem] border border-[var(--line)] bg-white/85 p-5">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--accent-strong)]">
                      Юридический предпросмотр
                    </p>
                    <h3 className="font-editorial mt-2 text-2xl text-[var(--foreground)]">
                      {preview.title}
                    </h3>
                  </div>

                  {preview.isReady ? (
                    <div className="inline-flex rounded-full border border-[var(--line)] bg-white/70 p-1 shadow-sm">
                      <button
                        type="button"
                        onClick={() => setPreviewMode("text")}
                        className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
                          previewMode === "text"
                            ? "bg-[var(--accent)] text-white"
                            : "text-[var(--muted)] hover:bg-[var(--accent-soft)]"
                        }`}
                      >
                        Текст
                      </button>
                      <button
                        type="button"
                        onClick={() => setPreviewMode("pdf")}
                        className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
                          previewMode === "pdf"
                            ? "bg-[var(--accent)] text-white"
                            : "text-[var(--muted)] hover:bg-[var(--accent-soft)]"
                        }`}
                      >
                        PDF
                      </button>
                    </div>
                  ) : null}
                </div>

                {preview.errors.length > 0 ? (
                  <div className="mt-4 flex flex-col gap-3">
                    {preview.errors.map((item, index) => (
                      <div
                        key={`${item.stepKey ?? "error"}-${index}`}
                        className="rounded-[1.2rem] border border-amber-200 bg-amber-50 p-4 text-sm leading-6 text-amber-950"
                      >
                        {item.message}
                      </div>
                    ))}
                  </div>
                ) : null}

                {preview.isReady && previewMode === "pdf" ? (
                  <div className="mt-5 overflow-hidden rounded-3xl border border-[var(--line)] bg-white">
                    {previewPdfUrl ? (
                      <iframe
                        key={previewPdfUrl}
                        title="PDF предпросмотра приложения"
                        src={previewPdfUrl}
                        className="h-[64vh] w-full bg-white"
                      />
                    ) : (
                      <div className="p-6 text-sm text-[var(--muted)]">
                        {isLoadingPreviewPdf ? "Готовим PDF..." : "PDF пока не готов."}
                      </div>
                    )}
                  </div>
                ) : (
                  <div className="mt-5 overflow-hidden rounded-3xl border border-[var(--line)] bg-white/80 p-4">
                    <pre className="whitespace-pre-wrap break-words text-sm leading-7 text-[var(--foreground)]">
                      {preview.content}
                    </pre>
                  </div>
                )}
              </section>
            ) : null}

            {createdAppendix ? (
              <section className="mt-8 rounded-[1.6rem] border border-[var(--line)] bg-white/85 p-5">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--accent-strong)]">
                      Итоговый документ
                    </p>
                    <h3 className="font-editorial mt-2 text-2xl text-[var(--foreground)]">
                      {createdAppendix.title}
                    </h3>
                  </div>

                  <div className="flex flex-wrap gap-2">
                    <div className="inline-flex rounded-full border border-[var(--line)] bg-white/70 p-1 shadow-sm">
                      <button
                        type="button"
                        onClick={() => setFinalMode("text")}
                        className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
                          finalMode === "text"
                            ? "bg-[var(--accent)] text-white"
                            : "text-[var(--muted)] hover:bg-[var(--accent-soft)]"
                        }`}
                      >
                        Текст
                      </button>
                      <button
                        type="button"
                        onClick={() => setFinalMode("pdf")}
                        className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
                          finalMode === "pdf"
                            ? "bg-[var(--accent)] text-white"
                            : "text-[var(--muted)] hover:bg-[var(--accent-soft)]"
                        }`}
                      >
                        PDF
                      </button>
                    </div>
                    <button
                      type="button"
                      onClick={handleDownloadFinalPdf}
                      disabled={isDownloadingPdf}
                      className="rounded-full border border-[var(--line)] bg-white px-4 py-2 text-sm font-semibold text-[var(--foreground)] transition hover:bg-stone-50 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      {isDownloadingPdf ? "Скачиваю PDF..." : "Скачать PDF"}
                    </button>
                  </div>
                </div>

                {finalMode === "pdf" && finalAppendixPdfUrl ? (
                  <div className="mt-5 overflow-hidden rounded-3xl border border-[var(--line)] bg-white">
                    <iframe
                      key={finalAppendixPdfUrl}
                      title="PDF итогового приложения"
                      src={finalAppendixPdfUrl}
                      className="h-[64vh] w-full bg-white"
                    />
                  </div>
                ) : (
                  <div className="mt-5 overflow-hidden rounded-3xl border border-[var(--line)] bg-white/80 p-4">
                    <pre className="whitespace-pre-wrap break-words text-sm leading-7 text-[var(--foreground)]">
                      {createdAppendix.content}
                    </pre>
                  </div>
                )}
              </section>
            ) : null}
          </section>

          <aside className="min-w-0 self-start xl:sticky xl:top-6">
            <div className="flex flex-col gap-6">
              <section className="paper-card rounded-4xl p-5">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--accent-strong)]">
                  Шаги приложения
                </p>
                <div className="mt-4 flex flex-col gap-2">
                  {flow.steps.map((step) => {
                    const stepValue = values[step.stepKey];
                    const isAnswered = stepValue !== null && stepValue !== undefined && `${stepValue}`.trim() !== "";
                    const isActive = step.stepKey === selectedStepKey;

                    return (
                      <button
                        key={step.stepKey}
                        type="button"
                        onClick={() => setSelectedStepKey(step.stepKey)}
                        className={`rounded-[1.25rem] border px-4 py-3 text-left transition ${
                          isActive
                            ? "border-[var(--accent)] bg-[var(--accent-soft)]"
                            : "border-[var(--line)] bg-white/70 hover:bg-white"
                        }`}
                      >
                        <p className="text-sm font-semibold text-[var(--foreground)]">{step.title}</p>
                        <p className="mt-1 text-xs text-[var(--muted)]">
                          {isAnswered ? "Заполнено" : "Ожидает ответа"}
                        </p>
                      </button>
                    );
                  })}
                </div>
              </section>

              <section className="paper-card rounded-4xl p-5">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--accent-strong)]">
                  AI Help
                </p>
                <h3 className="font-editorial mt-2 text-3xl leading-tight text-[var(--foreground)]">
                  Уточнение по текущему шагу
                </h3>
                <p className="mt-4 text-sm leading-7 text-[var(--muted)]">
                  DeepSeek получает контекст текущего шага, уже собранные факты и твой вопрос.
                </p>

                <textarea
                  value={aiQuestion}
                  onChange={(event) => setAiQuestion(event.target.value)}
                  placeholder="Например: зачем нужен этот пункт и как лучше ответить?"
                  rows={4}
                  className="mt-5 w-full rounded-3xl border border-[var(--line)] bg-white px-4 py-4 text-sm leading-7 text-[var(--foreground)] outline-none transition focus:border-[var(--accent)] focus:ring-2 focus:ring-[var(--accent-soft)]"
                />

                <button
                  type="button"
                  onClick={handleAskAi}
                  disabled={isAsking || !aiQuestion.trim()}
                  className="mt-5 rounded-full bg-[var(--accent)] px-6 py-3 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)] disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {isAsking ? "Спрашиваю..." : "Задать вопрос ИИ"}
                </button>

                {visibleAiHelp ? (
                  <div className="mt-6 rounded-3xl border border-[var(--line)] bg-white/85 p-4">
                    <div className="flex flex-wrap items-center justify-between gap-3">
                      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--muted)]">
                        {visibleAiHelp.source === "deepseek" ? "Ответ ассистента" : "Резервная подсказка"}
                      </p>
                      <span
                        className={`rounded-full px-3 py-1 text-[10px] font-semibold uppercase tracking-[0.18em] ${
                          visibleAiHelp.source === "deepseek"
                            ? "bg-emerald-100 text-emerald-900"
                            : "bg-amber-100 text-amber-900"
                        }`}
                      >
                        {visibleAiHelp.source === "deepseek" ? "live" : "fallback"}
                      </span>
                    </div>
                    <div className="mt-4 max-h-[36rem] space-y-4 overflow-y-auto pr-2 text-sm leading-7 text-[var(--foreground)]">
                      {aiAnswerParagraphs.map((paragraph, index) => (
                        <p key={`${index}-${paragraph.slice(0, 24)}`}>{paragraph}</p>
                      ))}
                    </div>
                  </div>
                ) : (
                  <EmptyState
                    className="mt-6"
                    title="Помощник пока не вызывался"
                    description="Задайте вопрос по текущему шагу, и ИИ объяснит, зачем нужен этот пункт и как лучше ответить."
                  />
                )}

                <div className="mt-6 border-t border-[var(--line)] pt-5 text-sm">
                  <Link className="font-semibold text-[var(--accent-strong)] transition hover:opacity-80" href={`/documents/${documentId}`}>
                    Вернуться к документу
                  </Link>
                </div>
              </section>
            </div>
          </aside>
        </div>
      ) : (
        <EmptyState title="Сценарий недоступен" description="Не удалось загрузить шаги приложения." />
      )}
    </AppShell>
  );
}
