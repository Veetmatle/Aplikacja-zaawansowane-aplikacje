import { useState, useRef, useEffect } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import {
  Search, ShoppingCart, User, LogOut, Shield, ClipboardList,
  ChevronDown, Menu, X, Plus, Heart, Bell
} from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { useAuthStore } from '@/stores/authStore';
import { useCartStore } from '@/stores/cartStore';
import { useLogout } from '@/hooks/useAuth';
import { useCart } from '@/hooks/useCart';
import { useCategories } from '@/hooks/useCategories';

export function Navbar() {
  const navigate = useNavigate();
  const location = useLocation();
  const [search, setSearch] = useState('');
  const [megaOpen, setMegaOpen] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const megaRef = useRef<HTMLDivElement>(null);
  const userMenuRef = useRef<HTMLDivElement>(null);

  const { user, isAuthenticated } = useAuthStore();
  const isAdmin = useAuthStore((s) => s.isAdmin);
  const { isOpen: cartOpen, setIsOpen: setCartOpen } = useCartStore();
  const { data: cart } = useCart();
  const { data: categories } = useCategories();
  const logoutMutation = useLogout();

  const totalItems = cart?.items.reduce((s, i) => s + i.quantity, 0) ?? 0;

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (search.trim()) navigate('/items?search=' + encodeURIComponent(search.trim()));
  };

  // Close menus on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (megaRef.current && !megaRef.current.contains(e.target as Node)) setMegaOpen(false);
      if (userMenuRef.current && !userMenuRef.current.contains(e.target as Node)) setUserMenuOpen(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  // Close on route change
  useEffect(() => {
    setMegaOpen(false);
    setMobileOpen(false);
    setUserMenuOpen(false);
  }, [location.pathname]);

  return (
    <header className="sticky top-0 z-50 shadow-md">
      {/* ── Level 1: Main navbar ─────────────────────────────── */}
      <div className="bg-navbar">
        <div className="container mx-auto px-4 h-16 flex items-center gap-4">

          {/* Logo */}
          <Link to="/" className="flex items-center gap-2 flex-shrink-0 mr-2">
            <div className="w-8 h-8 bg-accent rounded-lg flex items-center justify-center">
              <ShoppingCart className="h-4 w-4 text-white" />
            </div>
            <span className="text-white font-bold text-xl tracking-tight">ShopApp</span>
          </Link>

          {/* Search — centrum */}
          <form onSubmit={handleSearch} className="flex-1 max-w-2xl hidden sm:flex">
            <div className="flex w-full rounded-lg overflow-hidden border-2 border-white/20 focus-within:border-white/60 transition-colors">
              <Input
                placeholder="Czego szukasz?"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="flex-1 rounded-none border-0 bg-white text-gray-900 placeholder:text-gray-400 h-10 focus-visible:ring-0 focus-visible:ring-offset-0"
              />
              <button
                type="submit"
                className="bg-accent hover:bg-orange-600 text-white px-5 font-semibold text-sm transition-colors"
              >
                Szukaj
              </button>
            </div>
          </form>

          {/* Right actions */}
          <div className="ml-auto flex items-center gap-1">
            {/* Wystaw */}
            <Button
              variant="outline"
              size="sm"
              className="hidden sm:flex border-white/30 text-white hover:bg-white/10 hover:text-white hover:border-white/50 gap-1"
              asChild
            >
              <Link to="/items/new"><Plus className="h-3.5 w-3.5" />Wystaw</Link>
            </Button>

            {/* Koszyk */}
            <button
              onClick={() => setCartOpen(true)}
              className="relative p-2 text-white/80 hover:text-white hover:bg-white/10 rounded-lg transition-colors"
            >
              <ShoppingCart className="h-5 w-5" />
              {totalItems > 0 && (
                <span className="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] bg-accent text-white text-[10px] font-bold rounded-full flex items-center justify-center px-1">
                  {totalItems}
                </span>
              )}
            </button>

            {/* User menu */}
            {isAuthenticated ? (
              <div className="relative" ref={userMenuRef}>
                <button
                  onClick={() => setUserMenuOpen(!userMenuOpen)}
                  className="flex items-center gap-1.5 p-2 text-white/80 hover:text-white hover:bg-white/10 rounded-lg transition-colors"
                >
                  <div className="w-7 h-7 bg-white/20 rounded-full flex items-center justify-center">
                    <User className="h-3.5 w-3.5 text-white" />
                  </div>
                  <span className="hidden md:block text-sm font-medium">{user?.firstName}</span>
                  <ChevronDown className={'h-3.5 w-3.5 transition-transform ' + (userMenuOpen ? 'rotate-180' : '')} />
                </button>

                {userMenuOpen && (
                  <div className="absolute right-0 top-full mt-2 w-52 bg-white rounded-xl shadow-xl border border-gray-100 py-1 z-50">
                    <div className="px-4 py-2 border-b border-gray-100">
                      <p className="text-xs text-gray-500">Zalogowany jako</p>
                      <p className="text-sm font-semibold text-gray-900 truncate">{user?.email}</p>
                    </div>
                    <Link to="/profile" className="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">
                      <User className="h-4 w-4" />Profil
                    </Link>
                    <Link to="/orders" className="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">
                      <ClipboardList className="h-4 w-4" />Zamówienia
                    </Link>
                    {isAdmin() && (
                      <Link to="/admin/users" className="flex items-center gap-2 px-4 py-2 text-sm text-gray-700 hover:bg-gray-50">
                        <Shield className="h-4 w-4" />Panel admina
                      </Link>
                    )}
                    <div className="border-t border-gray-100 mt-1" />
                    <button
                      onClick={() => logoutMutation.mutate()}
                      className="flex items-center gap-2 px-4 py-2 text-sm text-red-600 hover:bg-red-50 w-full text-left"
                    >
                      <LogOut className="h-4 w-4" />Wyloguj się
                    </button>
                  </div>
                )}
              </div>
            ) : (
              <div className="hidden sm:flex items-center gap-1">
                <Button variant="ghost" size="sm" asChild className="text-white/80 hover:text-white hover:bg-white/10">
                  <Link to="/login">Zaloguj</Link>
                </Button>
                <Button size="sm" asChild className="bg-white text-primary hover:bg-white/90 font-semibold">
                  <Link to="/register">Zarejestruj</Link>
                </Button>
              </div>
            )}

            {/* Mobile hamburger */}
            <button
              className="sm:hidden p-2 text-white/80 hover:text-white hover:bg-white/10 rounded-lg"
              onClick={() => setMobileOpen(!mobileOpen)}
            >
              {mobileOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
            </button>
          </div>
        </div>
      </div>

      {/* ── Level 2: Category bar ────────────────────────────── */}
      <div className="hidden md:block bg-navbar-dark border-b border-white/10 relative" ref={megaRef}>
        <div className="container mx-auto px-4 flex items-center h-10 gap-6">
          <button
            onClick={() => setMegaOpen(!megaOpen)}
            className={'flex items-center gap-1.5 text-sm font-medium px-3 py-1 rounded transition-colors ' +
              (megaOpen ? 'bg-white/20 text-white' : 'text-white/80 hover:text-white hover:bg-white/10')}
          >
            <Menu className="h-4 w-4" />
            Kategorie
            <ChevronDown className={'h-3.5 w-3.5 transition-transform ' + (megaOpen ? 'rotate-180' : '')} />
          </button>
          <Link to="/items" className="text-sm text-white/70 hover:text-white transition-colors">Wszystkie ogłoszenia</Link>
          <Link to="/items?sort=newest" className="text-sm text-white/70 hover:text-white transition-colors">Nowe</Link>
          {isAuthenticated && (
            <Link to="/items/new" className="text-sm text-white/70 hover:text-white transition-colors">Sprzedaj</Link>
          )}
        </div>

        {/* Mega-menu dropdown */}
        {megaOpen && categories && categories.length > 0 && (
          <div className="absolute left-0 right-0 top-full bg-white shadow-2xl border-t border-gray-200 z-50">
            <div className="container mx-auto px-4 py-4">
              <div className="flex flex-wrap gap-x-8 gap-y-1">
                {categories.map((cat) => (
                  <Link
                    key={cat.id}
                    to={'/items?categoryId=' + cat.id}
                    className="flex items-center gap-2 px-2 py-1.5 text-sm text-gray-700 hover:text-primary hover:bg-blue-50 rounded-lg transition-colors min-w-[160px]"
                    onClick={() => setMegaOpen(false)}
                  >
                    <span className="w-1.5 h-1.5 rounded-full bg-primary/40 flex-shrink-0" />
                    {cat.name}
                  </Link>
                ))}
              </div>
            </div>
          </div>
        )}
      </div>

      {/* ── Mobile menu ──────────────────────────────────────── */}
      {mobileOpen && (
        <div className="sm:hidden bg-navbar-dark border-t border-white/10 p-4 space-y-3">
          <form onSubmit={handleSearch} className="flex">
            <Input
              placeholder="Szukaj..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="rounded-r-none bg-white text-gray-900"
            />
            <button
              type="submit"
              className="bg-accent text-white px-4 rounded-r-lg text-sm font-medium"
            >
              Szukaj
            </button>
          </form>
          <nav className="flex flex-col gap-1">
            <Link to="/items" className="text-sm py-2 px-2 rounded text-white/80 hover:text-white hover:bg-white/10">Przeglądaj ogłoszenia</Link>
            {isAuthenticated ? (
              <>
                <Link to="/items/new" className="text-sm py-2 px-2 rounded text-white/80 hover:text-white hover:bg-white/10">Wystaw ogłoszenie</Link>
                <Link to="/orders" className="text-sm py-2 px-2 rounded text-white/80 hover:text-white hover:bg-white/10">Zamówienia</Link>
                <Link to="/profile" className="text-sm py-2 px-2 rounded text-white/80 hover:text-white hover:bg-white/10">Profil</Link>
                {isAdmin() && <Link to="/admin/users" className="text-sm py-2 px-2 rounded text-white/80 hover:text-white hover:bg-white/10">Panel admina</Link>}
                <button className="text-sm py-2 px-2 text-left rounded text-red-400 hover:bg-white/10" onClick={() => { logoutMutation.mutate(); setMobileOpen(false); }}>Wyloguj</button>
              </>
            ) : (
              <>
                <Link to="/login" className="text-sm py-2 px-2 rounded text-white/80 hover:text-white hover:bg-white/10">Zaloguj</Link>
                <Link to="/register" className="text-sm py-2 px-2 rounded text-white/80 hover:text-white hover:bg-white/10">Zarejestruj</Link>
              </>
            )}
          </nav>
        </div>
      )}
    </header>
  );
}