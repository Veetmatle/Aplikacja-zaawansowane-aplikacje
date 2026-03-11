import { Link } from 'react-router-dom';
import { ArrowRight, Plus, ChevronLeft, ChevronRight, Smartphone, Shirt, Home, Car, BookOpen, Dumbbell, Grid3X3 } from 'lucide-react';
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
	{ title: 'Znajdź to, czego szukasz', subtitle: 'Tysiące ogłoszeń w jednym miejscu. Kupuj i sprzedawaj bezpiecznie.', cta: 'Przeglądaj ogłoszenia', href: '/items', gradient: 'from-blue-900/60 to-background' },
	{ title: 'Sprzedaj w 5 minut', subtitle: 'Dodaj ogłoszenie za darmo i dotrzyj do tysięcy kupujących.', cta: 'Wystaw ogłoszenie', href: '/items/new', gradient: 'from-amber-900/40 to-background' },
	{ title: 'Bezpieczne płatności', subtitle: 'Płać przez Przelewy24 — szybko, wygodnie i bezpiecznie.', cta: 'Dowiedz się więcej', href: '/items', gradient: 'from-green-900/40 to-background' },
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
	// Hero slider
	const [heroIdx, setHeroIdx] = useState(0);
	useEffect(() => {
		const timer = setInterval(() => setHeroIdx((i) => (i + 1) % heroSlides.length), 5000);
		return () => clearInterval(timer);
	}, []);

	// Data
	const { data: featured, isLoading: loadingFeatured } = useItems({ page: 1, pageSize: 10 });
	const { data: latest, isLoading: loadingLatest } = useItems({ page: 1, pageSize: 6 });
	const { data: categories } = useCategories();
	const addToCart = useAddToCart();

	// Carousel for featured
	const [emblaRef, emblaApi] = useEmblaCarousel({ loop: true, align: 'start', slidesToScroll: 1 }, [Autoplay({ delay: 3000, stopOnInteraction: true })]);
	const scrollPrev = useCallback(() => emblaApi?.scrollPrev(), [emblaApi]);
	const scrollNext = useCallback(() => emblaApi?.scrollNext(), [emblaApi]);

	const handleAddToCart = (itemId: string) => addToCart.mutate({ itemId, quantity: 1 });

	return (
		<div>
			{/* Section A — Hero Banner */}
			<section className="relative h-64 md:h-80 overflow-hidden">
				{heroSlides.map((slide, i) => (
					<div
						key={i}
						className={'absolute inset-0 flex items-center transition-opacity duration-700 bg-gradient-to-r ' + slide.gradient + (i === heroIdx ? ' opacity-100 z-10' : ' opacity-0 z-0')}
					>
						<div className="container mx-auto px-4">
							<h1 className="text-3xl md:text-5xl font-bold mb-3 max-w-xl">{slide.title}</h1>
							<p className="text-muted-foreground text-base md:text-lg mb-5 max-w-md">{slide.subtitle}</p>
							<Button variant="accent" size="lg" asChild>
								<Link to={slide.href}>{slide.cta}</Link>
							</Button>
						</div>
					</div>
				))}
				{/* Dots */}
				<div className="absolute bottom-4 left-1/2 -translate-x-1/2 flex gap-2 z-20">
					{heroSlides.map((_, i) => (
						<button
							key={i}
							onClick={() => setHeroIdx(i)}
							className={'h-2 rounded-full transition-all ' + (i === heroIdx ? 'w-6 bg-accent' : 'w-2 bg-white/30')}
							aria-label={'Slajd ' + (i + 1)}
						/>
					))}
				</div>
			</section>

			{/* Section B — Categories horizontal scroll */}
			{categories && categories.length > 0 && (
				<section className="py-8 border-b border-border">
					<div className="container mx-auto px-4">
						<h2 className="text-lg font-semibold mb-4">Kategorie</h2>
						<div className="flex gap-3 overflow-x-auto scrollbar-hide pb-1">
							{categories.map((cat) => (
								<Link
									key={cat.id}
									to={'/items?categoryId=' + cat.id}
									className="flex-shrink-0 w-28 flex flex-col items-center gap-2 rounded-xl border border-border bg-card p-3 text-center hover:bg-secondary/60 transition-colors"
								>
									<span className="text-primary">{getCatIcon(cat.name)}</span>
									<span className="text-xs font-medium line-clamp-1">{cat.name}</span>
								</Link>
							))}
						</div>
					</div>
				</section>
			)}

			{/* Section C — Featured carousel */}
			<section className="py-10">
				<div className="container mx-auto px-4">
					<div className="flex items-center justify-between mb-4">
						<h2 className="text-lg font-semibold">Polecane ogłoszenia</h2>
						<div className="flex gap-1">
							<Button variant="outline" size="icon" className="h-8 w-8" onClick={scrollPrev}><ChevronLeft className="h-4 w-4" /></Button>
							<Button variant="outline" size="icon" className="h-8 w-8" onClick={scrollNext}><ChevronRight className="h-4 w-4" /></Button>
						</div>
					</div>
					<div className="overflow-hidden" ref={emblaRef}>
						<div className="flex gap-3">
							{loadingFeatured
								? Array.from({ length: 5 }).map((_, i) => (
									<div key={i} className="flex-shrink-0 w-48"><Skeleton className="aspect-square rounded-xl" /><Skeleton className="h-4 mt-2 w-3/4" /></div>
								))
								: featured?.items.map((item) => (
									<div key={item.id} className="flex-shrink-0 w-48">
										<ItemCard item={item} variant="grid" onAddToCart={handleAddToCart} />
									</div>
								))}
						</div>
					</div>
				</div>
			</section>

			{/* Section D — Latest (OLX-style list) */}
			<section className="py-10 border-t border-border">
				<div className="container mx-auto px-4">
					<div className="flex items-center justify-between mb-4">
						<h2 className="text-lg font-semibold">Najnowsze ogłoszenia</h2>
					</div>
					<div className="space-y-3">
						{loadingLatest
							? Array.from({ length: 4 }).map((_, i) => (
								<Skeleton key={i} className="h-32 rounded-xl" />
							))
							: latest?.items.map((item) => (
								<ItemCard key={item.id} item={item} variant="list" onAddToCart={handleAddToCart} />
							))}
					</div>
					<div className="text-center mt-8">
						<Button variant="outline" size="lg" asChild>
							<Link to="/items" className="gap-2">
								Zobacz wszystkie ogłoszenia <ArrowRight className="h-4 w-4" />
							</Link>
						</Button>
					</div>
				</div>
			</section>

			{/* Section E — CTA banner */}
			<section className="bg-secondary py-12 border-t border-border">
				<div className="container mx-auto px-4 text-center">
					<h2 className="text-2xl md:text-3xl font-bold mb-3">Masz coś do sprzedania?</h2>
					<p className="text-muted-foreground mb-6 max-w-md mx-auto">Dodaj ogłoszenie za darmo i dotrzyj do tysięcy kupujących w całej Polsce.</p>
					<Button variant="accent" size="lg" asChild>
						<Link to="/items/new" className="gap-2"><Plus className="h-5 w-5" />Wystaw ogłoszenie za darmo</Link>
					</Button>
				</div>
			</section>
		</div>
	);
}
