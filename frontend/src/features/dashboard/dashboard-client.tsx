"use client";

import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { motion, AnimatePresence, useMotionValue, useSpring, useTransform, useScroll, useInView } from "framer-motion";
import {
  FileText, Sparkles, CheckCircle2, ArrowRight, Clock,
  ChevronRight, ShieldCheck, Layers, FileSignature,
  ClipboardList, CalendarDays, Home, PawPrint, Banknote, History,
  Star, TrendingUp, Users, Award, UserCircle, LogIn,
} from "lucide-react";
import { AppShell } from "@/components/app-shell";
import { EmptyState } from "@/components/ui/empty-state";
import { LoadingPanel } from "@/components/ui/loading-panel";
import { StatusPill } from "@/components/ui/status-pill";
import { ShimmerButton } from "@/components/ui/shimmer-button";
import { AnimatedCounter } from "@/components/ui/animated-counter";
import { rentgenApi } from "@/lib/api/rentgen-api";
import { getLocalDocumentIds } from "@/lib/local-history";
import { GuestWarningModal } from "@/features/dashboard/guest-warning-modal";
import { documentTypeLabel, formatDate } from "@/lib/presenters";
import { useAppSelector } from "@/store/hooks";
import Link from "next/link";
import { DocumentType, type DocumentListItemResponse, type DocumentTypeCard } from "@/types/api";

const FEATURES = [
  { icon: CheckCircle2, label: "Пошаговое заполнение" },
  { icon: ShieldCheck, label: "Проверка полноты данных" },
  { icon: FileText, label: "PDF-экспорт" },
];

const HOW_IT_WORKS = [
  { step: "01", icon: ClipboardList, label: "Ответьте на вопросы", desc: "Пошаговый диалог без лишнего" },
  { step: "02", icon: Sparkles, label: "Система проверит данные", desc: "Полнота и корректность" },
  { step: "03", icon: FileText, label: "Скачайте PDF", desc: "Готовый юридический договор" },
];

const SYSTEM_CAPABILITIES = [
  { icon: FileSignature, label: "Объясняет каждый пункт договора" },
  { icon: ShieldCheck, label: "Проверяет полноту и корректность" },
  { icon: Layers, label: "Формирует приложения к договору" },
];

const PREMIUM_ITEMS = [
  { icon: ClipboardList, name: "Акт приёма-передачи" },
  { icon: Home, name: "Опись имущества" },
  { icon: CalendarDays, name: "График платежей" },
  { icon: Star, name: "Правила проживания" },
  { icon: Banknote, name: "Обеспечит. платёж" },
  { icon: PawPrint, name: "С животными" },
];

const STATS = [
  { icon: TrendingUp, value: 1200, suffix: "+", label: "Договоров создано" },
  { icon: Users, value: 98, suffix: "%", label: "Довольных клиентов" },
  { icon: Award, value: 3, suffix: " мин", label: "Среднее время" },
];

const fadeUp = {
  hidden: { opacity: 0, y: 24 },
  show:   { opacity: 1, y: 0 },
};

const stagger = {
  hidden: {},
  show: { transition: { staggerChildren: 0.08 } },
};

function ScrollReveal({ children, className, delay = 0 }: { children: React.ReactNode; className?: string; delay?: number }) {
  const ref = useRef<HTMLDivElement>(null);
  const isInView = useInView(ref, { once: true, margin: "-80px" });
  return (
    <motion.div
      ref={ref}
      className={className}
      initial={{ opacity: 0, y: 32 }}
      animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 32 }}
      transition={{ duration: 0.55, ease: "easeOut", delay }}
    >
      {children}
    </motion.div>
  );
}

