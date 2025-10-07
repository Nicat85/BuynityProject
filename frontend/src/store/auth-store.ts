"use client";

import { create } from "zustand";
import { setTokens } from "@/lib/api-client";

type AuthState = {
  accessToken: string | null;
  refreshToken: string | null;
  setAuth: (t: { accessToken: string; refreshToken: string }) => void;
  clear: () => void;
};

export const useAuthStore = create<AuthState>((set) => ({
  accessToken: null,
  refreshToken: null,
  setAuth: ({ accessToken, refreshToken }) => {
    set({ accessToken, refreshToken });
    setTokens({ accessToken, refreshToken });
    if (typeof window !== "undefined") {
      localStorage.setItem("buynity_auth", JSON.stringify({ accessToken, refreshToken }));
    }
  },
  clear: () => {
    set({ accessToken: null, refreshToken: null });
    setTokens(null);
    if (typeof window !== "undefined") {
      localStorage.removeItem("buynity_auth");
    }
  },
}));

export function hydrateAuthFromStorage() {
  if (typeof window === "undefined") return;
  const raw = localStorage.getItem("buynity_auth");
  if (!raw) return;
  try {
    const parsed = JSON.parse(raw);
    if (parsed?.accessToken && parsed?.refreshToken) {
      useAuthStore.getState().setAuth(parsed);
    }
  } catch {}
}
