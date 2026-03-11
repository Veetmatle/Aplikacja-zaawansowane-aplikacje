# ShopApp — Full-Stack Marketplace

Kompletna aplikacja sprzedażowa (marketplace Allegro/OLX-style) — **backend** (ASP.NET Core 8, MSSQL) + **frontend** (React 18, TypeScript, Vite, Tailwind CSS) — uruchamiana przez Docker Compose.
Projekt stosuje Clean Architecture (Onion), SOLID, FluentValidation, JWT z refresh tokenami, integrację płatności Przelewy24, rate limiting, soft delete, strukturalne logowanie (Serilog), health checks oraz pełne pokrycie testami (backend + frontend).

---

## Architektura

```
ShopApp/
├── ShopApp.Core/                   # Encje domenowe, interfejsy — minimalne zależności
│   ├── Common/BaseEntity.cs        # Id, CreatedAt, UpdatedAt, DeletedAt (soft delete)
│   ├── Entities/                   # ApplicationUser, Item, Cart, Order, Category, Payment, RefreshToken
│   ├── Enums/                      # UserStatus, ItemStatus, OrderStatus, ItemCondition, PaymentStatus
│   └── Interfaces/
│       ├── IUnitOfWork.cs          # Transakcyjność: Begin/Commit/Rollback
│       ├── Repositories/           # IRepository<T>, IItemRepository, ICartRepository, IPaymentRepository, IRefreshTokenRepository
│       └── Services/               # ICurrentUserService, IFileStorageService, IDateTimeService, IPaymentGateway
│
├── ShopApp.Application/            # Logika biznesowa — zależy tylko od Core
│   ├── Common/                     # Result<T>, PagedResult<T>
│   ├── DTOs/                       # Auth, User, Item, Cart, Order, Payment DTOs (rekordy C#)
│   ├── Interfaces/                 # IAuthService, IItemService, ICartService, IPaymentService...
│   ├── Services/                   # AuthService, ItemService, CartService, OrderService, PaymentService...
│   ├── Validators/                 # FluentValidation: AuthValidators, ItemValidators, CartValidators, OrderValidators
│   └── Extensions/                 # AddApplication() DI extension
│
├── ShopApp.Infrastructure/         # EF Core, repozytoria, JWT, P24, pliki — zależy od Core + App
│   ├── Data/
│   │   ├── AppDbContext.cs         # IdentityDbContext + Fluent API + GlobalQueryFilters (soft delete) + auto UpdatedAt
│   │   └── DbSeeder.cs            # Role, admin, kategorie startowe (BEZ migracji)
│   ├── Repositories/
│   │   ├── Repository.cs          # Generyczna implementacja z soft delete w DeleteAsync
│   │   ├── SpecificRepositories.cs# Item, Cart, Order, Category, RefreshToken, Payment repos
│   │   └── UnitOfWork.cs          # EF Core implementacja IUnitOfWork (transakcje)
│   ├── Services/
│   │   ├── InfrastructureServices.cs # CurrentUserService, DateTimeService, LocalFileStorageService (z path traversal protection)
│   │   └── Przelewy24Service.cs   # Implementacja IPaymentGateway — sandbox/production P24 API
│   └── Extensions/                # AddInfrastructure() — DB, Identity, JWT, repozytoria, UnitOfWork, P24
│
├── ShopApp.API/                    # Kontrolery HTTP, middleware, Swagger — zależy od wszystkich
│   ├── Controllers/               # Auth, Users, AdminUsers, Items, Cart, Orders, Categories, Chatbot, Payments
│   │                              # Wszystkie kontrolery mają [ProducesResponseType] atrybuty
│   ├── Filters/                   # ValidationFilter (FluentValidation auto-validation)
│   ├── Middleware/                # ExceptionMiddleware (globalny handler błędów)
│   └── Extensions/                # SwaggerExtensions z JWT + XML documentation
│
├── ShopApp.Tests/
│   ├── ShopApp.UnitTests/         # xUnit + NSubstitute + FluentAssertions (40 testów)
│   │   ├── Services/              # AuthServiceTests, ItemServiceTests, CartServiceTests, OrderServiceTests, PaymentServiceTests
│   │   ├── Validators/            # ValidatorTests
│   │   └── Mocks/                 # FakePaymentGateway
│   ├── ShopApp.IntegrationTests/  # WebApplicationFactory + Testcontainers MsSql
│   │   ├── Fixtures/              # IntegrationTestBase z real MSSQL w Docker
│   │   └── Controllers/           # FullFlowTests (register→login→items→cart→order→payment)
│   └── ShopApp.ArchTests/         # NetArchTest — Dependency Rule (6 testów)
│
└── scripts/
    └── init-db-user.sql           # Tworzenie dedykowanego użytkownika SQL (nie SA!)
├── docs/
│   └── adr/                       # Architecture Decision Records
│       ├── 001-iunitofwork-in-core.md
│       └── 002-applicationuser-soft-delete.md
```

**Zasada zależności (Dependency Rule):** Core ← Application ← Infrastructure ← API
**Weryfikowana automatycznie** przez ShopApp.ArchTests (NetArchTest).

---

