# ANGULAR.md — Guía para generar CRUDs en Angular 18+ contra este backend

Este documento es el equivalente frontend de [`BackendEjemplo/ARCHITECTURE.md`](./BackendEjemplo/ARCHITECTURE.md): una plantilla operativa para generar, con Angular 18+, un módulo de features que consuma la API descripta en [`BackendEjemplo/README.md`](./BackendEjemplo/README.md). No asume un proyecto Angular ya creado — sirve tanto para arrancar uno nuevo como para agregarle un CRUD a uno existente.

> No hay código Angular en este repositorio todavía. Esta guía documenta el **contrato y las convenciones a seguir** para que el frontend hable el mismo idioma que el backend (mismos nombres de campo, misma paginación, mismo manejo de errores).

---

## 1. Convenciones de Angular 18+ a usar

- **Standalone components** siempre — nada de `NgModule` (salvo librerías de terceros que lo exijan).
- **`inject()`** en vez de inyección por constructor, para servicios y `ActivatedRoute`/`Router`.
- **Signals** para el estado local de cada componente (`signal`, `computed`) en vez de `BehaviorSubject` cuando el estado es simple (lista + loading + error). RxJS se reserva para el flujo de HTTP en sí (`HttpClient` sigue devolviendo `Observable`).
- **Control flow nativo** (`@if`, `@for`, `@switch`) — no `*ngIf`/`*ngFor`.
- **Formularios reactivos tipados** (`FormGroup<T>`, `nonNullable`) — no template-driven forms.
- **`OnPush`** change detection en todos los componentes.
- **`HttpClient` con `provideHttpClient(withFetch())`** en `app.config.ts`.

## 2. Estructura de carpetas (mapea 1:1 contra los bounded contexts del backend)

```
src/app/
├── core/
│   ├── models/
│   │   ├── page-response.model.ts     # PageResponse<T> genérico
│   │   └── base-page-request.model.ts # pageIndex/pageSize genérico
│   └── interceptors/
│       └── api-error.interceptor.ts   # traduce 400/404/409/500 a un formato uniforme
├── features/
│   ├── one-to-many-example/           # 1 feature folder por bounded context del backend
│   │   ├── clients/
│   │   │   ├── models/
│   │   │   │   ├── client.model.ts        # == ClientResource
│   │   │   │   └── save-client.model.ts   # == SaveClientResource
│   │   │   ├── client.service.ts
│   │   │   ├── client-list.component.ts
│   │   │   └── client-form.component.ts
│   │   └── orders/
│   │       └── ...misma forma que clients...
│   ├── many-to-many-example/
│   │   ├── students/ courses/ enrollments/
│   └── one-to-one-example/
│       ├── employees/ employee-profiles/
└── app.routes.ts
```

Cada carpeta de recurso (`clients/`, `orders/`, ...) es autocontenida, igual que cada bounded context en el backend: modelo(s), servicio, componente de listado, componente de formulario.

## 3. Checklist: agregar el CRUD de un recurso nuevo

Dado un recurso `<Entity>` ya expuesto por el backend en `api/v1/<entity>s`:

1. `models/<entity>.model.ts` — interfaz calcada de `<Entity>Resource` (mismos nombres de campo, camelCase — el backend ya serializa camelCase por default en `System.Text.Json`)
2. `models/save-<entity>.model.ts` — interfaz calcada de `Save<Entity>Resource`
3. `<entity>.service.ts` — wrapper de `HttpClient` con los 5 métodos CRUD + tipado del `PageResponse<T>`
4. `<entity>-list.component.ts` — tabla paginada + filtros + acciones (editar/eliminar)
5. `<entity>-form.component.ts` — alta/edición con reactive forms, validadores espejo de los `[Required]`/`[MaxLength]` del `Save<Entity>Resource`
6. Rutas en `app.routes.ts` (o en un `<bounded-context>.routes.ts` si se usa lazy loading por feature)
7. Probar contra el backend real los mismos casos del checklist de auditoría de `ARCHITECTURE.md` sección 6, ahora desde la UI: alta válida, alta inválida (400), alta duplicada/conflicto (409), edición de inexistente (404), borrado con dependientes (409)

## 4. Contrato: modelos TypeScript

### Genéricos (`core/models/`)

```typescript
// page-response.model.ts
export interface PageResponse<T> {
  data: T[];
  pageIndex: number;
  pageSize: number;
  totalRecords: number;
}

// base-page-request.model.ts
export interface BasePageRequest {
  pageIndex?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}
```

