"use client";

import { useState } from "react";
import Link from "next/link";
import { motion } from "framer-motion";
import {
  AlertTriangle, CheckCircle2, ArrowRight, FileText,
  Mail, Shield, ChevronDown, ChevronUp,
} from "lucide-react";
import { AppShell } from "@/components/app-shell";

const MISTAKES = [
  {
    number: "01",
    title: "Не указан точный адрес и характеристики объекта",
    risk: "Суд может признать договор незаключённым — предмет договора не определён.",
    fix: "Пропишите полный адрес, этаж, площадь, кадастровый номер.",
  },
  {
    number: "02",
    title: "Отсутствует акт приёма-передачи",
    risk: "Без акта невозможно доказать состояние квартиры на момент въезда. Арендодатель рискует потерять деньги за ущерб.",
    fix: "Составьте акт с описанием состояния стен, мебели, техники, счётчиков.",
  },
  {
    number: "03",
    title: "Нет описи имущества",
    risk: "Если что-то пропадёт или сломается — невозможно доказать, что это было в квартире.",
    fix: "Приложите список всего имущества с фотофиксацией.",
  },
  {
    number: "04",
    title: "Размытые условия оплаты",
    risk: "Споры о сроках, способе и сумме оплаты — самая частая причина конфликтов.",
    fix: "Укажите точную дату, сумму, способ (наличные/перевод), штраф за просрочку.",
  },
  {
    number: "05",
    title: "Не прописан порядок повышения аренды",
    risk: "Арендодатель повышает аренду в любой момент — арендатор ничего не может сделать.",
    fix: "Зафиксируйте условия и периодичность возможного повышения.",
  },
  {
    number: "06",
    title: "Отсутствуют условия досрочного расторжения",
    risk: "Одна из сторон просто уходит без предупреждения — другая теряет деньги и время.",
    fix: "Пропишите срок уведомления (обычно 30–60 дней) и штрафы.",
  },
  {
    number: "07",
    title: "Не указано, кто платит за коммунальные услуги",
    risk: "Долги по ЖКХ копятся — непонятно, кто должен платить.",
    fix: "Чётко разделите: что входит в аренду, что оплачивает арендатор отдельно.",
  },
  {
    number: "08",
    title: "Нет пункта о залоге и условиях его возврата",
    risk: "Арендодатель удерживает залог без оснований, арендатор не может его вернуть.",
    fix: "Укажите размер залога, срок возврата и основания для удержания.",
  },
  {
    number: "09",
    title: "Не прописан ремонт — кто за что отвечает",
    risk: "Сломался кран — кто чинит? Конфликт гарантирован.",
    fix: "Разделите: текущий ремонт (арендатор) и капитальный (арендодатель).",
  },
  {
    number: "10",
    title: "Договор не зарегистрирован (при аренде более 11 месяцев)",
    risk: "Договор от 12 месяцев без регистрации в Росреестре — юридически ничтожен.",
    fix: "Заключайте на 11 месяцев с пролонгацией или регистрируйте в Росреестре.",
  },
];

const fadeUp = {
  hidden: { opacity: 0, y: 16 },
  show: { opacity: 1, y: 0 },
};