## Co zostało zaimplementowane

### Bezpieczeństwo (krytyczne)
- ✅ **Refresh tokeny w DB** — SHA-256 hash, rotacja tokenów, wykrywanie ponownego użycia (revoke all), unieważnianie przy logout/change-password
- ✅ **Dedykowany użytkownik SQL** — connection string używa `shopapp_user` zamiast `sa`; skrypt `scripts/init-db-user.sql`
- ✅ **Rate limiting** — `auth` (10 req/min), `chatbot` (5 req/min) — wbudowany `System.Threading.RateLimiting` z .NET 8
- ✅ **Walidacja wejścia** — FluentValidation z auto-validating ActionFilter na wszystkich DTO
- ✅ **Path traversal protection** — `LocalFileStorageService` sanityzuje nazwy plików, weryfikuje czy wynikowa ścieżka mieści się w upload dir
- ✅ **Chatbot auth** — endpoint `POST /api/chatbot/ask` wymaga JWT lub `X-Session-Id` header (zapobieganie anonimowemu nadużyciu klucza Gemini)

### Niezawodność i idempotentność
- ✅ **Transakcyjność płatności** — `HandleNotificationAsync` opakowuje update Payment + Order w jedną transakcję DB (IUnitOfWork)
- ✅ **Idempotentność callbacku P24** — pole `ProcessedAt` na encji Payment sprawdzane przed przetworzeniem; duplikowane callbacki są ignorowane
- ✅ **SessionId unique constraint** — unikatowy indeks na `Payment.SessionId` zapobiega duplikatom

### Soft Delete
- ✅ **`DeletedAt` w BaseEntity** — wszystkie encje dziedziczące po `BaseEntity` mają wsparcie soft delete
- ✅ **`DeletedAt` w ApplicationUser** — ApplicationUser dziedziczy z IdentityUser (nie BaseEntity), więc DeletedAt dodany bezpośrednio; soft delete zamiast hard delete w `AdminUserService.DeleteUserAsync`
- ✅ **GlobalQueryFilters** — `HasQueryFilter(e => e.DeletedAt == null)` na **ApplicationUser**, Item, Category, Order, Cart, Payment, ItemPhoto
- ✅ **Repository.DeleteAsync** — ustawia `DeletedAt` zamiast fizycznego usuwania (dla `BaseEntity`-derived)
- ✅ **Auto UpdatedAt** — `SaveChangesAsync` override automatycznie ustawia `UpdatedAt` na zmodyfikowanych encjach (BaseEntity + ApplicationUser)

### Logowanie i monitoring
- ✅ **Serilog structured logging** — konsola + pliki rotowane dziennie (`logs/shopapp-*.log`)
- ✅ **Request logging** — `UseSerilogRequestLogging()` z wzbogaceniem o `RequestHost` i `UserAgent`
- ✅ **Korelacja logów** — `Enrich.FromLogContext()` pozwala na śledzenie requestów
- ✅ **Health Checks** — endpoint `/health` z checkiem SQL Server (`AddHealthChecks().AddSqlServer(...)`)

### Dokumentacja API
- ✅ **`[ProducesResponseType]`** — atrybuty na wszystkich action methods ze wszystkimi kodami (200/201/204/400/401/403/404/429)
- ✅ **XML Documentation** — `<GenerateDocumentationFile>` + `IncludeXmlComments()` w Swagger
- ✅ **Swagger UI** — pełna dokumentacja z kontraktami błędów, kodami HTTP i typami odpowiedzi

### Logika biznesowa
- ✅ **ItemService** — pełny CRUD z inkrementacją ViewCount, walidacją właściciela przy edit/delete
- ✅ **CartService** — koszyk gościa (session) i zalogowanego, merge koszyka po loginie, stock check
- ✅ **OrderService** — tworzenie zamówienia z koszyka, walidacja przejść statusów (Pending→Confirmed→Shipped→Delivered)
- ✅ **CategoryService** — CRUD z slug generowaniem
- ✅ **Serwerowy session ID** — `POST /api/cart/session` generuje UUID po stronie serwera

### Płatności Przelewy24
- ✅ **IPaymentGateway** — interfejs w Core (bez zależności od HTTP)
- ✅ **Przelewy24Service** — implementacja w Infrastructure (sandbox/production), SHA384 CRC signing
- ✅ **PaymentService** — orchestracja: init → redirect → notify → verify → update order (transakcyjnie)
- ✅ **PaymentsController** — `POST /initiate`, `POST /notify` (AllowAnonymous), `GET /status`, `GET /return`
- ✅ **FakePaymentGateway** — mock do testów jednostkowych

### Migracje
- ✅ **Wydzielone ze startup** — `DbSeeder` nie wywołuje `MigrateAsync()`
- ✅ **CLI flag** — `dotnet run --migrate` jako osobny krok pre-deploy
- ✅ **EF Core migrations** — `dotnet ef database update -p ShopApp.Infrastructure -s ShopApp.API`

### Testy
- ✅ **40 unit testów** — serwisy + walidatory (xUnit + NSubstitute + FluentAssertions)
- ✅ **6 testów architektury** — NetArchTest weryfikuje Dependency Rule
- ✅ **Integration tests** — Testcontainers + WebApplicationFactory (wymagają Docker)

