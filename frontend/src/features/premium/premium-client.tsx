"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { motion } from "framer-motion";
import {
  CheckCircle2, ArrowRight, Sparkles, FileText,
  ClipboardList, Home, CalendarDays, Banknote, PawPrint, Star,
  Shield, Zap, Users,
} from "lucide-react";
import { AppShell } from "@/components/app-shell";
import { rentgenApi } from "@/lib/api/rentgen-api";
import { DocumentType } from "@/types/api";
import { useAppSelector } from "@/store/hooks";

const FREE_FEATURES = [
  "Договор аренды жилого помещения",
  "Пошаговое заполнение с подсказками",
  "AI-объяснение каждого пункта",
  "Проверка полноты данных",
  "PDF-экспорт",
];

const PREMIUM_FEATURES = [
  { icon: ClipboardList, label: "Акт приёма-передачи квартиры" },
  { icon: Home, label: "Опись имущества" },
  { icon: CalendarDays, label: "График арендных платежей" },
  { icon: Banknote, label: "Соглашение об обеспечительном платеже" },
  { icon: PawPrint, label: "Приложение о проживании с животными" },
  { icon: Star, label: "Правила проживания" },
];

const REVIEWS = [
  { name: "Алексей М.", text: "Составил договор за 7 минут. Раньше платил юристу 5000₽ за то же самое.", role: "Арендодатель" },
  { name: "Екатерина С.", text: "Акт приёма-передачи спас меня — арендатор не смог предъявить претензии по царапинам.", role: "Арендодатель" },
  { name: "Дмитрий К.", text: "Наконец понял все пункты договора. AI объясняет понятным языком.", role: "Арендатор" },
];

const fadeUp = {
  hidden: { opacity: 0, y: 16 },
  show: { opacity: 1, y: 0 },
};

