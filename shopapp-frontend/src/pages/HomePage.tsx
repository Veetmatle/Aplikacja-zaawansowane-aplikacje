import { Link, useNavigate } from 'react-router-dom';
import { ArrowRight, Plus, ChevronLeft, ChevronRight, Smartphone, Shirt, Home, Car, BookOpen, Dumbbell, Grid3X3, Tag } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';
import { ItemCard } from '@/components/items/ItemCard';
import { useItems } from '@/hooks/useItems';
import { useCategories } from '@/hooks/useCategories';
import { useAddToCart } from '@/hooks/useCart';
import { useState, useEffect, useCallback } from 'react';
import useEmblaCarousel from 'embla-carousel-react';
import Autoplay from 'embla-carousel-autoplay';

const heroSlides = [
  { title: 'Znajdź to, czego szukasz', subtitle: 'Tysiące ogłoszeń w jednym miejscu. Kupuj i sprzedawaj bezpiecznie.', cta: 'Przeglądaj ogłoszenia', href: '/items', bg: 'from-[#1A1F71] to-[#2D3494]' },
  { title: 'Sprzedaj w 5 minut', subtitle: 'Dodaj ogłoszenie za darmo i dotrzyj do tysięcy kupujących.', cta: 'Wystaw ogłoszenie', href: '/items/new', bg: 'from-[#1A5276] to-[#2E86C1]' },
  { title: 'Bezpieczne płatności', subtitle: 'Płać przez Przelewy24 — szybko, wygodnie i bezpiecznie.', cta: 'Dowiedz się więcej', href: '/items', bg: 'from-[#145A32] to-[#1E8449]' },
];

const categoryIconMap: Record<string, React.ReactNode> = {
  elektronika: <Smartphone className="h-5 w-5" />,
  moda: <Shirt className="h-5 w-5" />,
  dom: <Home className="h-5 w-5" />,
  motoryzacja: <Car className="h-5 w-5" />,
  sport: <Dumbbell className="h-5 w-5" />,
  ksiazki: <BookOpen className="h-5 w-5" />,
};

function getCatIcon(name: string) {
  const key = name.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
  for (const [k, v] of Object.entries(categoryIconMap)) {
    if (key.includes(k)) return v;
  }
  return <Grid3X3 className="h-5 w-5" />;
}

