# ShopApp — Backend Skeleton

Online marketplace backend (Allegro/OLX-style) built with ASP.NET Core 8, MSSQL, Docker.

---

## Architektura

```
ShopApp/
├── ShopApp.Core/               # Encje domenowe, interfejsy — ZERO zależności zewnętrznych
│   ├── Common/BaseEntity.cs    # Id, CreatedAt, UpdatedAt
│   ├── Entities/               # ApplicationUser, Item, Cart, Order, Category, ItemPhoto
│   ├── Enums/                  # UserStatus, ItemStatus, OrderStatus, ItemCondition
│   └── Interfaces/
│       ├── Repositories/       # IRepository<T>, IItemRepository, ICartRepository...
│       └── Services/           # ICurrentUserService, IFileStorageService, IDateTimeService
│
├── ShopApp.Application/        # Logika biznesowa — zależy tylko od Core
│   ├── Common/                 # Result<T>, PagedResult<T>
│   ├── DTOs/                   # Auth, User, Item, Cart, Order DTOs (rekordy C#)
│   ├── Interfaces/             # IAuthService, IItemService, ICartService...
│   ├── Services/               # Implementacje serwisów (TODO: uzupełnić logikę)
│   └── Extensions/             # AddApplication() DI extension
│
├── ShopApp.Infrastructure/     # EF Core, repozytoria, JWT, pliki — zależy od Core + App
│   ├── Data/
│   │   ├── AppDbContext.cs     # IdentityDbContext + konfiguracja modeli
│   │   └── DbSeeder.cs         # Role, admin, kategorie startowe
│   ├── Repositories/           # Repository<T> + ItemRepository, CartRepository...
│   ├── Services/               # CurrentUserService, LocalFileStorageService
│   └── Extensions/             # AddInfrastructure() — DB, Identity, JWT, repozytoria
│
└── ShopApp.API/                # Kontrolery HTTP, middleware, Swagger — zależy od wszystkich
    ├── Controllers/            # Auth, Users, AdminUsers, Items, Cart, Orders, Categories, Chatbot
    ├── Middleware/             # ExceptionMiddleware (globalny handler błędów)
    └── Extensions/             # SwaggerExtensions z JWT
```

**Zasada zależności (Dependency Rule):** Core ← Application ← Infrastructure ← API

---

## Role i uprawnienia

| Rola  | Co może robić |
|-------|---------------|
| Brak (gość) | Przeglądać ogłoszenia, dodawać do koszyka (sesja) |
| `User` | Tworzyć/edytować własne ogłoszenia, składać zamówienia, zarządzać kontem |
| `Admin` | Wszystko powyżej + panel admina: ban/timeout/role użytkowników, statusy zamówień, kategorie |

---

## Szybki start

### 1. Sklonuj / rozpakuj projekt

```bash
unzip ShopApp.zip -d ShopApp
cd ShopApp
```

### 2. Uzupełnij `.env`

```bash
cp .env .env          # już istnieje, wyedytuj wartości
```

Zmień przynajmniej:
- `MSSQL_SA_PASSWORD` — silne hasło (wymóg MSSQL: wielka, mała, cyfra, znak specjalny, min 8 znaków)
- `JWT_SECRET_KEY` — losowy string min. 32 znaki (np. `openssl rand -base64 48`)
- `GEMINI_API_KEY` — klucz z https://aistudio.google.com/app/apikey

### 3. Uruchom Docker Compose

```bash
docker compose up -d --build
```

Przy pierwszym uruchomieniu:
1. MSSQL startuje i przechodzi healthcheck (~30s)
2. API startuje i **automatycznie stosuje migracje** (`DbSeeder.SeedAsync`)
3. Seed tworzy role `Admin`/`User`, admina `admin@shopapp.local` (hasło: `Admin@1234!`) oraz 6 kategorii

API dostępne pod: http://localhost:8080  
Swagger: http://localhost:8080/swagger

---

## Code First — Migracje

### Wymagania

```bash
dotnet tool install --global dotnet-ef
```

### Tworzenie pierwszej migracji

```bash
# Z katalogu głównego (ShopApp/)
dotnet ef migrations add InitialCreate \
  --project ShopApp.Infrastructure \
  --startup-project ShopApp.API \
  --output-dir Migrations

# Sprawdź wygenerowany snapshot — powinien zawierać:
# Users, Roles, UserRoles, Items, Categories, Carts, CartItems, Orders, OrderItems, ItemPhotos
```

### Zastosowanie migracji ręcznie

```bash
dotnet ef database update \
  --project ShopApp.Infrastructure \
  --startup-project ShopApp.API
```

> W środowisku Docker migracje są stosowane automatycznie przez `DbSeeder.SeedAsync()` przy starcie aplikacji.

### Kolejne migracje (np. nowe pole)

```bash
# 1. Zmień encję w ShopApp.Core/Entities/
# 2. Wygeneruj migrację
dotnet ef migrations add AddFieldXyz \
  --project ShopApp.Infrastructure \
  --startup-project ShopApp.API
# 3. Przebuduj kontener
docker compose up -d --build
```

