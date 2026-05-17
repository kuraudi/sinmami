import { createAsyncThunk, createSlice, type PayloadAction } from "@reduxjs/toolkit";
import { getApiErrorMessage } from "@/lib/api/client";
import { rentgenApi } from "@/lib/api/rentgen-api";
import type { CurrentUserResponse, PlanHeader } from "@/types/api";

export const PLAN_STORAGE_KEY = "rentgen-demo-plan";

type SessionState = {
  plan: PlanHeader;
  hydrated: boolean;
  currentUser: CurrentUserResponse | null;
  status: "idle" | "loading" | "failed";
  error: string | null;
};

const initialState: SessionState = {
  plan: "Free",
  hydrated: false,
  currentUser: null,
  status: "idle",
  error: null,
};

export const fetchCurrentUser = createAsyncThunk(
  "session/fetchCurrentUser",
  async (plan: PlanHeader, thunkApi) => {
    try {
      return await rentgenApi.getCurrentUser(plan);
    } catch (error) {
      return thunkApi.rejectWithValue(getApiErrorMessage(error));
    }
  },
);

const sessionSlice = createSlice({
  name: "session",
  initialState,
  reducers: {
    hydratePlan(state, action: PayloadAction<PlanHeader>) {
      state.plan = action.payload;
      state.hydrated = true;
    },
    setPlan(state, action: PayloadAction<PlanHeader>) {
      state.plan = action.payload;
    },
  },
  extraReducers(builder) {
    builder
      .addCase(fetchCurrentUser.pending, (state) => {
        state.status = "loading";
        state.error = null;
      })
      .addCase(fetchCurrentUser.fulfilled, (state, action) => {
        state.status = "idle";
        state.currentUser = action.payload;
      })
      .addCase(fetchCurrentUser.rejected, (state, action) => {
        state.status = "failed";
        state.error =
          typeof action.payload === "string"
            ? action.payload
            : action.error.message ?? "Не удалось получить данные пользователя.";
      });
  },
});

export const { hydratePlan, setPlan } = sessionSlice.actions;
export default sessionSlice.reducer;
