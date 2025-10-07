"use client";

import { useAuthStore, hydrateAuthFromStorage } from "@/store/auth-store";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

export default function Protected({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const accessToken = useAuthStore((s) => s.accessToken);

  useEffect(() => {
    hydrateAuthFromStorage();
  }, []);

  useEffect(() => {
    if (!accessToken) {
      router.replace("/auth/login");
    }
  }, [accessToken, router]);

  if (!accessToken) return null;
  return <>{children}</>;
}