---

## Endpointy API

### Auth (`/api/auth`) — Rate limited: 10 req/min
| Metoda | Endpoint | Auth | Opis |
|--------|----------|------|------|
| POST | `/api/auth/register` | — | Rejestracja (zwraca JWT + refresh token) |
| POST | `/api/auth/login` | — | Logowanie |
| POST | `/api/auth/refresh` | — | Odśwież access token (rotacja refresh tokena) |
| POST | `/api/auth/logout` | ✅ | Unieważnia wszystkie refresh tokeny |
| POST | `/api/auth/change-password` | ✅ | Zmiana hasła + revoke tokenów |

### Items (`/api/items`)
| Metoda | Endpoint | Auth | Opis |
|--------|----------|------|------|
| GET | `/api/items?page=1&pageSize=20` | — | Lista z paginacją, filtrowaniem |
| GET | `/api/items/{id}` | — | Szczegóły (inkrement ViewCount) |
| GET | `/api/items/my` | ✅ | Moje ogłoszenia |
| POST | `/api/items` | ✅ | Utwórz ogłoszenie |
| PUT | `/api/items/{id}` | ✅ | Edytuj (tylko właściciel) |
| DELETE | `/api/items/{id}` | ✅ | Usuń — soft delete (tylko właściciel) |

### Cart (`/api/cart`)
| Metoda | Endpoint | Auth | Opis |
|--------|----------|------|------|
| POST | `/api/cart/session` | — | Generuj session ID (gość) |
| GET | `/api/cart` | —/✅ | Pobierz koszyk (wymaga session lub auth) |
| POST | `/api/cart/items` | —/✅ | Dodaj do koszyka |
| PUT | `/api/cart/items/{id}` | —/✅ | Zmień ilość |
| DELETE | `/api/cart/items/{id}` | —/✅ | Usuń z koszyka |
| DELETE | `/api/cart` | —/✅ | Wyczyść koszyk |

### Orders (`/api/orders`)
| Metoda | Endpoint | Auth | Opis |
|--------|----------|------|------|
| GET | `/api/orders` | ✅ | Moje zamówienia |
| GET | `/api/orders/{id}` | ✅ | Szczegóły zamówienia |
| POST | `/api/orders` | ✅ | Utwórz z koszyka |
| PATCH | `/api/orders/{id}/status` | Admin | Zmiana statusu (z walidacją przejść) |

### Payments (`/api/payments`)
| Metoda | Endpoint | Auth | Opis |
|--------|----------|------|------|
| POST | `/api/payments/{orderId}/initiate` | ✅ | Rozpocznij płatność → redirect URL |
| POST | `/api/payments/notify` | — | Callback P24 (weryfikacja podpisu, idempotentny) |
| GET | `/api/payments/{orderId}/status` | ✅ | Status płatności |
| GET | `/api/payments/return` | — | Return URL po płatności |

### Categories (`/api/categories`)
| Metoda | Endpoint | Auth | Opis |
|--------|----------|------|------|
| GET | `/api/categories` | — | Lista kategorii |
| GET | `/api/categories/{id}` | — | Szczegóły |
| POST | `/api/categories` | Admin | Utwórz |
| PUT | `/api/categories/{id}` | Admin | Edytuj |
| DELETE | `/api/categories/{id}` | Admin | Usuń (soft delete) |

### Chatbot (`/api/chatbot`) — Rate limited: 5 req/min
| Metoda | Endpoint | Auth | Opis |
|--------|----------|------|------|
| POST | `/api/chatbot/ask` | ✅/Session | Zapytaj chatbota (wymaga JWT lub X-Session-Id) |

### Admin Users (`/api/admin/users`)
| Metoda | Endpoint | Auth | Opis |
|--------|----------|------|------|
| GET | `/api/admin/users` | Admin | Lista użytkowników |
| GET | `/api/admin/users/{id}` | Admin | Szczegóły |
| POST | `/api/admin/users/{id}/ban` | Admin | Zbanuj |
| POST | `/api/admin/users/{id}/unban` | Admin | Odbanuj |
| POST | `/api/admin/users/{id}/timeout` | Admin | Timeout |
| DELETE | `/api/admin/users/{id}/timeout` | Admin | Usuń timeout |
| POST | `/api/admin/users/{id}/roles` | Admin | Przypisz rolę |
| DELETE | `/api/admin/users/{id}/roles/{roleName}` | Admin | Usuń rolę |
| DELETE | `/api/admin/users/{id}` | Admin | Usuń konto |

### Health Check
| Metoda | Endpoint | Auth | Opis |
|--------|----------|------|------|
| GET | `/health` | — | Health check (SQL Server) |

---

## Flow płatności Przelewy24

```
1. User → POST /api/orders              → tworzy Order (status: Pending)
2. User → POST /api/payments/{id}/initiate → P24 register → redirect URL
3. [użytkownik płaci na stronie P24]
4. P24  → POST /api/payments/notify      → weryfikacja CRC → transakcja: Payment.ProcessedAt + Order.Status = Confirmed
5. User → GET /api/payments/return       → redirect po powrocie
6. User → GET /api/payments/{id}/status  → sprawdź status
```

