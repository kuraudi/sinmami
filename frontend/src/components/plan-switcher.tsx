"use client";

import { useAppDispatch, useAppSelector } from "@/store/hooks";
import { setPlan } from "@/store/slices/session-slice";
import type { PlanHeader } from "@/types/api";

const plans: PlanHeader[] = ["Free", "Premium"];
const planLabels: Record<PlanHeader, string> = {
  Free: "Бесплатно",
  Premium: "Премиум",
};

export function PlanSwitcher() {
  const dispatch = useAppDispatch();
  const plan = useAppSelector((state) => state.session.plan);
  const hydrated = useAppSelector((state) => state.session.hydrated);

  if (!hydrated) {
    return (
      <div className="inline-flex rounded-full border border-[var(--line)] bg-white/70 p-1 shadow-sm">
        <span className="rounded-full px-4 py-2 text-sm font-semibold text-[var(--muted)]">
          Загрузка тарифа
        </span>
      </div>
    );
  }

  return (
    <div className="inline-flex rounded-full border border-[var(--line)] bg-white/70 p-1 shadow-sm">
      {plans.map((item) => {
        const active = item === plan;

        return (
          <button
            key={item}
            type="button"
            onClick={() => dispatch(setPlan(item))}
            className={`rounded-full px-4 py-2 text-sm font-semibold transition ${
              active
                ? "bg-[var(--accent)] text-white"
                : "text-[var(--muted)] hover:bg-[var(--accent-soft)]"
            }`}
          >
            {planLabels[item]}
          </button>
        );
      })}
    </div>
  );
}
