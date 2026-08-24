# BackendEjemplo

Proyecto plantilla de backend REST con **ASP.NET Core 10**, pensado como ejercicio didáctico de arquitectura en capas (**Repository → Service → Controller**) y de los distintos tipos de relaciones que se pueden modelar con **Entity Framework Core** sobre **PostgreSQL**.

El proyecto está organizado en **bounded contexts** independientes; cada uno es un ejercicio autocontenido que ilustra un tipo de relación distinto, pero todos comparten la misma arquitectura, las mismas convenciones y la misma infraestructura (`Shared`).

> **¿Vas a generar código nuevo (a mano o con un LLM) sobre este proyecto?** Ver [`ARCHITECTURE.md`](./ARCHITECTURE.md) — plantillas de código paso a paso y checklist de auditoría para agregar un bounded context nuevo sin repetir bugs ya conocidos. Para escribir o revisar un DTO de entrada (`Save*Resource`) con campos `[Required]`, ver [`VALIDATION.md`](./VALIDATION.md). Para consumir esta API desde un frontend Angular 18+, ver [`ANGULAR.md`](../ANGULAR.md).

## Stack técnico

| Componente | Detalle |
|---|---|
| Runtime | .NET 10 / ASP.NET Core 10 (minimal hosting model) |
| ORM | Entity Framework Core 10 (`Npgsql.EntityFrameworkCore.PostgreSQL`) |
| Base de datos | PostgreSQL |
| Logging | Serilog (consola + archivo rotativo diario en `logs/`) |
| Documentación de API | `Microsoft.AspNetCore.OpenApi` (OpenAPI 3.1) + Swashbuckle SwaggerUI |
| Convención de nombres en BD | snake_case (tablas, columnas, PKs, FKs, índices) vía `ModelBuilderExtensions.UseSnakeCaseNamingConvention()` |

## Arquitectura

Cada bounded context sigue la misma estructura de carpetas y el mismo flujo de responsabilidades:

```
<BoundedContext>/
├── Controllers/                     # Recibe la request HTTP, valida ModelState, traduce BaseResponse → IActionResult
├── Services/                        # Implementación de la lógica de negocio
├── Persistence/Repositories/        # Implementación de acceso a datos (EF Core)
├── Resources/                       # DTOs de entrada (Save*Resource) y salida (*Resource)
├── Mapping/                         # Extension methods Entity <-> Resource (*Mappings.cs)
└── Domain/
    ├── Models/                      # Entidades EF Core (POCOs)
    ├── Enums/                       # Enums de dominio (ej. estados)
    ├── Repositories/                # Interfaces de repositorio (I*Repository)
    └── Services/                    # Interfaces de servicio (I*Service) y Services/Communication/*PageRequest
```

**Flujo de una request**: `Controller` → valida `ModelState` → mapea `Resource → Entity` → llama al `Service` → el `Service` aplica reglas de negocio y usa el `Repository` → el `Repository` traduce a queries EF Core sobre el `AppDbContext` → el `Service` devuelve un `BaseResponse<TEntity>` → el `Controller` lo mapea a `Entity → Resource` y a un código HTTP.

### `Shared/` — infraestructura común a todos los bounded contexts

