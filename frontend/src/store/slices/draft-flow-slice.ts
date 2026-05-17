import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { getApiErrorMessage } from "@/lib/api/client";
import { rentgenApi } from "@/lib/api/rentgen-api";
import type {
  AskAiResponse,
  DraftDetailsResponse,
  DraftStepItemResponse,
  NextQuestionResponse,
  PlanHeader,
  ValidateDraftResponse,
} from "@/types/api";

type DraftBundle = {
  draft: DraftDetailsResponse;
  steps: DraftStepItemResponse[];
  nextQuestion: NextQuestionResponse | null;
};

type DraftFlowState = {
  draft: DraftDetailsResponse | null;
  steps: DraftStepItemResponse[];
  currentQuestion: NextQuestionResponse | null;
  validation: ValidateDraftResponse | null;
  aiHelp: AskAiResponse | null;
  isLoading: boolean;
  isSaving: boolean;
  isValidating: boolean;
  isGenerating: boolean;
  isAsking: boolean;
  error: string | null;
  lastGeneratedDocumentId: string | null;
};

const initialState: DraftFlowState = {
  draft: null,
  steps: [],
  currentQuestion: null,
  validation: null,
  aiHelp: null,
  isLoading: false,
  isSaving: false,
  isValidating: false,
  isGenerating: false,
  isAsking: false,
  error: null,
  lastGeneratedDocumentId: null,
};

function getPlan(getState: () => unknown) {
  return (getState() as { session: { plan: PlanHeader } }).session.plan;
}

async function loadBundle(draftId: string, plan: PlanHeader): Promise<DraftBundle> {
  const [draft, steps, nextQuestion] = await Promise.all([
    rentgenApi.getDraft(draftId, plan),
    rentgenApi.getDraftSteps(draftId, plan),
    rentgenApi.getNextQuestion(draftId, plan),
  ]);

  return { draft, steps, nextQuestion };
}

export const bootstrapDraftFlow = createAsyncThunk(
  "draftFlow/bootstrap",
  async (draftId: string, thunkApi) => {
    try {
      const plan = getPlan(thunkApi.getState);
      return await loadBundle(draftId, plan);
    } catch (error) {
      return thunkApi.rejectWithValue(getApiErrorMessage(error));
    }
  },
);

export const saveDraftAnswer = createAsyncThunk(
  "draftFlow/saveAnswer",
  async (
    payload: { draftId: string; stepKey: string; value: string | number | boolean | null },
    thunkApi,
  ) => {
    try {
      const plan = getPlan(thunkApi.getState);
      await rentgenApi.saveAnswer(payload.draftId, plan, {
        stepKey: payload.stepKey,
        value: payload.value,
      });
      return await loadBundle(payload.draftId, plan);
    } catch (error) {
      return thunkApi.rejectWithValue(getApiErrorMessage(error));
    }
  },
);

export const askDraftHelp = createAsyncThunk(
  "draftFlow/askHelp",
  async (payload: { draftId: string; question: string; stepKey?: string }, thunkApi) => {
    try {
      const plan = getPlan(thunkApi.getState);
      return await rentgenApi.askAi(payload.draftId, plan, payload);
    } catch (error) {
      return thunkApi.rejectWithValue(getApiErrorMessage(error));
    }
  },
);

export const validateDraftFlow = createAsyncThunk(
  "draftFlow/validate",
  async (draftId: string, thunkApi) => {
    try {
      const plan = getPlan(thunkApi.getState);
      return await rentgenApi.validateDraft(draftId, plan);
    } catch (error) {
      return thunkApi.rejectWithValue(getApiErrorMessage(error));
    }
  },
);

export const generateDraftDocument = createAsyncThunk(
  "draftFlow/generate",
  async (draftId: string, thunkApi) => {
    try {
      const plan = getPlan(thunkApi.getState);
      return await rentgenApi.generateDocument(draftId, plan);
    } catch (error) {
      return thunkApi.rejectWithValue(getApiErrorMessage(error));
    }
  },
);

const draftFlowSlice = createSlice({
  name: "draftFlow",
  initialState,
  reducers: {
    clearDraftFlow(state) {
      state.draft = null;
      state.steps = [];
      state.currentQuestion = null;
      state.validation = null;
      state.aiHelp = null;
      state.error = null;
      state.lastGeneratedDocumentId = null;
    },
  },
  extraReducers(builder) {
    builder
      .addCase(bootstrapDraftFlow.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(bootstrapDraftFlow.fulfilled, (state, action) => {
        state.isLoading = false;
        state.draft = action.payload.draft;
        state.steps = action.payload.steps;
        state.currentQuestion = action.payload.nextQuestion;
        state.validation = null;
        state.aiHelp = null;
      })
      .addCase(bootstrapDraftFlow.rejected, (state, action) => {
        state.isLoading = false;
        state.error =
          typeof action.payload === "string"
            ? action.payload
            : action.error.message ?? "Не удалось загрузить черновик.";
      })
      .addCase(saveDraftAnswer.pending, (state) => {
        state.isSaving = true;
        state.error = null;
      })
      .addCase(saveDraftAnswer.fulfilled, (state, action) => {
        state.isSaving = false;
        state.draft = action.payload.draft;
        state.steps = action.payload.steps;
        state.currentQuestion = action.payload.nextQuestion;
        state.validation = null;
      })
      .addCase(saveDraftAnswer.rejected, (state, action) => {
        state.isSaving = false;
        state.error =
          typeof action.payload === "string"
            ? action.payload
            : action.error.message ?? "Не удалось сохранить ответ.";
      })
      .addCase(askDraftHelp.pending, (state) => {
        state.isAsking = true;
        state.error = null;
        state.aiHelp = null;
      })
      .addCase(askDraftHelp.fulfilled, (state, action) => {
        state.isAsking = false;
        state.aiHelp = action.payload;
      })
      .addCase(askDraftHelp.rejected, (state, action) => {
        state.isAsking = false;
        state.error =
          typeof action.payload === "string"
            ? action.payload
            : action.error.message ?? "Не удалось получить ответ ИИ.";
      })
      .addCase(validateDraftFlow.pending, (state) => {
        state.isValidating = true;
        state.error = null;
      })
      .addCase(validateDraftFlow.fulfilled, (state, action) => {
        state.isValidating = false;
        state.validation = action.payload;
        if (state.draft) {
          state.draft.status = action.payload.status;
          state.draft.completionPercent = action.payload.completionPercent;
        }
      })
      .addCase(validateDraftFlow.rejected, (state, action) => {
        state.isValidating = false;
        state.error =
          typeof action.payload === "string"
            ? action.payload
            : action.error.message ?? "Не удалось выполнить проверку черновика.";
      })
      .addCase(generateDraftDocument.pending, (state) => {
        state.isGenerating = true;
        state.error = null;
      })
      .addCase(generateDraftDocument.fulfilled, (state, action) => {
        state.isGenerating = false;
        state.lastGeneratedDocumentId = action.payload.documentId;
      })
      .addCase(generateDraftDocument.rejected, (state, action) => {
        state.isGenerating = false;
        state.error =
          typeof action.payload === "string"
            ? action.payload
            : action.error.message ?? "Не удалось сформировать предпросмотр документа.";
      });
  },
});

export const { clearDraftFlow } = draftFlowSlice.actions;
export default draftFlowSlice.reducer;
