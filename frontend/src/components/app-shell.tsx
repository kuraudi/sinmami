"use client";

import dynamic from "next/dynamic";
import Link from "next/link";
import { SiteFooter } from "@/components/site-footer";

const PlanSwitcher = dynamic(
  () => import("@/components/plan-switcher").then((m) => m.PlanSwitcher),
  {
    ssr: false,
    loading: () => (
      <div className="inline-flex rounded-full border border-(--line) bg-white/70 p-1 shadow-sm">
        <span className="rounded-full px-4 py-2 text-sm font-semibold text-(--muted)">
          Загрузка тарифа
        </span>
      </div>
    ),
  },
);

type AppShellProps = {
  eyebrow: string;
  title: string;
  subtitle?: string;
  children: React.ReactNode;
};

export function AppShell({ eyebrow, title, subtitle, children }: AppShellProps) {
  return (
    <div className="subtle-grid min-h-dvh">
      <div className="mx-auto flex max-w-7xl flex-col gap-5 px-4 py-5 md:px-8">

        {/* Navbar */}
        <header className="flex items-center justify-between rounded-2xl border border-(--line) bg-white/90 px-5 py-3 shadow-sm backdrop-blur-md md:px-7">
          <Link href="/" className="flex items-center gap-2.5">
            <div
              className="flex h-8 w-8 items-center justify-center rounded-xl text-white"
              style={{ background: "var(--gradient)" }}
            >
              <svg width="16" height="16" viewBox="0 0 16 16" fill="none" aria-hidden="true">
                <path d="M4 2h6l3 3v9a1 1 0 01-1 1H4a1 1 0 01-1-1V3a1 1 0 011-1z" stroke="white" strokeWidth="1.4" strokeLinejoin="round"/>
                <path d="M10 2v3h3M5.5 7h5M5.5 9.5h3.5" stroke="white" strokeWidth="1.4" strokeLinecap="round"/>
              </svg>
            </div>
            <span className="text-base font-semibold tracking-tight text-(--foreground)">
              RentGen
            </span>
            <span className="rounded-full bg-indigo-50 px-2 py-0.5 text-[10px] font-bold uppercase tracking-widest text-indigo-500">
              MVP
            </span>
          </Link>

          <PlanSwitcher />
        </header>

        {/* Page title */}
        <div className="px-1 pt-2">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-indigo-500">
            {eyebrow}
          </p>
          <h1 className="font-editorial mt-2 text-4xl leading-tight text-(--foreground) md:text-5xl">
            {title}
          </h1>
          {subtitle && (
            <p className="mt-3 max-w-2xl text-base leading-7 text-(--muted)">
              {subtitle}
            </p>
          )}
        </div>

        {children}

        <SiteFooter />
      </div>
    </div>
  );
}
