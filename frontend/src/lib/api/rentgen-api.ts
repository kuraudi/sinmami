import { AxiosError } from "axios";
import { apiClient, withPlan } from "@/lib/api/client";
import type {
  AppendixFlowResponse,
  AppendixPreviewResponse,
  AppendixDetailsResponse,
  AppendixSummaryResponse,
  AppendixType,
  AskAiResponse,
  AuthResponse,
  CreateDraftResponse,
  CurrentUserResponse,
  DocumentDetailsResponse,
  DocumentListItemResponse,
  DocumentType,
  DocumentTypeCard,
  DraftDetailsResponse,
  DraftStepItemResponse,
  GenerateDocumentResponse,
  GuideResponse,
  NextQuestionResponse,
  PlanHeader,
  SaveAnswerResponse,
  ScenarioDefinition,
  StepValue,
  ValidateDraftResponse,
} from "@/types/api";

function extractFileName(contentDisposition?: string) {
  if (!contentDisposition) {
    return null;
  }

  const utf8Match = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1]);
  }

  const fallbackMatch = contentDisposition.match(/filename="?([^"]+)"?/i);
  return fallbackMatch?.[1] ?? null;
}

function getApiBaseUrl() {
  const baseUrl = apiClient.defaults.baseURL;
  return typeof baseUrl === "string" ? baseUrl.replace(/\/$/, "") : "";
}

