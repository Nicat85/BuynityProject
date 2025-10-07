"use client";

import api from "@/lib/api-client";
import { useAuthStore } from "@/store/auth-store";
import Link from "next/link";
import { useRouter } from "next/navigation";
import React from "react";

export default function LoginPage() {
  const router = useRouter();
  const setAuth = useAuthStore((s) => s.setAuth);
  const [emailOrPhone, setEmailOrPhone] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [error, setError] = React.useState<string | null>(null);
  const [loading, setLoading] = React.useState(false);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    try {
      const res = await api.post("/auth/login", {
        emailOrPhoneNumber: emailOrPhone,
        password,
      });
      const data = res.data?.data || res.data?.Data || res.data;
      const accessToken = data.accessToken || data.AccessToken;
      const refreshToken = data.refreshToken || data.RefreshToken;
      if (!accessToken || !refreshToken) throw new Error("Token tapılmadı");
      setAuth({ accessToken, refreshToken });
      router.push("/products");
    } catch (e: any) {
      setError(e?.response?.data?.message || e.message || "Xəta baş verdi");
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="mx-auto max-w-md p-6">
      <h1 className="text-xl font-semibold mb-6">Daxil ol</h1>
      <form onSubmit={onSubmit} className="space-y-4">
        <input
          type="text"
          placeholder="Email və ya nömrə"
          className="w-full border rounded px-3 py-2"
          value={emailOrPhone}
          onChange={(e) => setEmailOrPhone(e.target.value)}
          required
        />
        <input
          type="password"
          placeholder="Şifrə"
          className="w-full border rounded px-3 py-2"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
        />
        {error && <p className="text-red-600 text-sm">{error}</p>}
        <button
          type="submit"
          className="w-full bg-blue-600 text-white rounded px-3 py-2 disabled:opacity-50"
          disabled={loading}
        >
          {loading ? "Gözləyin..." : "Daxil ol"}
        </button>
      </form>
      <p className="mt-4 text-sm">
        Hesabın yoxdur? <Link href="/auth/register" className="text-blue-600">Qeydiyyat</Link>
      </p>
    </main>
  );
}
