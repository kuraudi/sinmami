"use client";

import { useEffect } from "react";
import Link from "next/link";
import { motion } from "framer-motion";
import { X, UserCircle, ShieldAlert, History, Smartphone } from "lucide-react";

type Props = {
  onClose: () => void;
  onContinue: () => void;
};

export function GuestWarningModal({ onClose, onContinue }: Props) {
  useEffect(() => {
    function handleKey(e: KeyboardEvent) {
      if (e.key === "Escape") onClose();
    }
    document.addEventListener("keydown", handleKey);
    return () => document.removeEventListener("keydown", handleKey);
  }, [onClose]);

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.2 }}
      className="fixed inset-0 z-50 flex items-end justify-center sm:items-center p-4"
      style={{ background: "rgba(10,8,30,0.65)", backdropFilter: "blur(6px)" }}
      onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}
    >
      <motion.div
        initial={{ opacity: 0, y: 40, scale: 0.95 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        exit={{ opacity: 0, y: 24, scale: 0.97 }}
        transition={{ duration: 0.3, ease: [0.22, 1, 0.36, 1] }}
        className="relative w-full max-w-sm overflow-hidden rounded-3xl bg-white shadow-2xl"
      >
        <button
          type="button"
          onClick={onClose}
          aria-label="Закрыть"
          className="absolute right-4 top-4 z-10 flex h-7 w-7 items-center justify-center rounded-full bg-black/6 text-stone-400 transition hover:bg-black/10 hover:text-stone-600"
        >
          <X size={14} strokeWidth={2} />
        </button>

        {/* Шапка */}
        <div className="relative overflow-hidden px-6 pb-6 pt-6"
          style={{ background: "linear-gradient(135deg, #1e1b4b 0%, #4338ca 100%)" }}>
          <div className="pointer-events-none absolute -right-8 -top-8 h-32 w-32 rounded-full"
            style={{ background: "radial-gradient(circle, rgba(139,92,246,0.35) 0%, transparent 70%)", filter: "blur(20px)" }} />
          <div className="relative">
            <div className="mb-3 flex h-11 w-11 items-center justify-center rounded-2xl bg-white/10">
              <ShieldAlert size={20} strokeWidth={1.75} className="text-amber-300" />
            </div>
            <h3 className="font-editorial text-2xl leading-snug text-white">
              Договор может потеряться
            </h3>
            <p className="mt-2 text-sm leading-6 text-white/60">
              Без аккаунта история хранится только в этом браузере.
            </p>
          </div>
        </div>

        {/* Риски */}
        <div className="px-6 py-4 border-b border-stone-100">
          <div className="flex flex-col gap-2.5">
            {[
              { icon: History, text: "История не сохранится между устройствами" },
              { icon: Smartphone, text: "Очистка браузера удалит доступ к договору" },
            ].map(({ icon: Icon, text }) => (
              <div key={text} className="flex items-center gap-3 text-sm text-stone-500">
                <div className="flex h-6 w-6 shrink-0 items-center justify-center rounded-lg bg-amber-50">
                  <Icon size={12} strokeWidth={1.75} className="text-amber-500" />
                </div>
                {text}
              </div>
            ))}
          </div>
        </div>

        {/* Кнопки */}
        <div className="flex flex-col gap-2.5 px-6 py-5">
          <Link
            href="/register"
            className="inline-flex w-full items-center justify-center gap-2 rounded-2xl py-3 text-sm font-bold transition hover:opacity-90 active:scale-[0.98]"
            style={{ background: "linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%)", color: "#ffffff", boxShadow: "0 4px 16px rgba(79,70,229,0.45)" }}
          >
            <UserCircle size={15} strokeWidth={1.75} />
            Создать аккаунт — бесплатно
          </Link>
          <button
            type="button"
            onClick={onContinue}
            className="inline-flex w-full items-center justify-center gap-2 rounded-2xl border border-stone-200 py-3 text-sm font-semibold text-stone-700 transition hover:bg-stone-50"
            style={{ boxShadow: "0 2px 8px rgba(0,0,0,0.08)" }}
          >
            Продолжить без аккаунта
          </button>
        </div>
      </motion.div>
    </motion.div>
  );
}