**Bezpieczeństwo płatności:**
- Weryfikacja CRC podpisu SHA384 przed jakąkolwiek zmianą w DB
- Idempotentność: `Payment.ProcessedAt` sprawdzane przed przetworzeniem — duplikaty ignorowane
- Transakcyjność: update Payment + Order w jednej transakcji (`IUnitOfWork.BeginTransactionAsync`)
- Unique constraint na `Payment.SessionId` — dodatkowa ochrona przed duplikatami

---

## Konfiguracja

### Zmienne środowiskowe (`.env`)
```bash
MSSQL_SA_PASSWORD=YourStrong!Passw0rd       # Tylko do setup
SHOPAPP_DB_PASSWORD=ShopApp_User!2024        # Hasło użytkownika DB
JWT_SECRET_KEY=MinimumLength32Characters!    # JWT signing key
GEMINI_API_KEY=your-gemini-api-key           # Google Gemini
P24_MERCHANT_ID=0                            # Przelewy24 sandbox
P24_CRC_KEY=test-crc-key                     # Przelewy24 CRC
P24_REPORT_KEY=test-report-key               # Przelewy24 Report Key
```

Skopiuj `.env.example` → `.env` i uzupełnij wartości.

### appsettings.json
Konfiguracja w `ShopApp.API/appsettings.json`:
- `ConnectionStrings:DefaultConnection` — połączenie z MSSQL (użytkownik `shopapp_user`)
- `Jwt` — klucz, issuer, audience, czas ważności
- `Przelewy24` — MerchantId, PosId, CrcKey, ReportKey, Sandbox (true/false)
- `Gemini:ApiKey` — klucz API chatbota
- `FileStorage:BasePath` — ścieżka uploadu plików (puste = `wwwroot/uploads`)
- `Serilog` — konfiguracja poziomu logowania

---

## Uruchomienie

### 1. Migracje (osobny krok)
```bash
dotnet run --project ShopApp.API -- --migrate
# lub:
dotnet ef database update -p ShopApp.Infrastructure -s ShopApp.API
```

### 2. Start aplikacji
```bash
dotnet run --project ShopApp.API
```

### 3. Tworzenie użytkownika DB (jednorazowo)
```bash
sqlcmd -S localhost -U sa -P <sa_password> -i scripts/init-db-user.sql
```

---

## Testy

```bash
# Unit testy (40 testów — bez Dockera)
dotnet test ShopApp.Tests/ShopApp.UnitTests

# Testy architektury (6 testów — Dependency Rule)
dotnet test ShopApp.Tests/ShopApp.ArchTests

# Testy integracyjne (wymagają Docker!)
dotnet test ShopApp.Tests/ShopApp.IntegrationTests

# Wszystkie testy
dotnet test
```

---

## Struktura plików — szczegółowy opis

### ShopApp.Core
| Plik | Opis |
|------|------|
| `Common/BaseEntity.cs` | Bazowa encja: Id (Guid), CreatedAt, UpdatedAt, **DeletedAt** (soft delete), IsDeleted |
| `Entities/ApplicationUser.cs` | Użytkownik: Identity + FirstName, LastName, Status, RefreshTokens |
| `Entities/ApplicationRole.cs` | Rola: Identity + Description |
| `Entities/Item.cs` | Ogłoszenie: Title, Price, Quantity, Status, Condition, ViewCount |
| `Entities/ItemPhoto.cs` | Zdjęcie ogłoszenia: Url, IsPrimary, Order |
| `Entities/Category.cs` | Kategoria: Name, Slug, IsActive, hierarchia (ParentCategory) |
| `Entities/Cart.cs` | Koszyk: UserId/SessionId, Items, ExpiresAt |
| `Entities/Order.cs` | Zamówienie: OrderNumber, Status, PaymentStatus, TotalAmount, adres |
| `Entities/Payment.cs` | Płatność: OrderId, SessionId, Amount, Status, Provider, RedirectUrl, **ProcessedAt** (idempotentność) |
| `Entities/RefreshToken.cs` | Token odświeżający: TokenHash (SHA-256), ExpiresAt, RevokedAt |
| `Enums/Enums.cs` | UserStatus, ItemStatus, OrderStatus, ItemCondition, PaymentStatus |
| `Interfaces/IUnitOfWork.cs` | **Interfejs Unit of Work** — BeginTransaction, Commit, Rollback, SaveChanges |
| `Interfaces/Repositories/IRepository.cs` | Generyczny interfejs CRUD |
| `Interfaces/Repositories/ISpecificRepositories.cs` | Specyficzne repozytoria (Item, Cart, Order, Category, RefreshToken, Payment) |
| `Interfaces/Services/IInfrastructureServices.cs` | ICurrentUserService, IFileStorageService, IDateTimeService |
| `Interfaces/Services/IPaymentGateway.cs` | Abstrakcja bramki płatności + PaymentRequest/Result/Notification |

