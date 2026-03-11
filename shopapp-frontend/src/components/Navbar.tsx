import { Link, useNavigate, useLocation } from 'react-router-dom';
import { ShoppingCart, Menu, X, Search, User, LogOut, Package, Shield, Plus } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { useAuthStore } from '@/stores/authStore';
import { useCartStore } from '@/stores/cartStore';
import { useCart } from '@/hooks/useCart';
import { useLogout } from '@/hooks/useAuth';
import { useState } from 'react';

export function Navbar() {
  const { isAuthenticated, user, isAdmin } = useAuthStore();
  const toggleCart = useCartStore((s) => s.toggleCart);
  const { data: cart } = useCart();
  const logoutMutation = useLogout();
  const navigate = useNavigate();
  const location = useLocation();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [search, setSearch] = useState('');

  const isHomePage = location.pathname === '/';

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (search.trim()) {
      navigate(`/items?search=${encodeURIComponent(search.trim())}`);
      setSearch('');
    }
  };

  return (
    <header className="sticky top-0 z-50 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="container mx-auto flex h-16 items-center justify-between px-4">
        {/* Logo */}
        <Link to="/" className="flex items-center gap-2">
          <Package className="h-7 w-7 text-accent" />
          <span className="font-display text-xl font-bold text-primary">ShopApp</span>
        </Link>

        {/* Search — desktop (hidden on homepage which has its own hero search) */}
        {!isHomePage && (
          <form onSubmit={handleSearch} className="hidden md:flex flex-1 max-w-md mx-8">
            <div className="relative w-full">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Szukaj ogłoszeń..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="pl-10"
              />
            </div>
          </form>
        )}

        {/* Right side */}
        <div className="flex items-center gap-2">
          {isAuthenticated && (
            <Button variant="ghost" size="sm" asChild className="hidden sm:flex">
              <Link to="/items/new">
                <Plus className="h-4 w-4 mr-1" />
                Wystaw
              </Link>
            </Button>
          )}

          {/* Cart */}
          <Button variant="ghost" size="icon" onClick={toggleCart} className="relative" aria-label="Koszyk">
            <ShoppingCart className="h-5 w-5" />
            {cart && cart.totalItems > 0 && (
              <Badge className="absolute -top-1 -right-1 h-5 w-5 flex items-center justify-center p-0 text-[10px]" variant="destructive">
                {cart.totalItems}
              </Badge>
            )}
          </Button>

          {/* User menu — desktop */}
          {isAuthenticated ? (
            <div className="hidden sm:flex items-center gap-1">
              <Button variant="ghost" size="sm" asChild>
                <Link to="/profile">
                  <User className="h-4 w-4 mr-1" />
                  {user?.firstName}
                </Link>
              </Button>
              <Button variant="ghost" size="sm" asChild>
                <Link to="/orders">Zamówienia</Link>
              </Button>
              {isAdmin() && (
                <Button variant="ghost" size="sm" asChild>
                  <Link to="/admin/users">
                    <Shield className="h-4 w-4 mr-1" />
                    Admin
                  </Link>
                </Button>
              )}
              <Button variant="ghost" size="icon" onClick={() => logoutMutation.mutate()} aria-label="Wyloguj">
                <LogOut className="h-4 w-4" />
              </Button>
            </div>
          ) : (
            <div className="hidden sm:flex items-center gap-2">
              <Button variant="ghost" size="sm" asChild>
                <Link to="/login">Zaloguj</Link>
              </Button>
              <Button variant="accent" size="sm" asChild>
                <Link to="/register">Zarejestruj</Link>
              </Button>
            </div>
          )}

          {/* Mobile hamburger */}
          <Button variant="ghost" size="icon" className="sm:hidden" onClick={() => setMobileOpen(!mobileOpen)}>
            {mobileOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
          </Button>
        </div>
      </div>

      {/* Mobile menu */}
      {mobileOpen && (
        <div className="sm:hidden border-t bg-background p-4 space-y-3">
          <form onSubmit={handleSearch}>
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input placeholder="Szukaj..." value={search} onChange={(e) => setSearch(e.target.value)} className="pl-10" />
            </div>
          </form>
          <nav className="flex flex-col gap-2">
            <Link to="/items" className="text-sm py-2" onClick={() => setMobileOpen(false)}>Przeglądaj</Link>
            {isAuthenticated ? (
              <>
                <Link to="/items/new" className="text-sm py-2" onClick={() => setMobileOpen(false)}>Wystaw przedmiot</Link>
                <Link to="/orders" className="text-sm py-2" onClick={() => setMobileOpen(false)}>Zamówienia</Link>
                <Link to="/profile" className="text-sm py-2" onClick={() => setMobileOpen(false)}>Profil</Link>
                {isAdmin() && <Link to="/admin/users" className="text-sm py-2" onClick={() => setMobileOpen(false)}>Panel admina</Link>}
                <button className="text-sm py-2 text-left text-destructive" onClick={() => { logoutMutation.mutate(); setMobileOpen(false); }}>Wyloguj</button>
              </>
            ) : (
              <>
                <Link to="/login" className="text-sm py-2" onClick={() => setMobileOpen(false)}>Zaloguj</Link>
                <Link to="/register" className="text-sm py-2" onClick={() => setMobileOpen(false)}>Zarejestruj</Link>
              </>
            )}
          </nav>
        </div>
      )}
    </header>
  );
}
