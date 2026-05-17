"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { AppShell } from "@/components/app-shell";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingPanel } from "@/components/ui/loading-panel";
import { StatusPill } from "@/components/ui/status-pill";
import { rentgenApi } from "@/lib/api/rentgen-api";
import {
  appendixTypeLabel,
  documentTypeLabel,
  formatDate,
  splitParagraphs,
} from "@/lib/presenters";
import { useAppSelector } from "@/store/hooks";
import type { AppendixType, DocumentDetailsResponse } from "@/types/api";
import {
  AppendixType as AppendixTypeEnum,
  GuideType,
  SubscriptionPlan,
} from "@/types/api";

const appendixCatalog: AppendixType[] = [
  AppendixTypeEnum.HandoverAct,
  AppendixTypeEnum.InventoryList,
  AppendixTypeEnum.PetAddendum,
  AppendixTypeEnum.PaymentSchedule,
  AppendixTypeEnum.DepositAgreement,
  AppendixTypeEnum.HouseRules,
];

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

export function DocumentPageClient({ documentId }: { documentId: string }) {
  const plan = useAppSelector((state) => state.session.plan);

  const [document, setDocument] = useState<DocumentDetailsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isDownloadingDocumentPdf, setIsDownloadingDocumentPdf] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [previewMode, setPreviewMode] = useState<"pdf" | "text">("pdf");

  useEffect(() => {
    let isCancelled = false;

    async function loadDocument() {
      setIsLoading(true);
      setError(null);

      try {
        const response = await rentgenApi.getDocument(documentId, plan);
        if (!isCancelled) {
          setDocument(response);
        }
      } catch (loadError) {
        if (!isCancelled) {
          setError(
            loadError instanceof Error
              ? loadError.message
              : "Не удалось загрузить документ.",
          );
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }

    void loadDocument();

    return () => {
      isCancelled = true;
    };
  }, [documentId, plan]);

  async function handleDownloadDocumentPdf() {
    try {
      setError(null);
      setIsDownloadingDocumentPdf(true);
      const result = await rentgenApi.downloadDocumentPdf(documentId, plan);
      triggerDownload(result.blob, result.fileName);
    } catch (downloadError) {
      setError(
        downloadError instanceof Error
          ? downloadError.message
          : "Не удалось скачать PDF договора.",
      );
    } finally {
      setIsDownloadingDocumentPdf(false);
    }
  }

  const guideParagraphs = useMemo(
    () => splitParagraphs(document?.guide?.content),
    [document?.guide?.content],
  );

  const existingAppendixTypes = new Set(
    document?.appendices.map((item) => item.appendixType) ?? [],
  );
  const documentPdfUrl = rentgenApi.getDocumentPdfInlineUrl(documentId);
  const planLabel =
    document?.plan === SubscriptionPlan.Premium ? "Премиум" : "Бесплатно";

  return (
    <AppShell
      eyebrow="Document preview"
      title="Предпросмотр договора и приложений"
      subtitle="Откройте итоговый договор, скачайте PDF и при необходимости перейдите в отдельные формы приложений."
    >
      {isLoading ? (
        <LoadingPanel
          className="paper-card rounded-4xl"
          title="Загружаем документ"
          description="Подтягиваем договор, мини-гайд и связанные приложения."
          lines={4}
        />
      ) : document ? (
        <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_380px]">
          <section className="paper-card min-w-0 overflow-hidden rounded-4xl p-5 sm:p-6 md:p-8">
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div className="min-w-0 flex-1">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--accent-strong)]">
                  {documentTypeLabel(document.documentType)}
                </p>
                <h2 className="font-editorial mt-2 break-words text-4xl leading-tight text-[var(--foreground)]">
                  {document.title}
                </h2>
                <p className="mt-3 break-words text-sm leading-6 text-[var(--muted)]">
                  Сформирован: {formatDate(document.generatedAtUtc)}
                </p>
              </div>

              <div className="flex flex-col items-end gap-3">
                <StatusPill status={document.status} />
              </div>
            </div>

            <div className="mt-6 flex flex-wrap gap-2 text-xs font-semibold text-[var(--muted)]">
              <span className="rounded-full bg-stone-100 px-3 py-1">
                тариф: {planLabel}
              </span>
              <span className="rounded-full bg-stone-100 px-3 py-1">
                приложений: {document.appendices.length}
              </span>
            </div>

            <div className="mt-6 flex flex-wrap items-center gap-2 sm:mt-8 sm:gap-3">
              <div className="inline-flex rounded-full border border-[var(--line)] bg-white/70 p-1 shadow-sm">
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
              </div>

              <button
                type="button"
                onClick={handleDownloadDocumentPdf}
                disabled={isDownloadingDocumentPdf}
                className="rounded-full border border-[var(--line)] bg-white px-4 py-2 text-sm font-semibold text-[var(--foreground)] transition hover:bg-stone-50 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isDownloadingDocumentPdf ? "Скачиваю PDF..." : "Скачать PDF"}
              </button>

              {document.draftId ? (
                <Link
                  href={`/drafts/${document.draftId}`}
                  className="rounded-full border border-[var(--line)] bg-white px-4 py-2 text-sm font-semibold text-[var(--foreground)] transition hover:bg-stone-50"
                >
                  Обновить договор
                </Link>
              ) : null}
            </div>

            {previewMode === "pdf" ? (
              <div className="mt-6 overflow-hidden rounded-[1.75rem] border border-[var(--line)] bg-white shadow-inner">
                <iframe
                  key={documentPdfUrl}
                  title="PDF договора"
                  src={documentPdfUrl}
                  className="h-[60vh] w-full bg-white sm:h-[70vh] md:h-[78vh]"
                />
              </div>
            ) : (
              <div className="mt-6 min-w-0 overflow-hidden rounded-[1.75rem] border border-[var(--line)] bg-[#fffefb] p-6 shadow-inner">
                <pre className="whitespace-pre-wrap break-words text-sm leading-7 text-[var(--foreground)]">
                  {document.content}
                </pre>
              </div>
            )}
          </section>

          <aside className="min-w-0 self-start xl:sticky xl:top-6">
            <div className="flex flex-col gap-6">
              <section className="paper-card min-w-0 overflow-hidden rounded-4xl p-5">
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--accent-strong)]">
                  Мини-гайд
                </p>
                <h3 className="font-editorial mt-2 break-words text-2xl text-[var(--foreground)]">
                  Что делать дальше
                </h3>
                {document.guide ? (
                  <p className="mt-2 text-sm text-[var(--muted)]">
                    {document.guide.guideType === GuideType.Personalized
                      ? "Персонализированный гайд"
                      : "Стандартный гайд"}
                  </p>
                ) : null}

                {document.guide ? (
                  <div className="mt-4 min-w-0 overflow-hidden rounded-[1.4rem] border border-[var(--line)] bg-white/80 p-4">
                    <div className="space-y-4">
                      {guideParagraphs.map((paragraph, index) => (
                        <p
                          key={`${index}-${paragraph.slice(0, 32)}`}
                          className="break-words text-sm leading-7 text-[var(--foreground)]"
                        >
                          {paragraph}
                        </p>
                      ))}
                    </div>
                  </div>
                ) : (
                  <EmptyState
                    className="mt-4"
                    eyebrow="Guide"
                    title="Гайд пока не загружен"
                    description="Если документ был сформирован без guide, его можно запросить позже."
                  />
                )}
              </section>

              <section className="paper-card min-w-0 overflow-hidden rounded-4xl p-5">
                <div className="flex items-start justify-between gap-4">
                  <div className="min-w-0 flex-1">
                    <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--accent-strong)]">
                      Приложения
                    </p>
                    <h3 className="font-editorial mt-2 break-words text-2xl text-[var(--foreground)]">
                      Дополнительные документы
                    </h3>
                  </div>

                  <span className="rounded-full bg-[var(--accent-soft)] px-3 py-1 text-xs font-semibold text-[var(--accent-strong)]">
                    {document.appendices.length}
                  </span>
                </div>

                {plan !== "Premium" ? (
                  <div className="mt-5 rounded-[1.3rem] border border-amber-200 bg-amber-50 p-4 text-sm leading-6 text-amber-950">
                    Сейчас включен режим <strong>Free</strong>. Дополнительные
                    приложения доступны только в Premium.
                  </div>
                ) : null}

                <div className="mt-5 flex flex-col gap-3">
                  {appendixCatalog.map((appendixType) => {
                    const exists = existingAppendixTypes.has(appendixType);

                    return (
                      <div
                        key={appendixType}
                        className="min-w-0 rounded-[1.3rem] border border-[var(--line)] bg-white/80 p-4"
                      >
                        <p className="break-words font-semibold text-[var(--foreground)]">
                          {appendixTypeLabel(appendixType)}
                        </p>
                        <div className="mt-3 flex flex-wrap gap-2">
                          <Link
                            href={
                              plan === "Premium"
                                ? `/documents/${documentId}/appendices/${appendixType}`
                                : "#"
                            }
                            aria-disabled={plan !== "Premium"}
                            className={`rounded-full px-4 py-2 text-xs font-semibold transition ${
                              plan === "Premium"
                                ? "bg-[var(--accent)] text-white hover:bg-[var(--accent-strong)]"
                                : "cursor-not-allowed bg-stone-300 text-white"
                            }`}
                          >
                            {exists ? "Открыть форму" : "Заполнить данные"}
                          </Link>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </section>

              {error ? (
                <div className="rounded-[1.3rem] border border-rose-200 bg-rose-50 p-4 text-sm leading-6 text-rose-900">
                  {error}
                </div>
              ) : null}
            </div>
          </aside>
        </div>
      ) : (
        <EmptyState
          title="Документ не найден"
          description="Проверьте ссылку или сформируйте договор заново."
        />
      )}
    </AppShell>
  );
}
