import Protected from "@/components/protected";

export default function AccountPage() {
  return (
    <Protected>
      <main className="mx-auto max-w-4xl p-6">
        <h1 className="text-xl font-semibold mb-2">Hesabım</h1>
        <p>Bu səhifə qorunur. Giriş tələb olunur.</p>
      </main>
    </Protected>
  );
}
