# ── Build stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore (layer-cached unless .csproj changes)
COPY ["ShopApp.API/ShopApp.API.csproj", "ShopApp.API/"]
COPY ["ShopApp.Application/ShopApp.Application.csproj", "ShopApp.Application/"]
COPY ["ShopApp.Core/ShopApp.Core.csproj", "ShopApp.Core/"]
COPY ["ShopApp.Infrastructure/ShopApp.Infrastructure.csproj", "ShopApp.Infrastructure/"]
RUN dotnet restore "ShopApp.API/ShopApp.API.csproj"

# Copy everything else and publish
COPY . .
WORKDIR "/src/ShopApp.API"
RUN dotnet publish "ShopApp.API.csproj" -c Release -o /app/publish --no-restore

# ── Runtime stage ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

# Create upload directory
RUN mkdir -p /app/wwwroot/uploads

EXPOSE 8080
ENTRYPOINT ["dotnet", "ShopApp.API.dll"]
