FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/Inventory/Inventory.API/Inventory.API.csproj", "src/Services/Inventory/Inventory.API/"]
COPY ["src/Services/Inventory/Inventory.Application/Inventory.Application.csproj", "src/Services/Inventory/Inventory.Application/"]
COPY ["src/Services/Inventory/Inventory.Domain/Inventory.Domain.csproj", "src/Services/Inventory/Inventory.Domain/"]
COPY ["src/Services/Inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj", "src/Services/Inventory/Inventory.Infrastructure/"]
COPY ["src/Services/Inventory/Inventory.Persistence/Inventory.Persistence.csproj", "src/Services/Inventory/Inventory.Persistence/"]
RUN dotnet restore "src/Services/Inventory/Inventory.API/Inventory.API.csproj"

COPY . .
RUN dotnet publish "src/Services/Inventory/Inventory.API/Inventory.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "Inventory.API.dll"]