### ShopApp.Application
| Plik | Opis |
|------|------|
| `Common/Result.cs` | Result<T> monad (Success/Failure/NotFound/Forbidden) |
| `Common/PagedResult.cs` | Paginowany wynik z TotalPages, HasNext/Previous |
| `DTOs/Auth/AuthDtos.cs` | RegisterDto, LoginDto, AuthResponseDto, RefreshTokenDto |
| `DTOs/Item/ItemDtos.cs` | ItemDto, ItemSummaryDto, CreateItemDto, UpdateItemDto, ItemQueryDto |
| `DTOs/Item/CategoryDtos.cs` | CategoryDto, CreateCategoryDto, UpdateCategoryDto |
| `DTOs/Cart/CartDtos.cs` | CartDto, CartItemDto, AddToCartDto, UpdateCartItemDto |
| `DTOs/Order/OrderDtos.cs` | OrderDto, OrderItemDto, CreateOrderDto, UpdateOrderStatusDto |
| `DTOs/Payment/PaymentDtos.cs` | InitiatePaymentDto, PaymentStatusDto, P24NotificationDto |
| `Interfaces/IServices.cs` | Interfejsy wszystkich serwisów aplikacyjnych |
| `Services/AuthService.cs` | JWT + refresh token rotation + logout/revoke |
| `Services/ItemService.cs` | CRUD ogłoszeń z ViewCount i walidacją właściciela |
| `Services/CartService.cs` | Koszyk gościa/zalogowanego, merge, stock check |
| `Services/OrderService.cs` | Zamówienia z walidacją przejść statusów |
| `Services/PaymentService.cs` | Orchestracja płatności: init → notify → verify (**transakcyjnie z IUnitOfWork, idempotentnie z ProcessedAt**) |
| `Services/CategoryService.cs` | CRUD kategorii z auto-generowaniem slug |
| `Services/UserService.cs` | Profil użytkownika |
| `Services/AdminUserService.cs` | Panel admina: ban/timeout/role |
| `Services/ChatbotService.cs` | Integracja z Google Gemini |
| `Validators/AuthValidators.cs` | FluentValidation dla Register, Login, ChangePassword |
| `Validators/ItemValidators.cs` | FluentValidation dla CreateItem, UpdateItem, Category |
| `Validators/CartValidators.cs` | FluentValidation dla AddToCart, UpdateCartItem |
| `Validators/OrderValidators.cs` | FluentValidation dla CreateOrder, UpdateOrderStatus |

### ShopApp.Infrastructure
| Plik | Opis |
|------|------|
| `Data/AppDbContext.cs` | EF Core DbContext z Fluent API, **GlobalQueryFilters (soft delete)**, auto **UpdatedAt** |
| `Data/DbSeeder.cs` | Seed ról, admina, kategorii (BEZ migracji!) |
| `Repositories/Repository.cs` | Generyczna implementacja IRepository<T> — **DeleteAsync robi soft delete** |
| `Repositories/SpecificRepositories.cs` | Item, Cart, Order, Category, RefreshToken, Payment repos |
| `Repositories/UnitOfWork.cs` | **EF Core implementacja IUnitOfWork** — transakcje DB |
| `Services/InfrastructureServices.cs` | CurrentUserService, DateTimeService, **LocalFileStorageService (konfigurowalny basePath + path traversal protection)** |
| `Services/Przelewy24Service.cs` | Implementacja IPaymentGateway — sandbox/production P24 API |
| `Extensions/ServiceCollectionExtensions.cs` | DI: DB, Identity, JWT, repozytoria, **UnitOfWork**, P24 |

### ShopApp.API
| Plik | Opis |
|------|------|
| `Program.cs` | Pipeline: **Serilog**, rate limiter, validation filter, **health checks**, --migrate flag |
| `Controllers/AuthController.cs` | Auth endpoints z rate limiting + **[ProducesResponseType]** |
| `Controllers/ItemsController.cs` | CRUD ogłoszeń + **[ProducesResponseType]** |
| `Controllers/CartController.cs` | Koszyk + server-side session generation + **[ProducesResponseType]** |
| `Controllers/OrdersController.cs` | Zamówienia + **[ProducesResponseType]** |
| `Controllers/PaymentsController.cs` | Płatności P24 + **[ProducesResponseType]** |
| `Controllers/CategoriesController.cs` | Kategorie + **[ProducesResponseType]** |
| `Controllers/ChatbotController.cs` | Chatbot z rate limiting + **wymóg auth/session** + **[ProducesResponseType]** |
| `Controllers/UsersController.cs` | Profil użytkownika + **[ProducesResponseType]** |
| `Controllers/AdminUsersController.cs` | Panel admina + **[ProducesResponseType]** |
| `Controllers/BaseController.cs` | Bazowy kontroler z FromResult() helper |
| `Filters/ValidationFilter.cs` | Auto-walidacja DTO przez FluentValidation |
| `Middleware/ExceptionMiddleware.cs` | Globalny handler wyjątków |
| `Extensions/SwaggerExtensions.cs` | Swagger z JWT bearer auth + **XML documentation comments** |

