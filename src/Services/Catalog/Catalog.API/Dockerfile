FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/Catalog/Catalog.API/Catalog.API.csproj", "src/Services/Catalog/Catalog.API/"]
COPY ["src/Services/Catalog/Catalog.Application/Catalog.Application.csproj", "src/Services/Catalog/Catalog.Application/"]
COPY ["src/Services/Catalog/Catalog.Domain/Catalog.Domain.csproj", "src/Services/Catalog/Catalog.Domain/"]
COPY ["src/Services/Catalog/Catalog.Infrastructure/Catalog.Infrastructure.csproj", "src/Services/Catalog/Catalog.Infrastructure/"]
COPY ["src/Services/Catalog/Catalog.Persistence/Catalog.Persistence.csproj", "src/Services/Catalog/Catalog.Persistence/"]
RUN dotnet restore "src/Services/Catalog/Catalog.API/Catalog.API.csproj"

COPY . .
RUN dotnet publish "src/Services/Catalog/Catalog.API/Catalog.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Catalog.API.dll"]
