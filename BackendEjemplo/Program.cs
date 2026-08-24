using BackendEjemplo.BotAudit.Domain.Repositories;
using BackendEjemplo.BotAudit.Domain.Services;
using BackendEjemplo.BotAudit.Persistence.Repositories;
using BackendEjemplo.BotAudit.Services;
using BackendEjemplo.ManyToManyExample.Domain.Repositories;
using BackendEjemplo.ManyToManyExample.Domain.Services;
using BackendEjemplo.ManyToManyExample.Persistence.Repositories;
using BackendEjemplo.ManyToManyExample.Services;
using BackendEjemplo.OneToManyExample.Domain.Repositories;
using BackendEjemplo.OneToManyExample.Domain.Services;
using BackendEjemplo.OneToManyExample.Persistence.Repositories;
using BackendEjemplo.OneToManyExample.Services;
using BackendEjemplo.OneToOneExample.Domain.Repositories;
using BackendEjemplo.OneToOneExample.Domain.Services;
using BackendEjemplo.OneToOneExample.Persistence.Repositories;
using BackendEjemplo.OneToOneExample.Services;
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

// Add services to the container.

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

builder.Services.AddScoped<IBotLogRepository, BotLogRepository>();
builder.Services.AddScoped<IBotLogService, BotLogService>();

builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IEmployeeProfileRepository, EmployeeProfileRepository>();
builder.Services.AddScoped<IEmployeeProfileService, EmployeeProfileService>();

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
    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            // ApplicationName es el nombre del ensamblado (el nombre real del proyecto,
            // sea cual sea) — nunca hardcodear un nombre de proyecto acá.
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