export default function HomePage() {
  const [heroIdx, setHeroIdx] = useState(0);
  useEffect(() => {
    const t = setInterval(() => setHeroIdx((i) => (i + 1) % heroSlides.length), 5000);
    return () => clearInterval(t);
  }, []);

  const { data: featured, isLoading: loadingFeatured } = useItems({ page: 1, pageSize: 10 });
  const { data: latest, isLoading: loadingLatest } = useItems({ page: 1, pageSize: 8 });
  const { data: categories } = useCategories();
  const addToCart = useAddToCart();

  const [emblaRef, emblaApi] = useEmblaCarousel(
    { loop: true, align: 'start' },
    [Autoplay({ delay: 3000, stopOnInteraction: true })]
  );
  const scrollPrev = useCallback(() => emblaApi?.scrollPrev(), [emblaApi]);
  const scrollNext = useCallback(() => emblaApi?.scrollNext(), [emblaApi]);

  const handleAddToCart = (itemId: string) => addToCart.mutate({ itemId, quantity: 1 });

  return (
    <div className="bg-[#F5F5F5] min-h-screen">

      {/* ── Hero ─────────────────────────────────────────────── */}
      <section className="relative h-56 md:h-72 overflow-hidden">
        {heroSlides.map((slide, i) => (
          <div
            key={i}
            className={'absolute inset-0 flex items-center bg-gradient-to-r transition-opacity duration-700 ' +
              slide.bg + (i === heroIdx ? ' opacity-100 z-10' : ' opacity-0 z-0')}
          >
            <div className="container mx-auto px-6">
              <h1 className="text-2xl md:text-4xl font-bold text-white mb-2 max-w-lg">{slide.title}</h1>
              <p className="text-white/80 text-sm md:text-base mb-5 max-w-md">{slide.subtitle}</p>
              <Link
                to={slide.href}
                className="inline-flex items-center gap-2 bg-orange-500 hover:bg-orange-600 text-white font-semibold px-5 py-2.5 rounded-lg text-sm transition-colors"
              >
                {slide.cta} <ArrowRight className="h-4 w-4" />
              </Link>
            </div>
          </div>
        ))}
        {/* Dots */}
        <div className="absolute bottom-4 left-1/2 -translate-x-1/2 flex gap-2 z-20">
          {heroSlides.map((_, i) => (
            <button
              key={i}
              onClick={() => setHeroIdx(i)}
              className={'h-2 rounded-full transition-all ' + (i === heroIdx ? 'w-6 bg-white' : 'w-2 bg-white/40')}
            />
          ))}
        </div>
      </section>

      {/* ── Kategorie ────────────────────────────────────────── */}
      {categories && categories.length > 0 && (
        <section className="bg-white border-b border-gray-200 py-4">
          <div className="container mx-auto px-4">
            <div className="flex gap-3 overflow-x-auto scrollbar-hide pb-1">
              {categories.map((cat) => (
                <Link
                  key={cat.id}
                  to={'/items?categoryId=' + cat.id}
                  className="flex-shrink-0 flex flex-col items-center gap-1.5 px-4 py-3 rounded-xl hover:bg-blue-50 hover:text-primary transition-colors group"
                >
                  <span className="text-gray-500 group-hover:text-primary transition-colors">
                    {getCatIcon(cat.name)}
                  </span>
                  <span className="text-xs font-medium text-gray-700 group-hover:text-primary whitespace-nowrap">{cat.name}</span>
                </Link>
              ))}
            </div>
          </div>
        </section>
      )}

      {/* ── Polecane (karuzela) ───────────────────────────────── */}
      <section className="py-8">
        <div className="container mx-auto px-4">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-bold text-gray-900">Polecane ogłoszenia</h2>
            <div className="flex gap-1">
              <button onClick={scrollPrev} className="w-8 h-8 rounded-full border border-gray-300 hover:border-primary hover:text-primary flex items-center justify-center transition-colors">
                <ChevronLeft className="h-4 w-4" />
              </button>
              <button onClick={scrollNext} className="w-8 h-8 rounded-full border border-gray-300 hover:border-primary hover:text-primary flex items-center justify-center transition-colors">
                <ChevronRight className="h-4 w-4" />
              </button>
            </div>
          </div>
          <div className="overflow-hidden" ref={emblaRef}>
            <div className="flex gap-3">
              {loadingFeatured
                ? Array.from({ length: 5 }).map((_, i) => (
                  <div key={i} className="flex-shrink-0 w-44 bg-white rounded-xl p-2">
                    <Skeleton className="aspect-square w-full rounded-lg" />
                    <Skeleton className="h-3.5 mt-2 w-3/4" />
                    <Skeleton className="h-4 mt-1.5 w-1/2" />
                  </div>
                ))
                : featured?.items.map((item) => (
                  <div key={item.id} className="flex-shrink-0 w-44">
                    <ItemCard item={item} variant="grid" onAddToCart={handleAddToCart} />
                  </div>
                ))}
            </div>
          </div>
        </div>
      </section>

      {/* ── Najnowsze ogłoszenia (lista OLX) ─────────────────── */}
      <section className="pb-10">
        <div className="container mx-auto px-4">
          <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
            {/* Header sekcji */}
            <div className="flex items-center justify-between px-5 py-4 border-b border-gray-100">
              <h2 className="text-lg font-bold text-gray-900">Najnowsze ogłoszenia</h2>
              <Link
                to="/items"
                className="flex items-center gap-1 text-sm text-primary hover:underline font-medium"
              >
                Zobacz wszystkie <ArrowRight className="h-3.5 w-3.5" />
              </Link>
            </div>
            {/* Lista */}
            <div className="divide-y divide-gray-100">
              {loadingLatest
                ? Array.from({ length: 5 }).map((_, i) => (
                  <div key={i} className="flex gap-4 p-4">
                    <Skeleton className="w-32 h-24 rounded-lg flex-shrink-0" />
                    <div className="flex-1 space-y-2 py-1">
                      <Skeleton className="h-4 w-2/3" />
                      <Skeleton className="h-3 w-1/3" />
                      <Skeleton className="h-5 w-1/4" />
                    </div>
                  </div>
                ))
                : latest?.items.map((item) => (
                  <ItemCard key={item.id} item={item} variant="list" onAddToCart={handleAddToCart} />
                ))}
            </div>
          </div>
        </div>
      </section>

      {/* ── CTA baner ────────────────────────────────────────── */}
      <section className="bg-[#1A1F71] py-12">
        <div className="container mx-auto px-4 text-center">
          <h2 className="text-2xl md:text-3xl font-bold text-white mb-3">Masz coś do sprzedania?</h2>
          <p className="text-white/70 mb-6 max-w-md mx-auto text-sm">Dodaj ogłoszenie za darmo i dotrzyj do tysięcy kupujących.</p>
          <Link
            to="/items/new"
            className="inline-flex items-center gap-2 bg-orange-500 hover:bg-orange-600 text-white font-semibold px-6 py-3 rounded-lg transition-colors"
          >
            <Plus className="h-4 w-4" />Wystaw ogłoszenie za darmo
          </Link>
        </div>
      </section>
    </div>
  );
}