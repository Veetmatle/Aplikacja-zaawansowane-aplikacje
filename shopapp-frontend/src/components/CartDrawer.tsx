import { X, ShoppingBag, Minus, Plus, Trash2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { useCartStore } from '@/stores/cartStore';
import { useCart, useUpdateCartItem, useRemoveCartItem } from '@/hooks/useCart';
import { formatPrice } from '@/lib/utils';

export function CartDrawer() {
  const { isOpen, closeCart } = useCartStore();
  const { data: cart, isLoading } = useCart();
  const updateMutation = useUpdateCartItem();
  const removeMutation = useRemoveCartItem();

  if (!isOpen) return null;

  return (
    <>
      {/* Overlay */}
      <div className="fixed inset-0 z-50 bg-black/50" onClick={closeCart} />

      {/* Drawer */}
      <div className="fixed right-0 top-0 z-50 h-full w-full max-w-md bg-background shadow-xl flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between border-b p-4">
          <h2 className="font-display text-lg font-semibold flex items-center gap-2">
            <ShoppingBag className="h-5 w-5" />
            Koszyk
          </h2>
          <Button variant="ghost" size="icon" onClick={closeCart}>
            <X className="h-5 w-5" />
          </Button>
        </div>

        {/* Items */}
        <div className="flex-1 overflow-y-auto p-4">
          {isLoading ? (
            <p className="text-center text-muted-foreground py-8">Ładowanie...</p>
          ) : !cart || cart.items.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-16 gap-4">
              <ShoppingBag className="h-16 w-16 text-muted-foreground/30" />
              <p className="text-muted-foreground">Koszyk jest pusty</p>
              <Button variant="accent" asChild onClick={closeCart}>
                <Link to="/items">Przeglądaj ogłoszenia</Link>
              </Button>
            </div>
          ) : (
            <div className="space-y-4">
              {cart.items.map((item) => (
                <div key={item.id} className="flex gap-3 rounded-lg border p-3">
                  <div className="h-16 w-16 rounded-md bg-muted flex-shrink-0 overflow-hidden">
                    {item.itemPhotoUrl ? (
                      <img src={item.itemPhotoUrl} alt={item.itemTitle} className="h-full w-full object-cover" />
                    ) : (
                      <div className="h-full w-full flex items-center justify-center text-muted-foreground">
                        <ShoppingBag className="h-6 w-6" />
                      </div>
                    )}
                  </div>
                  <div className="flex-1 min-w-0">
                    <Link to={`/items/${item.itemId}`} className="text-sm font-medium hover:underline line-clamp-1" onClick={closeCart}>
                      {item.itemTitle}
                    </Link>
                    <p className="text-sm font-semibold text-primary">{formatPrice(item.unitPrice)}</p>
                    <div className="flex items-center gap-2 mt-1">
                      <Button
                        variant="outline"
                        size="icon"
                        className="h-7 w-7"
                        disabled={updateMutation.isPending}
                        onClick={() => updateMutation.mutate({ cartItemId: item.id, data: { quantity: item.quantity - 1 } })}
                      >
                        <Minus className="h-3 w-3" />
                      </Button>
                      <span className="text-sm w-6 text-center">{item.quantity}</span>
                      <Button
                        variant="outline"
                        size="icon"
                        className="h-7 w-7"
                        disabled={updateMutation.isPending}
                        onClick={() => updateMutation.mutate({ cartItemId: item.id, data: { quantity: item.quantity + 1 } })}
                      >
                        <Plus className="h-3 w-3" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-7 w-7 ml-auto text-destructive"
                        onClick={() => removeMutation.mutate(item.id)}
                      >
                        <Trash2 className="h-3 w-3" />
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Footer */}
        {cart && cart.items.length > 0 && (
          <div className="border-t p-4 space-y-3">
            <div className="flex items-center justify-between font-semibold">
              <span>Suma:</span>
              <span className="text-lg">{formatPrice(cart.totalAmount)}</span>
            </div>
            <Button variant="accent" className="w-full" asChild onClick={closeCart}>
              <Link to="/cart">Przejdź do koszyka</Link>
            </Button>
          </div>
        )}
      </div>
    </>
  );
}
