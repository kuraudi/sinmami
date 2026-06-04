"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { motion } from "framer-motion";
import { Mail, Lock, Eye, EyeOff, User, ArrowRight, FileText } from "lucide-react";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import { registerThunk } from "@/store/slices/auth-slice";

export function RegisterClient() {
  const router = useRouter();
  const dispatch = useAppDispatch();
  const { status, error } = useAppSelector((s) => s.auth);

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [fullName, setFullName] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const result = await dispatch(registerThunk({ email, password, fullName: fullName || undefined }));
    if (registerThunk.fulfilled.match(result)) {
      router.push("/");
    }
  }

  return (
    <div className="subtle-grid noise-overlay min-h-dvh flex items-center justify-center px-4 py-12">
      <motion.div
        initial={{ opacity: 0, y: 24 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4 }}
        className="w-full max-w-md"
      >
        {/* Logo */}
        <Link href="/" className="mb-8 flex items-center justify-center gap-2.5">
          <div className="flex h-9 w-9 items-center justify-center rounded-xl text-white"
            style={{ background: "var(--gradient)" }}>
            <FileText size={16} strokeWidth={1.75} />
          </div>
          <span className="font-editorial text-2xl text-foreground">RentGen</span>
        </Link>

        <div className="rounded-3xl border border-white/60 bg-white/80 p-8 shadow-xl backdrop-blur-xl">
          <h1 className="font-editorial text-3xl text-foreground mb-1">Регистрация</h1>
          <p className="text-sm text-(--muted) mb-7">Создайте аккаунт — история договоров сохранится</p>

          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div>
              <label htmlFor="fullName" className="mb-1.5 block text-xs font-semibold text-(--muted)">Имя <span className="text-stone-400">(необязательно)</span></label>
              <div className="relative">
                <User size={14} strokeWidth={1.75} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-(--muted)" />
                <input
                  id="fullName"
                  type="text"
                  autoComplete="name"
                  value={fullName}
                  onChange={(e) => setFullName(e.target.value)}
                  placeholder="Иван Иванов"
                  className="w-full rounded-2xl border border-(--line) bg-stone-50 py-3 pl-10 pr-4 text-sm text-foreground outline-none transition focus:border-indigo-400 focus:bg-white"
                />
              </div>
            </div>

            <div>
              <label htmlFor="email" className="mb-1.5 block text-xs font-semibold text-(--muted)">Email</label>
              <div className="relative">
                <Mail size={14} strokeWidth={1.75} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-(--muted)" />
                <input
                  id="email"
                  type="email"
                  required
                  autoComplete="email"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="you@example.com"
                  className="w-full rounded-2xl border border-(--line) bg-stone-50 py-3 pl-10 pr-4 text-sm text-foreground outline-none transition focus:border-indigo-400 focus:bg-white"
                />
              </div>
            </div>

            <div>
              <label htmlFor="password" className="mb-1.5 block text-xs font-semibold text-(--muted)">Пароль</label>
              <div className="relative">
                <Lock size={14} strokeWidth={1.75} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-(--muted)" />
                <input
                  id="password"
                  type={showPassword ? "text" : "password"}
                  required
                  autoComplete="new-password"
                  minLength={6}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Минимум 6 символов"
                  className="w-full rounded-2xl border border-(--line) bg-stone-50 py-3 pl-10 pr-10 text-sm text-foreground outline-none transition focus:border-indigo-400 focus:bg-white"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3.5 top-1/2 -translate-y-1/2 text-(--muted) hover:text-foreground"
                >
                  {showPassword ? <EyeOff size={14} /> : <Eye size={14} />}
                </button>
              </div>
            </div>

            {error && (
              <p className="rounded-xl bg-red-50 border border-red-100 px-3 py-2.5 text-sm text-red-700">{error}</p>
            )}

            <button
              type="submit"
              disabled={status === "loading"}
              className="mt-1 inline-flex items-center justify-center gap-2 rounded-2xl py-3 text-sm font-semibold text-white transition hover:opacity-90 disabled:opacity-60"
              style={{ background: "var(--gradient)" }}
            >
              {status === "loading" ? (
                <><span className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-white/30 border-t-white" />Создаём аккаунт...</>
              ) : (
                <>Создать аккаунт <ArrowRight size={14} strokeWidth={2} /></>
              )}
            </button>
          </form>

          <p className="mt-6 text-center text-sm text-(--muted)">
            Уже есть аккаунт?{" "}
            <Link href="/login" className="font-semibold text-indigo-600 hover:text-indigo-800">
              Войти
            </Link>
          </p>
        </div>

        <p className="mt-6 text-center text-xs text-(--muted)">
          <Link href="/" className="hover:text-foreground">← Вернуться на главную</Link>
        </p>
      </motion.div>
    </div>
  );
}
