import { Link } from 'react-router-dom';
import { Package, ShoppingCart, MapPin, Clock } from 'lucide-react';
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

const conditionClass = (c: ItemCondition) =>
  c === ItemCondition.New
    ? 'bg-green-100 text-green-700 border-green-200'
    : 'bg-gray-100 text-gray-600 border-gray-200';

export function ItemCard({ item, variant = 'grid', onAddToCart }: ItemCardProps) {
  if (variant === 'list') {
    return (
      <Link to={'/items/' + item.id} className="group block">
        <div className="flex gap-4 px-5 py-4 hover:bg-gray-50 transition-colors">
          {/* Zdjęcie */}
          <div className="relative w-32 h-24 flex-shrink-0 rounded-lg bg-gray-100 overflow-hidden">
            {item.primaryPhotoUrl ? (
              <img src={item.primaryPhotoUrl} alt={item.title} className="h-full w-full object-cover" />
            ) : (
              <div className="h-full w-full flex items-center justify-center">
                <Package className="h-7 w-7 text-gray-300" />
              </div>
            )}
          </div>

          {/* Treść */}
          <div className="flex-1 min-w-0">
            <h3 className="font-semibold text-gray-900 text-sm line-clamp-2 mb-1 group-hover:text-primary transition-colors">
              {item.title}
            </h3>
            <div className="flex items-center gap-3 text-xs text-gray-400 mb-2">
              {item.location && (
                <span className="flex items-center gap-1">
                  <MapPin className="h-3 w-3" />{item.location}
                </span>
              )}
              <span className="flex items-center gap-1">
                <Clock className="h-3 w-3" />{formatDateShort(item.createdAt)}
              </span>
            </div>
            <span className={'inline-block text-[10px] font-medium px-2 py-0.5 rounded-full border ' + conditionClass(item.condition)}>
              {conditionLabel(item.condition)}
            </span>
          </div>

          {/* Cena + CTA */}
          <div className="flex flex-col items-end justify-between flex-shrink-0">
            <p className="font-bold text-lg text-gray-900 whitespace-nowrap">{formatPrice(item.price)}</p>
            {onAddToCart && (
              <button
                className="flex items-center gap-1 text-xs text-primary border border-primary/30 hover:bg-primary hover:text-white px-3 py-1.5 rounded-lg transition-colors"
                onClick={(e) => { e.preventDefault(); e.stopPropagation(); onAddToCart(item.id); }}
              >
                <ShoppingCart className="h-3.5 w-3.5" />
                <span className="hidden sm:inline">Do koszyka</span>
              </button>
            )}
          </div>
        </div>
      </Link>
    );
  }

  // Grid variant — dla karuzeli na homepage
  return (
    <Link to={'/items/' + item.id} className="group block">
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden hover:shadow-md hover:border-primary/30 transition-all">
        <div className="relative aspect-square bg-gray-100">
          {item.primaryPhotoUrl ? (
            <img src={item.primaryPhotoUrl} alt={item.title} className="h-full w-full object-cover" />
          ) : (
            <div className="h-full w-full flex items-center justify-center">
              <Package className="h-8 w-8 text-gray-300" />
            </div>
          )}
          {/* Hover: dodaj do koszyka */}
          {onAddToCart && (
            <button
              className="absolute bottom-2 right-2 opacity-0 group-hover:opacity-100 bg-primary text-white p-1.5 rounded-lg shadow-md transition-all"
              onClick={(e) => { e.preventDefault(); e.stopPropagation(); onAddToCart(item.id); }}
            >
              <ShoppingCart className="h-3.5 w-3.5" />
            </button>
          )}
        </div>
        <div className="p-3">
          <h3 className="text-xs text-gray-700 line-clamp-2 mb-1">{item.title}</h3>
          <p className="font-bold text-sm text-gray-900">{formatPrice(item.price)}</p>
        </div>
      </div>
    </Link>
  );
}