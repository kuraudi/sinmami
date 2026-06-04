import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { rentgenApi } from "@/lib/api/rentgen-api";

export const AUTH_TOKEN_KEY = "rentgen_token";
export const AUTH_USER_KEY = "rentgen_user";

export type AuthUser = {
  userId: string;
  email: string;
  fullName?: string | null;
  plan: string;
};

type AuthState = {
  user: AuthUser | null;
  token: string | null;
  isAuthenticated: boolean;
  status: "idle" | "loading" | "failed";
  error: string | null;
};

const initialState: AuthState = {
  user: null,
  token: null,
  isAuthenticated: false,
  status: "idle",
  error: null,
};

export const loginThunk = createAsyncThunk(
  "auth/login",
  async ({ email, password }: { email: string; password: string }, thunkApi) => {
    try {
      return await rentgenApi.login(email, password);
    } catch (e) {
      return thunkApi.rejectWithValue(e instanceof Error ? e.message : "Ошибка входа.");
    }
  },
);

export const registerThunk = createAsyncThunk(
  "auth/register",
  async ({ email, password, fullName }: { email: string; password: string; fullName?: string }, thunkApi) => {
    try {
      return await rentgenApi.register(email, password, fullName);
    } catch (e) {
      return thunkApi.rejectWithValue(e instanceof Error ? e.message : "Ошибка регистрации.");
    }
  },
);

const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    hydrateAuth(state) {
      if (typeof window === "undefined") return;
      const token = localStorage.getItem(AUTH_TOKEN_KEY);
      const userRaw = localStorage.getItem(AUTH_USER_KEY);
      if (token && userRaw) {
        try {
          state.user = JSON.parse(userRaw);
          state.token = token;
          state.isAuthenticated = true;
        } catch {
          // ignore
        }
      }
    },
    logout(state) {
      state.user = null;
      state.token = null;
      state.isAuthenticated = false;
      if (typeof window !== "undefined") {
        localStorage.removeItem(AUTH_TOKEN_KEY);
        localStorage.removeItem(AUTH_USER_KEY);
      }
    },
  },
  extraReducers(builder) {
    function onAuthSuccess(state: AuthState, payload: { token: string; userId: string; email: string; fullName?: string | null; plan: string }) {
      state.status = "idle";
      state.error = null;
      state.token = payload.token;
      state.isAuthenticated = true;
      state.user = { userId: payload.userId, email: payload.email, fullName: payload.fullName, plan: payload.plan };
      if (typeof window !== "undefined") {
        localStorage.setItem(AUTH_TOKEN_KEY, payload.token);
        localStorage.setItem(AUTH_USER_KEY, JSON.stringify(state.user));
      }
    }

    builder
      .addCase(loginThunk.pending, (state) => { state.status = "loading"; state.error = null; })
      .addCase(loginThunk.fulfilled, (state, action) => onAuthSuccess(state, action.payload))
      .addCase(loginThunk.rejected, (state, action) => {
        state.status = "failed";
        state.error = typeof action.payload === "string" ? action.payload : "Ошибка входа.";
      })
      .addCase(registerThunk.pending, (state) => { state.status = "loading"; state.error = null; })
      .addCase(registerThunk.fulfilled, (state, action) => onAuthSuccess(state, action.payload))
      .addCase(registerThunk.rejected, (state, action) => {
        state.status = "failed";
        state.error = typeof action.payload === "string" ? action.payload : "Ошибка регистрации.";
      });
  },
});

export const { hydrateAuth, logout } = authSlice.actions;
export default authSlice.reducer;
