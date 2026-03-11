import { Link, useNavigate } from 'react-router-dom';
import { Search, ArrowRight, Package } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { useItems } from '@/hooks/useItems';
import { useCategories } from '@/hooks/useCategories';
import { formatPrice } from '@/lib/utils';
import { ItemCondition } from '@/types/api';
import { useState } from 'react';

export default function HomePage() {
  const [search, setSearch] = useState('');
  const navigate = useNavigate();
  const { data: itemsData, isLoading } = useItems({ page: 1, pageSize: 8 });
  const { data: categories } = useCategories();
  const handleSearch = (e: React.FormEvent) => { e.preventDefault(); if (search.trim()) navigate("/items?search=" + encodeURIComponent(search.trim())); };

  return (
    <div>
      <section className="bg-primary text-primary-foreground py-16 md:py-24">
        <div className="container mx-auto px-4 text-center">
          <h1 className="text-3xl md:text-5xl font-bold mb-4">Znajdź to, czego szukasz</h1>
          <p className="text-lg md:text-xl opacity-90 mb-8 max-w-2xl mx-auto">Tysiące ogłoszeń w jednym miejscu. Kupuj i sprzedawaj bezpiecznie.</p>
          <form onSubmit={handleSearch} className="max-w-lg mx-auto flex gap-2">
            <div className="relative flex-1"><Search className="absolute left-3 top-1/2 -translate-y-1/2 h-5 w-5 opacity-60" /><Input placeholder="Czego szukasz?" value={search} onChange={(e) => setSearch(e.target.value)} className="pl-11 h-12 bg-white text-foreground border-0" /></div>
            <Button type="submit" variant="accent" size="lg">Szukaj</Button>
          </form>
        </div>
      </section>
      {categories && categories.length > 0 && (
        <section className="container mx-auto px-4 py-12">
          <h2 className="text-2xl font-bold mb-6">Kategorie</h2>
          <div className="flex gap-3 overflow-x-auto pb-2">{categories.map((cat) => (
            <Link key={cat.id} to={"/items?categoryId=" + cat.id} className="flex-shrink-0 rounded-xl border bg-card px-5 py-3 text-sm font-medium hover:bg-secondary transition-colors">{cat.name}</Link>
          ))}</div>
        </section>
      )}
      <section className="container mx-auto px-4 py-12">
        <div className="flex items-center justify-between mb-6"><h2 className="text-2xl font-bold">Najnowsze ogłoszenia</h2><Button variant="ghost" asChild><Link to="/items" className="flex items-center gap-1">Zobacz wszystkie <ArrowRight className="h-4 w-4" /></Link></Button></div>
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
          {isLoading ? Array.from({ length: 8 }).map((_, i) => (<Card key={i} className="overflow-hidden"><Skeleton className="aspect-[4/3] w-full" /><CardContent className="p-4 space-y-2"><Skeleton className="h-4 w-3/4" /><Skeleton className="h-5 w-1/3" /></CardContent></Card>))
          : itemsData?.items.map((item) => (
            <Link key={item.id} to={"/items/" + item.id}>
              <Card className="overflow-hidden transition-all duration-200 hover:-translate-y-0.5 hover:shadow-md">
                <div className="relative aspect-[4/3] bg-muted">{item.primaryPhotoUrl ? <img src={item.primaryPhotoUrl} alt={item.title} className="h-full w-full object-cover" /> : <div className="h-full w-full flex items-center justify-center"><Package className="h-10 w-10 text-muted-foreground/30" /></div>}<Badge className="absolute top-2 left-2" variant={item.condition === ItemCondition.New ? 'success' : 'secondary'}>{item.condition === ItemCondition.New ? 'Nowy' : 'Używany'}</Badge></div>
                <CardContent className="p-4"><h3 className="font-medium text-sm line-clamp-2 mb-1">{item.title}</h3><p className="font-bold text-primary text-lg">{formatPrice(item.price)}</p></CardContent>
              </Card>
            </Link>))}
        </div>
      </section>
    </div>
  );
}
