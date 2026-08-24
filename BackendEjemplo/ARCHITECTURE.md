# ARCHITECTURE.md — Guía de generación y auditoría (vibe coding)

Este documento es un **manual operativo**, no una descripción: está pensado para que un LLM (u otro dev) genere código nuevo siguiendo exactamente los mismos patrones que el resto del proyecto, y para que ese código se pueda **auditar** contra una lista de verificación concreta antes de darlo por bueno.

Si buscás una descripción general del proyecto (stack, bounded contexts existentes, cómo correrlo), ver [`README.md`](./README.md). Este archivo asume que ya lo leíste. Para saber qué tipos de dato necesitan `?` en un DTO con `[Required]` (trampa real, encontrada 10 veces en este proyecto), ver [`VALIDATION.md`](./VALIDATION.md) antes de escribir un `Save*Resource` nuevo.

> **Regla de oro**: antes de escribir un bounded context nuevo, abrí uno existente equivalente (`OneToManyExample` para 1:N, `ManyToManyExample` para N:N, `OneToOneExample` para 1:1) y usalo como referencia línea por línea. Las plantillas de abajo son genéricas; el código real siempre gana si hay una divergencia.

> **`BackendEjemplo.Tests` es estrictamente opcional — nunca crearlo por defecto.** Esta plantilla también se usa para agregar endpoints nuevos sobre APIs ya existentes que todavía no tienen proyecto de test (la organización todavía no adoptó testing como práctica general). Un LLM que siga esta guía **NO debe** crear `BackendEjemplo.Tests/` ni agregar un `<Entity>ServiceTests.cs` a menos que el usuario lo pida explícitamente en esa conversación — ni el checklist de la sección 1 (paso 16) ni el checklist de auditoría de la sección 6 son excusa para crearlo sin que te lo pidan. Si el proyecto de test **ya existe** en el repo, sí corresponde sumarle el test del bounded context nuevo siguiendo la sección 8 — la opcionalidad aplica a *crear el proyecto*, no a mantenerlo una vez que ya existe.

> **Nombre del proyecto: nunca asumir "BackendEjemplo".** Ese nombre es específico de este repo de ejemplo — en toda la sección 0 (bootstrap) es un placeholder, no una convención a imponer.
> - **Proyecto nuevo** (arrancando desde cero, sección 0): antes de correr cualquier comando `dotnet new`, preguntarle al usuario cómo se debe llamar la solución/proyecto/namespace raíz. No elegirlo por tu cuenta ni copiar "BackendEjemplo" de esta guía.
> - **Proyecto ya existente** (sección 1 en adelante): respetar el nombre/namespace real del proyecto tal como está — nunca renombrarlo ni introducir "BackendEjemplo" en un namespace nuevo solo porque así aparece en esta guía.

Si el proyecto **ya existe** (hay al menos un bounded context armado), saltar directo a la sección 1. Si estás arrancando **desde cero**, empezar por la sección 0.

---

## 0. Bootstrap: crear el proyecto desde cero

Esta sección arma toda la infraestructura de `Shared/` — la base que después usa cualquier bounded context — antes de que exista ninguna entidad de negocio. Se hace una sola vez por proyecto.

### 0.1. Crear la solución y el proyecto

**Antes de correr nada de esto, preguntarle al usuario cómo se debe llamar el proyecto.** Todo lo que sigue en la sección 0 (y los namespaces `BackendEjemplo.*` de las secciones 1-9) usa "BackendEjemplo" como placeholder porque así se llama este repo de ejemplo — al bootstrapear un proyecto nuevo real, reemplazar ese nombre por el que haya indicado el usuario en todos los comandos, rutas de carpeta y namespaces.

```bash
dotnet new sln -n <NombreDelProyecto>
dotnet new webapi -o <NombreDelProyecto> --use-controllers
dotnet sln add <NombreDelProyecto>/<NombreDelProyecto>.csproj
```

`--use-controllers` es clave: este proyecto usa **controllers** (`ControllerBase` + atributos de ruta), no Minimal APIs — así el patrón Repository/Service/Controller tiene dónde vivir.

### 0.2. `BackendEjemplo.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.10" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.10">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.10" />
    <PackageReference Include="Microsoft.OpenApi" Version="2.11.0" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.3" />
    <PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="7.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore.SwaggerUI" Version="10.2.3" />
  </ItemGroup>

</Project>
```

`Microsoft.EntityFrameworkCore.Design` es lo que habilita `dotnet-ef migrations add`; sin él, el comando de la sección 5 falla. `Swashbuckle.AspNetCore.SwaggerUI` es solo el visor — la generación del documento OpenAPI la hace `Microsoft.AspNetCore.OpenApi` (nativo de ASP.NET Core), no Swashbuckle.

### 0.3. Herramienta local `dotnet-ef`

```bash
dotnet new tool-manifest   # crea .config/dotnet-tools.json en la raíz de la solución
dotnet tool install dotnet-ef --version 10.0.10
dotnet tool restore
```

### 0.4. `appsettings.json` / `appsettings.Development.json`

```json
// appsettings.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": { "Microsoft": "Warning", "Microsoft.AspNetCore": "Warning", "System": "Warning" }
    },
    "WriteTo": [
      { "Name": "Console", "Args": { "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}" } },
      { "Name": "File", "Args": { "path": "logs/apiportal-.log", "rollingInterval": "Day", "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}" } }
    ],
    "Enrich": [ "FromLogContext" ]
  },
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "ConnectionStrings": {
    // Plantilla sin password — el valor real va en user-secrets, nunca acá (ver README.md).
    "DefaultConnection": "host=localhost;port=5432;username=postgres;password=;database=postgres"
  },
  "AllowedHosts": "*",
  // Orígenes permitidos para CORS (ej. la URL del frontend Angular en producción).
  // Vacío = permitir cualquier origen (pensado solo para desarrollo local, ver 0.6).
  "Cors": {
    "AllowedOrigins": []
  }
}
```

```json
// appsettings.Development.json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } }
}
```

Después, inicializar user-secrets y cargar el password real (ver README.md sección "Configurar la cadena de conexión"):

```bash
dotnet user-secrets init --project BackendEjemplo/BackendEjemplo.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "host=localhost;port=5432;username=postgres;password=<tu-password>;database=postgres" --project BackendEjemplo/BackendEjemplo.csproj
```

### 0.5. `Shared/` — infraestructura común (crear en este orden)

#### `Shared/Domain/Repositories/Page.cs`

```csharp
namespace BackendEjemplo.Shared.Domain.Repositories
{
    public class Page<TEntity> where TEntity : class
    {
        public IEnumerable<TEntity> Data { get; set; }
        public int PageIndex { get; set; }       // Página actual (índice base 0)
        public int PageSize { get; set; }        // Tamaño de página
        public int TotalRecords { get; set; }
    }
}
```

#### `Shared/Domain/Services/Communication/BasePageRequest.cs`

```csharp
namespace BackendEjemplo.Shared.Domain.Services.Communication
{
    public class BasePageRequest
    {
        public int PageIndex { get; set; } = 0;
        public int PageSize { get; set; } = 10;
    }
}
```

#### `Shared/Domain/Services/Communication/PageResponse.cs`

```csharp
namespace BackendEjemplo.Shared.Domain.Services.Communication
{
    public class PageResponse<TEntity> where TEntity : class
    {
        public IEnumerable<TEntity> Data { get; set; } // Los datos paginados
        public int PageIndex { get; set; }       // Página actual (índice base 0)
        public int PageSize { get; set; }        // Tamaño de página
        public int TotalRecords { get; set; }   // Total de registros
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize); // Total de páginas calculado
    }
}
```

`Page<T>` (namespace `Repositories`) y `PageResponse<T>` (namespace `Services.Communication`) son deliberadamente dos clases distintas con la misma forma: `Page<T>` es el tipo interno que devuelve un `Repository`/`Service`, `PageResponse<T>` es el DTO que sale por la API. Mantenerlos separados evita que un cambio interno (ej. agregar un campo de auditoría a `Page<T>`) se filtre sin querer al contrato público.

#### `Shared/Mapping/PageMappings.cs`

```csharp
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.Shared.Mapping
{
    public static class PageMappings
    {
        public static PageResponse<TEntity> ToResponse<TEntity>(this Page<TEntity> page)
            where TEntity : class => new()
            {
                Data = page.Data,
                PageIndex = page.PageIndex,
                PageSize = page.PageSize,
                TotalRecords = page.TotalRecords
            };
    }
}
```

#### `Shared/Domain/Services/Communication/BaseResponse.cs`

```csharp
namespace BackendEjemplo.Shared.Domain.Services.Communication
{
    public class BaseResponse<TEntity> where TEntity : class
    {
        public string Message { get; set; }
        public TEntity Content { get; set; }
        public bool Success { get; set; }

        // Distingue un fallo por "no existe" (404) de uno por conflicto de negocio,
        // como intentar borrar un recurso que tiene otros recursos dependientes (409).
        public bool IsConflict { get; set; }

        public BaseResponse(string message, bool isConflict = false)
        {
            Success = false;
            Message = message;
            Content = default;
            IsConflict = isConflict;
        }

        public BaseResponse(TEntity resource)
        {
            Success = true;
            Message = string.Empty;
            Content = resource;
        }
    }
}
```

#### `Shared/Domain/Repositories/IUnitOfWork.cs`

```csharp
namespace BackendEjemplo.Shared.Domain.Repositories
{
    public interface IUnitOfWork
    {
        Task CompleteAsync(CancellationToken cancellationToken = default);
    }
}
```

#### `Shared/Domain/Repositories/IBaseRepository.cs`

```csharp
using System.Linq.Expressions;

