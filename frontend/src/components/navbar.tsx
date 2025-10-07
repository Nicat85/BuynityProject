"use client";

import Link from "next/link";
import { useAuthStore, hydrateAuthFromStorage } from "@/store/auth-store";
import { useEffect } from "react";
import api from "@/lib/api-client";
import { useRouter } from "next/navigation";

export default function Navbar() {
  const router = useRouter();
  const accessToken = useAuthStore((s) => s.accessToken);
  const refreshToken = useAuthStore((s) => s.refreshToken);
  const clear = useAuthStore((s) => s.clear);

  useEffect(() => {
    hydrateAuthFromStorage();
  }, []);

  const onLogout = async () => {
    try {
      if (refreshToken) {
        await api.post("/auth/logout", { refreshToken });
      }
    } catch {}
    clear();
    router.push("/");
  };

  return (
    <header className="bg-white border-b">
      <div className="mx-auto max-w-6xl p-4 flex items-center justify-between">
        <Link href="/" className="font-semibold">Buynity</Link>
        <nav className="flex items-center gap-4 text-sm">
          <Link href="/products">Məhsullar</Link>
          {!accessToken ? (
            <>
              <Link href="/auth/login">Daxil ol</Link>
              <Link href="/auth/register">Qeydiyyat</Link>
            </>
          ) : (
            <button onClick={onLogout} className="text-red-600">Çıxış</button>
          )}
        </nav>
      </div>
    </header>
  );
}
