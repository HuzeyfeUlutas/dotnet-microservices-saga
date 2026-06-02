FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/Order/Order.API/Order.API.csproj", "src/Services/Order/Order.API/"]
COPY ["src/Services/Order/Order.Application/Order.Application.csproj", "src/Services/Order/Order.Application/"]
COPY ["src/Services/Order/Order.Domain/Order.Domain.csproj", "src/Services/Order/Order.Domain/"]
COPY ["src/Services/Order/Order.Infrastructure/Order.Infrastructure.csproj", "src/Services/Order/Order.Infrastructure/"]
COPY ["src/Services/Order/Order.Persistence/Order.Persistence.csproj", "src/Services/Order/Order.Persistence/"]
RUN dotnet restore "src/Services/Order/Order.API/Order.API.csproj"

COPY . .
RUN dotnet publish "src/Services/Order/Order.API/Order.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Order.API.dll"]
