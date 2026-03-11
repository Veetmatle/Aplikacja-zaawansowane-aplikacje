import { Link, useNavigate } from 'react-router-dom';
import { ShoppingCart, Menu, X, Search, User, LogOut, Package, Shield, Plus, ChevronDown, ClipboardList } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { useAuthStore } from '@/stores/authStore';
import { useCartStore } from '@/stores/cartStore';
import { useCart } from '@/hooks/useCart';
import { useLogout } from '@/hooks/useAuth';
import { CategoryMegaMenu } from '@/components/CategoryMegaMenu';
import { useState } from 'react';

export function Navbar() {
  const { isAuthenticated, user, isAdmin } = useAuthStore();
  const toggleCart = useCartStore((s) => s.toggleCart);
  const { data: cart } = useCart();
  const logoutMutation = useLogout();
  const navigate = useNavigate();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [search, setSearch] = useState('');
  const [megaOpen, setMegaOpen] = useState(false);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (search.trim()) {
      navigate('/items?search=' + encodeURIComponent(search.trim()));
      setSearch('');
    }
  };

  return (
    <header className="sticky top-0 z-50 w-full">
      {/* Level 1 — Main bar */}
      <div className="bg-[hsl(var(--navbar))] border-b border-border/50">
        <div className="container mx-auto flex h-14 items-center gap-4 px-4">
          {/* Logo */}
          <Link to="/" className="flex items-center gap-2 flex-shrink-0">
            <Package className="h-6 w-6 text-accent" />
            <span className="font-display text-lg font-bold text-white">ShopApp</span>
          </Link>

          {/* Search — always visible on desktop */}
          <form onSubmit={handleSearch} className="hidden md:flex flex-1 max-w-2xl">
            <div className="flex w-full">
              <Input
                placeholder="Czego szukasz?"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="rounded-r-none border-r-0 bg-input/80 h-10 focus-visible:ring-0"
              />
              <Button type="submit" variant="accent" className="rounded-l-none px-6 h-10">
                <Search className="h-4 w-4 mr-1.5" />
                Szukaj
              </Button>
            </div>
          </form>

          {/* Right icons */}
          <div className="flex items-center gap-1 ml-auto">
            {isAuthenticated && (
              <Button variant="ghost" size="sm" asChild className="hidden sm:flex text-white/80 hover:text-white hover:bg-white/10">
                <Link to="/items/new">
                  <Plus className="h-4 w-4 mr-1" />
                  Wystaw
                </Link>
              </Button>
            )}

            {/* Cart */}
            <Button variant="ghost" size="icon" onClick={toggleCart} className="relative text-white/80 hover:text-white hover:bg-white/10" aria-label="Koszyk">
              <ShoppingCart className="h-5 w-5" />
              {cart && cart.totalItems > 0 && (
                <Badge className="absolute -top-1 -right-1 h-5 w-5 flex items-center justify-center p-0 text-[10px]" variant="destructive">
                  {cart.totalItems}
                </Badge>
              )}
            </Button>

            {isAuthenticated ? (
              <div className="hidden sm:flex items-center gap-0.5">
                <Button variant="ghost" size="sm" asChild className="text-white/80 hover:text-white hover:bg-white/10">
                  <Link to="/orders">
                    <ClipboardList className="h-4 w-4 mr-1" />
                    Zamówienia
                  </Link>
                </Button>
                <Button variant="ghost" size="sm" asChild className="text-white/80 hover:text-white hover:bg-white/10">
                  <Link to="/profile">
                    <User className="h-4 w-4 mr-1" />
                    {user?.firstName}
                  </Link>
                </Button>
                {isAdmin() && (
                  <Button variant="ghost" size="icon" asChild className="text-white/80 hover:text-white hover:bg-white/10">
                    <Link to="/admin/users"><Shield className="h-4 w-4" /></Link>
                  </Button>
                )}
                <Button variant="ghost" size="icon" onClick={() => logoutMutation.mutate()} className="text-white/80 hover:text-white hover:bg-white/10" aria-label="Wyloguj">
                  <LogOut className="h-4 w-4" />
                </Button>
              </div>
            ) : (
              <div className="hidden sm:flex items-center gap-1">
                <Button variant="ghost" size="sm" asChild className="text-white/80 hover:text-white hover:bg-white/10">
                  <Link to="/login">Zaloguj</Link>
                </Button>
                <Button variant="accent" size="sm" asChild>
                  <Link to="/register">Zarejestruj</Link>
                </Button>
              </div>
            )}

            <Button variant="ghost" size="icon" className="sm:hidden text-white/80 hover:text-white hover:bg-white/10" onClick={() => setMobileOpen(!mobileOpen)}>
              {mobileOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
            </Button>
          </div>
        </div>
      </div>

      {/* Level 2 — Category bar (desktop) */}
      <div className="hidden md:block bg-secondary/80 backdrop-blur border-b border-border/30 relative">
        <div className="container mx-auto flex items-center h-10 px-4 gap-6">
          <button
            onClick={() => setMegaOpen(!megaOpen)}
            className={'flex items-center gap-1.5 text-sm font-medium px-3 py-1.5 rounded-lg transition-colors ' + (megaOpen ? 'bg-primary text-primary-foreground' : 'text-foreground hover:bg-white/5')}
          >
            <Menu className="h-4 w-4" />
            Kategorie
            <ChevronDown className={'h-3.5 w-3.5 transition-transform ' + (megaOpen ? 'rotate-180' : '')} />
          </button>
          <Link to="/items" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Nowe ogłoszenia</Link>
          {isAuthenticated && (
            <Link to="/items/new" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Sprzedaj teraz</Link>
          )}
          <Link to="/items?minPrice=0&maxPrice=50" className="text-sm text-muted-foreground hover:text-foreground transition-colors">Okazje do 50 zł</Link>
        </div>
        <CategoryMegaMenu isOpen={megaOpen} onClose={() => setMegaOpen(false)} />
      </div>

      {/* Mobile menu */}
      {mobileOpen && (
        <div className="sm:hidden border-t border-border bg-card p-4 space-y-3">
          <form onSubmit={handleSearch}>
            <div className="flex">
              <Input placeholder="Szukaj..." value={search} onChange={(e) => setSearch(e.target.value)} className="rounded-r-none" />
              <Button type="submit" variant="accent" className="rounded-l-none"><Search className="h-4 w-4" /></Button>
            </div>
          </form>
          <nav className="flex flex-col gap-1">
            <Link to="/items" className="text-sm py-2 px-2 rounded hover:bg-secondary" onClick={() => setMobileOpen(false)}>Przeglądaj</Link>
            {isAuthenticated ? (
              <>
                <Link to="/items/new" className="text-sm py-2 px-2 rounded hover:bg-secondary" onClick={() => setMobileOpen(false)}>Wystaw przedmiot</Link>
                <Link to="/orders" className="text-sm py-2 px-2 rounded hover:bg-secondary" onClick={() => setMobileOpen(false)}>Zamówienia</Link>
                <Link to="/profile" className="text-sm py-2 px-2 rounded hover:bg-secondary" onClick={() => setMobileOpen(false)}>Profil</Link>
                {isAdmin() && <Link to="/admin/users" className="text-sm py-2 px-2 rounded hover:bg-secondary" onClick={() => setMobileOpen(false)}>Panel admina</Link>}
                <button className="text-sm py-2 px-2 text-left rounded text-destructive hover:bg-secondary" onClick={() => { logoutMutation.mutate(); setMobileOpen(false); }}>Wyloguj</button>
              </>
            ) : (
              <>
                <Link to="/login" className="text-sm py-2 px-2 rounded hover:bg-secondary" onClick={() => setMobileOpen(false)}>Zaloguj</Link>
                <Link to="/register" className="text-sm py-2 px-2 rounded hover:bg-secondary" onClick={() => setMobileOpen(false)}>Zarejestruj</Link>
              </>
            )}
          </nav>
        </div>
      )}
    </header>
  );
}
