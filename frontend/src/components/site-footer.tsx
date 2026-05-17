"use client";

import Link from "next/link";
import { motion } from "framer-motion";
import { FileText, Mail, Zap, Shield, ArrowUpRight } from "lucide-react";

const NAV = [
  {
    heading: "Продукт",
    links: [
      { label: "Создать договор", href: "/" },
      { label: "Договор аренды", href: "/" },
      { label: "Premium приложения", href: "/" },
    ],
  },
  {
    heading: "Правовая",
    links: [
      { label: "Публичная оферта", href: "/legal/offer" },
      { label: "Политика конфиденциальности", href: "/legal/privacy" },
    ],
  },
  {
    heading: "Поддержка",
    links: [
      { label: "support@rentgen.ru", href: "mailto:support@rentgen.ru" },
    ],
  },
];

export function SiteFooter() {
  const year = new Date().getFullYear();

  return (
    <footer className="relative mt-8 overflow-hidden rounded-3xl"
      style={{
        background: "linear-gradient(160deg, #0f0c29 0%, #1a1248 50%, #0f0c29 100%)",
      }}
    >
      {/* Animated top border */}
      <div className="absolute inset-x-0 top-0 h-px"
        style={{
          background: "linear-gradient(90deg, transparent 0%, #4f46e5 30%, #7c3aed 50%, #4f46e5 70%, transparent 100%)",
        }}
      />

      {/* Glow орб сверху-справа */}
      <div className="pointer-events-none absolute -top-32 -right-32 h-80 w-80 rounded-full"
        style={{
          background: "radial-gradient(circle, rgba(109,40,217,0.18) 0%, transparent 70%)",
          filter: "blur(40px)",
        }}
      />
      {/* Glow орб снизу-слева */}
      <div className="pointer-events-none absolute -bottom-16 -left-16 h-56 w-56 rounded-full"
        style={{
          background: "radial-gradient(circle, rgba(79,70,229,0.12) 0%, transparent 70%)",
          filter: "blur(30px)",
        }}
      />

      <div className="relative px-5 pt-10 sm:px-8 md:px-14">

        {/* Верхняя часть: лого + колонки */}
        <div className="grid gap-8 sm:gap-10 sm:grid-cols-2 md:grid-cols-[1.6fr_1fr_1fr_1fr] md:gap-12">

          {/* Бренд */}
          <div className="flex flex-col gap-5">
            <Link href="/" className="group flex w-fit items-center gap-3">
              <div
                className="flex h-10 w-10 items-center justify-center rounded-2xl transition group-hover:scale-105"
                style={{ background: "var(--gradient)" }}
              >
                <FileText size={18} strokeWidth={1.75} color="white" />
              </div>
              <span className="font-editorial text-2xl text-white">RentGen</span>
            </Link>

            <p className="max-w-55 text-sm leading-7 text-white/50">
              Договоры аренды — автоматически. Быстро, грамотно, без юриста.
            </p>

            {/* Статусы */}
            <div className="flex flex-col gap-2">
              <div className="flex items-center gap-2">
                <span className="relative flex h-2 w-2">
                  <span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-60" />
                  <span className="relative inline-flex h-2 w-2 rounded-full bg-emerald-400" />
                </span>
                <span className="text-xs text-white/50">Сервис работает</span>
              </div>
              <div className="flex items-center gap-2">
                <Shield size={12} strokeWidth={2} className="text-indigo-400" />
                <span className="text-xs text-white/50">Данные защищены</span>
              </div>
            </div>

            {/* Email */}
            <a
              href="mailto:support@rentgen.ru"
              className="group mt-1 flex w-fit items-center gap-2 rounded-xl border border-white/10 px-3 py-2 text-xs text-white/75! transition hover:border-indigo-500/50 hover:text-white!"
            >
              <Mail size={12} strokeWidth={1.75} />
              support@rentgen.ru
              <ArrowUpRight size={10} className="opacity-0 transition group-hover:opacity-100" />
            </a>
          </div>

          {/* Колонки навигации */}
          {NAV.map(({ heading, links }) => (
            <div key={heading}>
              <p className="mb-5 text-[10px] font-semibold uppercase tracking-[0.18em] text-indigo-300">
                {heading}
              </p>
              <ul className="flex flex-col gap-3">
                {links.map(({ label, href }) => (
                  <li key={label}>
                    <Link
                      href={href}
                      className="text-sm text-white/75! transition-colors duration-200 hover:text-white!"
                    >
                      {label}
                    </Link>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>

        {/* Большой watermark-текст снизу */}
        <div className="relative mt-8 select-none overflow-hidden">
          <p
            className="font-editorial text-[clamp(3rem,12vw,10rem)] uppercase leading-none tracking-tight"
            style={{
              background: "linear-gradient(180deg, rgba(255,255,255,0.07) 0%, rgba(255,255,255,0.02) 100%)",
              WebkitBackgroundClip: "text",
              WebkitTextFillColor: "transparent",
              backgroundClip: "text",
            }}
          >
            RentGen
          </p>

          {/* Копирайт поверх watermark */}
          <div className="absolute bottom-3 left-0 right-0 flex flex-wrap items-center justify-between gap-3 border-t border-white/6 pt-3">
            <p className="text-xs text-white/25">© {year} RentGen. Все права защищены.</p>
            <p className="text-xs text-white/15">ООО «RentGen» · ИНН *** · ОГРН ***</p>
          </div>
        </div>

      </div>
    </footer>
  );
}
