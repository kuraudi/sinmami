"use client";

import { useEffect, useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { X, Lock, CheckCircle2, Sparkles, ShieldCheck, Wifi } from "lucide-react";

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

function detectCardType(num: string): "visa" | "mc" | "mir" | null {
  const d = num.replace(/\s/g, "");
  if (d.startsWith("4")) return "visa";
  if (d.startsWith("5") || d.startsWith("2")) return "mc";
  if (d.startsWith("2200") || d.startsWith("2201") || d.startsWith("2202") || d.startsWith("2203") || d.startsWith("2204")) return "mir";
  return null;
}

function CardLogo({ type }: { type: "visa" | "mc" | "mir" | null }) {
  if (type === "visa") return (
    <svg viewBox="0 0 48 16" className="h-4 w-10 fill-current text-blue-700" aria-label="Visa">
      <text x="0" y="13" fontFamily="Arial" fontWeight="bold" fontSize="14" letterSpacing="-0.5">VISA</text>
    </svg>
  );
  if (type === "mc") return (
    <svg viewBox="0 0 38 24" className="h-5 w-8" aria-label="Mastercard">
      <circle cx="14" cy="12" r="10" fill="#EB001B" />
      <circle cx="24" cy="12" r="10" fill="#F79E1B" />
      <path d="M19 5.3a10 10 0 010 13.4A10 10 0 0119 5.3z" fill="#FF5F00" />
    </svg>
  );
  if (type === "mir") return (
    <svg viewBox="0 0 48 16" className="h-4 w-10" aria-label="МИР">
      <rect width="48" height="16" rx="3" fill="#00A76F" />
      <text x="4" y="12" fontFamily="Arial" fontWeight="bold" fontSize="10" fill="white">МИР</text>
    </svg>
  );
  return null;
}

export function PaymentModal({ onClose, onSuccess }: Props) {
  const [step, setStep] = useState<Step>("form");
  const [card, setCard] = useState("");
  const [expiry, setExpiry] = useState("");
  const [cvv, setCvv] = useState("");
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [processingMsg, setProcessingMsg] = useState("Устанавливаем защищённое соединение...");

  const cardType = detectCardType(card);

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

    const msgs = [
      "Устанавливаем защищённое соединение...",
      "Проверяем данные карты...",
      "Отправляем запрос в банк...",
      "Ожидаем подтверждение...",
    ];
    let i = 0;
    const iv = setInterval(() => {
      i++;
      if (i < msgs.length) setProcessingMsg(msgs[i]);
      else clearInterval(iv);
    }, 600);

    setTimeout(() => { clearInterval(iv); setStep("success"); }, 2800);
  }

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.2 }}
      className="fixed inset-0 z-50 flex items-end justify-center sm:items-center p-4"
      style={{ background: "rgba(8,6,24,0.72)", backdropFilter: "blur(8px)" }}
      onClick={(e) => { if (e.target === e.currentTarget && step === "form") onClose(); }}
    >
      <motion.div
        initial={{ opacity: 0, y: 40, scale: 0.96 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        exit={{ opacity: 0, y: 24, scale: 0.97 }}
        transition={{ duration: 0.3, ease: [0.22, 1, 0.36, 1] }}
        className="relative w-full max-w-sm overflow-hidden rounded-2xl shadow-2xl"
        style={{ background: "#f8f9fb" }}
      >
        <AnimatePresence mode="wait">

          {/* ── Форма ── */}
          {step === "form" && (
            <motion.div key="form" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}>

              {/* Шапка эквайринга */}
              <div className="flex items-center justify-between border-b border-stone-200 bg-white px-5 py-3.5">
                <div className="flex items-center gap-2.5">
                  <div className="flex h-7 w-7 items-center justify-center rounded-lg"
                    style={{ background: "linear-gradient(135deg, #4f46e5, #7c3aed)" }}>
                    <ShieldCheck size={14} strokeWidth={2} className="text-white" />
                  </div>
                  <div>
                    <p className="text-xs font-bold text-stone-800 leading-none">RentGen Pay</p>
                    <p className="text-[10px] text-stone-400 leading-none mt-0.5">Защищённый платёж</p>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <div className="flex items-center gap-1 rounded-full bg-emerald-50 px-2 py-0.5">
                    <div className="h-1.5 w-1.5 rounded-full bg-emerald-500" />
                    <span className="text-[10px] font-semibold text-emerald-700">SSL</span>
                  </div>
                  <button type="button" onClick={onClose} aria-label="Закрыть"
                    className="flex h-6 w-6 items-center justify-center rounded-full bg-stone-100 text-stone-400 transition hover:bg-stone-200">
                    <X size={12} strokeWidth={2.5} />
                  </button>
                </div>
              </div>

              {/* Сумма */}
              <div className="border-b border-stone-200 bg-white px-5 py-4">
                <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-stone-400">К оплате</p>
                <div className="mt-1 flex items-baseline gap-2">
                  <span className="text-3xl font-bold text-stone-900">299 ₽</span>
                  <span className="text-sm text-stone-400">· Премиум договор</span>
                </div>
                <p className="mt-1 text-xs text-stone-400">Одноразовый платёж · RentGen Premium</p>
              </div>

              {/* Форма карты */}
              <div className="px-5 py-5 flex flex-col gap-4">
                {/* Номер карты */}
                <div>
                  <label className="mb-1.5 block text-xs font-semibold text-stone-500">Номер карты</label>
                  <div className="relative">
                    <input
                      type="text"
                      inputMode="numeric"
                      placeholder="0000 0000 0000 0000"
                      value={card}
                      onChange={(e) => setCard(formatCardNumber(e.target.value))}
                      className="w-full rounded-xl border border-stone-200 bg-white py-3 pl-4 pr-12 text-sm font-mono text-stone-900 shadow-sm outline-none transition focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100"
                    />
                    <div className="absolute right-3.5 top-1/2 -translate-y-1/2">
                      {cardType ? <CardLogo type={cardType} /> : (
                        <div className="flex gap-1">
                          <div className="h-4 w-6 rounded-sm bg-stone-200" />
                          <div className="h-4 w-6 rounded-sm bg-stone-200" />
                        </div>
                      )}
                    </div>
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="mb-1.5 block text-xs font-semibold text-stone-500">Срок действия</label>
                    <input
                      type="text"
                      inputMode="numeric"
                      placeholder="ММ / ГГ"
                      value={expiry}
                      onChange={(e) => setExpiry(formatExpiry(e.target.value))}
                      className="w-full rounded-xl border border-stone-200 bg-white px-4 py-3 text-sm font-mono text-stone-900 shadow-sm outline-none transition focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100"
                    />
                  </div>
                  <div>
                    <label className="mb-1.5 block text-xs font-semibold text-stone-500">CVV / CVC</label>
                    <input
                      type="password"
                      inputMode="numeric"
                      placeholder="•••"
                      maxLength={4}
                      value={cvv}
                      onChange={(e) => setCvv(e.target.value.replace(/\D/g, "").slice(0, 4))}
                      className="w-full rounded-xl border border-stone-200 bg-white px-4 py-3 text-sm font-mono text-stone-900 shadow-sm outline-none transition focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100"
                    />
                  </div>
                </div>

                <div>
                  <label className="mb-1.5 block text-xs font-semibold text-stone-500">Имя держателя карты</label>
                  <input
                    type="text"
                    placeholder="IVAN IVANOV"
                    value={name}
                    onChange={(e) => setName(e.target.value.toUpperCase())}
                    className="w-full rounded-xl border border-stone-200 bg-white px-4 py-3 text-sm font-mono uppercase text-stone-900 shadow-sm outline-none transition focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100"
                  />
                </div>

                {error && (
                  <div className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-2.5 text-xs text-rose-700">{error}</div>
                )}

                <button
                  type="button"
                  onClick={handlePay}
                  className="inline-flex w-full items-center justify-center gap-2 rounded-xl py-3.5 text-sm font-bold text-white shadow-lg transition hover:opacity-90 active:scale-[0.98]"
                  style={{ background: "linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%)", color: "#ffffff", boxShadow: "0 4px 20px rgba(79,70,229,0.45)" }}
                >
                  <Lock size={13} strokeWidth={2.5} />
                  Оплатить 299 ₽
                </button>

                <div className="flex items-center gap-3">
                  <div className="h-px flex-1 bg-stone-200" />
                  <span className="text-[11px] text-stone-400">или</span>
                  <div className="h-px flex-1 bg-stone-200" />
                </div>

                <button
                  type="button"
                  onClick={() => { setError(null); setStep("success"); }}
                  className="inline-flex w-full items-center justify-center gap-2 rounded-xl border border-emerald-200 bg-emerald-50 py-3 text-sm font-semibold text-emerald-700 transition hover:bg-emerald-100"
                >
                  <CheckCircle2 size={14} strokeWidth={2} />
                  Я уже оплатил
                </button>
              </div>

              {/* Подвал */}
              <div className="border-t border-stone-200 bg-white px-5 py-3 flex items-center justify-between">
                <div className="flex items-center gap-1.5 text-[10px] text-stone-400">
                  <Wifi size={11} strokeWidth={2} />
                  Данные передаются по TLS 1.3
                </div>
                <div className="flex items-center gap-2">
                  <div className="h-4 w-7 rounded-sm bg-blue-700 flex items-center justify-center">
                    <span className="text-[7px] font-bold text-white">VISA</span>
                  </div>
                  <svg viewBox="0 0 28 18" className="h-4 w-7" aria-label="Mastercard">
                    <circle cx="10" cy="9" r="7" fill="#EB001B" />
                    <circle cx="18" cy="9" r="7" fill="#F79E1B" />
                    <path d="M14 3.8a7 7 0 010 10.4A7 7 0 0114 3.8z" fill="#FF5F00" />
                  </svg>
                  <div className="h-4 w-8 rounded-sm bg-green-600 flex items-center justify-center">
                    <span className="text-[7px] font-bold text-white">МИР</span>
                  </div>
                </div>
              </div>
            </motion.div>
          )}

          {/* ── Обработка ── */}
          {step === "processing" && (
            <motion.div key="processing" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
              className="flex flex-col items-center gap-5 bg-white px-8 py-16 text-center">
              <div className="relative">
                <div className="h-14 w-14 animate-spin rounded-full border-4 border-stone-100 border-t-indigo-600" />
                <div className="absolute inset-0 flex items-center justify-center">
                  <Lock size={16} strokeWidth={2} className="text-indigo-600" />
                </div>
              </div>
              <div>
                <p className="font-bold text-stone-900">Обработка платежа</p>
                <motion.p
                  key={processingMsg}
                  initial={{ opacity: 0, y: 4 }}
                  animate={{ opacity: 1, y: 0 }}
                  className="mt-1.5 text-sm text-stone-500"
                >
                  {processingMsg}
                </motion.p>
              </div>
              <div className="flex gap-1.5">
                {[0, 1, 2].map((i) => (
                  <motion.div key={i} className="h-1.5 w-1.5 rounded-full bg-indigo-400"
                    animate={{ opacity: [0.3, 1, 0.3] }}
                    transition={{ duration: 1.2, repeat: Infinity, delay: i * 0.2 }} />
                ))}
              </div>
            </motion.div>
          )}

          {/* ── Успех ── */}
          {step === "success" && (
            <motion.div key="success" initial={{ opacity: 0 }} animate={{ opacity: 1 }}
              className="flex flex-col items-center gap-5 bg-white px-8 py-14 text-center">
              <motion.div
                initial={{ scale: 0, rotate: -20 }}
                animate={{ scale: 1, rotate: 0 }}
                transition={{ type: "spring", stiffness: 240, damping: 18 }}
                className="flex h-20 w-20 items-center justify-center rounded-full"
                style={{ background: "linear-gradient(135deg, #d1fae5, #a7f3d0)" }}
              >
                <CheckCircle2 size={36} strokeWidth={1.75} className="text-emerald-600" />
              </motion.div>
              <div>
                <p className="text-xl font-bold text-stone-900">Платёж подтверждён</p>
                <p className="mt-1 text-sm text-stone-500">Транзакция успешно проведена</p>
                <div className="mt-3 inline-flex items-center gap-1.5 rounded-full border border-stone-200 bg-stone-50 px-3 py-1 text-xs text-stone-500">
                  <span className="font-mono">#{Math.random().toString(36).slice(2, 10).toUpperCase()}</span>
                  <span>·</span>
                  <span>299 ₽</span>
                </div>
              </div>
              <button
                type="button"
                onClick={onSuccess}
                className="inline-flex items-center gap-2 rounded-xl px-8 py-3.5 text-sm font-bold text-white transition hover:opacity-90"
                style={{ background: "linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%)", color: "#ffffff", boxShadow: "0 4px 20px rgba(79,70,229,0.4)" }}
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
