FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/ApiGateways/Marketplace.ApiGateway/Marketplace.ApiGateway.csproj", "src/ApiGateways/Marketplace.ApiGateway/"]
RUN dotnet restore "src/ApiGateways/Marketplace.ApiGateway/Marketplace.ApiGateway.csproj"

COPY . .
RUN dotnet publish "src/ApiGateways/Marketplace.ApiGateway/Marketplace.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Marketplace.ApiGateway.dll"]
