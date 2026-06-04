"use client";

import { useEffect, useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { X, CreditCard, Lock, CheckCircle2, Sparkles, Star } from "lucide-react";

type Step = "form" | "processing" | "success";

type Props = {
  onClose: () => void;
  onSuccess: () => void;
};

function formatCardNumber(v: string) {
  return v.replace(/\D/g, "").slice(0, 16).replace(/(.{4})/g, "$1 ").trim();
}
function formatExpiry(v: string) {
  const d = v.replace(/\D/g, "").slice(0, 4);
  if (d.length >= 3) return d.slice(0, 2) + "/" + d.slice(2);
  return d;
}

export function PaymentModal({ onClose, onSuccess }: Props) {
  const [step, setStep] = useState<Step>("form");
  const [card, setCard] = useState("");
  const [expiry, setExpiry] = useState("");
  const [cvv, setCvv] = useState("");
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    function handleKey(e: KeyboardEvent) {
      if (e.key === "Escape" && step === "form") onClose();
    }
    document.addEventListener("keydown", handleKey);
    return () => document.removeEventListener("keydown", handleKey);
  }, [onClose, step]);

  function handlePay() {
    const digits = card.replace(/\s/g, "");
    if (digits.length < 16) { setError("Введите полный номер карты"); return; }
    if (expiry.length < 5) { setError("Введите срок действия карты"); return; }
    if (cvv.length < 3) { setError("Введите CVV"); return; }
    if (!name.trim()) { setError("Введите имя держателя карты"); return; }
    setError(null);
    setStep("processing");
    setTimeout(() => setStep("success"), 2200);
  }

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.2 }}
      className="fixed inset-0 z-50 flex items-end justify-center sm:items-center p-4"
      style={{ background: "rgba(10,8,30,0.65)", backdropFilter: "blur(6px)" }}
      onClick={(e) => { if (e.target === e.currentTarget && step === "form") onClose(); }}
    >
      <motion.div
        initial={{ opacity: 0, y: 40, scale: 0.95 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        exit={{ opacity: 0, y: 24, scale: 0.97 }}
        transition={{ duration: 0.3, ease: [0.22, 1, 0.36, 1] }}
        className="relative w-full max-w-sm overflow-hidden rounded-3xl bg-white shadow-2xl"
      >
        <AnimatePresence mode="wait">
          {step === "form" && (
            <motion.div key="form" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}>
              {/* Шапка */}
              <button
                type="button"
                onClick={onClose}
                aria-label="Закрыть"
                className="absolute right-4 top-4 z-10 flex h-7 w-7 items-center justify-center rounded-full bg-black/6 text-stone-400 transition hover:bg-black/10 hover:text-stone-600"
              >
                <X size={14} strokeWidth={2} />
              </button>

              <div className="px-6 pb-5 pt-6 relative overflow-hidden"
                style={{ background: "linear-gradient(135deg, #1e1b4b 0%, #4338ca 100%)" }}>
                <div className="pointer-events-none absolute -right-6 -top-6 h-28 w-28 rounded-full"
                  style={{ background: "radial-gradient(circle, rgba(139,92,246,0.4) 0%, transparent 70%)", filter: "blur(18px)" }} />
                <div className="relative">
                  <div className="mb-3 flex h-11 w-11 items-center justify-center rounded-2xl bg-white/10">
                    <Sparkles size={20} strokeWidth={1.75} className="text-amber-300" />
                  </div>
                  <h3 className="font-editorial text-2xl text-white leading-snug">Премиум доступ</h3>
                  <p className="mt-1 text-sm text-white/60">Единоразовый платёж за договор</p>
                  <div className="mt-3 inline-flex items-baseline gap-1">
                    <span className="text-3xl font-bold text-white">299 ₽</span>
                    <span className="text-sm text-white/50">/ документ</span>
                  </div>
                </div>
              </div>

              {/* Что включено */}
              <div className="border-b border-stone-100 px-6 py-3">
                <div className="flex flex-col gap-1.5">
                  {["Расширенные разделы договора", "Персонализированный гайд", "Приложения и акты"].map((f) => (
                    <div key={f} className="flex items-center gap-2.5 text-xs text-stone-600">
                      <Star size={11} strokeWidth={2} className="shrink-0 text-amber-400" />
                      {f}
                    </div>
                  ))}
                </div>
              </div>

              {/* Форма */}
              <div className="px-6 py-5 flex flex-col gap-3">
                <div>
                  <label className="mb-1.5 block text-xs font-semibold text-stone-500">Номер карты</label>
                  <div className="relative">
                    <CreditCard size={15} strokeWidth={1.75} className="absolute left-3.5 top-1/2 -translate-y-1/2 text-stone-400" />
                    <input
                      type="text"
                      inputMode="numeric"
                      placeholder="0000 0000 0000 0000"
                      value={card}
                      onChange={(e) => setCard(formatCardNumber(e.target.value))}
                      className="w-full rounded-2xl border border-stone-200 bg-stone-50 py-2.5 pl-9 pr-4 text-sm text-foreground outline-none transition focus:border-indigo-400 focus:bg-white"
                    />
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="mb-1.5 block text-xs font-semibold text-stone-500">Срок действия</label>
                    <input
                      type="text"
                      inputMode="numeric"
                      placeholder="MM/YY"
                      value={expiry}
                      onChange={(e) => setExpiry(formatExpiry(e.target.value))}
                      className="w-full rounded-2xl border border-stone-200 bg-stone-50 px-4 py-2.5 text-sm text-foreground outline-none transition focus:border-indigo-400 focus:bg-white"
                    />
                  </div>
                  <div>
                    <label className="mb-1.5 block text-xs font-semibold text-stone-500">CVV</label>
                    <input
                      type="password"
                      inputMode="numeric"
                      placeholder="•••"
                      maxLength={4}
                      value={cvv}
                      onChange={(e) => setCvv(e.target.value.replace(/\D/g, "").slice(0, 4))}
                      className="w-full rounded-2xl border border-stone-200 bg-stone-50 px-4 py-2.5 text-sm text-foreground outline-none transition focus:border-indigo-400 focus:bg-white"
                    />
                  </div>
                </div>

                <div>
                  <label className="mb-1.5 block text-xs font-semibold text-stone-500">Имя держателя</label>
                  <input
                    type="text"
                    placeholder="IVAN IVANOV"
                    value={name}
                    onChange={(e) => setName(e.target.value.toUpperCase())}
                    className="w-full rounded-2xl border border-stone-200 bg-stone-50 px-4 py-2.5 text-sm text-foreground outline-none transition focus:border-indigo-400 focus:bg-white"
                  />
                </div>

                {error && (
                  <p className="rounded-2xl bg-rose-50 px-4 py-2.5 text-xs text-rose-700">{error}</p>
                )}

                <button
                  type="button"
                  onClick={handlePay}
                  className="inline-flex w-full items-center justify-center gap-2 rounded-2xl py-3 text-sm font-bold transition hover:opacity-90 active:scale-[0.98]"
                  style={{ background: "linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%)", color: "#ffffff", boxShadow: "0 4px 16px rgba(79,70,229,0.4)" }}
                >
                  <Lock size={13} strokeWidth={2} />
                  Оплатить 299 ₽
                </button>

                <div className="flex items-center gap-3">
                  <div className="h-px flex-1 bg-stone-100" />
                  <span className="text-[11px] text-stone-400">или</span>
                  <div className="h-px flex-1 bg-stone-100" />
                </div>

                <button
                  type="button"
                  onClick={() => { setError(null); setStep("success"); }}
                  className="inline-flex w-full items-center justify-center gap-2 rounded-2xl border border-emerald-200 bg-emerald-50 py-2.5 text-sm font-semibold text-emerald-700 transition hover:bg-emerald-100"
                >
                  <CheckCircle2 size={14} strokeWidth={2} />
                  Я уже оплатил
                </button>

                <p className="text-center text-[10px] text-stone-400">
                  Защищено SSL · Данные не сохраняются
                </p>
              </div>
            </motion.div>
          )}

          {step === "processing" && (
            <motion.div key="processing" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
              className="flex flex-col items-center gap-4 px-8 py-16 text-center">
              <div className="h-12 w-12 animate-spin rounded-full border-4 border-indigo-100 border-t-indigo-600" />
              <div>
                <p className="font-semibold text-foreground">Обрабатываем платёж</p>
                <p className="mt-1 text-sm text-stone-500">Подождите несколько секунд...</p>
              </div>
            </motion.div>
          )}

          {step === "success" && (
            <motion.div key="success" initial={{ opacity: 0, scale: 0.95 }} animate={{ opacity: 1, scale: 1 }}
              className="flex flex-col items-center gap-4 px-8 py-14 text-center">
              <motion.div
                initial={{ scale: 0 }}
                animate={{ scale: 1 }}
                transition={{ type: "spring", stiffness: 260, damping: 20 }}
                className="flex h-16 w-16 items-center justify-center rounded-full bg-emerald-100"
              >
                <CheckCircle2 size={32} strokeWidth={1.75} className="text-emerald-600" />
              </motion.div>
              <div>
                <p className="text-xl font-bold text-foreground">Оплата прошла!</p>
                <p className="mt-1.5 text-sm text-stone-500 leading-6">Доступ к премиум-документу открыт. Сейчас сгенерируем договор.</p>
              </div>
              <button
                type="button"
                onClick={onSuccess}
                className="inline-flex items-center gap-2 rounded-2xl px-8 py-3 text-sm font-bold text-white transition hover:opacity-90"
                style={{ background: "linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%)", color: "#ffffff", boxShadow: "0 4px 16px rgba(79,70,229,0.4)" }}
              >
                <Sparkles size={14} strokeWidth={1.75} />
                Сгенерировать договор
              </button>
            </motion.div>
          )}
        </AnimatePresence>
      </motion.div>
    </motion.div>
  );
}