namespace BackendEjemplo.Shared.Domain.Repositories
{
    public interface IBaseRepository<TEntity> where TEntity : class
    {
        Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        void Update(TEntity entity);
        void Remove(TEntity entity);
        Task<IEnumerable<TEntity>> ListAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken cancellationToken = default);
        Task<Page<TEntity>> ListPageAsync(
            int pageNumber,
            int recordsPerPage,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken cancellationToken = default);
    }
}
```

#### `Shared/Persistence/Context/AppDbContext.cs` (versión inicial, vacía de entidades)

```csharp
using BackendEjemplo.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BackendEjemplo.Shared.Persistence.Context
{
    public class AppDbContext: DbContext
    {
        // Cada bounded context agrega acá su(s) DbSet<T> (ver sección 2.7 / 1 paso 13).

        public AppDbContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cada bounded context agrega acá su configuración de entidades/relaciones
            // (ver sección 2.7). El orden importa: registrar las entidades ANTES de
            // llamar a UseSnakeCaseNamingConvention(), porque esa extensión recorre
            // los metadatos ya construidos por ModelBuilder.

            modelBuilder.UseSnakeCaseNamingConvention();

            // Concurrencia optimista "gratis": xmin es una columna de sistema que ya
            // existe en toda tabla de Postgres. Mapearla como concurrency token hace
            // que todo UPDATE/DELETE generado por EF incluya "AND xmin = @valorLeido";
            // si otra transacción ya modificó la fila entremedio, 0 filas se ven
            // afectadas y EF lanza DbUpdateConcurrencyException (el GlobalExceptionHandler
            // la traduce a 409). Se aplica a TODA entidad automáticamente vía este loop —
            // un bounded context nuevo la recibe gratis, sin tocar nada acá. Ver sección
            // 9 para el detalle de por qué el nombre de la propiedad shadow ("Version")
            // no importa y por qué la migración que esto genera no ejecuta DDL real.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property<uint>("Version")
                    .IsRowVersion();
            }

            //tratar las fechas en formato UTC
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                            v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc), // Al guardar
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc) // Al leer (Corrige el tipo a UTC)
                        ));
                    }
                }
            }
        }
    }
}
```

#### `Shared/Persistence/Repositories/UnitOfWork.cs`

```csharp
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Persistence.Context;

namespace BackendEjemplo.Shared.Persistence.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
```

#### `Shared/Persistence/Repositories/BaseRepository.cs`

```csharp
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BackendEjemplo.Shared.Persistence.Repositories
{
    public abstract class BaseRepository<TEntity>(AppDbContext context)
        : IBaseRepository<TEntity> where TEntity : class
    {
        protected readonly AppDbContext _context = context;
        protected const int MaxPageSize = 100;

        public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            await _context.Set<TEntity>().AddAsync(entity, cancellationToken);
        }

        protected IQueryable<TEntity> GetQuery(
            Expression<Func<TEntity, bool>>? filter,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy
            )
        {
            // 1. Preparar la consulta base con AsNoTracking para optimizar lecturas
            var query = _context.Set<TEntity>().AsNoTracking();

            // 2. Aplicar filtro si existe (afecta tanto al conteo como a los datos)
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // 3. Aplicar ordenamiento si existe
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            // 4. Devolver la consulta para su posterior uso
            return query;
        }

        public virtual async Task<IEnumerable<TEntity>> ListAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken cancellationToken = default)
        {
            var query = GetQuery(filter, orderBy);
            return await query.ToListAsync(cancellationToken);
        }

        public virtual async Task<Page<TEntity>> ListPageAsync(
            int pageIndex,
            int pageSize,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken cancellationToken = default)
        {
            // 1. Validaciones de paginación segura
            if (pageIndex < 0) pageIndex = 0;

            if (pageSize < 1) pageSize = 10;
            if (pageSize > MaxPageSize) pageSize = MaxPageSize;

            //2. Obtener la consulta
            var query = GetQuery(filter, orderBy);

            // 3. Contar el total de registros que cumplen el filtro.
            // No se puede paralelizar con Task.WhenAll: ambas consultas comparten la misma
            // instancia de DbContext, y EF Core no admite operaciones async concurrentes
            // sobre un mismo contexto (lanza "A second operation was started on this context...").
            var totalRecords = await query.CountAsync(cancellationToken);

            // 4. Aplicar paginación y obtener los datos
            var data = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new Page<TEntity>
            {
                Data = data,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }

        public virtual void Remove(TEntity entity)
        {
            _context.Set<TEntity>().Remove(entity);
        }

        public virtual void Update(TEntity entity)
        {
            _context.Set<TEntity>().Update(entity);
        }
    }
}
```

Si un bounded context necesita `Include()` en el listado paginado (entidades con navegación obligatoria, ver sección 3), su repositorio concreto hace `override` de `ListPageAsync` copiando este cuerpo y agregando el `Include` entre `.Take(pageSize)` y `.ToListAsync(...)` — nunca modificar este `BaseRepository` para eso, es intencionalmente agnóstico de relaciones.

#### `Shared/Extensions/StringExtensions.cs`

```csharp
namespace BackendEjemplo.Shared.Extensions
{
    public static class StringExtensions
    {
        public static string ToSnakeCase(this string text)
        {
            static IEnumerable<char> Convert(CharEnumerator e)
            {
                if (!e.MoveNext()) yield break;
                yield return char.ToLower(e.Current);
                while (e.MoveNext())
                {
                    if (char.IsUpper(e.Current))
                    {
                        yield return '_';
                        yield return char.ToLower(e.Current);
                    }
                    else
                    {
                        yield return e.Current;
                    }
                }
            }

            return new string(Convert(text.GetEnumerator()).ToArray());
        }
    }
}
```

#### `Shared/Extensions/ModelBuilderExtensions.cs`

```csharp
using Microsoft.EntityFrameworkCore;

namespace BackendEjemplo.Shared.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static void UseSnakeCaseNamingConvention(this ModelBuilder builder)
        {
            foreach (var entity in builder.Model.GetEntityTypes())
            {
                entity.SetTableName(entity.GetTableName().ToSnakeCase());
                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(property.GetColumnName().ToSnakeCase());
                }

                foreach (var key in entity.GetKeys())
                {
                    key.SetName(key.GetName().ToSnakeCase());
                }

                foreach (var foreignKey in entity.GetForeignKeys())
                {
                    foreignKey.SetConstraintName(foreignKey.GetConstraintName().ToSnakeCase());
                }

                foreach (var index in entity.GetIndexes())
                {
                    index.SetDatabaseName(index.GetDatabaseName().ToSnakeCase());
                }
            }
        }
    }
}
```

#### `Shared/Extensions/ControllerBaseExtensions.cs`

```csharp
using BackendEjemplo.Shared.Domain.Services.Communication;
using Microsoft.AspNetCore.Mvc;

namespace BackendEjemplo.Shared.Extensions
{
    public static class ControllerBaseExtensions
    {
        // Traduce un BaseResponse<T> fallido (Success == false) a un ProblemDetails
        // consistente con el resto de la API (mismo formato que ValidationProblem()
        // y que el 500 del GlobalExceptionHandler): 409 si es un conflicto de negocio
        // (IsConflict == true), o el status code que indique el caller — típicamente
        // 404 para "no existe" en GetById/Put/Delete, 400 para reglas de creación
        // (ej. FK a un padre inexistente).
        public static ObjectResult ToProblem<TEntity>(this ControllerBase controller, BaseResponse<TEntity> response, int failureStatusCode)
            where TEntity : class
        {
            var statusCode = response.IsConflict ? StatusCodes.Status409Conflict : failureStatusCode;
            return controller.Problem(detail: response.Message, statusCode: statusCode);
        }
    }
}
```

**Todo error de la API es un `ProblemDetails`, sin excepción** — validación (`ValidationProblem`), negocio (`ToProblem` de arriba) y no controlado (`GlobalExceptionHandler` de abajo) comparten el mismo shape JSON y, al pasar todos por la misma infraestructura de `AddProblemDetails()`, el mismo `traceId` de correlación. Antes de este helper, los errores de negocio devolvían un string plano (`return NotFound(result.Message)`) — evitá volver a ese patrón.

#### `Shared/Middleware/GlobalExceptionHandler.cs`

```csharp
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BackendEjemplo.Shared.Middleware
{
    public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            // El cliente cancelo el request (cerro la pestania, timeout, etc.) - no es un error real,
            // asi que no lo logueamos como tal y dejamos que ASP.NET Core simplemente corte la conexion.
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
                return true;

            // Concurrencia optimista (xmin, ver AppDbContext): otra request modificó o
            // borró la misma fila entre que esta la leyó y la guardó. Es una condición
            // esperable bajo uso concurrente, no un bug — se loguea como warning (no
            // error) y se traduce a 409 en vez de caer en el 500 genérico de abajo.
            if (exception is DbUpdateConcurrencyException)
            {
                logger.LogWarning(exception, "Conflicto de concurrencia en {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

                httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

                return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    Exception = exception,
                    ProblemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Conflict",
                        Detail = "El recurso fue modificado o eliminado por otro proceso mientras se procesaba esta solicitud. Volvé a cargarlo e intentá de nuevo.",
                        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10"
                    }
                });
            }

            logger.LogError(exception, "Error no controlado en {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

            // TryWriteAsync pasa por el mismo pipeline de ProblemDetails que usan
            // Problem()/ValidationProblem() en los controllers (registrado via
            // AddProblemDetails() en Program.cs) — mismo formato, mismo traceId,
            // en vez de armar el JSON a mano y arriesgarse a que se desincronice.
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Ocurrió un error inesperado.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1"
                }
            });
        }
    }
}
```

### 0.6. `Program.cs` (composition root, sin bounded contexts todavía)

```csharp
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Middleware;
using BackendEjemplo.Shared.Persistence.Context;
using BackendEjemplo.Shared.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Sin esto, los enums (OrderState, EnrollmentState, etc.) viajan como el
        // entero subyacente ("state": 0) tanto al leer como al escribir, obligando
        // a todo consumidor a hardcodear el mapeo número <-> nombre. Con el
        // converter, la API lee y escribe el nombre del enum ("state": "Pending").
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// AddOpenApi (más abajo) genera el schema a partir de Http.Json.JsonOptions, NO de
// Mvc.JsonOptions configurado arriba — son dos opciones independientes. Sin esto,
// el runtime ya serializa los enums como string pero el documento OpenAPI/Swagger
// seguiría documentándolos como "type: integer", desincronizando la doc del comportamiento real.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    // El generador de OpenAPI 3.1 documenta los parametros de ruta/query como
    // "type": ["integer", "string"] con un pattern, ya que en la URL siempre viajan
    // como texto. El Swagger UI de Swashbuckle no valida bien ese esquema en
    // parametros requeridos y bloquea el "Execute" con "Required field is not provided".
    // Se simplifica el esquema a un "integer" plano para que sea compatible.
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        foreach (var parameter in operation.Parameters ?? [])
        {
            if (parameter.Schema is Microsoft.OpenApi.OpenApiSchema schema && schema.Type is not null &&
                schema.Type.Value.HasFlag(Microsoft.OpenApi.JsonSchemaType.Integer))
            {
                schema.Type = Microsoft.OpenApi.JsonSchemaType.Integer;
                schema.Pattern = null;
            }
        }
        return Task.CompletedTask;
    });
});
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            // Cors:AllowedOrigins configurado (ej. el dominio del frontend Angular en
            // producción): solo esos orígenes pueden llamar a la API.
            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
        else
        {
            // Sin orígenes configurados (típico en desarrollo local, con el frontend
            // corriendo en otro puerto): permitir cualquier origen. No dejar así en
            // producción — configurar Cors:AllowedOrigins en el appsettings del entorno.
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString)
        .LogTo(Log.Information, [DbLoggerCategory.Database.Command.Name]);

    // EnableSensitiveDataLogging vuelca valores reales de parametros en el log
    // (potencialmente sensibles) y EnableDetailedErrors agrega overhead extra:
    // solo tienen sentido en desarrollo, nunca en produccion.
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    }
});

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// === Cada bounded context registra acá sus IXRepository/XRepository e IXService/XService ===
// builder.Services.AddScoped<IFooRepository, FooRepository>();
// builder.Services.AddScoped<IFooService, FooService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

