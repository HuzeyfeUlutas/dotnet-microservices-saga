FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/Notification/Notification.API/Notification.API.csproj", "src/Services/Notification/Notification.API/"]
COPY ["src/Services/Notification/Notification.Application/Notification.Application.csproj", "src/Services/Notification/Notification.Application/"]
COPY ["src/Services/Notification/Notification.Domain/Notification.Domain.csproj", "src/Services/Notification/Notification.Domain/"]
COPY ["src/Services/Notification/Notification.Infrastructure/Notification.Infrastructure.csproj", "src/Services/Notification/Notification.Infrastructure/"]
COPY ["src/Services/Notification/Notification.Persistence/Notification.Persistence.csproj", "src/Services/Notification/Notification.Persistence/"]
RUN dotnet restore "src/Services/Notification/Notification.API/Notification.API.csproj"

COPY . .
RUN dotnet publish "src/Services/Notification/Notification.API/Notification.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Notification.API.dll"]
