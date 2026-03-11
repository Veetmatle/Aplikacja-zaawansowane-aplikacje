import { useState, useRef, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { ChevronRight, Grid3X3, Smartphone, Shirt, Home, Car, Palette, BookOpen, Dumbbell, Baby, Wrench } from 'lucide-react';
import { useCategories } from '@/hooks/useCategories';
import type { CategoryDto } from '@/types/api';

const categoryIcons: Record<string, React.ReactNode> = {
  default: <Grid3X3 className="h-4 w-4" />,
  elektronika: <Smartphone className="h-4 w-4" />,
  moda: <Shirt className="h-4 w-4" />,
  dom: <Home className="h-4 w-4" />,
  motoryzacja: <Car className="h-4 w-4" />,
  sztuka: <Palette className="h-4 w-4" />,
  ksiazki: <BookOpen className="h-4 w-4" />,
  sport: <Dumbbell className="h-4 w-4" />,
  dziecko: <Baby className="h-4 w-4" />,
  narzedzia: <Wrench className="h-4 w-4" />,
};

function getIcon(name: string) {
  const key = name.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
  for (const [k, v] of Object.entries(categoryIcons)) {
    if (key.includes(k)) return v;
  }
  return categoryIcons.default;
}

interface Props {
  isOpen: boolean;
  onClose: () => void;
}

export function CategoryMegaMenu({ isOpen, onClose }: Props) {
  const { data: categories } = useCategories();
  const [hoveredId, setHoveredId] = useState<string | null>(null);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClick(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) onClose();
    }
    if (isOpen) document.addEventListener('mousedown', handleClick);
    return () => document.removeEventListener('mousedown', handleClick);
  }, [isOpen, onClose]);

  if (!isOpen || !categories) return null;

  const hovered = categories.find((c) => c.id === hoveredId);

  return (
    <div ref={ref} className="absolute left-0 top-full w-full bg-card border-t border-border shadow-2xl z-50" onMouseLeave={onClose}>
      <div className="container mx-auto flex">
        {/* Left: main categories */}
        <div className="w-72 border-r border-border py-3">
          {categories.map((cat) => (
            <Link
              key={cat.id}
              to={'/items?categoryId=' + cat.id}
              className={
                'flex items-center justify-between px-4 py-2.5 text-sm transition-colors ' +
                (hoveredId === cat.id ? 'bg-secondary text-foreground' : 'text-muted-foreground hover:bg-secondary/50 hover:text-foreground')
              }
              onMouseEnter={() => setHoveredId(cat.id)}
              onClick={onClose}
            >
              <span className="flex items-center gap-2.5">
                {getIcon(cat.name)}
                {cat.name}
              </span>
              {cat.subCategories && cat.subCategories.length > 0 && (
                <ChevronRight className="h-3.5 w-3.5 opacity-50" />
              )}
            </Link>
          ))}
        </div>

        {/* Right: subcategories */}
        <div className="flex-1 p-6">
          {hovered && hovered.subCategories && hovered.subCategories.length > 0 ? (
            <div>
              <h3 className="font-display font-semibold text-sm text-muted-foreground mb-4">
                {hovered.name}
              </h3>
              <div className="grid grid-cols-3 gap-x-8 gap-y-2">
                {hovered.subCategories.map((sub: CategoryDto) => (
                  <Link
                    key={sub.id}
                    to={'/items?categoryId=' + sub.id}
                    className="text-sm py-1.5 text-foreground hover:text-primary transition-colors"
                    onClick={onClose}
                  >
                    {sub.name}
                  </Link>
                ))}
              </div>
            </div>
          ) : (
            <div className="flex items-center justify-center h-full text-muted-foreground text-sm">
              Najedź na kategorię, aby zobaczyć podkategorie
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