export function DashboardClient() {
  const router = useRouter();
  const plan = useAppSelector((state) => state.session.plan);

  const isAuthenticated = useAppSelector((s) => s.auth.isAuthenticated);

  const [documentTypes, setDocumentTypes] = useState<DocumentTypeCard[]>([]);
  const [documents, setDocuments] = useState<DocumentListItemResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showGuestWarning, setShowGuestWarning] = useState(false);

  // Mouse tracking for hero gradient
  const heroRef = useRef<HTMLDivElement>(null);
  const mouseX = useMotionValue(0.5);
  const mouseY = useMotionValue(0.5);
  const smoothX = useSpring(mouseX, { stiffness: 60, damping: 20 });
  const smoothY = useSpring(mouseY, { stiffness: 60, damping: 20 });

  const gradientX = useTransform(smoothX, [0, 1], ["0%", "100%"]);
  const gradientY = useTransform(smoothY, [0, 1], ["0%", "100%"]);

  function handleMouseMove(e: React.MouseEvent<HTMLDivElement>) {
    const rect = heroRef.current?.getBoundingClientRect();
    if (!rect) return;
    mouseX.set((e.clientX - rect.left) / rect.width);
    mouseY.set((e.clientY - rect.top) / rect.height);
  }

  // Parallax for background orbs
  const { scrollY } = useScroll();
  const orb1Y = useTransform(scrollY, [0, 600], [0, -80]);
  const orb2Y = useTransform(scrollY, [0, 600], [0, -40]);
  const orb3Y = useTransform(scrollY, [0, 600], [0, 60]);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      setIsLoading(true);
      setError(null);
      try {
        const localIds = getLocalDocumentIds();
        const [types, allDocs] = await Promise.all([
          rentgenApi.getDocumentTypes(plan),
          localIds.length > 0 ? rentgenApi.getDocuments(plan) : Promise.resolve([]),
        ]);
        if (!cancelled) {
          setDocumentTypes(types);
          setDocuments(allDocs.filter((d) => localIds.includes(d.documentId)));
        }
      } catch (e) {
        if (!cancelled) setError(e instanceof Error ? e.message : "Не удалось загрузить страницу.");
      } finally {
        if (!cancelled) setIsLoading(false);
      }
    }
    void load();
    return () => { cancelled = true; };
  }, [plan]);

  function handleCreate() {
    if (!isAuthenticated) {
      setShowGuestWarning(true);
      return;
    }
    void createDraft();
  }

  async function createDraft() {
    try {
      setIsCreating(true);
      setError(null);
      const draft = await rentgenApi.createDraft(DocumentType.RentalAgreement, plan);
      router.push(`/drafts/${draft.draftId}`);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Не удалось создать черновик.");
      setIsCreating(false);
    }
  }

  return (
    <AppShell
      eyebrow="Rental agreement workspace"
      title="Генератор договора аренды"
      subtitle="Система сама проведёт по всем шагам, проверит данные и сформирует готовый документ."
    >
      <motion.div
        className="grid gap-4 lg:grid-cols-12"
        variants={stagger}
        initial="hidden"
        animate="show"
      >

        {/* ── Hero ── */}
        <div
          ref={heroRef}
          onMouseMove={handleMouseMove}
          className="relative lg:col-span-12 flex flex-col lg:flex-row rounded-3xl overflow-hidden"
          style={{ boxShadow: "var(--shadow-lg)" }}
        >
          {/* Mouse-following gradient glow */}
          <motion.div
            className="pointer-events-none absolute inset-0 z-0 opacity-40"
            style={{
              background: "radial-gradient(400px circle at var(--gx) var(--gy), rgba(99,102,241,0.18), transparent 60%)",
              "--gx": gradientX,
              "--gy": gradientY,
            } as React.CSSProperties}
          />

          {/* Левая — glass */}
          <motion.div variants={fadeUp}
            className="relative flex flex-1 flex-col p-6 sm:p-8 md:p-12 rounded-t-3xl lg:rounded-l-3xl lg:rounded-tr-none overflow-hidden"
            style={{ background: "rgba(255,255,255,0.82)", backdropFilter: "blur(24px)", WebkitBackdropFilter: "blur(24px)", borderRight: "1px solid rgba(255,255,255,0.5)" }}>

            <div className="pointer-events-none absolute inset-0"
              style={{
                backgroundImage: "radial-gradient(rgba(79,70,229,0.12) 1px, transparent 1px)",
                backgroundSize: "24px 24px",
              }} />
            <motion.div
              style={{ y: orb1Y, background: "radial-gradient(circle, rgba(79,70,229,0.08) 0%, transparent 70%)", filter: "blur(24px)" }}
              className="pointer-events-none absolute -bottom-16 -right-16 h-64 w-64 rounded-full"
            />

            <div className="relative flex h-full flex-col">
              <div className="flex items-center gap-3">
                <div className="flex h-12 w-12 items-center justify-center rounded-2xl text-white shadow-lg"
                  style={{ background: "var(--gradient)" }}>
                  <FileText size={22} strokeWidth={1.75} />
                </div>
                <span className="inline-flex items-center gap-2 rounded-full border border-indigo-200 bg-indigo-50 px-3 py-1 text-xs font-semibold text-indigo-600">
                  <span className="relative flex h-2 w-2">
                    <span className="pulse-ring absolute inline-flex h-full w-full rounded-full bg-indigo-400" />
                    <span className="relative inline-flex h-2 w-2 rounded-full bg-indigo-500" />
                  </span>
                  Доступно сейчас
                </span>
              </div>

              <h2 className="font-editorial mt-5 text-3xl leading-tight text-foreground sm:text-4xl md:text-5xl">
                Создать черновик<br />договора аренды
              </h2>
              <p className="mt-3 text-sm leading-7 text-(--muted)">
                Ответьте на вопросы — система сформирует договор, проверит все данные и подготовит персональный гайд.
              </p>

              <div className="mt-4 flex flex-wrap gap-2">
                {FEATURES.map(({ icon: Icon, label }) => (
                  <motion.span
                    key={label}
                    whileHover={{ scale: 1.04, y: -1 }}
                    transition={{ duration: 0.15 }}
                    className="inline-flex items-center gap-2 rounded-full border border-(--line) bg-white px-3 py-1.5 text-xs font-medium text-(--muted) cursor-default"
                  >
                    <Icon size={13} strokeWidth={2} className="shrink-0 text-emerald-500" />
                    {label}
                  </motion.span>
                ))}
              </div>

              <div className="mt-auto pt-8 flex items-center gap-4">
                <ShimmerButton
                  type="button"
                  onClick={handleCreate}
                  disabled={isCreating}
                  className="group inline-flex items-center gap-2.5 rounded-2xl px-7 py-3.5 text-sm font-semibold text-white shadow-lg transition-all hover:shadow-xl hover:opacity-90 active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-60"
                  style={{ background: "var(--gradient)" }}
                >
                  {isCreating ? (
                    <>
                      <span className="h-4 w-4 animate-spin rounded-full border-2 border-white/30 border-t-white" />
                      Создаём...
                    </>
                  ) : (
                    <>
                      Создать договор
                      <ArrowRight size={16} strokeWidth={2} className="transition-transform group-hover:translate-x-1" />
                    </>
                  )}
                </ShimmerButton>
                <span className="flex items-center gap-1.5 text-xs text-(--muted)">
                  <Clock size={12} strokeWidth={2} />
                  2–5 минут
                </span>
              </div>
            </div>
          </motion.div>

          {/* Правая — тёмная */}
          <motion.div variants={fadeUp}
            className="relative flex flex-1 flex-col p-6 pb-10 sm:p-8 sm:pb-12 md:p-12 rounded-b-3xl lg:rounded-r-3xl lg:rounded-bl-none overflow-hidden"
            style={{ background: "linear-gradient(135deg, #1e1b4b 0%, #312e81 100%)" }}>

            <div className="absolute inset-y-0 left-0 w-px bg-white/10" />
            <div className="pointer-events-none absolute inset-0"
              style={{
                backgroundImage: "radial-gradient(rgba(255,255,255,0.06) 1px, transparent 1px)",
                backgroundSize: "24px 24px",
              }} />
            <motion.div
              style={{ y: orb2Y }}
              className="pointer-events-none absolute -top-16 -right-16 h-64 w-64 rounded-full"
            >
              <div className="h-full w-full rounded-full"
                style={{ background: "radial-gradient(circle, rgba(139,92,246,0.25) 0%, transparent 65%)", filter: "blur(40px)" }} />
            </motion.div>

            <div className="relative flex h-full flex-col text-white">
              <div className="flex items-center justify-between">
                <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-white/10">
                  <Sparkles size={20} strokeWidth={1.75} />
                </div>
                <span className="rounded-full border border-white/10 bg-white/5 px-3 py-1 text-[10px] font-semibold uppercase tracking-widest text-white/40">
                  Автоматически
                </span>
              </div>

              <h3 className="font-editorial mt-5 text-2xl leading-snug sm:text-3xl">
                Система делает<br />всё за вас
              </h3>
              <p className="mt-3 text-sm leading-6 text-white/55">
                Не знаете как заполнить пункт? Система объяснит и подскажет что нужно указать.
              </p>

              <div className="mt-6 flex flex-col gap-2.5">
                {SYSTEM_CAPABILITIES.map(({ icon: Icon, label }, i) => (
                  <motion.div key={label}
                    initial={{ opacity: 0, x: -10 }}
                    animate={{ opacity: 1, x: 0 }}
                    transition={{ delay: 0.3 + i * 0.1, duration: 0.35 }}
                    whileHover={{ x: 4 }}
                    className="flex items-center gap-3 rounded-2xl bg-white/10 px-4 py-3 transition hover:bg-white/15 cursor-default"
                  >
                    <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl bg-indigo-500/25">
                      <Icon size={15} strokeWidth={1.75} className="text-indigo-300" />
                    </span>
                    <span className="text-sm text-white/80">{label}</span>
                  </motion.div>
                ))}
              </div>

              <div className="mt-6 pt-0 lg:mt-auto lg:pt-6">
                <div className="flex items-center gap-2 rounded-2xl bg-white/10 px-4 py-3 text-xs text-white/55">
                  <Clock size={12} strokeWidth={2} />
                  Автоматическая генерация — от 2 до 5 минут
                </div>
              </div>
            </div>
          </motion.div>
        </div>

        {/* ── Статы (скрыто) ── */}
        {/* <ScrollReveal className="rounded-3xl border border-(--line) bg-white p-4 sm:p-6 lg:col-span-12">
          <div className="grid grid-cols-3 divide-x divide-(--line)">
            {STATS.map(({ icon: Icon, value, suffix, label }) => (
              <div key={label} className="flex flex-col items-center gap-1 px-2 text-center sm:px-4">
                <div className="mb-1 hidden h-9 w-9 items-center justify-center rounded-xl sm:flex"
                  style={{ background: "var(--gradient-soft)" }}>
                  <Icon size={16} strokeWidth={1.75} className="text-indigo-600" />
                </div>
                <span className="text-xl font-bold tabular-nums text-foreground sm:text-2xl md:text-3xl">
                  <AnimatedCounter to={value} suffix={suffix} />
                </span>
                <span className="text-[10px] text-(--muted) sm:text-xs">{label}</span>
              </div>
            ))}
          </div>
        </ScrollReveal> */}

        {/* ── Как это работает ── */}
        <ScrollReveal className="bento-card rounded-3xl p-5 sm:p-6 lg:col-span-7" delay={0.05}>
          <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-indigo-500">Как это работает</p>
          <div className="relative mt-5">
            <div className="absolute top-5 left-10 right-10 hidden border-t-2 border-dashed border-indigo-200 lg:block" />
            <div className="grid grid-cols-3 gap-2 sm:gap-3">
              {HOW_IT_WORKS.map(({ step, icon: Icon, label, desc }, i) => (
                <motion.div
                  key={step}
                  initial={{ opacity: 0, y: 12 }}
                  whileInView={{ opacity: 1, y: 0 }}
                  viewport={{ once: true }}
                  transition={{ delay: i * 0.12, duration: 0.4 }}
                  className="relative flex flex-col gap-3"
                >
                  <motion.div
                    whileHover={{ scale: 1.1, rotate: 3 }}
                    transition={{ duration: 0.2 }}
                    className="relative z-10 flex h-10 w-10 items-center justify-center rounded-2xl text-white shadow-sm"
                    style={{ background: "var(--gradient)" }}
                  >
                    <Icon size={16} strokeWidth={1.75} />
                  </motion.div>
                  <div>
                    <span className="text-[10px] font-bold tabular-nums text-indigo-400">{step}</span>
                    <p className="mt-0.5 text-sm font-semibold text-foreground">{label}</p>
                    <p className="mt-1 text-xs leading-5 text-(--muted)">{desc}</p>
                  </div>
                </motion.div>
              ))}
            </div>
          </div>
        </ScrollReveal>

        {/* ── Premium приложения ── */}
        <ScrollReveal className="bento-card rounded-3xl p-5 sm:p-6 lg:col-span-5" delay={0.1}>
          <div className="flex items-center justify-between gap-2">
            <div className="flex items-center gap-2">
              <span
                className="rounded-full px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-widest"
                style={{ background: "var(--gradient-soft)", color: "var(--primary)" }}
              >
                Premium
              </span>
              <p className="text-sm font-semibold text-foreground">Приложения к договору</p>
            </div>
            <ChevronRight size={16} strokeWidth={1.75} className="text-(--muted)" />
          </div>
          <p className="mt-1 text-xs text-(--muted)">Доступны после генерации договора</p>
          <div className="mt-4 grid grid-cols-2 gap-2">
            {PREMIUM_ITEMS.map(({ icon: Icon, name }, i) => (
              <motion.div
                key={name}
                initial={{ opacity: 0, scale: 0.92 }}
                whileInView={{ opacity: 1, scale: 1 }}
                viewport={{ once: true }}
                transition={{ delay: i * 0.06, duration: 0.3 }}
                whileHover={{ scale: 1.03, y: -1 }}
                className="flex items-center gap-2 rounded-xl border border-(--line) bg-background px-3 py-2.5 cursor-default"
              >
                <Icon size={13} strokeWidth={1.75} className="shrink-0 text-indigo-400" />
                <span className="text-xs font-medium text-(--muted)">{name}</span>
              </motion.div>
            ))}
          </div>
        </ScrollReveal>

        {/* ── История ── */}
        <ScrollReveal className="bento-card rounded-3xl p-5 sm:p-6 md:p-8 lg:col-span-12" delay={0.05}>
          <div className="flex items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div
                className="flex h-10 w-10 items-center justify-center rounded-xl text-indigo-600"
                style={{ background: "var(--primary-soft)" }}
              >
                <History size={18} strokeWidth={1.75} />
              </div>
              <div>
                <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-indigo-500">История</p>
                <h2 className="font-editorial text-xl text-foreground">Последние документы</h2>
              </div>
            </div>
            {documents.length > 0 && (
              <span
                className="rounded-full px-3 py-1 text-xs font-semibold text-indigo-600"
                style={{ background: "var(--primary-soft)" }}
              >
                {documents.length}
              </span>
            )}
          </div>

          {isLoading ? (
            <LoadingPanel className="mt-6" title="Загружаем историю" description="Подтягиваем документы и статусы." lines={3} />
          ) : documents.length === 0 ? (
            <EmptyState
              className="mt-6"
              eyebrow="Пока пусто"
              title="История появится после первой генерации"
              description="Создайте черновик, пройдите сценарий и откройте предпросмотр договора."
            />
          ) : (
            <motion.div
              className="mt-6 grid gap-3 sm:grid-cols-2 xl:grid-cols-3"
              variants={stagger}
              initial="hidden"
              animate="show"
            >
              {documents.map((doc) => (
                <motion.button
                  key={doc.documentId}
                  variants={fadeUp}
                  type="button"
                  onClick={() => router.push(`/documents/${doc.documentId}`)}
                  className="group cursor-pointer rounded-2xl border border-(--line) bg-background px-5 py-4 text-left transition hover:border-indigo-300 hover:shadow-md active:scale-[0.99]"
                  whileHover={{ y: -3, boxShadow: "0 8px 30px rgba(79,70,229,0.12)" }}
                  transition={{ duration: 0.2 }}
                >
                  <div className="flex items-start justify-between gap-2">
                    <p className="text-sm font-semibold text-foreground transition group-hover:text-indigo-600">
                      {doc.title}
                    </p>
                    <StatusPill status={doc.status} />
                  </div>
                  <p className="mt-1.5 text-xs text-(--muted)">
                    {documentTypeLabel(doc.documentType)} · {formatDate(doc.createdAtUtc)}
                  </p>
                  <div className="mt-3 flex flex-wrap gap-1.5">
                    <span className="inline-flex items-center gap-1 rounded-full border border-(--line) bg-white px-2.5 py-0.5 text-[11px] text-(--muted)">
                      <FileText size={10} strokeWidth={1.75} />
                      гайд: {doc.hasGuide ? "есть" : "нет"}
                    </span>
                    <span className="inline-flex items-center gap-1 rounded-full border border-(--line) bg-white px-2.5 py-0.5 text-[11px] text-(--muted)">
                      <Layers size={10} strokeWidth={1.75} />
                      приложений: {doc.appendicesCount}
                    </span>
                  </div>
                </motion.button>
              ))}
            </motion.div>
          )}
        </ScrollReveal>

      </motion.div>

      {error && (
        <motion.div
          initial={{ opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          className="mt-2 rounded-2xl border border-red-200 bg-red-50 px-5 py-4 text-sm text-red-700"
        >
          {error}
        </motion.div>
      )}

      {/* Модалка предупреждения для гостя */}
      <AnimatePresence>
        {showGuestWarning && (
          <GuestWarningModal
            onClose={() => setShowGuestWarning(false)}
            onContinue={() => { setShowGuestWarning(false); void createDraft(); }}
          />
        )}
      </AnimatePresence>
    </AppShell>
  );
}
