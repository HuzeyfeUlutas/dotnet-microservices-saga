# Service Skeleton Template

This document is the fixed source of truth for creating a new empty service skeleton.

Use this template when creating services such as Inventory, Payment, Order, Notification, Shipment, Basket, etc.

Do not copy the current Catalog implementation when creating a new service. Catalog may evolve over time and may contain business logic, entities, handlers, endpoints, database code, or other implementation details. This template represents the original empty service structure that new services must follow.

## Service Location

Create every service under:

```text
src/Services/{ServiceName}/
```

Example:

```text
src/Services/Inventory/
```

## Required Project Structure

Each service must contain exactly these projects during initial scaffolding:

```text
src/Services/{ServiceName}/
  {ServiceName}.API/
  {ServiceName}.Application/
  {ServiceName}.Domain/
  {ServiceName}.Infrastructure/
  {ServiceName}.Persistence/
```

Example for Inventory:

```text
src/Services/Inventory/
  Inventory.API/
  Inventory.Application/
  Inventory.Domain/
  Inventory.Infrastructure/
  Inventory.Persistence/
```

## Project SDKs

The API project must use the ASP.NET Core Web SDK:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
```

All other projects must use the standard .NET SDK:

```xml
<Project Sdk="Microsoft.NET.Sdk">
```

## Common Project Settings

Every project must use:

```xml
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
</PropertyGroup>
```

## API Project Packages

The `{ServiceName}.API` project must include these package references:

```xml
<ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.6" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.4.0" />
</ItemGroup>
```

Do not add other NuGet packages during initial scaffolding unless the user explicitly asks for them.

## Project References

Use this dependency direction exactly.

### `{ServiceName}.API`

References:

```text
{ServiceName}.Application
{ServiceName}.Infrastructure
{ServiceName}.Persistence
```

Example:

```xml
<ItemGroup>
    <ProjectReference Include="..\Inventory.Application\Inventory.Application.csproj" />
    <ProjectReference Include="..\Inventory.Infrastructure\Inventory.Infrastructure.csproj" />
    <ProjectReference Include="..\Inventory.Persistence\Inventory.Persistence.csproj" />
</ItemGroup>
```

### `{ServiceName}.Application`

References:

```text
{ServiceName}.Domain
```

Example:

```xml
<ItemGroup>
    <ProjectReference Include="..\Inventory.Domain\Inventory.Domain.csproj" />
</ItemGroup>
```

### `{ServiceName}.Domain`

Has no project references.

### `{ServiceName}.Infrastructure`

References:

```text
{ServiceName}.Application
```

Example:

```xml
<ItemGroup>
    <ProjectReference Include="..\Inventory.Application\Inventory.Application.csproj" />
</ItemGroup>
```

If the user later asks for messaging, this project is the default place to configure `MassTransit`, broker connectivity, consumers, and Application-to-bus adapters.

### `{ServiceName}.Persistence`

References:

```text
{ServiceName}.Application
```

Example:

```xml
<ItemGroup>
    <ProjectReference Include="..\Inventory.Application\Inventory.Application.csproj" />
</ItemGroup>
```

If the user later asks for outbox support, keep the outbox tables and `DbContext` configuration in this project.

## Initial API Files

The API project must include:

```text
Program.cs
appsettings.json
appsettings.Development.json
Properties/launchSettings.json
```

The initial `Program.cs` must stay minimal:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
```

## Initial Content Rules

During initial service scaffolding:

- Keep `{ServiceName}.Domain` empty.
- Keep `{ServiceName}.Application` empty.
- Keep `{ServiceName}.Infrastructure` empty.
- Keep `{ServiceName}.Persistence` empty.
- Do not create entities.
- Do not create value objects.
- Do not create aggregates.
- Do not create repositories.
- Do not create DbContexts.
- Do not create migrations.
- Do not create controllers.
- Do not create endpoints.
- Do not create handlers.
- Do not create commands or queries.
- Do not create DTOs.
- Do not create validators.
- Do not add business logic.
- Do not add service-specific folders unless the user explicitly asks for them.

The goal is to create only the empty Clean Architecture project skeleton.

## Solution Registration

After creating the projects, add all new projects to:

```text
MarketplaceOrderPlatform.sln
```

The new service projects must appear in the solution and must build successfully.

## Naming Rules

Use the service name consistently in folder names, project names, namespaces, and project references.

Examples:

```text
Inventory.API
Inventory.Application
Inventory.Domain
Inventory.Infrastructure
Inventory.Persistence
```

Do not use `Catalog` names, namespaces, or references in a newly generated service.

## Important Reminder

This file is the source of truth for new service scaffolding. If the current Catalog service differs from this document, ignore Catalog and follow this document.
