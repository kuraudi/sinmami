"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { motion, AnimatePresence } from "framer-motion";
import { FileText, Layers, LogOut, ArrowRight, Sparkles, Star, CheckCircle2, Crown } from "lucide-react";
import { AppShell } from "@/components/app-shell";
import { StatusPill } from "@/components/ui/status-pill";
import { LoadingPanel } from "@/components/ui/loading-panel";
import { EmptyState } from "@/components/ui/empty-state";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import { logout } from "@/store/slices/auth-slice";
import { rentgenApi } from "@/lib/api/rentgen-api";
import { getLocalDocumentIds } from "@/lib/local-history";
import { documentTypeLabel, formatDate } from "@/lib/presenters";
import type { DocumentListItemResponse } from "@/types/api";

type UpgradeStep = "idle" | "payment" | "success";

const PREMIUM_FEATURES = [
  "Расширенные разделы договора",
  "Персонализированный гайд арендодателя",
  "Приложения: акты, описи, регламент",
  "AI-помощник при заполнении",
];

export function AccountClient() {
  const router = useRouter();
  const dispatch = useAppDispatch();
  const { user, isAuthenticated } = useAppSelector((s) => s.auth);
  const plan = useAppSelector((s) => s.session.plan);

  const [documents, setDocuments] = useState<DocumentListItemResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [upgradeStep, setUpgradeStep] = useState<UpgradeStep>("idle");

  useEffect(() => {
    if (!isAuthenticated) {
      router.push("/login");
      return;
    }

    async function load() {
      setIsLoading(true);
      try {
        const localIds = getLocalDocumentIds();
        if (localIds.length > 0) {
          const all = await rentgenApi.getDocuments(plan);
          setDocuments(all.filter((d) => localIds.includes(d.documentId)));
        }
      } finally {
        setIsLoading(false);
      }
    }
    void load();
  }, [isAuthenticated, plan, router]);

  function handleLogout() {
    dispatch(logout());
    router.push("/");
  }

  const isPremium = plan === "Premium";

  return (
    <AppShell eyebrow="Личный кабинет" title="Мой аккаунт">
      <div className="flex flex-col gap-5">

        {/* Профиль */}
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          className="rounded-3xl border border-(--line) bg-white p-6 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4"
        >
          <div className="flex items-center gap-4">
            <div className="flex h-14 w-14 items-center justify-center rounded-2xl text-white text-xl font-bold"
              style={{ background: "var(--gradient)" }}>
              {user?.fullName?.[0]?.toUpperCase() ?? user?.email?.[0]?.toUpperCase() ?? "U"}
            </div>
            <div>
              <p className="font-semibold text-foreground">{user?.fullName ?? "Пользователь"}</p>
              <p className="text-sm text-(--muted)">{user?.email}</p>
              <span className="mt-1 inline-flex rounded-full bg-indigo-50 px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-widest text-indigo-600">
                {user?.plan ?? "Free"}
              </span>
            </div>
          </div>

          <button
            type="button"
            onClick={handleLogout}
            className="inline-flex items-center gap-2 rounded-2xl border border-(--line) px-4 py-2.5 text-sm font-semibold text-(--muted) transition hover:bg-red-50 hover:border-red-200 hover:text-red-600"
          >
            <LogOut size={14} strokeWidth={1.75} />
            Выйти
          </button>
        </motion.div>

        {/* Блок тарифа */}
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.04 }}
          className="overflow-hidden rounded-3xl border border-(--line)"
        >
          {isPremium ? (
            /* Premium статус */
            <div className="relative overflow-hidden p-6"
              style={{ background: "linear-gradient(135deg, #1e1b4b 0%, #4338ca 100%)" }}>
              <div className="pointer-events-none absolute -right-10 -top-10 h-40 w-40 rounded-full"
                style={{ background: "radial-gradient(circle, rgba(139,92,246,0.35) 0%, transparent 70%)", filter: "blur(24px)" }} />
              <div className="relative flex items-center justify-between gap-4">
                <div className="flex items-center gap-4">
                  <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/10">
                    <Crown size={22} strokeWidth={1.75} className="text-amber-300" />
                  </div>
                  <div>
                    <p className="text-[10px] font-bold uppercase tracking-[0.22em] text-white/50">Тариф</p>
                    <p className="text-xl font-bold text-white">Premium</p>
                    <p className="text-sm text-white/60">Все возможности открыты</p>
                  </div>
                </div>
                <div className="flex flex-col gap-1 text-right">
                  <span className="rounded-full bg-white/10 px-3 py-1 text-xs font-semibold text-white">Активен</span>
                </div>
              </div>
            </div>
          ) : (
            /* Free + апгрейд */
            <AnimatePresence mode="wait">
              {upgradeStep === "idle" && (
                <motion.div key="idle" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
                  className="bg-white p-6">
                  <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
                    <div className="flex items-center gap-4">
                      <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-stone-100">
                        <Star size={20} strokeWidth={1.75} className="text-stone-400" />
                      </div>
                      <div>
                        <p className="text-[10px] font-bold uppercase tracking-[0.22em] text-stone-400">Тариф</p>
                        <p className="text-xl font-bold text-foreground">Free</p>
                        <p className="text-sm text-(--muted)">Базовый договор</p>
                      </div>
                    </div>
                    <button
                      type="button"
                      onClick={() => setUpgradeStep("payment")}
                      className="inline-flex shrink-0 items-center gap-2 rounded-2xl px-5 py-2.5 text-sm font-bold text-white transition hover:opacity-90"
                      style={{ background: "linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%)", color: "#ffffff", boxShadow: "0 4px 16px rgba(79,70,229,0.35)" }}
                    >
                      <Sparkles size={14} strokeWidth={1.75} />
                      Улучшить до Premium
                    </button>
                  </div>

                  <div className="mt-5 grid grid-cols-1 gap-2 sm:grid-cols-2 border-t border-stone-100 pt-5">
                    {PREMIUM_FEATURES.map((f) => (
                      <div key={f} className="flex items-center gap-2.5 text-xs text-stone-500">
                        <div className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full bg-indigo-50">
                          <Sparkles size={10} strokeWidth={2} className="text-indigo-500" />
                        </div>
                        {f}
                      </div>
                    ))}
                  </div>
                </motion.div>
              )}

              {upgradeStep === "payment" && (
                <motion.div key="payment" initial={{ opacity: 0, y: 8 }} animate={{ opacity: 1, y: 0 }} exit={{ opacity: 0 }}
                  className="bg-white p-6">
                  <div className="relative overflow-hidden rounded-2xl p-5 mb-5"
                    style={{ background: "linear-gradient(135deg, #1e1b4b 0%, #4338ca 100%)" }}>
                    <p className="text-[10px] font-bold uppercase tracking-[0.22em] text-white/50">Оплата Premium</p>
                    <div className="mt-2 flex items-baseline gap-1.5">
                      <span className="text-3xl font-bold text-white">990 ₽</span>
                      <span className="text-sm text-white/50">/ месяц</span>
                    </div>
                    <p className="mt-1 text-sm text-white/60">Отменить можно в любое время</p>
                  </div>

                  <div className="flex flex-col gap-3">
                    <button
                      type="button"
                      onClick={() => setUpgradeStep("success")}
                      className="inline-flex w-full items-center justify-center gap-2 rounded-2xl py-3 text-sm font-bold transition hover:opacity-90 active:scale-[0.98]"
                      style={{ background: "linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%)", color: "#ffffff", boxShadow: "0 4px 16px rgba(79,70,229,0.4)" }}
                    >
                      <CheckCircle2 size={15} strokeWidth={2} />
                      Подтвердить оплату
                    </button>
                    <button
                      type="button"
                      onClick={() => setUpgradeStep("idle")}
                      className="w-full rounded-2xl border border-stone-200 py-2.5 text-sm font-semibold text-stone-600 transition hover:bg-stone-50"
                    >
                      Отмена
                    </button>
                  </div>
                </motion.div>
              )}

              {upgradeStep === "success" && (
                <motion.div key="success" initial={{ opacity: 0, scale: 0.97 }} animate={{ opacity: 1, scale: 1 }}
                  className="flex flex-col items-center gap-4 bg-white p-8 text-center">
                  <motion.div
                    initial={{ scale: 0 }}
                    animate={{ scale: 1 }}
                    transition={{ type: "spring", stiffness: 260, damping: 20 }}
                    className="flex h-16 w-16 items-center justify-center rounded-full bg-emerald-100"
                  >
                    <CheckCircle2 size={32} strokeWidth={1.75} className="text-emerald-600" />
                  </motion.div>
                  <div>
                    <p className="text-xl font-bold text-foreground">Готово!</p>
                    <p className="mt-1.5 text-sm text-(--muted) leading-6">
                      Оплата прошла. Тариф Premium активирован.<br />
                      Обновите страницу или переключите тариф вверху.
                    </p>
                  </div>
                </motion.div>
              )}
            </AnimatePresence>
          )}
        </motion.div>

        {/* История документов */}
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.07 }}
          className="rounded-3xl border border-(--line) bg-white p-6"
        >
          <div className="flex items-center gap-3 mb-5">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl text-indigo-600"
              style={{ background: "var(--primary-soft)" }}>
              <FileText size={18} strokeWidth={1.75} />
            </div>
            <div>
              <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-indigo-500">История</p>
              <h2 className="font-editorial text-xl text-foreground">Мои договоры</h2>
            </div>
          </div>

          {isLoading ? (
            <LoadingPanel title="Загружаем историю" description="Подтягиваем ваши документы." lines={3} />
          ) : documents.length === 0 ? (
            <EmptyState
              eyebrow="Пока пусто"
              title="Договоров нет"
              description="Создайте первый договор — он появится здесь."
            />
          ) : (
            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
              {documents.map((doc) => (
                <motion.button
                  key={doc.documentId}
                  type="button"
                  onClick={() => router.push(`/documents/${doc.documentId}`)}
                  whileHover={{ y: -2, boxShadow: "0 8px 30px rgba(79,70,229,0.10)" }}
                  transition={{ duration: 0.2 }}
                  className="group cursor-pointer rounded-2xl border border-(--line) bg-stone-50 px-5 py-4 text-left transition hover:border-indigo-300"
                >
                  <div className="flex items-start justify-between gap-2">
                    <p className="text-sm font-semibold text-foreground group-hover:text-indigo-600">
                      {doc.title}
                    </p>
                    <StatusPill status={doc.status} />
                  </div>
                  <p className="mt-1.5 text-xs text-(--muted)">
                    {documentTypeLabel(doc.documentType)} · {formatDate(doc.createdAtUtc)}
                  </p>
                  <div className="mt-3 flex gap-1.5">
                    <span className="inline-flex items-center gap-1 rounded-full border border-(--line) bg-white px-2.5 py-0.5 text-[11px] text-(--muted)">
                      <Layers size={10} />
                      приложений: {doc.appendicesCount}
                    </span>
                  </div>
                </motion.button>
              ))}
            </div>
          )}
        </motion.div>

        {/* CTA создать новый */}
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.12 }}
          className="rounded-3xl border border-(--line) bg-white p-6 flex flex-col sm:flex-row items-center justify-between gap-4"
        >
          <div>
            <p className="font-semibold text-foreground">Нужен новый договор?</p>
            <p className="text-sm text-(--muted)">Займёт 5–10 минут</p>
          </div>
          <button
            type="button"
            onClick={() => router.push("/")}
            className="inline-flex items-center gap-2 rounded-2xl px-6 py-3 text-sm font-semibold text-white transition hover:opacity-90"
            style={{ background: "var(--gradient)" }}
          >
            Создать договор
            <ArrowRight size={14} strokeWidth={2} />
          </button>
        </motion.div>

      </div>
    </AppShell>
  );
}