### Cofnięcie ostatniej migracji

```bash
dotnet ef migrations remove \
  --project ShopApp.Infrastructure \
  --startup-project ShopApp.API
```

---

## Endpointy API (skrót)

### Auth
| Metoda | Ścieżka | Opis |
|--------|---------|------|
| POST | `/api/auth/register` | Rejestracja (tworzy rolę `User`) |
| POST | `/api/auth/login` | Logowanie → JWT |
| POST | `/api/auth/refresh` | Odświeżenie tokena |
| POST | `/api/auth/logout` | Wylogowanie (auth) |
| POST | `/api/auth/change-password` | Zmiana hasła (auth) |

### Użytkownik
| Metoda | Ścieżka | Opis |
|--------|---------|------|
| GET | `/api/users/me` | Profil zalogowanego |
| PUT | `/api/users/me` | Aktualizacja profilu |

### Admin — zarządzanie użytkownikami
| Metoda | Ścieżka | Opis |
|--------|---------|------|
| GET | `/api/admin/users` | Lista użytkowników (paginacja + search) |
| GET | `/api/admin/users/{id}` | Szczegóły użytkownika |
| POST | `/api/admin/users/{id}/ban` | Ban z powodem |
| POST | `/api/admin/users/{id}/unban` | Odban |
| POST | `/api/admin/users/{id}/timeout` | Timeout do daty |
| DELETE | `/api/admin/users/{id}/timeout` | Usuń timeout |
| POST | `/api/admin/users/{id}/roles` | Nadaj rolę |
| DELETE | `/api/admin/users/{id}/roles/{roleName}` | Usuń rolę |
| DELETE | `/api/admin/users/{id}` | Usuń użytkownika |

### Ogłoszenia
| Metoda | Ścieżka | Opis |
|--------|---------|------|
| GET | `/api/items?page=1&pageSize=20&search=x&categoryId=x&minPrice=0&maxPrice=999` | Lista (publiczne) |
| GET | `/api/items/{id}` | Szczegóły + licznik wyświetleń |
| GET | `/api/items/my` | Moje ogłoszenia (auth) |
| POST | `/api/items` | Nowe ogłoszenie (auth) |
| PUT | `/api/items/{id}` | Edycja własnego (auth) |
| DELETE | `/api/items/{id}` | Usuń własne (auth) |

### Koszyk (nagłówek `X-Session-Id` dla gości)
| Metoda | Ścieżka | Opis |
|--------|---------|------|
| GET | `/api/cart` | Zawartość koszyka |
| POST | `/api/cart/items` | Dodaj przedmiot |
| PUT | `/api/cart/items/{cartItemId}` | Zmień ilość |
| DELETE | `/api/cart/items/{cartItemId}` | Usuń z koszyka |
| DELETE | `/api/cart` | Wyczyść koszyk |

### Zamówienia
| Metoda | Ścieżka | Opis |
|--------|---------|------|
| GET | `/api/orders` | Moje zamówienia (auth) |
| GET | `/api/orders/{id}` | Szczegóły zamówienia |
| POST | `/api/orders` | Złóż zamówienie z koszyka |
| PATCH | `/api/orders/{id}/status` | Zmień status (admin) |

### Chatbot
| Metoda | Ścieżka | Opis |
|--------|---------|------|
| POST | `/api/chatbot/ask` | `{ "question": "...", "context": "..." }` |

---

## Chatbot — Gemini 2.0 Flash

Endpoint `/api/chatbot/ask` wywołuje Gemini 2.0 Flash z system promptem dostosowanym do marketplace.

**Podpięcie własnego pliku wiedzy (MCP):**
1. Wgraj plik do `ShopApp.API/wwwroot/knowledge/` (np. `faq.txt`)
2. W `ChatbotService.AskAsync` załaduj plik i przekaż jako `context`:
   ```csharp
   var context = await File.ReadAllTextAsync("wwwroot/knowledge/faq.txt", ct);
   return await AskAsync(question, context, ct);
   ```

---

## Uzupełnianie kodu

Miejsca oznaczone `// TODO` do wypełnienia:
- `AuthService.RefreshTokenAsync` — przechowywanie refresh tokenów w DB
- `ItemService`, `CartService`, `OrderService` — pełna logika biznesowa (szkielet jest)
- `LocalFileStorageService` → zastąpić Azure Blob / S3 na produkcji
- Upload zdjęć do `ItemPhoto` — dodać endpoint `/api/items/{id}/photos`

---

## Zmienne środowiskowe

| Zmienna | Opis | Wymagana |
|---------|------|----------|
| `MSSQL_SA_PASSWORD` | Hasło SA do MSSQL | ✅ |
| `JWT_SECRET_KEY` | Klucz podpisywania JWT (min 32 znaki) | ✅ |
| `GEMINI_API_KEY` | Klucz Gemini API | ✅ |
