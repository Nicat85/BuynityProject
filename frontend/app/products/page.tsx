"use client";

import api from "@/lib/api-client";
import Link from "next/link";
import React from "react";

type Product = {
  id: string;
  name: string;
  price: number;
  mainImageUrl?: string | null;
};

type Paged<T> = { data: T[]; pageNumber: number; totalRecords: number } | { Data: T[]; PageNumber: number; TotalRecords: number } | any;

export default function ProductsPage() {
  const [items, setItems] = React.useState<Product[]>([]);
  const [loading, setLoading] = React.useState(true);

  React.useEffect(() => {
    const run = async () => {
      try {
        const res = await api.get("/product/search", { params: { page: 1, pageSize: 12 } });
        const base = res.data?.data || res.data?.Data || res.data;
        const paged: Paged<Product> = base;
        const list = paged.data || paged.Data || paged?.data?.data || [];
        setItems(list as Product[]);
      } catch (e) {
        setItems([]);
      } finally {
        setLoading(false);
      }
    };
    run();
  }, []);

  if (loading) return <main className="p-6">Yüklənir...</main>;

  return (
    <main className="mx-auto max-w-6xl p-6">
      <h1 className="text-xl font-semibold mb-4">Məhsullar</h1>
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
        {items.map((p) => (
          <Link key={p.id} href={`/products/${p.id}`} className="border rounded p-3 bg-white hover:shadow">
            <div className="aspect-square bg-gray-100 mb-2 overflow-hidden">
              {p.mainImageUrl ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img src={p.mainImageUrl} alt={p.name} className="w-full h-full object-cover" />
              ) : null}
            </div>
            <div className="text-sm font-medium">{p.name}</div>
            <div className="text-blue-700 font-semibold">{p.price} ₼</div>
          </Link>
        ))}
      </div>
    </main>
  );
}
