import { X } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import type { CategoryDto } from '@/types/api';

interface FiltersPanelProps {
  search: string;
  onSearchChange: (val: string) => void;
  categories: CategoryDto[] | undefined;
  activeCategoryId: string | undefined;
  onCategoryChange: (id: string | undefined) => void;
  minPrice: string;
  maxPrice: string;
  onMinPriceChange: (val: string) => void;
  onMaxPriceChange: (val: string) => void;
  onClear: () => void;
}

export function FiltersPanel({
  search, onSearchChange, categories,
  activeCategoryId, onCategoryChange,
  minPrice, maxPrice, onMinPriceChange, onMaxPriceChange,
  onClear,
}: FiltersPanelProps) {
  return (
    <div className="space-y-5">
      <div>
        <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2 block">Szukaj</label>
        <Input
          placeholder="Wpisz frazę..."
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
        />
      </div>

      <div>
        <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2 block">Kategoria</label>
        <div className="space-y-0.5 max-h-60 overflow-y-auto">
          {categories?.map((c) => (
            <button
              key={c.id}
              onClick={() => onCategoryChange(activeCategoryId === c.id ? undefined : c.id)}
              className={
                'block w-full text-left px-3 py-2 text-sm rounded-lg transition-colors ' +
                (activeCategoryId === c.id
                  ? 'bg-primary text-primary-foreground'
                  : 'hover:bg-secondary text-foreground')
              }
            >
              {c.name}
            </button>
          ))}
        </div>
      </div>

      <div>
        <label className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-2 block">Cena (PLN)</label>
        <div className="flex gap-2">
          <Input type="number" placeholder="Od" value={minPrice} onChange={(e) => onMinPriceChange(e.target.value)} />
          <Input type="number" placeholder="Do" value={maxPrice} onChange={(e) => onMaxPriceChange(e.target.value)} />
        </div>
      </div>

      <Button variant="outline" size="sm" onClick={onClear} className="w-full gap-1">
        <X className="h-4 w-4" />
        Wyczyść filtry
      </Button>
    </div>
  );
}
