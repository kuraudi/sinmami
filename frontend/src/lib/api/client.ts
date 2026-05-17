import axios from "axios";
import type { ApiErrorResponse, PlanHeader } from "@/types/api";

export const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://127.0.0.1:5099/api",
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 45000,
});

export function withPlan(plan: PlanHeader) {
  return {
    headers: {
      "X-Plan": plan,
    },
  };
}

export function getApiErrorMessage(error: unknown) {
  if (axios.isAxiosError<ApiErrorResponse>(error)) {
    return (
      error.response?.data?.errors?.[0]?.message ??
      error.response?.data?.message ??
      error.response?.data?.details ??
      error.message
    );
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Не удалось выполнить запрос.";
}

export function getApiErrorPayload(error: unknown) {
  if (axios.isAxiosError<ApiErrorResponse>(error)) {
    return error.response?.data;
  }

  return undefined;
}
