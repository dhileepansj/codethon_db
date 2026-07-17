import { createSlice, createAsyncThunk } from "@reduxjs/toolkit";
import { authService } from "@/services/authService";
import type { LoginRequest, LoginResponse } from "@/types";

interface AuthState {
  isAuthenticated: boolean;
  user: LoginResponse | null;
  isLoading: boolean;
  error: string | null;
}

const storedUser = sessionStorage.getItem("user");

const initialState: AuthState = {
  isAuthenticated: !!sessionStorage.getItem("token"),
  user: storedUser ? JSON.parse(storedUser) : null,
  isLoading: false,
  error: null,
};

export const login = createAsyncThunk<LoginResponse, LoginRequest>(
  "auth/login",
  async (credentials, { rejectWithValue }) => {
    try {
      const data = await authService.login(credentials);
      sessionStorage.setItem("token", data.token);
      sessionStorage.setItem("user", JSON.stringify(data));
      return data;
    } catch (err: any) {
      return rejectWithValue(err.response?.data?.message || "Login failed");
    }
  }
);

const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    logout(state) {
      state.isAuthenticated = false;
      state.user = null;
      sessionStorage.removeItem("token");
      sessionStorage.removeItem("user");
    },
    clearError(state) {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(login.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(login.fulfilled, (state, action) => {
        state.isLoading = false;
        state.isAuthenticated = true;
        state.user = action.payload;
      })
      .addCase(login.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.payload as string;
      });
  },
});

export const { logout, clearError } = authSlice.actions;
export default authSlice.reducer;
