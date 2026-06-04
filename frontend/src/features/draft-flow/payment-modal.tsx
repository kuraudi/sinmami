"use client";

import { useEffect, useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { X, Lock, CheckCircle2, Sparkles, ShieldCheck, Eye, EyeOff } from "lucide-react";

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
  if (/^5[1-5]/.test(d) || /^2[2-7]/.test(d)) return "mc";
  if (/^220[0-4]/.test(d)) return "mir";
  return null;
}

function VisaLogo() {
  return (
    <svg viewBox="0 0 48 16" className="h-4" style={{ width: 44 }}>
      <text x="0" y="13" fontFamily="Arial Black, Arial" fontWeight="900" fontSize="15" fill="#1a1f71" letterSpacing="-0.5">VISA</text>
    </svg>
  );
}
function MastercardLogo() {
  return (
    <svg viewBox="0 0 38 24" style={{ height: 20, width: 32 }}>
      <circle cx="14" cy="12" r="10" fill="#EB001B" />
      <circle cx="24" cy="12" r="10" fill="#F79E1B" />
      <path d="M19 4.8a10 10 0 010 14.4A10 10 0 0119 4.8z" fill="#FF5F00" />
    </svg>
  );
}
function MirLogo() {
  return (
    <svg viewBox="0 0 54 20" style={{ height: 20, width: 40 }}>
      <rect width="54" height="20" rx="4" fill="#00A76F" />
      <text x="6" y="14" fontFamily="Arial" fontWeight="bold" fontSize="10" fill="white">МИР</text>
    </svg>
  );
}

const PROCESSING_MSGS = [
  "Устанавливаем защищённое соединение...",
  "Проверяем реквизиты карты...",
  "Отправляем запрос в банк-эмитент...",
  "Ожидаем подтверждение от банка...",
  "Завершаем транзакцию...",
];

