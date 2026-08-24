# Validación de DTOs: cuándo `[Required]` necesita `?`

Referencia rápida para no repetir el bug encontrado en `SaveEmployeeResource.HireDate` (y otros 9 casos más en este proyecto, 10 en total — ver `ARCHITECTURE.md` sección 7). Aplica a **cualquier** `Save*Resource` nuevo, en este proyecto o en cualquier API ASP.NET Core.

## El problema en una frase

`[Required]` solo chequea `valor == null`. Si el tipo de la propiedad **no puede ser `null`**, `[Required]` nunca tiene la oportunidad de fallar — sin importar que el cliente jamás haya mandado ese campo.

## Por qué pasa

Cuando el JSON de un `POST`/`PUT` no incluye una propiedad, el binder (`System.Text.Json` vía `[FromBody]`) simplemente no la toca. La propiedad queda con lo que ya tenía el objeto recién construido:

- Si el tipo es una **reference type** (`string`, un array, otra clase), ese valor por defecto es `null`. `[Required]` lo detecta correctamente.
- Si el tipo es un **value type no-nullable** (`int`, `bool`, `DateTime`, `DateOnly`, un `enum`, etc.), el valor por defecto es `default(T)` — **nunca `null`**: `0`, `false`, `0001-01-01`, el primer valor del enum, `Guid.Empty`... `[Required]` ve ese valor, no es `null`, lo da por válido.

Resultado: el campo "requerido" en realidad no exige nada — un `POST` sin ese campo persiste silenciosamente el valor por defecto en vez de devolver 400. Es un comportamiento de .NET en general, no un bug de este proyecto ni de `RequiredAttribute` específicamente — pero es fácil pisarlo si no se sabe.

## La regla (mnemotécnica)

> **¿El tipo está definido como `struct` en el BCL/C#? Entonces necesita `?` para que `[Required]` funcione. ¿Es una `class`? Ya funciona bien tal cual.**

## Tabla de referencia

| Tipo | ¿Value type (`struct`)? | ¿Necesita `T?` con `[Required]`? | Valor por defecto si falta en el JSON |
|---|---|---|---|
| `string` | No | **No** — ya funciona | `null` (y `[Required]` también rechaza `""` salvo `AllowEmptyStrings = true`) |
| Clase custom anidada (otro DTO) | No | **No** — ya funciona | `null` |
| `List<T>` / `T[]` / cualquier colección | No | No, pero ver nota ⚠️ abajo | `null` |
| `int`, `long`, `short`, `byte`, `uint`, `ulong`, etc. | Sí | **Sí** (`int?`, `long?`, ...) | `0` |
| `decimal`, `double`, `float` | Sí | **Sí** | `0` / `0.0` |
| `bool` | Sí | **Sí** (`bool?`) | `false` |
| `DateTime`, `DateOnly`, `TimeOnly`, `DateTimeOffset` | Sí | **Sí** | `0001-01-01` (mínimo del tipo) |
| `Guid` | Sí | **Sí** | `00000000-0000-0000-0000-000000000000` |
| Cualquier `enum` (`OrderState`, `EnrollmentState`, etc.) | Sí | **Sí** | El miembro con valor `0` del enum (el primero declarado, salvo que se hayan asignado valores explícitos) |
| `char` | Sí | **Sí** | `'\0'` |
| Un `struct` custom propio | Sí | **Sí** | `default` de ese struct |

⚠️ **Nota sobre colecciones**: aunque `List<T>`/`T[]` no necesitan `?` para que `[Required]` detecte que faltan del JSON, `[Required]` **no** detecta una lista vacía (`[]`) — eso sí "está presente", solo que vacía. Si el campo debe tener al menos un elemento, agregar `[MinLength(1)]` además de `[Required]`.

## Ejemplo: correcto vs incorrecto

```csharp
public class SaveCourseResource
{
    [Required, MaxLength(100)]
    public string Name { get; set; }       // OK tal cual: string es reference type

    [Required]
    public int Credits { get; set; }       // ❌ MAL: int no-nullable, [Required] nunca falla.
                                            //    Un POST sin "credits" guarda Credits = 0
                                            //    sin avisar (bug real encontrado en este
                                            //    proyecto, ver ARCHITECTURE.md sección 7).

    [Required]
    public int? Credits2 { get; set; }     // ✅ BIEN: int? sí puede ser null de verdad.
}
```

Y en el `Mapping`, desenvolver con `.Value` — es seguro porque el `Controller` ya cortó con `ValidationProblem(ModelState)` si el campo vino `null` (ver `ARCHITECTURE.md` sección 2.6):

```csharp
public static Course ToEntity(this SaveCourseResource r) => new()
{
    Name = r.Name,
    Credits = r.Credits2!.Value
};
```

## Checklist rápido para un `Save*Resource` nuevo

- [ ] Todo campo `[Required]` cuyo tipo sea un `struct` (ver tabla arriba) está declarado con `?`
- [ ] El `Mapping.ToEntity()` correspondiente usa `.Value` para desenvolver cada uno de esos campos
- [ ] El `Controller` valida `ModelState.IsValid` (o corre bajo `[ApiController]`, que lo hace automático) **antes** de llamar a `.ToEntity()` — sin eso, `.Value` podría explotar con `InvalidOperationException` sobre un `Nullable<T>` sin valor
- [ ] Si el campo requerido es una colección que debe tener al menos un elemento, se agregó `[MinLength(1)]` además de `[Required]`

## Casos ya corregidos en este proyecto

Ver `ARCHITECTURE.md` sección 7 ("Errores reales que ya se cometieron en este proyecto") para el detalle de cada caso encontrado y corregido: `SaveEmployeeResource.HireDate`, `SaveBotLogResource.Fecha`/`.Falla`, `SaveCourseResource.Credits`, `SaveOrderResource.TotalAmount`/`.ClientId`, `SaveEmployeeProfileResource.BirthDate`/`.EmployeeId`, `SaveEnrollmentResource.StudentId`/`.CourseId`.
