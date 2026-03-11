import { Link, useSearchParams } from 'react-router-dom';
import { Package, X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Pagination } from '@/components/Pagination';
import { useItems } from '@/hooks/useItems';
import { useCategories } from '@/hooks/useCategories';
import { formatPrice } from '@/lib/utils';
import { useDebounce } from '@/hooks/useDebounce';
import { ItemCondition } from '@/types/api';
import { useState } from 'react';

export default function ItemsPage() {
  const [sp, setSp] = useSearchParams();
  const page = Number(sp.get('page') || '1');
  const categoryId = sp.get('categoryId') || undefined;
  const [search, setSearch] = useState(sp.get('search') || '');
  const [minP, setMinP] = useState(sp.get('minPrice') || '');
  const [maxP, setMaxP] = useState(sp.get('maxPrice') || '');
  const ds = useDebounce(search, 400);
  const { data, isLoading } = useItems({ page, pageSize: 12, search: ds || undefined, categoryId, minPrice: minP ? Number(minP) : undefined, maxPrice: maxP ? Number(maxP) : undefined });
  const { data: cats } = useCategories();
  const up = (k: string, v: string | undefined) => { const p = new URLSearchParams(sp); if (v) p.set(k, v); else p.delete(k); p.set('page', '1'); setSp(p); };
  const goPage = (pg: number) => { const p = new URLSearchParams(sp); p.set('page', pg.toString()); setSp(p); };
  return (
    <div className="container mx-auto px-4 py-8">
      <h1 className="text-2xl font-bold mb-6">Ogłoszenia</h1>
      <div className="flex gap-8">
        <aside className="hidden lg:block w-64 flex-shrink-0 space-y-4">
          <Input placeholder="Szukaj..." value={search} onChange={(e) => { setSearch(e.target.value); up('search', e.target.value || undefined); }} />
          <div className="space-y-1">{cats?.map((c) => (
            <button key={c.id} onClick={() => up('categoryId', categoryId === c.id ? undefined : c.id)} className={"block w-full text-left px-3 py-1.5 text-sm rounded-lg " + (categoryId === c.id ? 'bg-primary text-primary-foreground' : 'hover:bg-secondary')}>{c.name}</button>
          ))}</div>
          <div className="flex gap-2"><Input type="number" placeholder="Od" value={minP} onChange={(e)=>{setMinP(e.target.value);up('minPrice',e.target.value||undefined);}} /><Input type="number" placeholder="Do" value={maxP} onChange={(e)=>{setMaxP(e.target.value);up('maxPrice',e.target.value||undefined);}} /></div>
          <Button variant="outline" size="sm" onClick={()=>{setSp({});setSearch('');setMinP('');setMaxP('')}} className="w-full"><X className="h-4 w-4 mr-1" />Wyczyść</Button>
        </aside>
        <div className="flex-1">
          <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
            {isLoading ? Array.from({length:6}).map((_,i)=>(<Card key={i}><Skeleton className="aspect-[4/3] w-full" /><CardContent className="p-4 space-y-2"><Skeleton className="h-4 w-3/4" /></CardContent></Card>))
            : data?.items.map((item)=>(<Link key={item.id} to={"/items/" + item.id}><Card className="overflow-hidden transition-all hover:-translate-y-0.5 hover:shadow-md"><div className="relative aspect-[4/3] bg-muted">{item.primaryPhotoUrl ? <img src={item.primaryPhotoUrl} alt={item.title} className="h-full w-full object-cover" /> : <div className="h-full w-full flex items-center justify-center"><Package className="h-10 w-10 text-muted-foreground/30" /></div>}<Badge className="absolute top-2 left-2" variant={item.condition===ItemCondition.New?'success':'secondary'}>{item.condition===ItemCondition.New?'Nowy':'Używany'}</Badge></div><CardContent className="p-4"><h3 className="font-medium text-sm line-clamp-2 mb-1">{item.title}</h3><p className="font-bold text-primary text-lg">{formatPrice(item.price)}</p></CardContent></Card></Link>))}
          </div>
          {data && data.totalPages > 1 && <Pagination page={page} totalPages={data.totalPages} onPageChange={goPage} />}
          {data && data.items.length === 0 && <div className="text-center py-16"><Package className="h-16 w-16 text-muted-foreground/30 mx-auto mb-4" /><p className="text-lg font-medium">Brak ogłoszeń</p></div>}
        </div>
      </div>
    </div>
  );
}