| Carpeta/archivo | Responsabilidad |
|---|---|
| `Shared/Persistence/Context/AppDbContext.cs` | Único `DbContext` del proyecto; todos los bounded contexts registran sus entidades y relaciones aquí (`OnModelCreating`) |
| `Shared/Persistence/Repositories/BaseRepository.cs` | Implementación genérica de `IBaseRepository<T>`: `AddAsync`, `Update`, `Remove`, `ListAsync`, `ListPageAsync` (con paginación segura, tope `MaxPageSize = 100`) |
| `Shared/Domain/Repositories/IBaseRepository.cs` | Contrato genérico de repositorio, con `CancellationToken` en todos los métodos async |
| `Shared/Persistence/Repositories/UnitOfWork.cs` / `IUnitOfWork.cs` | `CompleteAsync()` — hace efectivo `SaveChangesAsync` sobre el `AppDbContext` compartido |
| `Shared/Domain/Services/Communication/BaseResponse.cs` | Envoltorio de resultado de negocio: `Success`, `Message`, `Content`, `IsConflict` (distingue 404 de 409) |
| `Shared/Domain/Services/Communication/PageResponse.cs` / `Shared/Domain/Repositories/Page.cs` | `Page<TEntity>` (interno, entre Repository y Service) vs `PageResponse<TResource>` (de cara a la API) |
| `Shared/Domain/Services/Communication/BasePageRequest.cs` | Base de todo `*PageRequest` (`PageIndex`, `PageSize`) |
| `Shared/Mapping/PageMappings.cs` | Extension `Page<T>.ToResponse()` |
| `Shared/Middleware/GlobalExceptionHandler.cs` | `IExceptionHandler` global: loguea la excepción y la escribe como `ProblemDetails` 500 vía `IProblemDetailsService` (mismo formato/`traceId` que el resto de la API); retorna temprano sin loguear si la request fue cancelada por el cliente (`OperationCanceledException` + `RequestAborted`) |
| `Shared/Extensions/ModelBuilderExtensions.cs` | Convención snake_case para tablas/columnas/índices/constraints |
| `Shared/Extensions/ControllerBaseExtensions.cs` | `this.ToProblem(result, failureStatusCode)` — traduce un `BaseResponse<T>` fallido al `ProblemDetails` correspondiente (404/400/409 según `IsConflict`) |

### Convenciones y decisiones de diseño

- **Paginación**: todo listado expone `pageIndex`/`pageSize` (querystring) y responde `{ data, pageIndex, pageSize, totalRecords }`. `pageSize` se acota a `MaxPageSize = 100`; un `pageIndex` fuera de rango se clampea a la última página válida en vez de dar error.
- **Sorting**: todo listado acepta `sortBy`/`sortDescending` (querystring). Cada `Service` define una whitelist propia de columnas ordenables; un `sortBy` vacío o no reconocido cae en silencio al orden por defecto (normalmente por `id`) — nunca rompe la query ni expone una columna arbitraria. Detalle en `ARCHITECTURE.md` sección 4.
- **Contrato de errores unificado**: validación (`ModelState`), errores de negocio (`BaseResponse`) y excepciones no controladas responden siempre el mismo shape `ProblemDetails`, nunca un string plano. `BaseResponse<T>.IsConflict` distingue "no existe" (**404**) de "conflicto de negocio" (**409**), ej. intentar borrar un recurso que tiene dependientes — ver detalle en `ARCHITECTURE.md` sección 4.
- **Enums como string**: todo enum de dominio (`OrderState`, `EnrollmentState`) viaja en JSON como el nombre del valor (`"state": "Pending"`), nunca como el entero subyacente — configurado en `Program.cs` con `JsonStringEnumConverter`.
- **Borrado de relaciones**: todas las FKs entre bounded contexts usan `DeleteBehavior.Restrict` (nunca `Cascade`). El `Service` del lado "padre" siempre hace un pre-check antes de borrar y devuelve un 409 con mensaje claro, en vez de dejar que la excepción de la base de datos llegue como un 500 genérico.
- **Concurrencia optimista**: toda entidad está protegida contra "last write wins" silencioso vía la columna de sistema `xmin` de Postgres — si dos requests leen el mismo recurso y ambas intentan guardar, la segunda recibe **409** en vez de pisar el cambio de la primera. No requiere ningún campo extra en los `Resource` — ver `ARCHITECTURE.md` sección 9.
- **Fixup automático de EF Core**: al crear un hijo (`Order`, `Enrollment`, `EmployeeProfile`) justo después de haber cargado su padre desde el mismo `DbContext` (sin `AsNoTracking`), EF Core enlaza automáticamente la navegación sin necesidad de un `Include` adicional ni una recarga extra.
- **`CancellationToken`**: se propaga de punta a punta — se bindea automáticamente desde `HttpContext.RequestAborted` en la firma de cada acción del controller, y desde ahí baja explícito por `Service → Repository → EF Core`.
- **Fechas de auditoría** (`RegistrationDate`, `OrderDate`, `HireDate`, `EnrollmentDate`): se fijan en el `Mapping` (`DateTime.UtcNow`) al crear el recurso, nunca las envía el cliente.
- **Entidades "hijas" con estado** (`Order`, `Enrollment`): nacen siempre en su estado inicial (`Pending`/`Active`) y solo cambian de estado a través de un endpoint `PATCH state/{id}` dedicado — el estado no es editable vía `PUT`.
- **DTOs de solo lectura embebidos**: cuando un resource anida a otro (ej. `OrderResource.Client`, `EnrollmentResource.Student`/`Course`, `EmployeeProfileResource.Employee`), la anidación va siempre en una sola dirección — el lado "principal" nunca vuelve a anidar al "dependiente" — para evitar recursión infinita en el mapeo.
- **Migraciones**: el proyecto usa EF Core Migrations (no `EnsureCreated()`); `Program.cs` corre `context.Database.Migrate()` al iniciar.
- **Filtros de rango de fecha sin hora** (`BotLogPageRequest.StartDate`/`EndDate`, `ClientPageRequest.StartRegistrationDate`/`EndRegistrationDate`, `OrderPageRequest.StartOrderDate`/`EndOrderDate`, `EnrollmentPageRequest.StartEnrollmentDate`/`EndEnrollmentDate`, `EmployeePageRequest.StartHireDate`/`EndHireDate`): todos usan `DateOnly?`, no `DateTime?` — es más preciso para quien arma la request (no tiene que inventar una hora). "El día" se interpreta en la **zona horaria de negocio** (`America/Lima`, configurada en un único lugar: `DateOnlyExtensions.BusinessTimeZone`), no en UTC — un registro creado a las 20:00 hora Lima cuenta para ese día calendario aunque en UTC ya haya cruzado la medianoche. Detalle y motivo en `ARCHITECTURE.md` sección 4 ("Zona horaria en filtros de fecha").