export function ChecklistClient() {
  const [openIndex, setOpenIndex] = useState<number | null>(null);
  const [email, setEmail] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!email.trim()) return;
    setIsSubmitting(true);
    setTimeout(() => {
      setSubmitted(true);
      setIsSubmitting(false);
    }, 800);
  }

  return (
    <AppShell eyebrow="Полезное" title="10 ошибок в договоре аренды">
      <div className="flex flex-col gap-6">

        {/* Hero-блок */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.4 }}
          className="relative overflow-hidden rounded-3xl p-6 sm:p-8 text-white"
          style={{ background: "linear-gradient(135deg, #1e1b4b 0%, #312e81 100%)" }}
        >
          <div className="pointer-events-none absolute -top-16 -right-16 h-64 w-64 rounded-full"
            style={{ background: "radial-gradient(circle, rgba(139,92,246,0.3) 0%, transparent 65%)", filter: "blur(40px)" }} />
          <div className="relative">
            <div className="flex items-center gap-3 mb-4">
              <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-white/10">
                <AlertTriangle size={18} strokeWidth={1.75} className="text-amber-400" />
              </div>
              <span className="rounded-full border border-white/15 bg-white/10 px-3 py-1 text-xs font-semibold text-white/70">
                Бесплатный гайд
              </span>
            </div>
            <h2 className="font-editorial text-2xl sm:text-3xl leading-snug mb-3">
              Эти ошибки стоят арендаторам<br className="hidden sm:block" /> и арендодателям тысячи рублей
            </h2>
            <p className="text-sm text-white/65 leading-6 max-w-lg">
              Разобрали 10 самых частых ошибок в договорах аренды — с реальными последствиями и готовыми формулировками для исправления.
            </p>
            <div className="mt-5 flex flex-wrap gap-3 text-xs text-white/50">
              <span className="flex items-center gap-1.5"><Shield size={12} /> Юридически выверено</span>
              <span className="flex items-center gap-1.5"><FileText size={12} /> 10 пунктов с примерами</span>
              <span className="flex items-center gap-1.5"><CheckCircle2 size={12} /> Бесплатно</span>
            </div>
          </div>
        </motion.div>

        <div className="grid gap-5 lg:grid-cols-[minmax(0,1fr)_340px]">

          {/* Список ошибок */}
          <div className="flex flex-col gap-3">
            {MISTAKES.map((item, idx) => (
              <motion.div
                key={item.number}
                variants={fadeUp}
                initial="hidden"
                animate="show"
                transition={{ duration: 0.3, delay: idx * 0.04 }}
                className="overflow-hidden rounded-2xl border border-(--line) bg-white"
              >
                <button
                  type="button"
                  onClick={() => setOpenIndex(openIndex === idx ? null : idx)}
                  className="flex w-full items-start gap-4 px-5 py-4 text-left transition hover:bg-stone-50"
                >
                  <span className="mt-0.5 shrink-0 text-[11px] font-bold tabular-nums text-indigo-400">{item.number}</span>
                  <span className="flex-1 text-sm font-semibold text-foreground">{item.title}</span>
                  {openIndex === idx
                    ? <ChevronUp size={15} strokeWidth={2} className="mt-0.5 shrink-0 text-(--muted)" />
                    : <ChevronDown size={15} strokeWidth={2} className="mt-0.5 shrink-0 text-(--muted)" />
                  }
                </button>
                {openIndex === idx && (
                  <motion.div
                    initial={{ opacity: 0, height: 0 }}
                    animate={{ opacity: 1, height: "auto" }}
                    exit={{ opacity: 0, height: 0 }}
                    transition={{ duration: 0.2 }}
                    className="border-t border-(--line) px-5 pb-4 pt-3"
                  >
                    <div className="mb-3 rounded-xl bg-amber-50 border border-amber-100 px-3 py-2.5">
                      <p className="text-[10px] font-bold uppercase tracking-[0.15em] text-amber-600 mb-1">Риск</p>
                      <p className="text-sm text-amber-900 leading-6">{item.risk}</p>
                    </div>
                    <div className="rounded-xl bg-emerald-50 border border-emerald-100 px-3 py-2.5">
                      <p className="text-[10px] font-bold uppercase tracking-[0.15em] text-emerald-600 mb-1">Как исправить</p>
                      <p className="text-sm text-emerald-900 leading-6">{item.fix}</p>
                    </div>
                  </motion.div>
                )}
              </motion.div>
            ))}
          </div>

          {/* Сайдбар: форма + CTA */}
          <aside className="flex flex-col gap-4 lg:sticky lg:top-6 lg:self-start">

            {/* Email форма */}
            <div className="rounded-3xl border border-(--line) bg-white overflow-hidden">
              <div className="px-5 py-4 border-b border-(--line)"
                style={{ background: "linear-gradient(135deg, #f0f0ff 0%, #f5f3ff 100%)" }}>
                <p className="text-xs font-bold uppercase tracking-[0.18em] text-indigo-500 mb-1">Получите на почту</p>
                <p className="text-sm font-semibold text-foreground">Чек-лист в PDF + шаблон договора</p>
              </div>
              <div className="p-5">
                {submitted ? (
                  <motion.div
                    initial={{ opacity: 0, scale: 0.95 }}
                    animate={{ opacity: 1, scale: 1 }}
                    className="flex flex-col items-center gap-3 py-4 text-center"
                  >
                    <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-50">
                      <CheckCircle2 size={22} strokeWidth={1.75} className="text-emerald-500" />
                    </div>
                    <p className="text-sm font-semibold text-foreground">Отправили на почту!</p>
                    <p className="text-xs text-(--muted) leading-5">Проверьте папку «Входящие» и «Спам»</p>
                  </motion.div>
                ) : (
                  <form onSubmit={handleSubmit} className="flex flex-col gap-3">
                    <div>
                      <label htmlFor="checklist-email" className="mb-1.5 block text-xs font-semibold text-(--muted)">
                        Email
                      </label>
                      <div className="relative">
                        <Mail size={14} strokeWidth={1.75} className="absolute left-3 top-1/2 -translate-y-1/2 text-(--muted)" />
                        <input
                          id="checklist-email"
                          type="email"
                          required
                          value={email}
                          onChange={(e) => setEmail(e.target.value)}
                          placeholder="example@mail.ru"
                          className="w-full rounded-xl border border-(--line) bg-stone-50 py-2.5 pl-9 pr-3 text-sm text-foreground outline-none transition focus:border-indigo-400 focus:bg-white"
                        />
                      </div>
                    </div>
                    <button
                      type="submit"
                      disabled={isSubmitting}
                      className="inline-flex items-center justify-center gap-2 rounded-xl py-2.5 text-sm font-semibold text-white transition hover:opacity-90 disabled:opacity-60"
                      style={{ background: "var(--gradient)" }}
                    >
                      {isSubmitting ? (
                        <><span className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-white/30 border-t-white" />Отправляем...</>
                      ) : (
                        <><Mail size={14} strokeWidth={1.75} />Получить чек-лист</>
                      )}
                    </button>
                    <p className="text-[10px] text-(--muted) leading-4 text-center">
                      Без спама. Только чек-лист и полезные советы раз в месяц.
                    </p>
                  </form>
                )}
              </div>
            </div>

            {/* CTA — создать договор */}
            <div className="rounded-3xl border border-(--line) bg-white p-5">
              <p className="text-xs font-bold uppercase tracking-[0.16em] text-indigo-500 mb-2">Лучше не рисковать</p>
              <p className="text-sm font-semibold text-foreground mb-1">Создайте договор без ошибок</p>
              <p className="text-xs text-(--muted) leading-5 mb-4">
                Система сама проведёт по всем пунктам, объяснит каждый и проверит полноту данных.
              </p>
              <Link
                href="/"
                className="inline-flex w-full items-center justify-center gap-2 rounded-2xl py-2.5 text-sm font-semibold text-white transition hover:opacity-90"
                style={{ background: "var(--gradient)" }}
              >
                Создать договор бесплатно
                <ArrowRight size={14} strokeWidth={2} />
              </Link>
            </div>

          </aside>
        </div>
      </div>
    </AppShell>
  );
}