bool databaseConnected = false;

try
{
    using (var scope = app.Services.CreateScope())
    using (var context = scope.ServiceProvider.GetService<AppDbContext>())
    {
        context.Database.Migrate();
    }
    databaseConnected = true;
}
catch (Exception ex)
{
    Console.WriteLine($"Error while connecting to database: {ex.Message}");
}

if (databaseConnected)
{
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            // ApplicationName es el nombre del ensamblado (el nombre real del proyecto,
            // sea cual sea) — nunca hardcodear acá el nombre de un proyecto puntual.
            options.SwaggerEndpoint("/openapi/v1.json", $"{builder.Environment.ApplicationName} API");
        });
        app.MapGet("/swagger/v1/swagger.json", () => Results.Redirect("/openapi/v1.json")).ExcludeFromDescription();
    }
    else
    {
        // HSTS fuerza HTTPS via header y solo tiene sentido fuera de desarrollo
        // (en local/HTTP molesta al navegador con cache de redirecciones).
        app.UseHsts();
    }

    app.UseExceptionHandler();

    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    });

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseCors("Default");

    app.UseRouting();

    app.MapControllers();

    app.Run();
}
```

Nota sobre el arranque condicionado a `databaseConnected`: si la conexión a la base falla, la app **no** monta el pipeline HTTP (no hay `app.Run()` en el `else` implícito) — falla rápido y explícito en el log de consola, en vez de levantar una API que después tira 500 en cada request. Es una decisión de diseño válida para un ejercicio/demo; en un servicio productivo real normalmente se prefiere que la app levante igual y exponga un health check que reporte el estado de la base por separado.

### 0.7. Verificar el bootstrap

En este punto el proyecto compila (`dotnet build`) y corre (`dotnet run`), pero no expone ningún endpoint de negocio todavía — `AddControllers()`/`MapControllers()` no tienen nada que mapear porque no existe ningún `Controller`. Ese es el punto de partida correcto para empezar la sección 1 con el primer bounded context (típicamente uno simple sin relaciones, como `BotAudit`, para validar que toda la cadena Repository → Service → Controller → `AppDbContext` → Postgres funciona antes de sumar relaciones).

---

## 1. Checklist: agregar un bounded context nuevo

Orden recomendado (cada paso depende del anterior):

1. `Domain/Models/<Entity>.cs` — entidad EF Core (POCO)
2. `Domain/Enums/<Entity>State.cs` — si la entidad tiene una máquina de estados
3. `Domain/Repositories/I<Entity>Repository.cs` — contrato del repositorio
4. `Persistence/Repositories/<Entity>Repository.cs` — implementación (hereda `BaseRepository<T>`)
5. `Domain/Services/Communication/<Entity>PageRequest.cs` — filtros de listado
6. `Domain/Services/Communication/Change<Entity>StateRequest.cs` — si aplica cambio de estado
7. `Domain/Services/I<Entity>Service.cs` — contrato del servicio
8. `Services/<Entity>Service.cs` — implementación (reglas de negocio)
9. `Resources/<Entity>Resource.cs` — DTO de salida
10. `Resources/Save<Entity>Resource.cs` — DTO de entrada (create/update)
11. `Mapping/<Entity>Mappings.cs` — `ToEntity()` / `ToResource()`
12. `Controllers/<Entity>sController.cs` — endpoints REST
13. Registrar la entidad y sus relaciones en `Shared/Persistence/Context/AppDbContext.cs` (`DbSet<>` + `OnModelCreating`)
14. Registrar `IXRepository`/`XRepository` e `IXService`/`XService` en `Program.cs` (`AddScoped`)
15. Generar la migración (ver sección 5)
16. **Solo si `BackendEjemplo.Tests/` ya existe en el repo** (ver disclaimer más arriba — nunca crear el proyecto por iniciativa propia): `BackendEjemplo.Tests/<BoundedContext>/<Entity>ServiceTests.cs` — unit tests del `Service` (ver sección 8) — escribirlos ANTES del paso 17, no después: si el filtro de `ListPageAsync` o el pre-check de conflicto tienen un bug, el test lo encuentra sin necesitar Postgres
17. Compilar (`dotnet build`); si hay proyecto de test, correr `dotnet test`; en cualquier caso, probar en vivo los casos de la sección 6 (checklist de auditoría) contra una base real

No saltees pasos ni cambies el orden: por ejemplo, escribir el `Controller` antes que el `Service` casi siempre termina generando una firma que no coincide con la interfaz.

---

## 2. Plantillas de código

Los placeholders `<Entity>` / `<entity>` (PascalCase / camelCase) se reemplazan por el nombre real (ej. `Product` / `product`). Donde la entidad tiene una relación 1:N hacia un padre, se usa `<Parent>`/`<ParentId>` como referencia (ver `Order`/`ClientId` en `OneToManyExample` para el caso real).

### 2.1. Entidad (`Domain/Models/<Entity>.cs`)

```csharp
namespace BackendEjemplo.<BoundedContext>.Domain.Models
{
    public class <Entity>
    {
        public long Id { get; set; }
        public string Name { get; set; }
        // ...otras propiedades escalares

        // Si depende de un padre (relación N:1 / 1:1 dependiente):
        public <Parent> Parent { get; set; }
        public long ParentId { get; set; }
    }
}
```

### 2.2. Repositorio

```csharp
// Domain/Repositories/I<Entity>Repository.cs
using BackendEjemplo.<BoundedContext>.Domain.Models;
using BackendEjemplo.Shared.Domain.Repositories;

namespace BackendEjemplo.<BoundedContext>.Domain.Repositories
{
    public interface I<Entity>Repository: IBaseRepository<<Entity>>
    {
        Task<<Entity>?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    }
}
```

```csharp
// Persistence/Repositories/<Entity>Repository.cs
using BackendEjemplo.<BoundedContext>.Domain.Models;
using BackendEjemplo.<BoundedContext>.Domain.Repositories;
using BackendEjemplo.Shared.Persistence.Context;
using BackendEjemplo.Shared.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BackendEjemplo.<BoundedContext>.Persistence.Repositories
{
    public class <Entity>Repository(AppDbContext context) : BaseRepository<<Entity>>(context), I<Entity>Repository
    {
        public async Task<<Entity>?> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            // Si la entidad tiene navegaciones que la respuesta necesita, agregar
            // .Include(p => p.Parent) aquí (ver nota "fixup vs Include" en la sección 4).
            return await _context.<Entities>.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        }

        // Solo si hace falta Include también en el listado paginado — copiar el
        // override completo de ListPageAsync desde OrderRepository/EnrollmentRepository
        // (agrega el Include DESPUÉS de Skip/Take, antes de ToListAsync).
    }
}
```

### 2.3. PageRequest (filtros de listado)

```csharp
using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.<BoundedContext>.Domain.Services.Communication
{
    public class <Entity>PageRequest: BasePageRequest
    {
        public string? Name { get; set; }
        // ...un campo opcional (nullable) por cada filtro que el listado debe soportar
    }
}
```

### 2.4. Servicio

```csharp
// Domain/Services/I<Entity>Service.cs
using BackendEjemplo.<BoundedContext>.Domain.Models;
using BackendEjemplo.<BoundedContext>.Domain.Services.Communication;
using BackendEjemplo.Shared.Domain.Repositories;
using BackendEjemplo.Shared.Domain.Services.Communication;

namespace BackendEjemplo.<BoundedContext>.Domain.Services
{
    public interface I<Entity>Service
    {
        Task<Page<<Entity>>> ListPageAsync(<Entity>PageRequest request, CancellationToken cancellationToken = default);
        Task<BaseResponse<<Entity>>> FindByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<BaseResponse<<Entity>>> AddAsync(<Entity> entity, CancellationToken cancellationToken = default);
        Task<BaseResponse<<Entity>>> UpdateAsync(long id, <Entity> entity, CancellationToken cancellationToken = default);
        Task<BaseResponse<<Entity>>> DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
```

```csharp
// Services/<Entity>Service.cs
using BackendEjemplo.Shared.Extensions;
using System.Linq.Expressions;

namespace BackendEjemplo.<BoundedContext>.Services
{
    public class <Entity>Service(
        I<Entity>Repository <entity>Repository,
        // + repos de entidades hijas si hay que precheckear conflictos de borrado
        IUnitOfWork unitOfWork) : I<Entity>Service
    {
        // Whitelist de columnas por las que el cliente puede pedir orden (query params
        // sortBy/sortDescending, heredados de BasePageRequest — ver sección 4 "Sorting").
        // Un sortBy no listado acá cae en silencio al defaultColumn de ApplySort, nunca
        // rompe la query ni expone una columna no pensada para ordenar.
        private static readonly Dictionary<string, Expression<Func<<Entity>, object>>> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = e => e.Id,
            ["name"] = e => e.Name
            // ...una entrada por cada columna que tenga sentido exponer para ordenar
        };