### ShopApp.Tests
| Plik | Opis |
|------|------|
| `ShopApp.UnitTests/Services/AuthServiceTests.cs` | 7 testów: register, login, refresh, logout |
| `ShopApp.UnitTests/Services/ItemServiceTests.cs` | 6 testów: CRUD, ownership, ViewCount |
| `ShopApp.UnitTests/Services/CartServiceTests.cs` | 5 testów: add, stock, clear, merge |
| `ShopApp.UnitTests/Services/OrderServiceTests.cs` | 4 testy: create, stock check, status transitions |
| `ShopApp.UnitTests/Services/PaymentServiceTests.cs` | **8 testów**: init, notify, verify, ownership, **idempotentność (ProcessedAt)**, **transakcyjność** |
| `ShopApp.UnitTests/Validators/ValidatorTests.cs` | 9 testów: walidacja DTO |
| `ShopApp.UnitTests/Mocks/FakePaymentGateway.cs` | Mock IPaymentGateway dla testów |
| `ShopApp.IntegrationTests/Fixtures/IntegrationTestBase.cs` | Testcontainers MSSQL + WebApplicationFactory |
| `ShopApp.IntegrationTests/Controllers/FullFlowTests.cs` | E2E: register → login → items → cart → order |
| `ShopApp.ArchTests/DependencyTests.cs` | 6 testów Dependency Rule (NetArchTest) |

---

## Changelog — zmiany wprowadzone w tej iteracji

### ❌ Naprawione poważne braki

1. **Idempotentność callbacku płatności (Outbox-lite)**
   - Dodano `ProcessedAt` do `Payment` entity
   - `HandleNotificationAsync` sprawdza `ProcessedAt` zamiast samego `Status`
   - Update Payment + Order w jednej transakcji DB (`IUnitOfWork`)
   - Nowy interfejs `IUnitOfWork` w Core, implementacja `UnitOfWork` w Infrastructure
   - Nowy test: `HandleNotificationAsync_WhenAlreadyProcessed_ReturnsSuccessWithoutChanges`

2. **Dokumentacja błędów w Swaggerze**
   - Dodano `[ProducesResponseType]` na **wszystkich** action methods we wszystkich 9 kontrolerach
   - Włączono `<GenerateDocumentationFile>` w csproj
   - Dodano `IncludeXmlComments()` w SwaggerExtensions
   - Swagger teraz pokazuje kontrakty błędów (400, 401, 403, 404, 429)

3. **Health Checks**
   - Endpoint `/health` z checkiem SQL Server
   - Pakiet `AspNetCore.HealthChecks.SqlServer`

### ⚠️ Naprawione problemy dyskusyjne

4. **LocalFileStorageService — bezpieczeństwo**
   - Konfigurowalny `basePath` z `FileStorage:BasePath` w appsettings
   - Sanityzacja nazw plików (`Path.GetFileName` + usunięcie invalid chars)
   - Weryfikacja czy wynikowa ścieżka mieści się w upload directory (path traversal prevention)

5. **Chatbot auth**
   - `POST /api/chatbot/ask` wymaga JWT auth lub `X-Session-Id` header
   - Zapobiega anonimowemu nadużyciu klucza Gemini

6. **Soft Delete z GlobalQueryFilters**
   - `DeletedAt` w `BaseEntity` — wspólne dla wszystkich encji
   - `HasQueryFilter(e => e.DeletedAt == null)` na Item, Category, Order, Cart, Payment, ItemPhoto
   - `Repository.DeleteAsync` ustawia `DeletedAt` zamiast fizycznego usuwania
   - Auto `UpdatedAt` w `SaveChangesAsync` override

7. **Serilog structured logging**
   - Konsola z formatowanym outputem + pliki rotowane dziennie
   - Request logging z `RequestHost` i `UserAgent`
   - Bootstrap logger dla błędów przy starcie
   - Konfiguracja przez `Serilog` sekcję w appsettings

---

## Role i uprawnienia

| Rola  | Co może robić |
|-------|---------------|
| Brak (gość) | Przeglądać ogłoszenia, dodawać do koszyka (wymaga `POST /api/cart/session`) |
| `User` | Tworzyć/edytować własne ogłoszenia, składać zamówienia, płacić, zarządzać kontem |
| `Admin` | Wszystko powyżej + ban/timeout/role użytkowników, statusy zamówień, kategorie |

---

## Technologie

- **ASP.NET Core 8** — Web API
- **Entity Framework Core 8** — ORM + SQL Server + GlobalQueryFilters
- **ASP.NET Core Identity** — Autentykacja (JWT + refresh tokens)
- **FluentValidation** — Walidacja DTO
- **Serilog** — Structured logging (konsola + pliki)
- **System.Threading.RateLimiting** — Rate limiting (.NET 8 built-in)
- **AspNetCore.HealthChecks.SqlServer** — Health checks
- **xUnit + NSubstitute + FluentAssertions** — Testy jednostkowe
- **Testcontainers + WebApplicationFactory** — Testy integracyjne
- **NetArchTest** — Testy architektury
- **Przelewy24 API** — Płatności (sandbox)
- **Google Gemini** — Chatbot AI
- **Docker + Docker Compose** — Konteneryzacja (backend + frontend + MSSQL)

---

## Frontend — React SPA

### Stack technologiczny