export function PremiumClient() {
  const router = useRouter();
  const plan = useAppSelector((s) => s.session.plan);
  const [isCreating, setIsCreating] = useState(false);

  async function handleStart() {
    try {
      setIsCreating(true);
      const draft = await rentgenApi.createDraft(DocumentType.RentalAgreement, plan);
      router.push(`/drafts/${draft.draftId}`);
    } catch {
      setIsCreating(false);
    }
  }

  return (
    <AppShell eyebrow="Тарифы" title="Полный пакет документов">
      <div className="flex flex-col gap-6">

        {/* Hero */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.4 }}
          className="relative overflow-hidden rounded-3xl p-6 sm:p-10 text-white text-center"
          style={{ background: "linear-gradient(135deg, #1e1b4b 0%, #4f46e5 100%)" }}
        >
          <div className="pointer-events-none absolute -top-20 left-1/2 -translate-x-1/2 h-80 w-80 rounded-full"
            style={{ background: "radial-gradient(circle, rgba(139,92,246,0.35) 0%, transparent 65%)", filter: "blur(50px)" }} />
          <div className="relative">
            <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-white/10">
              <Sparkles size={20} strokeWidth={1.75} />
            </div>
            <h2 className="font-editorial text-3xl sm:text-4xl leading-tight mb-3">
              Создайте идеальный договор аренды<br className="hidden sm:block" /> за 10 минут
            </h2>
            <p className="text-sm text-white/70 leading-7 max-w-xl mx-auto">
              Бесплатно. Без скрытых условий. С гайдом по сделке и полным пакетом документов.
            </p>
            <div className="mt-6 flex flex-wrap justify-center gap-3 text-xs text-white/55">
              <span className="flex items-center gap-1.5"><Shield size={12} />Юридически грамотно</span>
              <span className="flex items-center gap-1.5"><Zap size={12} />За 10 минут</span>
              <span className="flex items-center gap-1.5"><Users size={12} />1200+ договоров</span>
            </div>
          </div>
        </motion.div>

        {/* Тарифы */}
        <div className="grid gap-4 sm:grid-cols-2">

          {/* Free */}
          <motion.div variants={fadeUp} initial="hidden" animate="show" transition={{ duration: 0.35, delay: 0.05 }}
            className="rounded-3xl border border-(--line) bg-white p-6 flex flex-col">
            <div className="mb-4">
              <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-(--muted) mb-1">Базовый</p>
              <p className="text-3xl font-bold text-foreground">Бесплатно</p>
              <p className="mt-1 text-sm text-(--muted)">Основной договор аренды</p>
            </div>
            <div className="flex flex-col gap-2.5 flex-1">
              {FREE_FEATURES.map((f) => (
                <div key={f} className="flex items-start gap-2.5">
                  <CheckCircle2 size={14} strokeWidth={2} className="mt-0.5 shrink-0 text-emerald-500" />
                  <span className="text-sm text-foreground">{f}</span>
                </div>
              ))}
            </div>
            <button
              type="button"
              onClick={handleStart}
              disabled={isCreating}
              className="mt-6 inline-flex w-full items-center justify-center gap-2 rounded-2xl border border-(--line) py-3 text-sm font-semibold text-foreground transition hover:bg-stone-50 disabled:opacity-50"
            >
              {isCreating ? "Создаём..." : "Начать бесплатно"}
              <ArrowRight size={14} strokeWidth={2} />
            </button>
          </motion.div>

          {/* Premium */}
          <motion.div variants={fadeUp} initial="hidden" animate="show" transition={{ duration: 0.35, delay: 0.1 }}
            className="relative rounded-3xl overflow-hidden p-6 flex flex-col text-white"
            style={{ background: "linear-gradient(135deg, #1e1b4b 0%, #312e81 100%)" }}>
            <div className="pointer-events-none absolute -top-12 -right-12 h-48 w-48 rounded-full"
              style={{ background: "radial-gradient(circle, rgba(139,92,246,0.3) 0%, transparent 65%)", filter: "blur(30px)" }} />
            <div className="relative">
              <div className="flex items-center justify-between mb-4">
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-indigo-300 mb-1">Premium</p>
                  <p className="text-3xl font-bold">Полный пакет</p>
                  <p className="mt-1 text-sm text-white/60">Договор + все приложения</p>
                </div>
                <span className="rounded-full bg-amber-400/20 border border-amber-400/30 px-3 py-1 text-xs font-bold text-amber-300">
                  Скоро
                </span>
              </div>

              <p className="text-xs font-bold uppercase tracking-[0.16em] text-indigo-300 mb-3">Включает все документы</p>
              <div className="flex flex-col gap-2.5">
                {PREMIUM_FEATURES.map(({ icon: Icon, label }) => (
                  <div key={label} className="flex items-center gap-3">
                    <div className="flex h-6 w-6 shrink-0 items-center justify-center rounded-lg bg-white/10">
                      <Icon size={12} strokeWidth={1.75} className="text-indigo-300" />
                    </div>
                    <span className="text-sm text-white/85">{label}</span>
                  </div>
                ))}
              </div>

              <div className="mt-5 flex flex-col gap-3">
                <div className="flex items-center gap-2 rounded-2xl bg-white/10 px-4 py-3">
                  <Sparkles size={14} strokeWidth={1.75} className="text-indigo-300 shrink-0" />
                  <span className="text-sm text-white/70">Персонализированный AI-гайд по сделке</span>
                </div>
                <div className="flex items-center gap-2 rounded-2xl bg-white/10 px-4 py-3">
                  <FileText size={14} strokeWidth={1.75} className="text-indigo-300 shrink-0" />
                  <span className="text-sm text-white/70">ZIP-архив всех документов в PDF и DOCX</span>
                </div>
              </div>

              <button
                type="button"
                disabled
                className="mt-5 inline-flex w-full items-center justify-center gap-2 rounded-2xl bg-white/15 border border-white/20 py-3 text-sm font-semibold text-white/60 cursor-not-allowed"
              >
                Скоро будет доступно
              </button>
            </div>
          </motion.div>
        </div>

        {/* Отзывы */}
        <motion.div variants={fadeUp} initial="hidden" animate="show" transition={{ duration: 0.35, delay: 0.15 }}
          className="rounded-3xl border border-(--line) bg-white p-6 sm:p-8">
          <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-indigo-500 mb-4">Что говорят пользователи</p>
          <div className="grid gap-4 sm:grid-cols-3">
            {REVIEWS.map((r) => (
              <div key={r.name} className="rounded-2xl border border-(--line) bg-stone-50 p-4">
                <p className="text-sm text-foreground leading-6 mb-3">«{r.text}»</p>
                <div>
                  <p className="text-xs font-semibold text-foreground">{r.name}</p>
                  <p className="text-[10px] text-(--muted)">{r.role}</p>
                </div>
              </div>
            ))}
          </div>
        </motion.div>

        {/* Финальный CTA */}
        <motion.div variants={fadeUp} initial="hidden" animate="show" transition={{ duration: 0.35, delay: 0.2 }}
          className="rounded-3xl border border-(--line) bg-white p-6 sm:p-8 flex flex-col sm:flex-row items-center justify-between gap-5">
          <div>
            <p className="text-lg font-bold text-foreground mb-1">Готовы начать?</p>
            <p className="text-sm text-(--muted) leading-6">Базовый договор — бесплатно. Займёт 5–10 минут.</p>
          </div>
          <div className="flex flex-col sm:flex-row items-center gap-3 shrink-0">
            <Link href="/checklist" className="text-sm font-semibold text-indigo-600 underline underline-offset-2 hover:text-indigo-800">
              10 ошибок в договоре →
            </Link>
            <button
              type="button"
              onClick={handleStart}
              disabled={isCreating}
              className="inline-flex items-center gap-2 rounded-2xl px-6 py-3 text-sm font-bold text-white transition hover:opacity-90 disabled:opacity-50"
              style={{ background: "var(--gradient)" }}
            >
              {isCreating ? "Создаём..." : "Создать договор"}
              <ArrowRight size={14} strokeWidth={2} />
            </button>
          </div>
        </motion.div>

      </div>
    </AppShell>
  );
}
