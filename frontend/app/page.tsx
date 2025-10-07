import Link from "next/link";

export default function HomePage() {
  return (
    <main className="mx-auto max-w-5xl p-6 space-y-6">
      <h1 className="text-2xl font-semibold">Buynity</h1>
      <div className="flex items-center gap-4">
        <Link href="/auth/login" className="text-blue-600 hover:underline">Daxil ol</Link>
        <Link href="/auth/register" className="text-blue-600 hover:underline">Qeydiyyat</Link>
        <Link href="/products" className="text-blue-600 hover:underline">Məhsullar</Link>
      </div>
    </main>
  );
}
