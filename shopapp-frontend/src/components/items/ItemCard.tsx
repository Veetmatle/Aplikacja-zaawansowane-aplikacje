import { Link } from 'react-router-dom';
import { Package, ShoppingCart, MapPin, Clock } from 'lucide-react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { formatPrice, formatDateShort } from '@/lib/utils';
import { ItemCondition } from '@/types/api';
import type { ItemSummaryDto } from '@/types/api';

export interface ItemCardProps {
  item: ItemSummaryDto;
  variant?: 'grid' | 'list';
  onAddToCart?: (itemId: string) => void;
}

const conditionLabel = (c: ItemCondition) =>
  c === ItemCondition.New ? 'Nowy' : c === ItemCondition.Used ? 'Używany' : 'Odnowiony';

export function ItemCard({ item, variant = 'grid', onAddToCart }: ItemCardProps) {
  if (variant === 'list') {
    return (
      <Link to={'/items/' + item.id} className="block">
        <Card className="overflow-hidden transition-all hover:bg-secondary/50">
          <div className="flex gap-4 p-3">
            {/* Photo */}
            <div className="relative w-40 h-[120px] flex-shrink-0 rounded-lg bg-muted overflow-hidden">
              {item.primaryPhotoUrl ? (
                <img src={item.primaryPhotoUrl} alt={item.title} className="h-full w-full object-cover" />
              ) : (
                <div className="h-full w-full flex items-center justify-center">
                  <Package className="h-8 w-8 text-muted-foreground/30" />
                </div>
              )}
              <Badge
                className="absolute top-1.5 left-1.5 text-[10px] px-1.5 py-0"
                variant={item.condition === ItemCondition.New ? 'success' : 'secondary'}
              >
                {conditionLabel(item.condition)}
              </Badge>
            </div>

            {/* Details */}
            <div className="flex-1 min-w-0 py-1">
              <h3 className="font-semibold text-base line-clamp-1 mb-1">{item.title}</h3>
              <div className="flex items-center gap-3 text-xs text-muted-foreground mb-2">
                {item.location && (
                  <span className="flex items-center gap-1"><MapPin className="h-3 w-3" />{item.location}</span>
                )}
                <span className="flex items-center gap-1"><Clock className="h-3 w-3" />{formatDateShort(item.createdAt)}</span>
              </div>
              <p className="text-xs text-muted-foreground line-clamp-2">
                {item.categoryName}
              </p>
            </div>

            {/* Price & actions */}
            <div className="flex flex-col items-end justify-between flex-shrink-0 py-1">
              <p className="font-bold text-lg text-accent whitespace-nowrap">{formatPrice(item.price)}</p>
              {onAddToCart && (
                <Button
                  variant="outline"
                  size="sm"
                  className="gap-1"
                  onClick={(e) => { e.preventDefault(); e.stopPropagation(); onAddToCart(item.id); }}
                >
                  <ShoppingCart className="h-3.5 w-3.5" />
                  <span className="hidden sm:inline">Do koszyka</span>
                </Button>
              )}
            </div>
          </div>
        </Card>
      </Link>
    );
  }

  // Grid variant
  return (
    <Link to={'/items/' + item.id} className="group block">
      <Card className="overflow-hidden transition-all duration-200 hover:-translate-y-0.5 hover:shadow-md hover:shadow-primary/5">
        <div className="relative aspect-square bg-muted">
          {item.primaryPhotoUrl ? (
            <img src={item.primaryPhotoUrl} alt={item.title} className="h-full w-full object-cover" />
          ) : (
            <div className="h-full w-full flex items-center justify-center">
              <Package className="h-10 w-10 text-muted-foreground/30" />
            </div>
          )}
          <Badge
            className="absolute top-2 left-2"
            variant={item.condition === ItemCondition.New ? 'success' : 'secondary'}
          >
            {conditionLabel(item.condition)}
          </Badge>
          {onAddToCart && (
            <Button
              variant="accent"
              size="icon"
              className="absolute bottom-2 right-2 h-8 w-8 opacity-0 group-hover:opacity-100 transition-opacity md:opacity-0 sm:opacity-100"
              onClick={(e) => { e.preventDefault(); e.stopPropagation(); onAddToCart(item.id); }}
              aria-label="Dodaj do koszyka"
            >
              <ShoppingCart className="h-4 w-4" />
            </Button>
          )}
        </div>
        <CardContent className="p-3">
          <h3 className="font-medium text-sm line-clamp-2 mb-1">{item.title}</h3>
          <p className="font-bold text-accent text-lg">{formatPrice(item.price)}</p>
        </CardContent>
      </Card>
    </Link>
  );
}