## Bounded contexts

### 1. `BotAudit` — CRUD simple (una sola entidad)

Registro de auditoría de ejecuciones de un bot (`BotLog`). Es el ejemplo más simple: un único CRUD sin relaciones, útil como punto de partida antes de pasar a los ejercicios de relaciones.

- **Entidad**: `BotLog` (bot, servidor, subflujo, fecha, usuario, plataforma, documento, mensaje, si falló)
- **Endpoints**: `GET /api/v1/bot_logs` (paginado/filtrable), `GET /api/v1/bot_logs/{id}`, `POST`, `PUT /{id}`, `DELETE /{id}`

### 2. `OneToManyExample` — relación uno a muchos

Un `Client` tiene muchos `Order`. FK explícita `Order.ClientId`, `DeleteBehavior.Restrict`.

- **Entidades**: `Client` (nombre, apellido, email, teléfono, fecha de registro) — `Order` (fecha, monto total, estado `OrderState`: `Pending`/`Paid`/`Sent`, `Client`)
- **Reglas de negocio**:
  - Un `Order` no puede crearse con un `ClientId` inexistente (→ 400).
  - Un `Order` nace siempre en `Pending`; su estado solo cambia vía `PATCH`.
  - No se puede borrar un `Client` que tiene `Order`s asociados (→ 409).
- **Endpoints**:
  | Método | Ruta | Descripción |
  |---|---|---|
  | GET | `/api/v1/clients` | Paginado, filtra por nombre/email/teléfono/rango de fecha de registro |
  | GET | `/api/v1/clients/{id}` | Detalle |
  | POST | `/api/v1/clients` | Alta |
  | PUT | `/api/v1/clients/{id}` | Edición |
  | DELETE | `/api/v1/clients/{id}` | Baja (409 si tiene pedidos) |
  | GET | `/api/v1/orders` | Paginado, filtra por rango de fecha, estado, nombre de cliente |
  | POST | `/api/v1/orders` | Alta (nace en `Pending`) |
  | PATCH | `/api/v1/orders/state/{id}` | Cambia el estado del pedido |