        public async Task<BaseResponse<<Entity>>> AddAsync(<Entity> entity, CancellationToken cancellationToken = default)
        {
            await <entity>Repository.AddAsync(entity, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken); // NUNCA olvidar este await — sin él, la request devuelve 200/201 pero no persiste nada
            return new BaseResponse<<Entity>>(entity);
        }

        public async Task<BaseResponse<<Entity>>> DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var existing = await <entity>Repository.FindByIdAsync(id, cancellationToken);
            if (existing is null) return new BaseResponse<<Entity>>($"<Entity> con id {id} no existe"); // 404

            // Si otra entidad depende de esta (FK Restrict), pre-chequear ANTES de intentar
            // borrar para devolver un 409 legible en vez de dejar que reviente la constraint:
            // var dependents = await childRepository.ListPageAsync(0, 1, c => c.ParentId == id, cancellationToken: cancellationToken);
            // if (dependents.TotalRecords > 0)
            //     return new BaseResponse<<Entity>>($"No se puede eliminar... porque tiene ... asociados", isConflict: true); // 409

            <entity>Repository.Remove(existing);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new BaseResponse<<Entity>>(existing);
        }

        public async Task<BaseResponse<<Entity>>> FindByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var existing = await <entity>Repository.FindByIdAsync(id, cancellationToken);
            if (existing is null) return new BaseResponse<<Entity>>($"<Entity> con id {id} no existe");
            return new BaseResponse<<Entity>>(existing);
        }

        public async Task<Page<<Entity>>> ListPageAsync(<Entity>PageRequest request, CancellationToken cancellationToken = default)
        {
            Expression<Func<<Entity>, bool>>? filter = entity =>
                (string.IsNullOrWhiteSpace(request.Name) || entity.Name.Contains(request.Name));
                // OJO: siempre "IsNullOrWhiteSpace(x) || Contains(x)", NUNCA "!IsNullOrWhiteSpace(x) || Contains(x)"
                // (el "!" invertido hace que el filtro devuelva 0 filas cuando no se manda el query param)

            return await <entity>Repository.ListPageAsync(
                request.PageIndex,
                request.PageSize,
                filter,
                // defaultColumn es el orden aplicado cuando el cliente no manda sortBy (o
                // manda uno no reconocido). Casi siempre e => e.Id; si el listado ya tenía
                // un orden de negocio implícito (ej. "más reciente primero"), usar esa
                // columna como default y agregar defaultDescending: true.
                orderBy: q => q.ApplySort(request.SortBy, request.SortDescending, SortableColumns, defaultColumn: e => e.Id),
                cancellationToken: cancellationToken);
        }

        public async Task<BaseResponse<<Entity>>> UpdateAsync(long id, <Entity> entity, CancellationToken cancellationToken = default)
        {
            var existing = await <entity>Repository.FindByIdAsync(id, cancellationToken);
            if (existing is null) return new BaseResponse<<Entity>>($"<Entity> con id {id} no existe");

            existing.Name = entity.Name; // copiar campo por campo — nunca reasignar la referencia completa

            <entity>Repository.Update(existing);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new BaseResponse<<Entity>>(existing);
        }
    }
}
```

### 2.5. Resources + Mapping

```csharp
// Resources/<Entity>Resource.cs — DTO de salida (solo lo que el cliente debe ver)
public class <Entity>Resource
{
    public long Id { get; set; }
    public string Name { get; set; }
}

// Resources/Save<Entity>Resource.cs — DTO de entrada (create/update)
using System.ComponentModel.DataAnnotations;

public class Save<Entity>Resource
{
    [Required, MaxLength(100)]
    public string Name { get; set; }
    // NO incluir campos que el servidor calcula (fechas de auditoría, estado inicial,
    // ids autogenerados) — eso va fijo en el Mapping, nunca lo manda el cliente.

    // Si el DTO tiene un campo [Required] que sea un value type (long, int, DateOnly,
    // DateTime, bool, decimal — NO string), SIEMPRE declararlo nullable (long?, int?,
    // DateOnly?, etc.). [Required] sobre un value type no-nullable nunca dispara: el
    // binding nunca lo deja en null si falta en el JSON, lo deja en default(T) (0,
    // false, 0001-01-01...), y ese default pasa la validación sin que el cliente haya
    // mandado nada (tabla completa de qué tipos necesitan "?" en VALIDATION.md; se
    // encontró este bug 10 veces en este proyecto, ver sección 7 más abajo). Ejemplo real:
    [Required]
    public long? ParentId { get; set; }
}
```

```csharp
// Mapping/<Entity>Mappings.cs
public static class <Entity>Mappings
{
    public static <Entity>Resource ToResource(this <Entity> e) => new()
    {
        Id = e.Id,
        Name = e.Name
    };

    public static <Entity> ToEntity(this Save<Entity>Resource r) => new()
    {
        Name = r.Name,
        // .Value es seguro: el Controller ya devolvió ValidationProblem(ModelState) si
        // ParentId vino null (ver arriba, por qué ParentId es "long?" y no "long").
        ParentId = r.ParentId!.Value
        // CreatedDate = DateTime.UtcNow, State = <Enum>.Initial, etc. si aplica
    };
}
```

### 2.6. Controller

```csharp
using Microsoft.AspNetCore.Mvc;

namespace BackendEjemplo.<BoundedContext>.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class <Entity>sController(I<Entity>Service <entity>Service): ControllerBase
    {
        [HttpGet]
        public async Task<PageResponse<<Entity>Resource>> Get<Entity>sPaginatedAsync([FromQuery] <Entity>PageRequest request, CancellationToken cancellationToken)
        {
            var result = await <entity>Service.ListPageAsync(request, cancellationToken);
            return new PageResponse<<Entity>Resource>
            {
                Data = result.Data.Select(p => p.ToResource()),
                PageIndex = result.PageIndex,
                PageSize = result.PageSize,
                TotalRecords = result.TotalRecords,
            };
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            var result = await <entity>Service.FindByIdAsync(id, cancellationToken);
            if (!result.Success) return this.ToProblem(result, StatusCodes.Status404NotFound);
            return Ok(result.Content.ToResource());
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] Save<Entity>Resource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var entity = resource.ToEntity();
            var result = await <entity>Service.AddAsync(entity, cancellationToken);
            // ToProblem ya resuelve 409 automáticamente si result.IsConflict == true;
            // el segundo parámetro es el status a usar cuando NO es un conflicto.
            if (!result.Success) return this.ToProblem(result, StatusCodes.Status400BadRequest);

            // El Location header debe apuntar a ESTA ruta con el id real generado — no a
            // otro controller, no a un Uri("") vacío, no a nameof(PostAsync).
            return Created($"/api/v1/<entity>s/{result.Content.Id}", result.Content.ToResource());
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(long id, [FromBody] Save<Entity>Resource resource, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var entity = resource.ToEntity();
            var result = await <entity>Service.UpdateAsync(id, entity, cancellationToken);
            if (!result.Success) return this.ToProblem(result, StatusCodes.Status404NotFound); // "no existe" en Update siempre es 404, no 400

            return Ok(result.Content.ToResource());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
        {
            var result = await <entity>Service.DeleteAsync(id, cancellationToken);
            if (!result.Success) return this.ToProblem(result, StatusCodes.Status404NotFound);
            return Ok(result.Content.ToResource());
        }
    }
}
```

`ToProblem(result, failureStatusCode)` (definido en `Shared/Extensions/ControllerBaseExtensions.cs`, sección 0.5) es el único punto donde se decide 404 vs 400 vs 409 — nunca escribir `NotFound(result.Message)`/`BadRequest(result.Message)`/`Conflict(result.Message)` a mano en un controller nuevo, eso reintroduce la inconsistencia de contrato (string plano) que este helper resuelve.

### 2.7. Registro en `AppDbContext` (`OnModelCreating`)

```csharp
modelBuilder.Entity<<Entity>>().ToTable("<entity>s");
modelBuilder.Entity<<Entity>>().HasKey(p => p.Id);
modelBuilder.Entity<<Entity>>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
modelBuilder.Entity<<Entity>>().Property(p => p.Name).IsRequired().HasMaxLength(100);

// Si hay relación con un padre (1:N) — SIEMPRE Restrict, nunca Cascade:
modelBuilder.Entity<<Parent>>()
    .HasMany(p => p.<Entity>s)
    .WithOne(p => p.Parent)
    .HasForeignKey(p => p.ParentId)
    .OnDelete(DeleteBehavior.Restrict);