| Technologia | Rola |
|------------|------|
| React 18 + TypeScript (strict) | UI framework |
| Vite | Bundler i dev server |
| React Router v6 | Client-side routing z `createBrowserRouter` |
| TanStack Query v5 | Fetching, caching, mutacje, invalidation |
| Zustand | Globalny state (auth, koszyk UI) |
| React Hook Form + Zod | Formularze z walidacją schematów |
| Axios | HTTP client z interceptorami JWT |
| Tailwind CSS v3 | Styling (ciepła paleta marketplace) |
| Radix UI | Prymitywy dostępności (Select, Label) |
| Lucide React | Ikony |
| Vitest + jsdom | Testy jednostkowe |
| class-variance-authority | Warianty komponentów UI |

### Struktura frontendu

```
shopapp-frontend/
├── public/
├── src/
│   ├── api/                    # Warstwa integracji z backendem
│   │   ├── client.ts           # Axios instance + interceptory JWT (refresh queue)
│   │   ├── auth.ts             # login, register, refresh, logout, changePassword
│   │   ├── items.ts            # CRUD ogłoszeń + getMyItems
│   │   ├── cart.ts             # addItem, updateItem, removeItem, clear
│   │   ├── orders.ts           # create, getOrders, getOrder
│   │   ├── categories.ts       # CRUD kategorii
│   │   ├── payments.ts         # initiate, getStatus
│   │   ├── chatbot.ts          # ask
│   │   └── admin.ts            # getUsers, ban/unban, roles, deleteUser + usersApi (me)
│   ├── components/
│   │   ├── ui/                 # Komponenty bazowe (Button, Input, Card, Badge, Select, Skeleton, Label, Textarea)
│   │   ├── Navbar.tsx          # Sticky navbar z search, cart badge, user menu, mobile hamburger
│   │   ├── Footer.tsx          # Stopka
│   │   ├── CartDrawer.tsx      # Wysuwany panel koszyka z prawej strony
│   │   ├── Pagination.tsx      # Numeryczna paginacja z ellipsis
│   │   ├── LoadingSpinner.tsx  # Spinner z Loader2 icon
│   │   ├── ErrorBoundary.tsx   # Class component z friendly error page
│   │   └── ProtectedRoute.tsx  # ProtectedRoute (auth) + AdminRoute (role check)
│   ├── hooks/
│   │   ├── useAuth.ts          # useLogin, useRegister, useLogout, useChangePassword
│   │   ├── useCart.ts          # useCart, useAddToCart, useUpdateCartItem, useRemoveCartItem
│   │   ├── useItems.ts         # useItems, useItem, useMyItems, useCreateItem, useUpdateItem, useDeleteItem
│   │   ├── useOrders.ts        # useOrders, useOrder, useCreateOrder (z 409 conflict handling)
│   │   ├── usePayments.ts      # useInitiatePayment (redirect), usePaymentStatus (polling 3s)
│   │   ├── useCategories.ts    # useCategories (staleTime 10min)
│   │   └── useDebounce.ts      # Generic debounce hook
│   ├── pages/
│   │   ├── HomePage.tsx        # Hero + search + kategorie + 8 najnowszych ogłoszeń
│   │   ├── ItemsPage.tsx       # Sidebar filtrów + siatka + paginacja (URL params sync)
│   │   ├── ItemDetailPage.tsx  # Zdjęcie + szczegóły + "Dodaj do koszyka"
│   │   ├── ItemCreatePage.tsx  # Formularz nowego ogłoszenia (react-hook-form + zod)
│   │   ├── ItemEditPage.tsx    # Edycja ogłoszenia z prefill
│   │   ├── CartPage.tsx        # Lista pozycji + quantity stepper + podsumowanie
│   │   ├── CheckoutPage.tsx    # Dane dostawy + podsumowanie zamówienia
│   │   ├── OrdersPage.tsx      # Lista zamówień z kolorowymi badge'ami statusu
│   │   ├── OrderDetailPage.tsx # Szczegóły + przycisk "Zapłać przez P24"
│   │   ├── PaymentReturnPage.tsx # Strona powrotu z P24 (polling statusu)
│   │   ├── LoginPage.tsx       # Centered card + walidacja Zod
│   │   ├── RegisterPage.tsx    # 5 pól + hasło mismatch validation
│   │   ├── ProfilePage.tsx     # Dane użytkownika + moje ogłoszenia (edit/delete)
│   │   ├── NotFoundPage.tsx    # 404 z CTA
│   │   └── admin/
│   │       ├── AdminUsersPage.tsx      # Tabela użytkowników, ban/unban/delete
│   │       └── AdminCategoriesPage.tsx # CRUD kategorii inline
│   ├── stores/
│   │   ├── authStore.ts        # Zustand persist: user, tokens, isAuthenticated, isAdmin()
│   │   └── cartStore.ts        # Zustand: sessionId (guest cart), isOpen (drawer)
│   ├── types/
│   │   └── api.ts              # TypeScript interfaces 1:1 z DTO backendu + enums
│   ├── lib/
│   │   ├── utils.ts            # cn(), formatPrice(PLN), formatDate(pl-PL)
│   │   └── queryClient.ts      # TanStack Query konfiguracja (5min stale, toast on error)
│   ├── layouts/
│   │   └── PageLayout.tsx      # Navbar + Outlet + Footer + CartDrawer
│   ├── router.tsx              # createBrowserRouter z lazy loading (Suspense)
│   ├── App.tsx                 # QueryClientProvider + RouterProvider + Toaster
│   ├── main.tsx                # ReactDOM entry point
│   └── test/
│       ├── setup.ts            # @testing-library/jest-dom
│       ├── utils.test.ts       # formatPrice, formatDate, cn (7 testów)
│       └── authStore.test.ts   # authStore CRUD + isAdmin (6 testów)
├── Dockerfile                  # Multi-stage: node:20-alpine build → nginx:alpine runtime
├── nginx.conf                  # SPA routing, /api/ proxy, gzip, cache immutable
├── vite.config.ts              # React plugin, /api dev proxy
├── vitest.config.ts            # jsdom environment, globals
├── tailwind.config.ts          # Sora display font, marketplace color palette
├── tsconfig.json               # Strict mode, @/ path alias
└── package.json                # Scripts: dev, build, preview, lint, test
```