export const rentgenApi = {
  async register(payload: { email: string; password: string; fullName?: string }) {
    const response = await apiClient.post<AuthResponse>("/auth/register", payload);
    return response.data;
  },

  async login(payload: { email: string; password: string }) {
    const response = await apiClient.post<AuthResponse>("/auth/login", payload);
    return response.data;
  },

  async getCurrentUser(plan: PlanHeader) {
    const response = await apiClient.get<CurrentUserResponse>("/users/me", withPlan(plan));
    return response.data;
  },

  async getDocumentTypes(plan: PlanHeader) {
    const response = await apiClient.get<DocumentTypeCard[]>("/document-types", withPlan(plan));
    return response.data;
  },

  async getScenario(documentType: DocumentType, plan: PlanHeader) {
    const response = await apiClient.get<ScenarioDefinition>(
      `/document-types/${documentType}/scenario`,
      withPlan(plan),
    );
    return response.data;
  },

  async createDraft(documentType: DocumentType, plan: PlanHeader) {
    const response = await apiClient.post<CreateDraftResponse>(
      "/drafts",
      { documentType },
      withPlan(plan),
    );
    return response.data;
  },

  async getDraft(draftId: string, plan: PlanHeader) {
    const response = await apiClient.get<DraftDetailsResponse>(`/drafts/${draftId}`, withPlan(plan));
    return response.data;
  },

  async getDraftSteps(draftId: string, plan: PlanHeader) {
    const response = await apiClient.get<DraftStepItemResponse[]>(
      `/drafts/${draftId}/steps`,
      withPlan(plan),
    );
    return response.data;
  },

  async getNextQuestion(draftId: string, plan: PlanHeader) {
    try {
      const response = await apiClient.get<NextQuestionResponse>(
        `/drafts/${draftId}/next-question`,
        withPlan(plan),
      );
      return response.data;
    } catch (error) {
      if (error instanceof AxiosError && error.response?.status === 404) {
        return null;
      }

      throw error;
    }
  },

  async saveAnswer(
    draftId: string,
    plan: PlanHeader,
    payload: { stepKey: string; value: string | number | boolean | null },
  ) {
    const response = await apiClient.post<SaveAnswerResponse>(
      `/drafts/${draftId}/answers`,
      payload,
      withPlan(plan),
    );
    return response.data;
  },

  async askAi(draftId: string, plan: PlanHeader, payload: { question: string; stepKey?: string }) {
    const response = await apiClient.post<AskAiResponse>(
      `/drafts/${draftId}/ask`,
      payload,
      withPlan(plan),
    );
    return response.data;
  },

  async validateDraft(draftId: string, plan: PlanHeader) {
    const response = await apiClient.post<ValidateDraftResponse>(
      `/drafts/${draftId}/validate`,
      {},
      withPlan(plan),
    );
    return response.data;
  },

  async generateDocument(draftId: string, plan: PlanHeader) {
    const response = await apiClient.post<GenerateDocumentResponse>(
      `/drafts/${draftId}/generate`,
      {
        includeGuide: true,
        requestedAppendices: [],
      },
      withPlan(plan),
    );
    return response.data;
  },

  async getDocuments(plan: PlanHeader) {
    const response = await apiClient.get<DocumentListItemResponse[]>("/documents", withPlan(plan));
    return response.data;
  },

  async getDocument(documentId: string, plan: PlanHeader) {
    const response = await apiClient.get<DocumentDetailsResponse>(
      `/documents/${documentId}`,
      withPlan(plan),
    );
    return response.data;
  },

  async getGuide(documentId: string, plan: PlanHeader) {
    const response = await apiClient.get<GuideResponse>(
      `/documents/${documentId}/guide`,
      withPlan(plan),
    );
    return response.data;
  },

  async downloadDocumentPdf(documentId: string, plan: PlanHeader) {
    const response = await apiClient.get<Blob>(
      `/documents/${documentId}/pdf`,
      {
        ...withPlan(plan),
        responseType: "blob",
      },
    );

    return {
      blob: response.data,
      fileName:
        extractFileName(response.headers["content-disposition"]) ??
        `rentgen-document-${documentId.slice(0, 8)}.pdf`,
    };
  },

  getDocumentPdfUrl(documentId: string) {
    return `${getApiBaseUrl()}/documents/${documentId}/pdf`;
  },

  getDocumentPdfInlineUrl(documentId: string) {
    return `${getApiBaseUrl()}/documents/${documentId}/pdf/inline`;
  },

  async downloadGuidePdf(documentId: string, plan: PlanHeader) {
    const response = await apiClient.get<Blob>(
      `/documents/${documentId}/guide/pdf`,
      {
        ...withPlan(plan),
        responseType: "blob",
      },
    );

    return {
      blob: response.data,
      fileName:
        extractFileName(response.headers["content-disposition"]) ??
        `rentgen-guide-${documentId.slice(0, 8)}.pdf`,
    };
  },

  async downloadAppendixPdf(appendixId: string, plan: PlanHeader) {
    const response = await apiClient.get<Blob>(
      `/appendices/${appendixId}/pdf`,
      {
        ...withPlan(plan),
        responseType: "blob",
      },
    );

    return {
      blob: response.data,
      fileName:
        extractFileName(response.headers["content-disposition"]) ??
        `rentgen-appendix-${appendixId.slice(0, 8)}.pdf`,
    };
  },

  getAppendixPdfUrl(appendixId: string) {
    return `${getApiBaseUrl()}/appendices/${appendixId}/pdf`;
  },

  getAppendixPdfInlineUrl(appendixId: string) {
    return `${getApiBaseUrl()}/appendices/${appendixId}/pdf/inline`;
  },

  async getAppendices(documentId: string, plan: PlanHeader) {
    const response = await apiClient.get<AppendixSummaryResponse[]>(
      `/documents/${documentId}/appendices`,
      withPlan(plan),
    );
    return response.data;
  },

  async getAppendixFlow(documentId: string, appendixType: AppendixType, plan: PlanHeader) {
    const response = await apiClient.get<AppendixFlowResponse>(
      `/documents/${documentId}/appendices/flow/${appendixType}`,
      withPlan(plan),
    );
    return response.data;
  },

  async previewAppendix(
    documentId: string,
    plan: PlanHeader,
    payload: { appendixType: AppendixType; answers: Record<string, StepValue> },
  ) {
    const response = await apiClient.post<AppendixPreviewResponse>(
      `/documents/${documentId}/appendices/preview`,
      payload,
      withPlan(plan),
    );
    return response.data;
  },

  async askAppendixAi(
    documentId: string,
    plan: PlanHeader,
    payload: {
      appendixType: AppendixType;
      question: string;
      stepKey?: string;
      answers: Record<string, StepValue>;
    },
  ) {
    const response = await apiClient.post<AskAiResponse>(
      `/documents/${documentId}/appendices/ask`,
      payload,
      withPlan(plan),
    );
    return response.data;
  },

  async previewAppendixPdf(
    documentId: string,
    plan: PlanHeader,
    payload: { appendixType: AppendixType; answers: Record<string, StepValue> },
  ) {
    const response = await apiClient.post<Blob>(
      `/documents/${documentId}/appendices/preview/pdf`,
      payload,
      {
        ...withPlan(plan),
        responseType: "blob",
      },
    );

    return {
      blob: response.data,
      fileName:
        extractFileName(response.headers["content-disposition"]) ??
        "rentgen-appendix-preview.pdf",
    };
  },

  async createAppendix(
    documentId: string,
    appendixType: AppendixType,
    plan: PlanHeader,
    answers: Record<string, StepValue> = {},
  ) {
    const response = await apiClient.post<AppendixSummaryResponse>(
      `/documents/${documentId}/appendices`,
      { appendixType, answers },
      withPlan(plan),
    );
    return response.data;
  },

  async getAppendix(appendixId: string, plan: PlanHeader) {
    const response = await apiClient.get<AppendixDetailsResponse>(
      `/appendices/${appendixId}`,
      withPlan(plan),
    );
    return response.data;
  },
};
