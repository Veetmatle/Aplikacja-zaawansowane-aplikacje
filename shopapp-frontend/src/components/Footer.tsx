import { Package } from 'lucide-react';

export function Footer() {
  return (
    <footer className="border-t bg-background mt-auto">
      <div className="container mx-auto flex items-center justify-between px-4 py-6 text-sm text-muted-foreground">
        <div className="flex items-center gap-2">
          <Package className="h-4 w-4" />
          <span>ShopApp © {new Date().getFullYear()}</span>
        </div>
        <p>Projekt edukacyjny — zaawansowane aplikacje</p>
      </div>
    </footer>
  );
}