```

Recordar agregar el `DbSet<<Entity>> <Entity>s { get; set; }` junto a los demás al inicio de la clase.

### 2.8. Registro en `Program.cs`

```csharp
builder.Services.AddScoped<I<Entity>Repository, <Entity>Repository>();
builder.Services.AddScoped<I<Entity>Service, <Entity>Service>();
```

Más el `using` correspondiente a los cuatro namespaces (`Domain.Repositories`, `Domain.Services`, `Persistence.Repositories`, `Services`) al tope del archivo.

---

## 3. Variantes según el tipo de relación

| Relación | Cómo se modela | Ejemplo real | Regla extra en el `Service` |
|---|---|---|---|
| Sin relación (CRUD simple) | Nada especial | `BotAudit` / `BotLog` | — |
| 1 a muchos | FK simple en el hijo (`ChildId`), `Restrict` | `Client` → `Order` | El hijo valida que el padre exista antes de crear (→ 400); el padre pre-chequea hijos antes de borrar (→ 409) |
| muchos a muchos | Entidad de unión explícita con su propio `Id`, **no** `HasMany().WithMany()` nativo | `Student`/`Course` → `Enrollment` | La unión valida ambos lados antes de crear (→ 400) y chequea duplicado vía índice único compuesto + pre-check (→ 409) |
| 1 a 1 | FK única en el dependiente + índice `UNIQUE` (`HasOne().WithOne().HasForeignKey<TDependent>()`), nunca clave primaria compartida | `Employee` ↔ `EmployeeProfile` | El dependiente valida que el principal exista (→ 400) y que no tenga ya un registro (→ 409) |
| Entidad con estados (`Order`, `Enrollment`) | Enum de estado + endpoint dedicado | `PATCH state/{id}` | Nace siempre en el estado inicial fijo en el `Mapping`; el estado **nunca** es editable por `PUT`, solo por el `PATCH` |

**Nota sobre "fixup" vs `Include`**: cuando el `Service.AddAsync` de una entidad dependiente carga a su padre desde el mismo `DbContext` (sin `AsNoTracking`) y después la agrega, EF Core enlaza la navegación automáticamente al guardar (no hace falta un `Include` extra ni recargar la entidad). Pero el `Repository.FindByIdAsync`/`ListPageAsync` de esa entidad SÍ necesita `.Include()` explícito, porque ahí se consulta desde cero.

---

## 4. Contratos JSON

### Listado paginado (`GET` de colección)

```json
{
  "data": [ { "id": 1, "name": "..." } ],
  "pageIndex": 0,
  "pageSize": 10,
  "totalRecords": 42
}
```
Query params: `?pageIndex=0&pageSize=10&sortBy=<columna>&sortDescending=false&<filtro1>=...&<filtro2>=...`. `pageSize` se acota server-side a 100 (`MaxPageSize`); no hace falta validarlo en el cliente. Un `pageIndex` fuera de rango (incluido uno absurdamente grande) no da error: el `BaseRepository` lo clampea a la última página válida y la devuelve.

### Sorting

`sortBy`/`sortDescending` viajan en `BasePageRequest`, así que todo listado los soporta sin trabajo extra. Cada `Service` define su propia whitelist de columnas ordenables (`SortableColumns`, ver sección 2.4) y la aplica con `QueryableSortExtensions.ApplySort`. Un `sortBy` vacío o no reconocido **nunca** rompe la query — cae en silencio al orden por defecto de ese listado (normalmente `id`, salvo que el listado ya tuviera un orden de negocio implícito, como "más reciente primero" en `Order`/`Enrollment`). No confiar en que Postgres devuelva las filas siempre en el mismo orden sin un `ORDER BY` explícito: por eso `ApplySort` siempre aplica alguna columna, nunca deja el query sin ordenar.

### Zona horaria en filtros de fecha

Todo filtro de rango de fecha (`startX`/`endX` en un `PageRequest`) usa `DateOnly`, no `DateTime` — quien arma la request no debería tener que inventar una hora para expresar "desde tal día hasta tal día". Query param: `?startRegistrationDate=2026-08-07&endRegistrationDate=2026-08-07` (formato `yyyy-MM-dd`, binding nativo de ASP.NET Core para `DateOnly`).

La columna contra la que se compara, en cambio, es un `DateTime`/UTC real (`timestamp with time zone` en Postgres, forzado a `Kind=Utc` por el loop de `AppDbContext.OnModelCreating`). Convertir un `DateOnly` a los límites de ese rango tiene una trampa: `date.ToDateTime(TimeOnly.MinValue)` da `2026-08-07T00:00:00` con `Kind=Unspecified`, que termina interpretándose como **medianoche UTC**, no como medianoche en la zona horaria de quien usa la API. Para una empresa que opera desde Perú (UTC-5), eso desalinea el filtro hasta 5 horas: un registro creado a las 20:00 hora Lima del día 6 se guarda como `2026-08-07T01:00:00Z` y, con la conversión ingenua, cae bajo `startDate=2026-08-07` en vez de `2026-08-06` — para cualquier persona en Lima, ese registro "fue el 6".

Por eso la conversión pasa por `Shared/Extensions/DateOnlyExtensions.cs`:

```csharp
public static class DateOnlyExtensions
{
    private static readonly TimeZoneInfo BusinessTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Lima");

    public static DateTime ToStartOfBusinessDayUtc(this DateOnly date) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue), BusinessTimeZone);

    public static DateTime ToEndOfBusinessDayUtc(this DateOnly date) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MaxValue), BusinessTimeZone);
}
```

Y el `Service` la usa así (mismo patrón en `BotLogService`, `ClientService`, `OrderService`, `EnrollmentService`, `EmployeeService`):

```csharp
(!request.StartRegistrationDate.HasValue || client.RegistrationDate >= request.StartRegistrationDate.Value.ToStartOfBusinessDayUtc()) &&
(!request.EndRegistrationDate.HasValue || client.RegistrationDate <= request.EndRegistrationDate.Value.ToEndOfBusinessDayUtc())
```

`BusinessTimeZone` es la **única línea a tocar** si la empresa opera desde otro huso horario — no hay una constante de timezone repetida por bounded context. `"America/Lima"` no tiene horario de verano (offset fijo todo el año), así que acá no hace falta lidiar con ambigüedad de DST; en un huso que sí lo tenga, `TimeZoneInfo.ConvertTimeToUtc` ya resuelve el offset correcto para cada fecha puntual. El id es un IANA time zone ID — .NET lo resuelve en cualquier plataforma (Windows incluido) desde .NET Core 3.0, sin necesitar ICU.

Un `Service` que agregue su propio filtro de rango de fecha **siempre** debe usar `DateOnly` + `ToStartOfBusinessDayUtc()`/`ToEndOfBusinessDayUtc()` — nunca `DateTime` crudo ni `.ToDateTime(TimeOnly.MinValue)` directo (eso reintroduce el desalineamiento de zona horaria).

### Enums

Todo enum de dominio (`OrderState`, `EnrollmentState`, etc.) se serializa y deserializa como **string con el nombre del valor**, nunca como el entero subyacente:

```json
{ "id": 3, "totalAmount": 100, "state": "Pending" }
```

Esto lo garantiza `JsonStringEnumConverter`, configurado dos veces en `Program.cs` (una para `Mvc.JsonOptions`, que afecta a los `Controller`, y otra para `Http.Json.JsonOptions`, que afecta al schema que genera `AddOpenApi()`) — **son dos configuraciones independientes**, y hay que registrar el converter en ambas o el runtime y la documentación de Swagger quedan desincronizados entre sí. Un `PATCH state/{id}` con un valor que no matchea ningún nombre del enum (ej. `"state": "NotARealState"`) devuelve **400** con el detalle del campo que falló, vía el mismo mecanismo de `ModelState`.

### Errores: un único contrato (`ProblemDetails`) para todo

**Todo** error de la API — validación, negocio (404/400/409) y no controlado (500) — responde el mismo shape `ProblemDetails` (RFC 9457), con `traceId` para correlacionar con el log de Serilog. No existen dos formatos de error distintos en esta API; si un endpoint nuevo devuelve un string plano o un objeto custom para un error, es un bug de contrato, no una variante válida.

#### Error de validación (400, `ModelState` inválido)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "name": ["The Name field is required."] },
  "traceId": "00-...-00"
}
```
Generado automáticamente por `[ApiController]` antes de que la acción se ejecute (o, si el controller agrega errores custom a `ModelState` dentro de la acción — ej. una validación cruzada entre dos campos — vía `return ValidationProblem(ModelState);` explícito, mismo shape).

#### Error de negocio (400/404/409, `BaseResponse.Success == false`)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "detail": "Cliente con id 999 no existe",
  "traceId": "00-...-00"
}
```
El mensaje de negocio va en `detail`, no en el body entero. Código HTTP: **404** si es "no existe", **409** si es "conflicto de negocio" (`IsConflict == true`), **400** si es una validación de negocio que no depende de existencia (ej. FK a un padre inexistente al crear). Se genera con el helper `this.ToProblem(result, failureStatusCode)` (sección 2.6) — nunca a mano con `NotFound(result.Message)`/`BadRequest(result.Message)`.

#### Error no controlado (500)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Ocurrió un error inesperado.",
  "status": 500,
  "traceId": "00-...-00"
}
```
Genérico, sin detalle de la excepción real (eso solo va al log de Serilog vía `logger.LogError` en `GlobalExceptionHandler`) — nunca debe llegar un stack trace al cliente.

---

## 5. Generar la migración

Desde la raíz de la solución:

```bash
dotnet tool run dotnet-ef migrations add <NombreDeLaMigracion> \
  --project BackendEjemplo/BackendEjemplo.csproj \
  --startup-project BackendEjemplo/BackendEjemplo.csproj \
  --output-dir Shared/Persistence/Migrations
```

Después de generarla, **siempre abrir el `.cs` de la migración y leerlo** antes de aplicarla — es la forma más rápida de detectar un mapeo mal configurado (ej. una FK apuntando a la columna equivocada, un índice único que faltaba, un `Cascade` donde debía ir `Restrict`). Si algo no coincide con lo esperado, no se parchea la migración a mano: se corrige `AppDbContext` y se borra/regenera (`dotnet-ef migrations remove`).

`Program.cs` aplica migraciones automáticamente al arrancar (`context.Database.Migrate()`), así que no hace falta correr `database update` a mano salvo que se quiera aplicar sin levantar la API.

---

## 6. Checklist de auditoría (antes de dar por terminado un bounded context)

Verificación funcional (probar contra una base real, no asumir):

