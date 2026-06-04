import { configureStore } from "@reduxjs/toolkit";
import draftFlowReducer from "@/store/slices/draft-flow-slice";
import sessionReducer from "@/store/slices/session-slice";
import authReducer from "@/store/slices/auth-slice";

export function makeStore() {
  return configureStore({
    reducer: {
      session: sessionReducer,
      draftFlow: draftFlowReducer,
      auth: authReducer,
    },
  });
}

export type AppStore = ReturnType<typeof makeStore>;
export type RootState = ReturnType<AppStore["getState"]>;
export type AppDispatch = AppStore["dispatch"];