### 3. `ManyToManyExample` — relación muchos a muchos

`Student` y `Course` se relacionan a través de una **entidad de unión explícita** `Enrollment` (con `EnrollmentDate` y `State`), en vez del "skinny many-to-many" nativo de EF Core — así la relación tiene datos propios y cada entidad conserva su propio repositorio/servicio/controller.

- **Entidades**: `Student` (nombre, apellido, email) — `Course` (nombre, código, créditos) — `Enrollment` (fecha de inscripción, estado `EnrollmentState`: `Active`/`Completed`/`Cancelled`, `Student`, `Course`)
- **Reglas de negocio**:
  - Un `Enrollment` no puede crearse con `StudentId`/`CourseId` inexistentes (→ 400).
  - Un mismo alumno no puede inscribirse dos veces al mismo curso — índice único `(student_id, course_id)` en BD + pre-check en el service (→ 409).
  - `Enrollment` nace siempre en `Active`; su estado solo cambia vía `PATCH`.
  - No se puede borrar un `Student` o `Course` que tenga inscripciones asociadas (→ 409).
- **Endpoints**:
  | Método | Ruta | Descripción |
  |---|---|---|
  | GET/POST/PUT/DELETE | `/api/v1/students` | CRUD completo (409 al borrar con inscripciones) |
  | GET/POST/PUT/DELETE | `/api/v1/courses` | CRUD completo (409 al borrar con inscripciones) |
  | GET | `/api/v1/enrollments` | Paginado, filtra por alumno, curso, estado, rango de fecha |
  | POST | `/api/v1/enrollments` | Alta (nace en `Active`; 400 FK inválida, 409 duplicado) |
  | PATCH | `/api/v1/enrollments/state/{id}` | Cambia el estado de la inscripción |

### 4. `OneToOneExample` — relación uno a uno

Un `Employee` tiene **como máximo** un `EmployeeProfile`. Se modela con una **FK única** (`EmployeeProfile.EmployeeId` + índice `UNIQUE`, generado automáticamente por EF Core al configurar la relación como 1:1), en vez de clave primaria compartida — así `EmployeeProfile` conserva su propio `Id` identity, consistente con el resto de entidades del proyecto.

- **Entidades**: `Employee` (nombre, apellido, email, puesto, fecha de contratación) — lado opcional de la relación (puede no tener perfil) — `EmployeeProfile` (biografía, dirección, teléfono, fecha de nacimiento, `Employee`) — lado obligatorio/dependiente
- **Reglas de negocio**:
  - Un `EmployeeProfile` no puede crearse con `EmployeeId` inexistente (→ 400).
  - Un `Employee` no puede tener más de un `EmployeeProfile` — índice único en BD + pre-check en el service (→ 409).
  - No se puede borrar un `Employee` que ya tiene perfil (→ 409). Borrar un `EmployeeProfile`, en cambio, no tiene restricciones (nada depende de él).
- **Endpoints**:
  | Método | Ruta | Descripción |
  |---|---|---|
  | GET/POST/PUT/DELETE | `/api/v1/employees` | CRUD completo (409 al borrar con perfil asociado) |
  | GET/POST/PUT/DELETE | `/api/v1/employeeprofiles` | CRUD completo (400 FK inválida, 409 duplicado) |

## Cómo correr el proyecto

> Los comandos de esta sección asumen que se ejecutan desde la **raíz de la solución** (la carpeta que contiene `BackendEjemplo.slnx` y `.config/`, un nivel arriba de este `README.md`).

### Prerrequisitos

- .NET SDK 10
- PostgreSQL accesible (por defecto se espera en `localhost:5432`)

### 1. Configurar la cadena de conexión