- [ ] `POST` con body válido → **201** con `Location` header apuntando al recurso real recién creado
- [ ] `POST` con `ModelState` inválido → **400** con detalle de campos
- [ ] `POST` con FK a un padre inexistente (si aplica) → **400** con mensaje legible, no un 500 de FK violation
- [ ] `POST` duplicado (si hay restricción de unicidad, ej. N:N o 1:1) → **409**, no 500
- [ ] `GET /{id}` inexistente → **404**
- [ ] `GET` de colección sin filtros → devuelve **todas** las filas (no cero) — regresión clásica si el filtro quedó con un `!` de más
- [ ] `PUT /{id}` inexistente → **404** (no 400)
- [ ] `DELETE` de un recurso con dependientes (si aplica `Restrict`) → **409** con mensaje legible, no 500 de FK violation
- [ ] `DELETE /{id}` inexistente → **404**
- [ ] Cambios vía `PATCH state/{id}` (si aplica) persisten y la respuesta no tira `NullReferenceException` por una navegación no incluida

Verificación de código:

- [ ] Toda FK entre bounded contexts usa `DeleteBehavior.Restrict` (nunca `Cascade`)
- [ ] Todo `Service.DeleteAsync` que depende de una FK `Restrict` hace el pre-check de conflicto antes de borrar
- [ ] Todo método que muta estado (`AddAsync`/`UpdateAsync`/`DeleteAsync`/`ChangeStateAsync`) llama `await unitOfWork.CompleteAsync(cancellationToken)` — buscar el método y confirmar que el `await` está ahí, no asumirlo
- [ ] Todos los métodos async de `Repository`/`Service`/`Controller` reciben y propagan `CancellationToken cancellationToken = default`
- [ ] Los filtros de `ListPageAsync` usan `string.IsNullOrWhiteSpace(x) || x.Contains(...)`, sin un `!` de más
- [ ] Las fechas de auditoría (creación, registro, etc.) se fijan en el `Mapping` con `DateTime.UtcNow`, nunca las manda el cliente
- [ ] Las entidades con estado nacen en un estado fijo (no configurable desde `Save*Resource`) y solo cambian vía `PATCH`
- [ ] Los DTOs anidados van en una sola dirección (el "padre" no vuelve a anidar al "hijo") para evitar recursión infinita en el mapeo
- [ ] `SaveXResource` no expone campos que el servidor calcula (ids, fechas de auditoría, estado inicial)
- [ ] La ruta del `Controller` es `"api/v1/[controller]"` (con la barra inicial) — sin ella, ASP.NET resuelve una ruta pegada tipo `/api/v1clients`
- [ ] `EnableSensitiveDataLogging()`/`EnableDetailedErrors()` siguen gateados por `IsDevelopment()` — nunca agregar una excepción a esto
- [ ] Todo error de negocio en el `Controller` usa `this.ToProblem(result, failureStatusCode)` — nunca `NotFound(result.Message)`/`BadRequest(result.Message)`/`Conflict(result.Message)` a mano (rompe el contrato unificado de `ProblemDetails`, ver sección 4)
- [ ] `dotnet build` sin errores nuevos (warnings `CS8618` preexistentes de nullability son aceptables, no agregar warnings nuevos sin justificar)
- [ ] **Si `BackendEjemplo.Tests/` ya existía en el repo** (nunca crearlo de cero por tu cuenta — ver disclaimer al inicio del documento): `dotnet test` corre en verde y el bounded context nuevo tiene su `<Entity>ServiceTests.cs` (ver sección 8). Si no existía, este ítem no aplica.
- [ ] La entidad nueva no necesita nada especial para la concurrencia optimista (xmin) — la recibe automáticamente por el loop en `OnModelCreating` (sección 9); solo verificar que no se haya excluido a mano
- [ ] `ListPageAsync` del `Service` nuevo pasa un `orderBy` armado con `ApplySort` y su propia `SortableColumns` (sección 2.4/4) — nunca dejar el listado sin `orderBy` (paginación no determinística)
- [ ] Todo filtro de rango de fecha nuevo (`startX`/`endX`) usa `DateOnly` en el `PageRequest` y `ToStartOfBusinessDayUtc()`/`ToEndOfBusinessDayUtc()` en el `Service` (sección 4, "Zona horaria en filtros de fecha") — nunca `DateTime` crudo ni `.ToDateTime(TimeOnly.MinValue)` directo
- [ ] Ningún filtro de `ListPageAsync` interpola (`$"..."`) una propiedad de la entidad dentro del lambda — compila a `string.Format`, que Npgsql no traduce (falla en runtime, no en build). Para combinar columnas, usar `+` (concatenación). Tampoco usar `EF.Functions.*` (`ILike`, etc.) en un filtro que se vaya a testear compilando y ejecutando en memoria (sección 8.2) — esos métodos tiran excepción fuera de una traducción a SQL real
- [ ] Todo campo `[Required]` en un `Save*Resource` es un tipo que puede ser `null` de verdad (`string`, o `T?` para value types: `int?`, `DateOnly?`, `bool?`, etc.) — `[Required]` sobre un value type no-nullable nunca falla, porque el binding nunca lo deja en `null` (queda en `default(T)`, ej. `0001-01-01` para `DateOnly`). Tabla completa de tipos en `VALIDATION.md`

---

## 7. Errores reales que ya se cometieron en este proyecto (no repetir)

Lista extraída de bugs encontrados y corregidos durante el desarrollo de este backend — sirven como catálogo de "qué buscar" al auditar código nuevo generado por IA:

- **Filtro invertido**: `!string.IsNullOrWhiteSpace(x) || x.Contains(...)` en vez de `string.IsNullOrWhiteSpace(x) || x.Contains(...)` — hacía que el listado devolviera 0 filas cuando no se mandaba el filtro.
- **`Task.WhenAll` sobre el mismo `DbContext`**: dos queries async en paralelo sobre la misma instancia de `AppDbContext` (ej. contar + traer datos) — EF Core no soporta operaciones concurrentes sobre un mismo contexto, lanza `InvalidOperationException`. Las queries de un mismo `DbContext` siempre van secuenciales.
- **`unitOfWork.CompleteAsync()` faltante**: `Update`/`Remove` que nunca llaman `SaveChangesAsync` — la request devuelve 200 pero no persiste nada.
- **`Location` header roto**: `nameof(PostAsync)`, `new Uri("")`, o una ruta hardcodeada de otro controller copy-pasteada sin ajustar.
- **`HasForeignKey(p => p.Id)` en vez de `HasForeignKey(p => p.ChildId)`**: hace que la PK del hijo se convierta en la FK, matando su autoincremento y limitando la relación a un hijo por padre.
- **Ruta de controller sin barra**: `[Route("/api/v1[controller]")]` resuelve a `/api/v1clients` en vez de `/api/v1/clients`.
- **404 vs 400 invertidos**: usar `BadRequest` para "no existe" (debería ser `NotFound`), o viceversa.
- **`Cascade` por defecto sin decisión consciente**: EF Core aplica `Cascade` si no se especifica `OnDelete` — borrar un padre borra silenciosamente a todos sus hijos. Siempre `Restrict` salvo que el negocio pida explícitamente lo contrario.
- **Conflicto de borrado sin pre-check**: dejar que la `DbUpdateException`/`PostgresException` de una FK `Restrict` llegue cruda al `GlobalExceptionHandler` y se transforme en un 500 genérico, en vez de pre-chequear en el `Service` y devolver un 409 legible.
- **`ValueGeneratedOnAdd()` en una fecha sin default real en la BD**: la columna queda en `0001-01-01` porque EF espera que la BD la genere, pero Postgres no tiene ningún `DEFAULT` configurado — la fecha hay que fijarla explícitamente en el `Mapping`.
- **Falta de `.Include()` en el repositorio de una entidad con navegación obligatoria**: la entidad se guarda bien, pero el `Mapping.ToResource()` tira `NullReferenceException` al serializar porque la navegación nunca se cargó.
- **`EnableSensitiveDataLogging`/`EnableDetailedErrors` siempre encendidos**: vuelcan valores reales de parámetros al log — deben estar gateados por `IsDevelopment()`.
- **Excepciones silenciadas con `try/catch` vacío o genérico** en el `Service`: oculta bugs reales; el manejo de errores no controlados va centralizado en `GlobalExceptionHandler`, no disperso en cada método.
- **Overflow de `pageIndex * pageSize` en `BaseRepository.ListPageAsync`**: `pageIndex` solo se validaba por abajo (`if (pageIndex < 0) pageIndex = 0`), no por arriba. Un `pageIndex` suficientemente grande (ej. `?pageIndex=30000000&pageSize=100`) hacía que `pageIndex * pageSize` desbordara `int` y quedara negativo, y Postgres rechazaba el `OFFSET` resultante con `PostgresException: 2201X: OFFSET no debe ser negativo` (500 crudo). **Corregido** clampeando `pageIndex` a la última página válida (calculada a partir de `totalRecords`/`pageSize`) antes de armar el `Skip()` — de paso, pedir una página fuera de rango ahora devuelve la última página válida en vez de un error. Verificado en vivo contra Postgres real el 2026-08-07.
- **Listados paginados sin `ORDER BY`**: varios `Service.ListPageAsync` no pasaban ningún `orderBy` al repositorio. Sin un `ORDER BY` explícito, Postgres no garantiza el mismo orden de filas entre una página y la siguiente — podía repetir o saltear registros al paginar. Corregido: todo listado ahora tiene un orden por defecto determinístico (ver "Sorting" en sección 4).
- **Paréntesis faltante al convertir un filtro de rango de `DateOnly` a `DateTime`** (`BotLogService.ListPageAsync`): al cambiar `StartDate`/`EndDate` de `DateTime?` a `DateOnly?` (más preciso para un filtro que solo tiene sentido a nivel de día, no de hora — ver sección 4, "Zona horaria en filtros de fecha") se armó `log.Fecha >= request.StartDate.Value.ToDateTime(TimeOnly.MinValue) && (...)` sin cerrar el paréntesis que agrupaba el chequeo de `StartDate`. Por precedencia de operadores (`||` liga más flojo que `&&`), eso metió los chequeos de `EndDate`, `Mensaje` y `Falla` **dentro** del `||` de `StartDate` — compilaba sin error, pero cuando no se mandaba `StartDate` (el caso normal) esos tres filtros quedaban anulados en silencio. Se coló porque no había ningún test que combinara un filtro sin `StartDate` con otro filtro (`Falla`, `Mensaje`); el test `ListPageAsync_FiltersByFalla` que ya existía dejó de servir como red de seguridad porque tampoco mandaba `StartDate`, así que "pasaba" con el comportamiento roto. Moraleja: al tocar un filtro con múltiples cláusulas `(A || B) && (C || D) && ...`, contar paréntesis a mano no alcanza — correr el test de ese filtro específico (o agregar uno) y confirmar en rojo→verde.
- **Día UTC en vez de día de negocio en filtros de fecha**: la primera versión de la conversión `DateOnly → DateTime` (`date.ToDateTime(TimeOnly.MinValue)`, con `Kind=Unspecified` reinterpretado como UTC) definía "el día" como el día calendario en UTC, no en la zona horaria de la empresa (Perú, UTC-5). Un registro creado a las 20:00 hora Lima ya había cruzado la medianoche en UTC, así que quedaba clasificado bajo el día siguiente. No llegó a producción: se detectó al aplicar el mismo patrón de `BotLogPageRequest` a los demás filtros de fecha (`Client`, `Order`, `Enrollment`, `Employee`) y razonar sobre el caso límite antes de darlo por terminado. Corregido centralizando la conversión en `DateOnlyExtensions.ToStartOfBusinessDayUtc()`/`ToEndOfBusinessDayUtc()` (sección 4), que resuelve el offset contra `America/Lima` en vez de asumir UTC.
- **String interpolado sobre columnas de la entidad dentro de un filtro** (`OrderService.ListPageAsync`, filtro `ClientFullName`): `$"{order.Client.Name.ToLower()} {order.Client.LastName.ToLower()}".Contains(...)` compila a `string.Format(...)` porque el compilador de C# traduce cualquier `$"..."` asignado a `string` así — y como los argumentos son columnas de la entidad (no una variable local), EF Core tiene que traducir ese `string.Format` a SQL, y el proveedor de Npgsql no sabe cómo (`InvalidOperationException: Translation of method 'string.Format' failed`, **en runtime**, no en build, así que el `dotnet build` en verde no lo detecta). Reproducido en vivo contra Postgres real: `GET /api/v1/orders?clientFullName=Perez` devolvía 500. Corregido reemplazando la interpolación por concatenación con `+` (`order.Client.Name.ToLower() + " " + order.Client.LastName.ToLower()`), que sí se traduce (a `||` de Postgres). Regla general: nunca interpolar (`$"..."`) sobre una propiedad de la entidad dentro de un lambda de filtro — solo sobre variables locales/closures, que EF Core evalúa en memoria antes de traducir.
- **`EF.Functions.ILike` rompe el patrón de test de este proyecto**: durante el arreglo anterior se probó usar `EF.Functions.ILike(...)` (más idiomático en Postgres, case-insensitive nativo vía `ILIKE`) en vez de `.ToLower().Contains(...)`. Compila y funciona contra Postgres real, pero la implementación CLR de `EF.Functions.ILike` tira `InvalidOperationException` a propósito si se ejecuta fuera de una traducción a SQL — y el helper `CaptureListPageFilter` de la sección 8.2 compila el filtro y lo ejecuta directo contra POCOs en memoria (LINQ to Objects), exactamente ese caso. El test de este filtro (`OrderServiceTests`) falló con `"The 'ILike' method is not supported because the query has switched to client-evaluation"`. Se optó por `.ToLower()`/`Contains()`/`+`, que funcionan tanto traducidos a SQL como ejecutados en memoria. Moraleja: cualquier método de `EF.Functions` (`ILike`, `Like`, funciones específicas de Postgres) es SQL-only — no usarlo en un filtro que se vaya a testear con este patrón de "compilar y ejecutar en memoria".
- **`[Required]` no funciona sobre un value type no-nullable** (`SaveEmployeeResource.HireDate`, al convertirlo de `DateTime` autogenerado a `DateOnly` pedido por el cliente): `RequiredAttribute.IsValid(value)` solo chequea `value == null`. Un `DateOnly HireDate` (no nullable) nunca es `null` — si el JSON no manda `hireDate`, System.Text.Json lo deserializa como `default(DateOnly)` (`0001-01-01`), no como `null`, así que `[Required]` lo da por válido y `ModelState.IsValid` queda en `true` sin que el cliente haya mandado nada. Reproducido en vivo: `POST /api/v1/employees` sin `hireDate` devolvía **201** con `hireDate: 0001-01-01` en vez de **400**. Aplica igual a cualquier otro value type no-nullable (`int`, `bool`, `decimal`, `DateTime`, etc.) — no es específico de `DateOnly` ni un bug de este proyecto, es un comportamiento general de `RequiredAttribute` en .NET. Corregido cambiando la propiedad del `Save*Resource` a nullable (`DateOnly? HireDate`) manteniendo `[Required]`, y desenvolviendo con `.Value` en el `Mapping` (seguro porque el `Controller` ya devolvió `ValidationProblem(ModelState)` antes si vino `null`). Regla general: todo campo `[Required]` en un `Save*Resource` debe ser un tipo que pueda ser `null` de verdad — string, o `T?` para value types — nunca un value type no-nullable.
  - **Una revisión del resto del proyecto encontró el mismo patrón 9 veces más**, todas corregidas igual (propiedad nullable + `[Required]` + `.Value` en el `Mapping`):
    - `SaveBotLogResource.Fecha` (`DateTime`) y `.Falla` (`bool`) — sin `fecha`/`falla` en el body, guardaba `0001-01-01` y `false` respectivamente sin avisar.
    - `SaveCourseResource.Credits` (`int`) — sin `credits`, guardaba `0` créditos sin avisar.
    - `SaveOrderResource.TotalAmount` (`decimal`) — sin `totalAmount`, guardaba un pedido de **$0** sin avisar (el peor caso de los encontrados: silenciosamente crea una orden con monto incorrecto).
    - `SaveEmployeeProfileResource.BirthDate` (`DateTime`) — mismo problema que `HireDate`, guardaba `0001-01-01`.
    - `SaveEnrollmentResource.StudentId`/`.CourseId` y `SaveOrderResource.ClientId` y `SaveEmployeeProfileResource.EmployeeId` (`long`, FKs): estos no llegaban a persistir datos incorrectos — el `Service` ya valida que el padre exista (`FindByIdAsync(0)` devuelve `null` → 400 "no existe") — pero el mensaje de error era confuso ("Alumno con id 0 no existe" en vez de "El campo StudentId es requerido"), y dependían de que cada `Service` tuviera ese pre-check para no fallar peor.
  - Verificado en vivo contra Postgres real para los 5 endpoints afectados (`bot_logs`, `courses`, `enrollments`, `orders`, `employeeprofiles`): todos devuelven ahora 400 con el detalle del campo faltante en vez de persistir un default silencioso.

---

## 8. Tests (`BackendEjemplo.Tests`)

> ⚠️ **Opcional — no crear por defecto.** Esta sección documenta un proyecto que existe en *este* repo de ejemplo, pero la plantilla se usa también para agregar endpoints a APIs reales que todavía no tienen ningún proyecto de test (testing no es una práctica adoptada aún en toda la organización). Un LLM que use esta guía **solo** crea `BackendEjemplo.Tests/` o le agrega un test nuevo **si el usuario lo pide explícitamente en la conversación** — nunca por iniciativa propia al agregar un bounded context. Leé esta sección para saber cómo hacerlo *cuando* corresponda, no como un paso obligatorio de todo flujo.

Proyecto de unit tests separado, en la raíz de la solución (sibling de `BackendEjemplo/`), referenciado en `BackendEjemplo.slnx`. Cubre la capa `Service` — que es donde vive prácticamente todo bug real encontrado en este proyecto (filtros invertidos, pre-checks de conflicto faltantes, validación de FK) — mockeando los repositorios, sin necesitar Postgres. No incluye (todavía) integration tests contra una base real ni tests de `Controller`/`Repository`.

### 8.1. Stack y estructura

```
BackendEjemplo.Tests/
├── BackendEjemplo.Tests.csproj    # xunit.v3 + AwesomeAssertions + Moq, referencia a BackendEjemplo.csproj
├── TestHelpers/
│   └── RepositoryMockExtensions.cs
├── BotAudit/BotLogServiceTests.cs
├── OneToManyExample/ClientServiceTests.cs, OrderServiceTests.cs
├── ManyToManyExample/StudentServiceTests.cs, CourseServiceTests.cs, EnrollmentServiceTests.cs
└── OneToOneExample/EmployeeServiceTests.cs, EmployeeProfileServiceTests.cs
```

