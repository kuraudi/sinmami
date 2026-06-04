"use client";

import { useEffect, useState } from "react";
import { Provider } from "react-redux";
import { makeStore, type AppStore } from "@/store";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import {
  fetchCurrentUser,
  hydratePlan,
  PLAN_STORAGE_KEY,
} from "@/store/slices/session-slice";
import { hydrateAuth } from "@/store/slices/auth-slice";
import type { PlanHeader } from "@/types/api";

function SessionBootstrap() {
  const dispatch = useAppDispatch();
  const plan = useAppSelector((state) => state.session.plan);
  const hydrated = useAppSelector((state) => state.session.hydrated);

  useEffect(() => {
    dispatch(hydrateAuth());

    const storedPlan = window.localStorage.getItem(PLAN_STORAGE_KEY);
    const planFromStorage: PlanHeader =
      storedPlan === "Premium" ? "Premium" : "Free";

    dispatch(hydratePlan(planFromStorage));
  }, [dispatch]);

  useEffect(() => {
    if (!hydrated) return;
    window.localStorage.setItem(PLAN_STORAGE_KEY, plan);
    void dispatch(fetchCurrentUser(plan));
  }, [dispatch, hydrated, plan]);

  return null;
}

export function StoreProvider({ children }: { children: React.ReactNode }) {
  const [store] = useState<AppStore>(() => makeStore());

  return (
    <Provider store={store}>
      <SessionBootstrap />
      {children}
    </Provider>
  );
}