### Kluczowe decyzje frontendowe

| Decyzja | Dlaczego |
|---------|----------|
| **Zustand zamiast Redux** | Minimalistyczny, brak boilerplate, wystarczający dla auth + cart UI state |
| **TanStack Query zamiast useState** | Server state management z automatycznym cache, invalidation, retry |
| **Zod + react-hook-form** | Type-safe walidacja schematów, zero re-renderów |
| **Lazy loading stron** | Każda strona to osobny chunk (~1-5KB gzipped), initial bundle 122KB gzipped |
| **Nginx proxy** | Brak CORS issues w produkcji, jeden origin dla frontend + API |
| **Template literals replaced with string concat** | Kompatybilność z PowerShell/Python toolingiem podczas generacji |

### Integracje frontend ↔ backend

- **JWT Refresh Queue** — Axios interceptor obsługuje race condition gdy wiele requestów zwróci 401 jednocześnie; `isRefreshing` + `failedQueue` pattern
- **Guest Cart** — `X-Session-Id` header generowany kliencko (UUID v4), wysyłany w interceptorze dla endpointów `/cart/*` gdy user niezalogowany
- **409 Conflict Handling** — `useCreateOrder` łapie 409, invaliduje `['cart']` i `['items']`, wyświetla toast "Produkt się wyprzedał"
- **Payment Redirect** — `useInitiatePayment` → `window.location.href = redirectUrl` (pełny redirect do P24, nie iframe)
- **Payment Polling** — `usePaymentStatus` z `refetchInterval: 3000ms` na stronie powrotu z P24

### Uruchomienie

```bash
# Development (bez Dockera)
cd shopapp-frontend
npm install
npm run dev          # http://localhost:5173 z proxy /api → localhost:8080

# Testy
npm test             # 13 testów (utils + authStore)

# Build produkcyjny
npm run build        # tsc --noEmit && vite build → dist/

# Docker (cały stack)
docker compose up --build -d
# Frontend: http://localhost:3000
# API:      http://localhost:8080
# MSSQL:    localhost:1433
```

---

## Architecture Decision Records (ADR)

Kluczowe decyzje architektoniczne udokumentowane w formacie Nygard:

| ADR | Tytuł | Status |
|-----|-------|--------|
| [001](docs/adr/001-iunitofwork-in-core.md) | IUnitOfWork w warstwie Core | Accepted |
| [002](docs/adr/002-applicationuser-soft-delete.md) | Soft Delete dla ApplicationUser (IdentityUser) | Accepted |

ADR opisują *dlaczego* podjęto dane decyzje — bezcenne przy onboardingu lub po 6 miesiącach przerwy.

---

## Notatki do przyszłego rozwoju

- **MediatR/CQRS** — rozważyć przy 5+ metodach publicznych w serwisach (aktualnie OK)
- **Redis dla koszyka** — sesje gościa przechowywane w MSSQL via EF Core; brak TTL/cleanup; migracja na Redis przed produkcją
- **OpenTelemetry** — TraceId korelacja requestów (po wdrożeniu)
- **Outbox Pattern** — pełny wzorzec z tabelą OutboxEvents dla krytycznych operacji (zamiast ProcessedAt)
- **CancellationToken audit** — ✅ wykonany; repozytoria i serwisy (Item, Cart, Order, Category, Payment) propagują `ct` poprawnie do EF Core; `UserManager`/`SignInManager` (Auth, User, AdminUser serwisy) **nie przyjmują** CancellationToken — to udokumentowane ograniczenie ASP.NET Core Identity, nie błąd projektu
- **ApplicationUser email uniqueness** — soft-deleted users nadal zajmują email w DB; przy re-rejestracji potrzebna dedykowana logika (np. suffix na email deleted usera, lub `IgnoreQueryFilters()`)
- **IUnitOfWork w Core** — pragmatyczna decyzja udokumentowana w [ADR-001](docs/adr/001-iunitofwork-in-core.md); przy wzroście do 3+ konsumentów rozważyć Domain Events