**Por qué `xunit.v3` (no `xunit`) y `AwesomeAssertions` (no `FluentAssertions`)**:
- El paquete clásico `xunit` (v2) está marcado como **obsoleto** en NuGet a favor de `xunit.v3` — el motor activamente mantenido. `dotnet new xunit` sigue scaffoldeando v2 por default (a la fecha de esto), así que hay que migrar los paquetes a mano después de crear el proyecto (ver comandos abajo). La migración de v2 a v3 no tocó ningún test de este proyecto porque no usamos `IAsyncLifetime`, `ITestOutputHelper` ni `async void` — si tu bounded context nuevo usa alguna de esas tres cosas, revisá la [guía oficial de migración](https://xunit.net/docs/getting-started/v3/migration) antes de escribir el test.
- `FluentAssertions` cambió de licencia MIT a una licencia comercial paga (Xceed) a partir de la v8, para organizaciones por encima de cierto umbral de ingresos — un riesgo legal real para una plantilla que otros van a reusar comercialmente. `AwesomeAssertions` es un fork 1:1 mantenido por la comunidad bajo MIT/Apache 2.0; el único cambio de código es el `using` (`AwesomeAssertions` en vez de `FluentAssertions`) — la API (`.Should().BeTrue()`, etc.) es idéntica.

Crear el proyecto (ya existe en este repo; documentado por si se arranca desde cero — sección 0):

```bash
dotnet new xunit -n BackendEjemplo.Tests -o BackendEjemplo.Tests --framework net10.0
cd BackendEjemplo.Tests
dotnet remove package xunit                  # v2, obsoleto
dotnet add package xunit.v3
dotnet add package AwesomeAssertions
dotnet add package Moq
dotnet add reference ../BackendEjemplo/BackendEjemplo.csproj
cd ..
dotnet sln BackendEjemplo.slnx add BackendEjemplo.Tests/BackendEjemplo.Tests.csproj
```

Después, agregar `<OutputType>Exe</OutputType>` al primer `<PropertyGroup>` del `.csproj` — a partir de xunit v3 el proyecto de test es un ejecutable autocontenido, no una librería que un runner externo carga por reflexión (podés incluso correrlo directo con `dotnet run` o el `.exe` generado, además de `dotnet test`).

Correr toda la suite desde la raíz de la solución:

```bash
dotnet test BackendEjemplo.Tests/BackendEjemplo.Tests.csproj
```

Todo método async del `Service` que recibe `CancellationToken` debe invocarse en los tests pasando `TestContext.Current.CancellationToken` (no `default`, no omitirlo) — es el token ambiente que xunit v3 expone por test, y permite que `dotnet test` cancele un test colgado de forma responsiva. El analyzer de xunit.v3 (`xUnit1051`) marca como warning cualquier llamada que no lo haga.

### 8.2. El helper clave: capturar y ejecutar el filtro real

El bug más traicionero de este proyecto (filtro invertido en `ListPageAsync`) no se detecta revisando el código a simple vista — hay que **ejecutar** la expresión LINQ contra datos de prueba. `TestHelpers/RepositoryMockExtensions.CaptureListPageFilter<TRepository, TEntity>` intercepta el `Expression<Func<TEntity, bool>>` que el `Service` arma y se lo pasa al mock del repositorio, lo compila, y lo corre en memoria:

```csharp
[Fact]
public async Task ListPageAsync_WithNoFilters_MatchesEverything()
{
    var getFilter = _clientRepository.CaptureListPageFilter<IClientRepository, Client>(RepositoryMockExtensions.EmptyPage<Client>());

    await _sut.ListPageAsync(new ClientPageRequest(), TestContext.Current.CancellationToken);

    var filter = getFilter()!.Compile();
    filter(SampleClient()).Should().BeTrue(); // sin filtros, CUALQUIER fila debe matchear
}
```

Sin este patrón, un test que solo verifica "el método no explota" no habría atrapado el bug real (`!string.IsNullOrWhiteSpace(x) || x.Contains(x)`) — hay que probar el comportamiento del filtro compilado, no solo que la llamada no lance una excepción.

### 8.3. Qué cubre cada test class

| Test class | Patrón que valida |
|---|---|
| `BotLogServiceTests` | CRUD simple sin relaciones: `CompleteAsync()` se llama en Add/Update/Delete pero no si el recurso no existe; filtros de `ListPageAsync` (incluye no-filtro = todo) |
| `ClientServiceTests` | Delete con pre-check de conflicto (1:N): sin dependientes → borra; con dependientes → 409 sin tocar la base; filtro `FullName` con lógica OR (`Name` o `LastName`) |
| `OrderServiceTests` | Validación de FK al crear (padre inexistente → falla sin persistir); `ChangeStateAsync` (transición de estado vía `PATCH`, no vía `Update`); filtro sobre navegación (`Client.Name`/`LastName`) |
| `StudentServiceTests` / `CourseServiceTests` | Mismo patrón de `ClientServiceTests`, versión reducida (mismo código, distinta entidad) |
| `EnrollmentServiceTests` | La regla propia de N:N: valida ambas FKs (`Student` y `Course`) por separado, y el pre-check de **duplicado** (mismo alumno + mismo curso) antes del insert |
| `EmployeeServiceTests` | Mismo patrón de `ClientServiceTests` aplicado a 1:1 (Delete con pre-check si ya tiene perfil) |
| `EmployeeProfileServiceTests` | La regla propia de 1:1: valida el padre (`Employee`) Y el pre-check de duplicado (el empleado ya tiene un perfil) antes del insert; `DeleteAsync` sin ningún pre-check (nada depende de un `EmployeeProfile`) |

### 8.4. Validación de que los tests realmente sirven

No basta con que los tests estén en verde — hay que confirmar que fallan cuando el código está roto. Se verificó reintroduciendo deliberadamente el bug del filtro invertido en `ClientService.ListPageAsync` (`!string.IsNullOrWhiteSpace(...)` en vez de `string.IsNullOrWhiteSpace(...)`): los 3 tests de filtro de `ClientServiceTests` fallaron inmediatamente (`ArgumentNullException` en dos casos, `Expected filter(other) to be False, but found True` en el tercero). Al revertir el cambio, la suite completa (50 tests) volvió a pasar. **Al escribir un test nuevo, hacé esta misma prueba**: romper a propósito la línea que el test dice cubrir y confirmar que efectivamente falla — un test que pasa incluso con el bug presente no está probando nada.

---

## 9. Concurrencia optimista (xmin)

Todas las entidades tienen protección automática contra el clásico "last write wins" silencioso: si la `Request A` lee un recurso, la `Request B` lo modifica o borra, y después `Request A` intenta guardar sus cambios basados en la versión vieja, la `Request A` recibe un **409** en vez de pisar silenciosamente el cambio de `Request B`.

### 9.1. Cómo funciona

Postgres tiene una columna de sistema `xmin` en toda tabla (el id de la transacción que escribió la fila por última vez), que cambia en cada `UPDATE`. `AppDbContext.OnModelCreating` mapea una propiedad shadow por entidad como concurrency token contra esa columna (sección 0.5):

```csharp
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    modelBuilder.Entity(entityType.ClrType)
        .Property<uint>("Version")
        .IsRowVersion();
}
```

Con esto, todo `UPDATE`/`DELETE` que genera EF Core agrega `WHERE ... AND xmin = @valorLeido`. Si 0 filas se ven afectadas (porque `xmin` ya cambió), EF lanza `DbUpdateConcurrencyException`, que `GlobalExceptionHandler` traduce a un **409** con un mensaje legible (sección 0.5) — el mismo mecanismo central que ya maneja el resto de los errores no controlados, sin tocar ningún `Service`/`Controller`/`Resource` individual.

**No hace falta ningún cambio de contrato**: no hay que exponer un campo `version`/`rowVersion` en ningún `Resource`, porque el patrón de esta plantilla es "leer y escribir dentro de la misma request" (`FindByIdAsync` → mutar → `Update`/`Remove` → `CompleteAsync`, todo con el mismo `DbContext` scoped) — el token viaja implícito en el `DbContext` tracking, nunca en el body HTTP. Esto protege contra el caso real (dos requests concurrentes pisándose), no requiere que el cliente mande nada de vuelta.

### 9.2. Detalle no obvio: el nombre de la propiedad shadow no importa, y la migración generada no ejecuta DDL real

El nombre `"Version"` es arbitrario — el proveedor de Npgsql reconoce automáticamente cualquier propiedad `uint` marcada `.IsRowVersion()` y la redirige a la columna física `xmin` sin importar cómo se llame en C#. Al generar la migración, el archivo `.cs` sí muestra operaciones `AddColumn<uint>(name: "xmin", type: "xid", rowVersion: true, ...)` / `DropColumn` — **pero el SQL real que Npgsql genera para esa migración no contiene ningún `ALTER TABLE`**, solo el `INSERT`/`DELETE` de bookkeeping en `__EFMigrationsHistory`. Se verificó con:

```bash
dotnet tool run dotnet-ef migrations script --project BackendEjemplo/BackendEjemplo.csproj --startup-project BackendEjemplo/BackendEjemplo.csproj --idempotent -o script.sql
```

El bloque correspondiente a la migración `AddXminConcurrencyToken` en `script.sql` no tiene ningún `ALTER TABLE` — Postgres nunca permitiría crear una columna llamada `xmin` (choca con la columna de sistema), y el generador de SQL de Npgsql lo sabe y omite la operación en silencio. **No editar esa migración a mano pensando que "falta" el DDL** — es intencional.

> Nota histórica: versiones viejas de `Npgsql.EntityFrameworkCore.PostgreSQL` (hasta la 6.x) exponían un método dedicado `UseXminAsConcurrencyToken()` a nivel de entidad. Fue marcado obsoleto y removido a partir de la 7.0 a favor del mecanismo estándar de EF Core (`.IsRowVersion()` sobre una propiedad `uint`) — si alguna vez ves `UseXminAsConcurrencyToken()` en un ejemplo online, es código desactualizado para este proyecto (.NET 10 / Npgsql 10.x).

### 9.3. Cómo se validó

El `DbUpdateConcurrencyException` real solo se dispara con una condición de carrera genuina contra Postgres — no es mockeable con Moq (por eso no hay un test de esto en `BackendEjemplo.Tests`, que es mock-only). Se armó un programa descartable (`dotnet new console`, referenciando `BackendEjemplo.csproj`) que abre **dos instancias de `AppDbContext`** contra la misma fila, simulando dos requests concurrentes:

```csharp
var clientA = await contextA.Clients.FirstAsync(c => c.Id == clientId);
var clientB = await contextB.Clients.FirstAsync(c => c.Id == clientId); // mismo xmin que A

clientA.Name = "ModificadoPorA";
await contextA.SaveChangesAsync(); // OK, bumpea xmin en la base

clientB.Name = "ModificadoPorB";
await contextB.SaveChangesAsync(); // DbUpdateConcurrencyException: contextB tiene el xmin viejo
```

Resultado real contra Postgres: `contextA` guarda sin problemas; `contextB` lanza `DbUpdateConcurrencyException` con el mensaje estándar de EF ("...data may have been modified or deleted since entities were loaded..."). Después de esto se corrió la suite completa de `BackendEjemplo.Tests` (50/50 verde) y un smoke test de CRUD normal (`POST`/`PUT`/`DELETE` de `Client`) contra la API real para confirmar que el cambio no rompe el camino feliz.