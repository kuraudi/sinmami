"use client";

import dynamic from "next/dynamic";
import Link from "next/link";
import { UserCircle, LogIn } from "lucide-react";
import { SiteFooter } from "@/components/site-footer";
import { MeshGradient } from "@/components/ui/mesh-gradient";
import { useAppSelector } from "@/store/hooks";

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
  const { isAuthenticated, user } = useAppSelector((s) => s.auth);

  return (
    <div className="subtle-grid noise-overlay min-h-dvh relative overflow-x-hidden">

      {/* Static aurora blobs */}
      <div className="pointer-events-none fixed inset-0 z-0 overflow-hidden">
        <div className="aurora-blob-1 absolute -top-40 -left-40 h-150 w-150 rounded-full opacity-25"
          style={{ background: "radial-gradient(circle, rgba(99,102,241,0.4) 0%, transparent 70%)", filter: "blur(90px)" }} />
        <div className="aurora-blob-2 absolute top-1/3 -right-40 h-125 w-125 rounded-full opacity-20"
          style={{ background: "radial-gradient(circle, rgba(139,92,246,0.45) 0%, transparent 70%)", filter: "blur(100px)" }} />
        <div className="aurora-blob-3 absolute -bottom-20 left-1/3 h-100 w-100 rounded-full opacity-15"
          style={{ background: "radial-gradient(circle, rgba(79,70,229,0.4) 0%, transparent 70%)", filter: "blur(80px)" }} />
      </div>

      <div className="relative z-10 mx-auto flex max-w-7xl flex-col gap-4 px-3 py-4 sm:px-6 md:px-8 md:gap-5 md:py-5">

        {/* Navbar */}
        <header className="flex items-center justify-between rounded-2xl border border-white/60 bg-white/70 px-4 py-3 shadow-sm backdrop-blur-xl sm:px-5 md:px-7" style={{ boxShadow: "0 1px 0 rgba(255,255,255,0.8) inset, 0 4px 20px rgba(79,70,229,0.08)" }}>
          <Link href="/" className="flex items-center gap-2">
            <div
              className="flex h-7 w-7 items-center justify-center rounded-xl text-white sm:h-8 sm:w-8"
              style={{ background: "var(--gradient)" }}
            >
              <svg width="14" height="14" viewBox="0 0 16 16" fill="none" aria-hidden="true">
                <path d="M4 2h6l3 3v9a1 1 0 01-1 1H4a1 1 0 01-1-1V3a1 1 0 011-1z" stroke="white" strokeWidth="1.4" strokeLinejoin="round"/>
                <path d="M10 2v3h3M5.5 7h5M5.5 9.5h3.5" stroke="white" strokeWidth="1.4" strokeLinecap="round"/>
              </svg>
            </div>
            <span className="text-sm font-semibold tracking-tight text-foreground sm:text-base">
              RentGen
            </span>
            <span className="hidden rounded-full bg-indigo-50 px-2 py-0.5 text-[10px] font-bold uppercase tracking-widest text-indigo-500 sm:inline">
              MVP
            </span>
          </Link>

          <div className="flex items-center gap-2">
            <PlanSwitcher />
            {isAuthenticated ? (
              <Link href="/account"
                className="flex items-center gap-1.5 rounded-xl border border-(--line) bg-white/80 px-3 py-2 text-xs font-semibold text-foreground transition hover:bg-indigo-50 hover:border-indigo-200 hover:text-indigo-700">
                <UserCircle size={14} strokeWidth={1.75} />
                <span className="hidden sm:inline">{user?.fullName?.split(" ")[0] ?? "ЛК"}</span>
              </Link>
            ) : (
              <Link href="/login"
                className="flex items-center gap-1.5 rounded-xl border border-(--line) bg-white/80 px-3 py-2 text-xs font-semibold text-foreground transition hover:bg-indigo-50 hover:border-indigo-200 hover:text-indigo-700">
                <LogIn size={14} strokeWidth={1.75} />
                <span className="hidden sm:inline">Войти</span>
              </Link>
            )}
          </div>
        </header>

        {/* Page title */}
        <div className="px-1 pt-1">
          <p className="text-[10px] font-semibold uppercase tracking-[0.22em] text-indigo-500 sm:text-xs">
            {eyebrow}
          </p>
          <h1 className="font-editorial mt-1.5 text-3xl leading-tight text-foreground sm:text-4xl md:text-5xl">
            {title}
          </h1>
          {subtitle && (
            <p className="mt-2 max-w-2xl text-sm leading-6 text-(--muted) sm:text-base sm:leading-7">
              {subtitle}
            </p>
          )}
        </div>

        {children}

      </div>
      <SiteFooter />
    </div>
  );
}
