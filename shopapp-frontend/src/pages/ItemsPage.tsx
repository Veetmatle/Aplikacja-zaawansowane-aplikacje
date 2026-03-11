import { Link, useSearchParams } from 'react-router-dom';
import { Package, SlidersHorizontal, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { Pagination } from '@/components/Pagination';
import { ItemCard } from '@/components/items/ItemCard';
import { FiltersPanel } from '@/components/items/FiltersPanel';
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
    page, pageSize: 12,
    search: ds || undefined,
    categoryId,
    minPrice: minP ? Number(minP) : undefined,
    maxPrice: maxP ? Number(maxP) : undefined,
  });
  const { data: cats } = useCategories();
  const addToCart = useAddToCart();

  const activeCat = cats?.find((c) => c.id === categoryId);

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
    <div className="container mx-auto px-4 py-6">
      {/* Breadcrumb */}
      <nav className="flex items-center gap-1.5 text-sm text-muted-foreground mb-4">
        <Link to="/" className="hover:text-foreground">Strona główna</Link>
        <ChevronRight className="h-3.5 w-3.5" />
        {activeCat ? (
          <>
            <Link to="/items" className="hover:text-foreground">Ogłoszenia</Link>
            <ChevronRight className="h-3.5 w-3.5" />
            <span className="text-foreground">{activeCat.name}</span>
          </>
        ) : (
          <span className="text-foreground">Ogłoszenia</span>
        )}
      </nav>

      <div className="flex gap-8">
        {/* Sidebar filters — desktop */}
        <aside className="hidden lg:block w-64 flex-shrink-0">
          <FiltersPanel
            search={search}
            onSearchChange={(v) => { setSearch(v); up('search', v || undefined); }}
            categories={cats}
            activeCategoryId={categoryId}
            onCategoryChange={(id) => up('categoryId', id)}
            minPrice={minP}
            maxPrice={maxP}
            onMinPriceChange={(v) => { setMinP(v); up('minPrice', v || undefined); }}
            onMaxPriceChange={(v) => { setMaxP(v); up('maxPrice', v || undefined); }}
            onClear={clearAll}
          />
        </aside>

        {/* Main content */}
        <div className="flex-1 min-w-0">
          {/* Top bar */}
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-3">
              <h1 className="text-xl font-bold">
                {activeCat ? activeCat.name : 'Wszystkie ogłoszenia'}
              </h1>
              {data && (
                <span className="text-sm text-muted-foreground">
                  ({data.totalCount} {data.totalCount === 1 ? 'ogłoszenie' : 'ogłoszeń'})
                </span>
              )}
            </div>
            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" className="lg:hidden gap-1" onClick={() => setShowFilters(!showFilters)}>
                <SlidersHorizontal className="h-4 w-4" />
                Filtry
              </Button>
              <Select defaultValue="newest">
                <SelectTrigger className="w-44 h-9 text-sm">
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

          {/* Mobile filters */}
          {showFilters && (
            <div className="lg:hidden mb-4 p-4 bg-card rounded-xl border border-border">
              <FiltersPanel
                search={search}
                onSearchChange={(v) => { setSearch(v); up('search', v || undefined); }}
                categories={cats}
                activeCategoryId={categoryId}
                onCategoryChange={(id) => up('categoryId', id)}
                minPrice={minP}
                maxPrice={maxP}
                onMinPriceChange={(v) => { setMinP(v); up('minPrice', v || undefined); }}
                onMaxPriceChange={(v) => { setMaxP(v); up('maxPrice', v || undefined); }}
                onClear={clearAll}
              />
            </div>
          )}

          {/* Items list (OLX-style vertical) */}
          <div className="space-y-3">
            {isLoading
              ? Array.from({ length: 6 }).map((_, i) => (
                  <Skeleton key={i} className="h-32 rounded-xl" />
                ))
              : data?.items.map((item) => (
                  <ItemCard key={item.id} item={item} variant="list" onAddToCart={handleAddToCart} />
                ))}
          </div>

          {/* Empty state */}
          {data && data.items.length === 0 && (
            <div className="text-center py-16">
              <Package className="h-16 w-16 text-muted-foreground/30 mx-auto mb-4" />
              <p className="text-lg font-medium mb-2">Brak ogłoszeń</p>
              <p className="text-sm text-muted-foreground">Zmień filtry lub spróbuj innej frazy.</p>
            </div>
          )}

          {/* Pagination */}
          {data && data.totalPages > 1 && (
            <div className="mt-6">
              <Pagination page={page} totalPages={data.totalPages} onPageChange={goPage} />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
