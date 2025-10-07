"use client";

import api from "@/lib/api-client";
import { useEffect, useState } from "react";

export default function ProductDetail({ params }: { params: { id: string } }) {
  const { id } = params;
  const [product, setProduct] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const run = async () => {
      try {
        const res = await api.get(`/product/${id}`);
        const base = res.data?.data || res.data?.Data || res.data;
        setProduct(base);
      } catch {
        setProduct(null);
      } finally {
        setLoading(false);
      }
    };
    run();
  }, [id]);

  if (loading) return <main className="p-6">Yüklənir...</main>;
  if (!product) return <main className="p-6">Məhsul tapılmadı</main>;

  return (
    <main className="mx-auto max-w-4xl p-6">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="aspect-square bg-gray-100 overflow-hidden">
          {product.mainImageUrl ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={product.mainImageUrl} alt={product.name} className="w-full h-full object-cover" />
          ) : null}
        </div>
        <div>
          <h1 className="text-2xl font-semibold mb-2">{product.name}</h1>
          <div className="text-blue-700 text-xl font-bold mb-4">{product.price} ₼</div>
          <p className="text-sm text-gray-700 whitespace-pre-wrap">{product.description}</p>
        </div>
      </div>
    </main>
  );
}