`appsettings.json` versiona la cadena de conexión **sin password** (solo como referencia de formato). El valor real se guarda en **user-secrets**, para no versionar credenciales:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "host=localhost;port=5432;username=postgres;password=<tu-password>;database=postgres" --project BackendEjemplo/BackendEjemplo.csproj
```

Esto solo funciona porque el `.csproj` ya tiene un `<UserSecretsId>` (generado con `dotnet user-secrets init`); ASP.NET Core carga automáticamente los user-secrets cuando `ASPNETCORE_ENVIRONMENT=Development`, con prioridad sobre `appsettings.json`. En otros ambientes (staging/producción), la cadena de conexión se debe proveer por variable de entorno (`ConnectionStrings__DefaultConnection`) o el secret manager que corresponda — nunca hardcodeada en `appsettings.json`.

En desarrollo local no hace falta tocar nada más: sin `Cors:AllowedOrigins` configurado, la API acepta requests de cualquier origen (útil con un frontend Angular corriendo en otro puerto). Para producción, restringir los orígenes permitidos en `appsettings.json`:

```json
{ "Cors": { "AllowedOrigins": ["https://mi-frontend.ejemplo.com"] } }
```

### 2. Restaurar herramientas locales (dotnet-ef)

El proyecto usa `dotnet-ef` como herramienta local (manifest en `.config/dotnet-tools.json`):

```bash
dotnet tool restore
```

### 3. Aplicar migraciones

`Program.cs` ya corre `context.Database.Migrate()` automáticamente al iniciar la aplicación, así que este paso es opcional (solo necesario si querés aplicar las migraciones sin levantar la API):

```bash
dotnet tool run dotnet-ef database update --project BackendEjemplo/BackendEjemplo.csproj --startup-project BackendEjemplo/BackendEjemplo.csproj
```

### 4. Levantar la API

```bash
dotnet run --project BackendEjemplo/BackendEjemplo.csproj
```

En `Development`, Swagger UI queda disponible en `/swagger` (redirige a `/openapi/v1.json`, generado por `Microsoft.AspNetCore.OpenApi`).

### Correr los tests

> `BackendEjemplo.Tests/` es **opcional**: existe en este repo de ejemplo, pero la plantilla también se usa para agregar endpoints a APIs reales que todavía no tienen testing adoptado. No es un paso obligatorio de ningún flujo — se crea/extiende únicamente si se pide explícitamente (ver `ARCHITECTURE.md` sección 8).

Si el proyecto de test existe (unit tests de la capa `Service`, xunit.v3 + AwesomeAssertions + Moq, sin necesidad de Postgres — ver `ARCHITECTURE.md` sección 8 para el detalle de qué cubren):

```bash
dotnet test BackendEjemplo.Tests/BackendEjemplo.Tests.csproj
```

### Generar una nueva migración (al agregar/modificar un bounded context)

```bash
dotnet tool run dotnet-ef migrations add <NombreDeLaMigracion> \
  --project BackendEjemplo/BackendEjemplo.csproj \
  --startup-project BackendEjemplo/BackendEjemplo.csproj \
  --output-dir Shared/Persistence/Migrations
```

## Estructura del repositorio

```
BackendEjemplo/                      # solución (.slnx) + manifest de herramientas (.config/)
├── ANGULAR.md                       # guía para consumir esta API desde un CRUD Angular 18+
├── BackendEjemplo/                  # proyecto ASP.NET Core
│   ├── README.md                    # este archivo
│   ├── ARCHITECTURE.md              # guía de generación de código + checklist de auditoría
│   ├── VALIDATION.md                # referencia: qué tipos necesitan `?` para que [Required] funcione
│   ├── Program.cs                   # composition root: DI, pipeline HTTP, Serilog, OpenAPI, migraciones
│   ├── appsettings.json / appsettings.Development.json
│   ├── BotAudit/
│   ├── OneToManyExample/
│   ├── ManyToManyExample/
│   ├── OneToOneExample/
│   └── Shared/
└── BackendEjemplo.Tests/            # OPCIONAL — unit tests de la capa Service (xunit.v3 + AwesomeAssertions + Moq). No crear salvo pedido explícito, ver "Correr los tests" y ARCHITECTURE.md sección 8.
```