export function PaymentModal({ onClose, onSuccess }: Props) {
  const [step, setStep] = useState<Step>("form");
  const [card, setCard] = useState("");
  const [expiry, setExpiry] = useState("");
  const [cvv, setCvv] = useState("");
  const [name, setName] = useState("");
  const [showCvv, setShowCvv] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [processingIdx, setProcessingIdx] = useState(0);
  const [txnId] = useState(() => Math.random().toString(36).slice(2, 12).toUpperCase());

  const cardType = detectCardType(card);

  useEffect(() => {
    function handleKey(e: KeyboardEvent) {
      if (e.key === "Escape" && step === "form") onClose();
    }
    document.addEventListener("keydown", handleKey);
    return () => document.removeEventListener("keydown", handleKey);
  }, [onClose, step]);

  useEffect(() => {
    if (step !== "processing") return;
    setProcessingIdx(0);
    let i = 0;
    const iv = setInterval(() => {
      i++;
      if (i < PROCESSING_MSGS.length) setProcessingIdx(i);
      else clearInterval(iv);
    }, 520);
    const done = setTimeout(() => { clearInterval(iv); setStep("success"); }, 3000);
    return () => { clearInterval(iv); clearTimeout(done); };
  }, [step]);

  function handlePay() {
    const digits = card.replace(/\s/g, "");
    if (digits.length < 16) { setError("Введите полный номер карты"); return; }
    if (expiry.length < 5) { setError("Введите срок действия"); return; }
    if (cvv.length < 3) { setError("Введите CVV/CVC код"); return; }
    if (!name.trim()) { setError("Введите имя держателя карты"); return; }
    setError(null);
    setStep("processing");
  }

  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}
      transition={{ duration: 0.2 }}
      className="fixed inset-0 z-50 flex items-end justify-center sm:items-center p-3 sm:p-4"
      style={{ background: "rgba(0,0,0,0.6)", backdropFilter: "blur(6px)" }}
      onClick={(e) => { if (e.target === e.currentTarget && step === "form") onClose(); }}
    >
      <motion.div
        initial={{ opacity: 0, y: 32, scale: 0.97 }}
        animate={{ opacity: 1, y: 0, scale: 1 }}
        exit={{ opacity: 0, y: 20, scale: 0.97 }}
        transition={{ duration: 0.28, ease: [0.22, 1, 0.36, 1] }}
        className="relative w-full max-w-[420px] overflow-hidden rounded-2xl shadow-2xl"
        style={{ background: "#ffffff" }}
      >
        <AnimatePresence mode="wait">

          {/* ══ ФОРМА ══ */}
          {step === "form" && (
            <motion.div key="form" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}>

              {/* Топ-бар банка */}
              <div className="flex items-center justify-between border-b px-5 py-3" style={{ borderColor: "#e5e7eb", background: "#fafafa" }}>
                <div className="flex items-center gap-3">
                  {/* Псевдологотип банка */}
                  <div className="flex h-8 w-8 items-center justify-center rounded-lg" style={{ background: "linear-gradient(135deg,#0f4c9e,#1a73e8)" }}>
                    <svg viewBox="0 0 16 16" fill="none" className="h-4 w-4">
                      <rect x="1" y="4" width="14" height="9" rx="1.5" stroke="white" strokeWidth="1.2" />
                      <path d="M1 7h14" stroke="white" strokeWidth="1.2" />
                    </svg>
                  </div>
                  <div>
                    <p className="text-[11px] font-bold leading-none text-gray-800">РентГен Банк</p>
                    <p className="mt-0.5 text-[10px] leading-none text-gray-400">Защищённая оплата</p>
                  </div>
                </div>
                <div className="flex items-center gap-2.5">
                  <div className="flex items-center gap-1 rounded-full border px-2 py-0.5" style={{ borderColor: "#d1fae5", background: "#f0fdf4" }}>
                    <ShieldCheck size={10} strokeWidth={2.5} className="text-emerald-600" />
                    <span className="text-[10px] font-bold text-emerald-700">SSL</span>
                  </div>
                  {step === "form" && (
                    <button type="button" onClick={onClose}
                      className="flex h-6 w-6 items-center justify-center rounded-full text-gray-400 transition hover:bg-gray-100 hover:text-gray-600">
                      <X size={13} strokeWidth={2.5} />
                    </button>
                  )}
                </div>
              </div>

              {/* Сумма и получатель */}
              <div className="border-b px-5 py-4" style={{ borderColor: "#f3f4f6" }}>
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-[11px] font-semibold uppercase tracking-widest text-gray-400">К оплате</p>
                    <p className="mt-0.5 text-[28px] font-bold leading-none text-gray-900">299 <span className="text-xl">₽</span></p>
                  </div>
                  <div className="text-right">
                    <p className="text-xs font-semibold text-gray-700">RentGen</p>
                    <p className="text-[11px] text-gray-400">Премиум договор</p>
                    <p className="mt-1 text-[10px] text-gray-300">ID: {txnId.slice(0, 8)}</p>
                  </div>
                </div>
              </div>

              {/* Форма карты */}
              <div className="px-5 py-4 flex flex-col gap-3">

                {/* Номер карты */}
                <div>
                  <label className="mb-1 block text-[11px] font-semibold text-gray-500">Номер карты</label>
                  <div className="relative">
                    <input
                      type="text"
                      inputMode="numeric"
                      autoComplete="cc-number"
                      placeholder="0000 0000 0000 0000"
                      value={card}
                      onChange={(e) => setCard(formatCardNumber(e.target.value))}
                      className="w-full rounded-xl border bg-gray-50 py-3 pl-4 pr-14 font-mono text-sm text-gray-900 outline-none transition"
                      style={{ borderColor: "#d1d5db" }}
                      onFocus={(e) => (e.target.style.borderColor = "#6366f1")}
                      onBlur={(e) => (e.target.style.borderColor = "#d1d5db")}
                    />
                    <div className="absolute right-3 top-1/2 -translate-y-1/2 flex items-center gap-1">
                      {cardType === "visa" && <VisaLogo />}
                      {cardType === "mc" && <MastercardLogo />}
                      {cardType === "mir" && <MirLogo />}
                      {!cardType && (
                        <div className="flex gap-1 opacity-30">
                          <div className="h-5 w-7 rounded-sm border border-gray-300" />
                        </div>
                      )}
                    </div>
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-3">
                  <div>
                    <label className="mb-1 block text-[11px] font-semibold text-gray-500">Срок действия</label>
                    <input
                      type="text"
                      inputMode="numeric"
                      autoComplete="cc-exp"
                      placeholder="ММ / ГГ"
                      value={expiry}
                      onChange={(e) => setExpiry(formatExpiry(e.target.value))}
                      className="w-full rounded-xl border bg-gray-50 px-4 py-3 font-mono text-sm text-gray-900 outline-none transition"
                      style={{ borderColor: "#d1d5db" }}
                      onFocus={(e) => (e.target.style.borderColor = "#6366f1")}
                      onBlur={(e) => (e.target.style.borderColor = "#d1d5db")}
                    />
                  </div>
                  <div>
                    <label className="mb-1 block text-[11px] font-semibold text-gray-500">CVV / CVC</label>
                    <div className="relative">
                      <input
                        type={showCvv ? "text" : "password"}
                        inputMode="numeric"
                        autoComplete="cc-csc"
                        placeholder="•••"
                        maxLength={4}
                        value={cvv}
                        onChange={(e) => setCvv(e.target.value.replace(/\D/g, "").slice(0, 4))}
                        className="w-full rounded-xl border bg-gray-50 py-3 pl-4 pr-10 font-mono text-sm text-gray-900 outline-none transition"
                        style={{ borderColor: "#d1d5db" }}
                        onFocus={(e) => (e.target.style.borderColor = "#6366f1")}
                        onBlur={(e) => (e.target.style.borderColor = "#d1d5db")}
                      />
                      <button type="button" tabIndex={-1}
                        onClick={() => setShowCvv(!showCvv)}
                        className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-gray-600">
                        {showCvv ? <EyeOff size={14} strokeWidth={1.75} /> : <Eye size={14} strokeWidth={1.75} />}
                      </button>
                    </div>
                  </div>
                </div>

                <div>
                  <label className="mb-1 block text-[11px] font-semibold text-gray-500">Имя держателя</label>
                  <input
                    type="text"
                    autoComplete="cc-name"
                    placeholder="IVAN IVANOV"
                    value={name}
                    onChange={(e) => setName(e.target.value.toUpperCase())}
                    className="w-full rounded-xl border bg-gray-50 px-4 py-3 font-mono text-sm uppercase text-gray-900 outline-none transition"
                    style={{ borderColor: "#d1d5db" }}
                    onFocus={(e) => (e.target.style.borderColor = "#6366f1")}
                    onBlur={(e) => (e.target.style.borderColor = "#d1d5db")}
                  />
                </div>

                {error && (
                  <div className="rounded-xl border border-red-100 bg-red-50 px-4 py-2.5 text-xs text-red-700">{error}</div>
                )}

                <button
                  type="button"
                  onClick={handlePay}
                  className="mt-1 inline-flex w-full items-center justify-center gap-2 rounded-xl py-3.5 text-sm font-bold text-white transition hover:opacity-90 active:scale-[0.98]"
                  style={{ background: "linear-gradient(135deg,#1a73e8,#0f4c9e)", color: "#fff", boxShadow: "0 4px 16px rgba(26,115,232,0.35)" }}
                >
                  <Lock size={13} strokeWidth={2.5} />
                  Оплатить 299 ₽
                </button>

                <div className="flex items-center gap-3">
                  <div className="h-px flex-1 bg-gray-100" />
                  <span className="text-[11px] text-gray-400">или</span>
                  <div className="h-px flex-1 bg-gray-100" />
                </div>

                <button
                  type="button"
                  onClick={() => { setError(null); setStep("success"); }}
                  className="inline-flex w-full items-center justify-center gap-2 rounded-xl border py-3 text-sm font-semibold transition hover:bg-gray-50"
                  style={{ borderColor: "#e5e7eb", color: "#374151" }}
                >
                  <CheckCircle2 size={14} strokeWidth={2} className="text-emerald-500" />
                  Я уже оплатил
                </button>
              </div>

              {/* Футер с логотипами */}
              <div className="border-t px-5 py-3 flex items-center justify-between" style={{ borderColor: "#f3f4f6", background: "#fafafa" }}>
                <p className="text-[10px] text-gray-400">Данные защищены · TLS 1.3</p>
                <div className="flex items-center gap-2">
                  <VisaLogo />
                  <MastercardLogo />
                  <MirLogo />
                  {/* Иконка 3DS */}
                  <div className="rounded border px-1.5 py-0.5 text-[8px] font-bold" style={{ borderColor: "#d1d5db", color: "#6b7280" }}>3DS</div>
                  {/* ЦБ РФ */}
                  <div className="rounded border px-1.5 py-0.5 text-[8px] font-bold" style={{ borderColor: "#d1d5db", color: "#6b7280" }}>ЦБ РФ</div>
                </div>
              </div>
            </motion.div>
          )}

          {/* ══ ОБРАБОТКА ══ */}
          {step === "processing" && (
            <motion.div key="processing" initial={{ opacity: 0 }} animate={{ opacity: 1 }} exit={{ opacity: 0 }}
              className="flex flex-col items-center gap-6 px-8 py-16 text-center">
              {/* Банковский спиннер */}
              <div className="relative">
                <svg className="h-16 w-16 -rotate-90" viewBox="0 0 56 56">
                  <circle cx="28" cy="28" r="24" fill="none" stroke="#e5e7eb" strokeWidth="4" />
                  <motion.circle cx="28" cy="28" r="24" fill="none" stroke="#1a73e8" strokeWidth="4"
                    strokeLinecap="round" strokeDasharray="150"
                    animate={{ strokeDashoffset: [150, 0] }}
                    transition={{ duration: 2.8, ease: "easeInOut" }}
                  />
                </svg>
                <div className="absolute inset-0 flex items-center justify-center">
                  <Lock size={18} strokeWidth={2} className="text-blue-600" />
                </div>
              </div>
              <div>
                <p className="font-bold text-gray-900">Обработка платежа</p>
                <motion.p key={processingIdx} initial={{ opacity: 0, y: 4 }} animate={{ opacity: 1, y: 0 }}
                  className="mt-2 text-sm text-gray-500">{PROCESSING_MSGS[processingIdx]}</motion.p>
              </div>
              <div className="w-full rounded-xl border bg-gray-50 px-4 py-3 text-left" style={{ borderColor: "#f3f4f6" }}>
                <p className="text-[11px] font-semibold text-gray-400 mb-1.5">Транзакция</p>
                <p className="font-mono text-xs text-gray-600">{txnId}</p>
                <p className="mt-0.5 text-[11px] text-gray-400">RentGen · 299 ₽</p>
              </div>
              <p className="text-[11px] text-gray-400">Не закрывайте страницу</p>
            </motion.div>
          )}

          {/* ══ УСПЕХ ══ */}
          {step === "success" && (
            <motion.div key="success" initial={{ opacity: 0 }} animate={{ opacity: 1 }}
              className="flex flex-col items-center gap-5 px-8 py-14 text-center">
              <motion.div
                initial={{ scale: 0, rotate: -15 }}
                animate={{ scale: 1, rotate: 0 }}
                transition={{ type: "spring", stiffness: 220, damping: 16 }}
                className="flex h-20 w-20 items-center justify-center rounded-full"
                style={{ background: "linear-gradient(135deg,#d1fae5,#a7f3d0)" }}
              >
                <CheckCircle2 size={38} strokeWidth={1.75} className="text-emerald-600" />
              </motion.div>

              <div>
                <p className="text-xl font-bold text-gray-900">Платёж подтверждён</p>
                <p className="mt-1 text-sm text-gray-500">Транзакция успешно обработана банком</p>
              </div>

              {/* Чек */}
              <div className="w-full rounded-2xl border px-5 py-4 text-left" style={{ borderColor: "#e5e7eb" }}>
                <div className="flex items-center justify-between border-b pb-3 mb-3" style={{ borderColor: "#f3f4f6" }}>
                  <p className="text-[11px] font-bold uppercase tracking-widest text-gray-400">Чек</p>
                  <span className="rounded-full bg-emerald-50 px-2.5 py-0.5 text-[10px] font-bold text-emerald-700">Оплачено</span>
                </div>
                <div className="flex flex-col gap-1.5 text-xs">
                  <div className="flex justify-between">
                    <span className="text-gray-400">Получатель</span>
                    <span className="font-semibold text-gray-800">RentGen</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">Услуга</span>
                    <span className="font-semibold text-gray-800">Премиум договор</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">Сумма</span>
                    <span className="font-bold text-gray-900">299 ₽</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">Транзакция</span>
                    <span className="font-mono text-[11px] text-gray-600">{txnId}</span>
                  </div>
                </div>
              </div>

              <button
                type="button"
                onClick={onSuccess}
                className="inline-flex w-full items-center justify-center gap-2 rounded-xl py-3.5 text-sm font-bold text-white transition hover:opacity-90"
                style={{ background: "linear-gradient(135deg,#1a73e8,#0f4c9e)", color: "#fff", boxShadow: "0 4px 16px rgba(26,115,232,0.3)" }}
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