`sortBy`/`sortDescending` los soporta todo listado del backend (heredan de `BasePageRequest` del lado C# también), pero **cada recurso solo acepta un subconjunto de nombres de columna** (la whitelist `SortableColumns` de su `Service`, ver `ARCHITECTURE.md` sección 2.4/4) — un `sortBy` no reconocido no rompe nada, el backend cae en silencio a su orden por defecto. Si un componente de listado expone una UI de ordenamiento (ej. click en el header de una columna), los valores que puede mandar en `sortBy` deben coincidir con esa whitelist del backend, no inventarse del lado del cliente.

### Por recurso (ejemplo con `Client`/`Order`, ver `OneToManyExample` en el backend)

```typescript
// features/one-to-many-example/clients/models/client.model.ts
export interface Client {
  id: number;
  name: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  registrationDate: string; // ISO 8601 — el backend siempre manda UTC
}

// save-client.model.ts — SIN id ni registrationDate: los calcula el servidor
export interface SaveClient {
  name: string;
  lastName: string;
  email: string;
  phoneNumber: string;
}

export interface ClientPageRequest extends BasePageRequest {
  fullName?: string;
  email?: string;
  phoneNumber?: string;
  startRegistrationDate?: string;
  endRegistrationDate?: string;
}
```

**Regla**: el modelo de lectura (`Client`) y el de escritura (`SaveClient`) son interfaces separadas, igual que `ClientResource`/`SaveClientResource` en el backend — nunca una sola interfaz con todos los campos opcionales. Si el recurso tiene una entidad anidada de solo lectura (ej. `Order.client: Client`), esa anidación va en una sola dirección, igual que en el backend.

**Filtros de rango de fecha** (`startRegistrationDate`/`endRegistrationDate` y equivalentes): van tipados `string` con formato `yyyy-MM-dd` (sin hora) — el backend los recibe como `DateOnly`. El componente que arme estos filtros (ej. un date picker) manda directamente el día calendario que eligió el usuario, **sin convertir a UTC ni a ningún otro huso**: el backend interpreta ese día en la zona horaria de negocio (`America/Lima`) y arma el rango correcto contra la columna UTC internamente (ver `ARCHITECTURE.md` sección 4). Si el Angular convierte a UTC antes de mandar el filtro, se reintroduce el mismo bug que el backend ya resolvió.

### Enums

El backend serializa todo enum de dominio como **string con el nombre del valor** (`"state": "Pending"`, nunca `"state": 0`). El modelo TypeScript debe reflejar eso con un **union type de literales de string**, no con un `enum` numérico de TypeScript ni con `number`:

```typescript
// features/one-to-many-example/orders/models/order.model.ts
export type OrderState = 'Pending' | 'Paid' | 'Sent';

export interface Order {
  id: number;
  orderDate: string;
  totalAmount: number;
  state: OrderState;
  client: Client;
}
```

Los nombres de los literales deben coincidir **exactamente** (mismo casing) con los del enum de C# — es el mismo contrato en ambas puntas, no una traducción.

## 5. Contrato: servicio HTTP

```typescript
// client.service.ts
import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PageResponse } from '../../../core/models/page-response.model';
import { Client, ClientPageRequest, SaveClient } from './models/client.model';

@Injectable({ providedIn: 'root' })
export class ClientService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/clients';

  list(request: ClientPageRequest): Observable<PageResponse<Client>> {
    let params = new HttpParams();
    Object.entries(request).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return this.http.get<PageResponse<Client>>(this.baseUrl, { params });
  }

  getById(id: number): Observable<Client> {
    return this.http.get<Client>(`${this.baseUrl}/${id}`);
  }

  create(resource: SaveClient): Observable<Client> {
    return this.http.post<Client>(this.baseUrl, resource);
  }

  update(id: number, resource: SaveClient): Observable<Client> {
    return this.http.put<Client>(`${this.baseUrl}/${id}`, resource);
  }

  delete(id: number): Observable<Client> {
    return this.http.delete<Client>(`${this.baseUrl}/${id}`);
  }
}
```

- **No** filtrar `pageIndex`/`pageSize` a mano en cada servicio — el `HttpParams` builder de arriba ya omite campos vacíos/`undefined`, calcado del criterio `IsNullOrWhiteSpace` que usa el backend para ignorar filtros no provistos.
- El nombre de cada query param **debe** coincidir en camelCase exacto con la propiedad del `*PageRequest` del backend (ASP.NET Core hace binding case-insensitive, pero mantené la paridad para que el contrato sea auditable a simple vista).

## 6. Contrato: manejo de errores por código HTTP

**Todo** error de esta API — validación, negocio o no controlado — responde el mismo shape `ProblemDetails` (RFC 9457), nunca un string plano ni un objeto custom distinto por caso. Esta tabla es el mapeo obligatorio — no improvisar manejo de errores por componente, centralizarlo en un interceptor:

| Código | Origen en el backend | Campo con el mensaje | Tratamiento en Angular |
|---|---|---|---|
| **400** (validación) | `ModelState` inválido | `errors: { campo: [...] }` | Mapear `errors` a los controles del formulario reactivo |
| **400** (negocio) | `BaseResponse.Success == false`, `IsConflict == false` | `detail` | Mostrar como error general del formulario (no es un campo específico, ej. FK a padre inexistente) |
| **404** | `BaseResponse.Success == false`, `IsConflict == false` | `detail` | Mostrar "no encontrado" / redirigir al listado si es una edición sobre un recurso borrado |
| **409** (negocio) | `BaseResponse.Success == false`, `IsConflict == true` | `detail` | Mostrar el mensaje tal cual como advertencia (ej. toast) — es un mensaje de negocio ya pensado para el usuario final, no requiere traducción |
| **409** (concurrencia) | `DbUpdateConcurrencyException` — otro proceso modificó/borró el recurso entre que se leyó y se guardó | `detail` | Mismo tratamiento que el 409 de negocio (toast), pero además conviene refrescar el recurso (volver a pedirlo) antes de dejar reintentar el guardado — el dato que tiene el formulario en memoria ya está desactualizado |
| **500** | Excepción no controlada (`GlobalExceptionHandler`) | `title` (genérico, sin `detail`) | Mensaje genérico ("Ocurrió un error, intentá de nuevo") — nunca mostrar `title`/`type` crudo como si fuera el mensaje de negocio |

```typescript
// core/interceptors/api-error.interceptor.ts
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

// Espejo de ProblemDetails (+ ValidationProblemDetails) que devuelve el backend.
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>; // solo presente en errores de validación (400)
}

export interface ApiError {
  status: number;
  isConflict: boolean;
  message: string;
  fieldErrors?: Record<string, string[]>;
  traceId?: string;
}

export const apiErrorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(
    catchError((err: HttpErrorResponse) => {
      const problem = err.error as ProblemDetails | null;
      const apiError: ApiError = {
        status: err.status,
        isConflict: err.status === 409,
        // detail trae el mensaje de negocio (404/400/409); si no hay detail
        // (ej. 500 genérico), caemos al title.
        message: problem?.detail ?? problem?.title ?? 'Ocurrió un error inesperado',
        fieldErrors: problem?.errors,
        traceId: problem?.traceId,
      };
      return throwError(() => apiError);
    }),
  );
```

Nota sobre el 409: el backend distingue 404 de 409 exactamente con el mismo criterio en todos los bounded contexts (`IsConflict` en `BaseResponse<T>`) — el frontend debería reflejar esa misma distinción en la UI (ej. 404 nunca debería aparecer como resultado de una acción del usuario sobre un listado ya cargado, mientras que 409 sí es una advertencia legítima y esperable). El `traceId` conviene loguearlo (ej. a la consola o a un servicio de telemetría) junto con el error mostrado al usuario — es la clave para correlacionar el reporte de un usuario con la línea exacta del log de Serilog del backend.

## 7. Componentes: forma esperada

### Listado (`<entity>-list.component.ts`)

- Estado con signals: `items = signal<Client[]>([])`, `loading = signal(false)`, `totalRecords = signal(0)`, `pageIndex = signal(0)`.
- Filtros como `FormGroup` separado del de alta/edición, con `debounceTime` antes de disparar `list()`.
- Paginación: nunca pedir más de lo que el usuario ve — replicar `pageSize` con el mismo tope mental de 100 que usa el backend (`MaxPageSize`), aunque el backend ya lo acota server-side.
- Acciones por fila: editar (navega al form en modo edición) y eliminar (confirma, llama `delete()`, en caso de 409 muestra el mensaje sin intentar reintentar automáticamente).

### Formulario (`<entity>-form.component.ts`)

- Un solo componente para alta y edición (`@Input() id?: number` o resuelto por ruta); si `id` está presente, precarga con `getById()`.
- Validadores del `FormGroup` en espejo exacto de las `DataAnnotations` del `Save<Entity>Resource` del backend (`Validators.required`, `Validators.maxLength(100)`, `Validators.email`, etc.) — así el usuario ve el error antes de golpear la API, y si igual llega un 400, el interceptor mapea `fieldErrors` a los mismos controles.
- Nunca incluir en el formulario campos que el backend calcula (fechas de auditoría, estado inicial, ids) — mismo criterio que "`SaveXResource` no expone campos que el servidor calcula" en `ARCHITECTURE.md`.
- Al guardar, deshabilitar el submit hasta que la request resuelva (éxito o error) — evita altas duplicadas por doble click.

### Entidades con estado (`Order`, `Enrollment`)

- El cambio de estado **no** va en el formulario de edición general — es una acción separada (ej. un dropdown de "cambiar estado" en el detalle/listado) que llama a un método dedicado del servicio (`changeState(id, state)` → `PATCH state/{id}`), replicando que el backend tampoco permite cambiar el estado vía `PUT`.

## 8. Checklist de auditoría (frontend)

- [ ] Los nombres de campo de cada modelo TypeScript coinciden exactamente (camelCase) con el `Resource`/`Save*Resource` del backend
- [ ] El modelo de lectura y el de escritura son interfaces separadas
- [ ] Los query params del listado coinciden exactamente con las propiedades del `*PageRequest` del backend
- [ ] El manejo de 400/404/409/500 está centralizado en el interceptor, no repetido por componente
- [ ] Los formularios no incluyen campos calculados por el servidor
- [ ] Las entidades con máquina de estados cambian de estado por una acción separada del `PUT` general
- [ ] Todo componente es standalone, usa `inject()`, `OnPush`, y control flow nativo (`@if`/`@for`)
- [ ] Los signals de `loading`/`error` se resetean correctamente entre requests (evitar loaders que quedan pegados en `true` si la request falla)
