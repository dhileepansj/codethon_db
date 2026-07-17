import httpClient from "./httpClient";
import type { LoginRequest, LoginResponse } from "@/types";

export const authService = {
  login: async (data: LoginRequest) => {
    const res = await httpClient.post<{ data: LoginResponse }>("/api/auth/login", data);
    return res.data.data;
  },

  whoami: async () => {
    const res = await httpClient.get("/api/auth/whoami");
    return res.data.data;
  },

  changePassword: async (newPassword: string) => {
    const res = await httpClient.post("/api/auth/change-password", { newPassword });
    return res.data;
  },
};
