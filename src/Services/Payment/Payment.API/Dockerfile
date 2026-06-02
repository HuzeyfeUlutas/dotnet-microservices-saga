FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/Payment/Payment.API/Payment.API.csproj", "src/Services/Payment/Payment.API/"]
COPY ["src/Services/Payment/Payment.Application/Payment.Application.csproj", "src/Services/Payment/Payment.Application/"]
COPY ["src/Services/Payment/Payment.Domain/Payment.Domain.csproj", "src/Services/Payment/Payment.Domain/"]
COPY ["src/Services/Payment/Payment.Infrastructure/Payment.Infrastructure.csproj", "src/Services/Payment/Payment.Infrastructure/"]
COPY ["src/Services/Payment/Payment.Persistence/Payment.Persistence.csproj", "src/Services/Payment/Payment.Persistence/"]
RUN dotnet restore "src/Services/Payment/Payment.API/Payment.API.csproj"

COPY . .
RUN dotnet publish "src/Services/Payment/Payment.API/Payment.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "Payment.API.dll"]
