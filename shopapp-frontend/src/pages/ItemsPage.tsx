import { Link, useSearchParams } from 'react-router-dom';
import { Package, SlidersHorizontal, ChevronRight, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { Pagination } from '@/components/Pagination';
import { ItemCard } from '@/components/items/ItemCard';
import { useItems } from '@/hooks/useItems';
import { useCategories } from '@/hooks/useCategories';
import { useAddToCart } from '@/hooks/useCart';
import { useDebounce } from '@/hooks/useDebounce';
import { useState } from 'react';

export default function ItemsPage() {
  const [sp, setSp] = useSearchParams();
  const page = Number(sp.get('page') || '1');
  const categoryId = sp.get('categoryId') || undefined;
  const [search, setSearch] = useState(sp.get('search') || '');
  const [minP, setMinP] = useState(sp.get('minPrice') || '');
  const [maxP, setMaxP] = useState(sp.get('maxPrice') || '');
  const [showFilters, setShowFilters] = useState(false);
  const ds = useDebounce(search, 400);

  const { data, isLoading } = useItems({
    page, pageSize: 15,
    search: ds || undefined,
    categoryId,
    minPrice: minP ? Number(minP) : undefined,
    maxPrice: maxP ? Number(maxP) : undefined,
  });
  const { data: cats } = useCategories();
  const addToCart = useAddToCart();
  const activeCat = cats?.find((c) => c.id === categoryId);
  const totalPages = data ? Math.ceil(data.totalCount / 15) : 0;

  const up = (k: string, v: string | undefined) => {
    const p = new URLSearchParams(sp);
    if (v) p.set(k, v); else p.delete(k);
    p.set('page', '1');
    setSp(p);
  };
  const goPage = (pg: number) => {
    const p = new URLSearchParams(sp);
    p.set('page', pg.toString());
    setSp(p);
  };
  const clearAll = () => { setSp({}); setSearch(''); setMinP(''); setMaxP(''); };
  const handleAddToCart = (itemId: string) => addToCart.mutate({ itemId, quantity: 1 });

  return (
    <div className="bg-[#F5F5F5] min-h-screen">
      <div className="container mx-auto px-4 py-6">

        {/* Breadcrumb */}
        <nav className="flex items-center gap-1 text-sm text-gray-500 mb-4">
          <Link to="/" className="hover:text-primary">Strona główna</Link>
          <ChevronRight className="h-3.5 w-3.5" />
          <span className="text-gray-900 font-medium">{activeCat?.name ?? 'Wszystkie ogłoszenia'}</span>
        </nav>

        <div className="flex gap-6">

          {/* Sidebar — filtry */}
          <aside className="hidden lg:block w-56 flex-shrink-0">
            <div className="bg-white rounded-xl border border-gray-200 p-4 sticky top-24">
              <h3 className="text-sm font-bold text-gray-900 mb-3">Filtry</h3>

              <div className="space-y-4">
                {/* Szukaj */}
                <div>
                  <label className="text-xs font-medium text-gray-500 uppercase tracking-wide block mb-1.5">Fraza</label>
                  <Input
                    placeholder="Szukaj..."
                    value={search}
                    onChange={(e) => { setSearch(e.target.value); up('search', e.target.value || undefined); }}
                    className="h-8 text-sm"
                  />
                </div>

                {/* Kategorie */}
                <div>
                  <label className="text-xs font-medium text-gray-500 uppercase tracking-wide block mb-1.5">Kategoria</label>
                  <div className="space-y-0.5">
                    <button
                      onClick={() => up('categoryId', undefined)}
                      className={'block w-full text-left px-2 py-1.5 text-sm rounded-lg transition-colors ' +
                        (!categoryId ? 'bg-primary text-white font-medium' : 'text-gray-700 hover:bg-gray-100')}
                    >
                      Wszystkie
                    </button>
                    {cats?.map((c) => (
                      <button
                        key={c.id}
                        onClick={() => up('categoryId', categoryId === c.id ? undefined : c.id)}
                        className={'block w-full text-left px-2 py-1.5 text-sm rounded-lg transition-colors ' +
                          (categoryId === c.id ? 'bg-primary text-white font-medium' : 'text-gray-700 hover:bg-gray-100')}
                      >
                        {c.name}
                      </button>
                    ))}
                  </div>
                </div>

                {/* Zakres cen */}
                <div>
                  <label className="text-xs font-medium text-gray-500 uppercase tracking-wide block mb-1.5">Cena (PLN)</label>
                  <div className="flex gap-2 items-center">
                    <Input
                      type="number"
                      placeholder="Od"
                      value={minP}
                      onChange={(e) => { setMinP(e.target.value); up('minPrice', e.target.value || undefined); }}
                      className="h-8 text-sm"
                    />
                    <span className="text-gray-400 text-xs">–</span>
                    <Input
                      type="number"
                      placeholder="Do"
                      value={maxP}
                      onChange={(e) => { setMaxP(e.target.value); up('maxPrice', e.target.value || undefined); }}
                      className="h-8 text-sm"
                    />
                  </div>
                </div>

                <button
                  onClick={clearAll}
                  className="w-full text-xs text-gray-500 hover:text-red-500 flex items-center gap-1 justify-center py-1 transition-colors"
                >
                  <X className="h-3.5 w-3.5" />Wyczyść filtry
                </button>
              </div>
            </div>
          </aside>

          {/* Główna kolumna */}
          <div className="flex-1 min-w-0">

            {/* Nagłówek z licznikiem i sortowaniem */}
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <h1 className="text-lg font-bold text-gray-900">
                  {activeCat?.name ?? 'Wszystkie ogłoszenia'}
                </h1>
                {data && (
                  <span className="text-sm text-gray-500">({data.totalCount})</span>
                )}
              </div>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  className="lg:hidden gap-1 text-xs h-8"
                  onClick={() => setShowFilters(!showFilters)}
                >
                  <SlidersHorizontal className="h-3.5 w-3.5" />Filtry
                </Button>
                <Select defaultValue="newest">
                  <SelectTrigger className="w-40 h-8 text-xs">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="newest">Najnowsze</SelectItem>
                    <SelectItem value="cheapest">Najtańsze</SelectItem>
                    <SelectItem value="expensive">Najdroższe</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            {/* Mobile filtry */}
            {showFilters && (
              <div className="lg:hidden mb-4 bg-white rounded-xl border border-gray-200 p-4">
                <div className="flex gap-2 mb-3">
                  <Input placeholder="Szukaj..." value={search} onChange={(e) => { setSearch(e.target.value); up('search', e.target.value || undefined); }} className="h-8 text-sm" />
                </div>
                <div className="flex gap-2">
                  <Input type="number" placeholder="Cena od" value={minP} onChange={(e) => { setMinP(e.target.value); up('minPrice', e.target.value || undefined); }} className="h-8 text-sm" />
                  <Input type="number" placeholder="Cena do" value={maxP} onChange={(e) => { setMaxP(e.target.value); up('maxPrice', e.target.value || undefined); }} className="h-8 text-sm" />
                </div>
                <button onClick={clearAll} className="mt-2 text-xs text-gray-500 hover:text-red-500 flex items-center gap-1">
                  <X className="h-3 w-3" />Wyczyść
                </button>
              </div>
            )}

            {/* Lista ogłoszeń */}
            <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
              {isLoading ? (
                <div className="divide-y divide-gray-100">
                  {Array.from({ length: 6 }).map((_, i) => (
                    <div key={i} className="flex gap-4 p-4">
                      <Skeleton className="w-32 h-24 rounded-lg flex-shrink-0" />
                      <div className="flex-1 space-y-2 py-1">
                        <Skeleton className="h-4 w-2/3" />
                        <Skeleton className="h-3 w-1/3" />
                        <Skeleton className="h-4 w-1/4" />
                      </div>
                    </div>
                  ))}
                </div>
              ) : data?.items.length === 0 ? (
                <div className="text-center py-16">
                  <Package className="h-14 w-14 text-gray-200 mx-auto mb-4" />
                  <p className="text-base font-semibold text-gray-700 mb-1">Brak ogłoszeń</p>
                  <p className="text-sm text-gray-400">Zmień filtry lub spróbuj innej frazy.</p>
                </div>
              ) : (
                <div className="divide-y divide-gray-100">
                  {data?.items.map((item) => (
                    <ItemCard key={item.id} item={item} variant="list" onAddToCart={handleAddToCart} />
                  ))}
                </div>
              )}
            </div>

            {/* Paginacja */}
            {totalPages > 1 && (
              <div className="mt-6 flex justify-center">
                <Pagination page={page} totalPages={totalPages} onPageChange={goPage} />
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}