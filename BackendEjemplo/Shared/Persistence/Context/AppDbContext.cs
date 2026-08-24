using BackendEjemplo.BotAudit.Domain.Models;
using BackendEjemplo.ManyToManyExample.Domain.Models;
using BackendEjemplo.OneToManyExample.Domain.Models;
using BackendEjemplo.OneToOneExample.Domain.Models;
using BackendEjemplo.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BackendEjemplo.Shared.Persistence.Context
{
    public class AppDbContext: DbContext
    {
        public DbSet<BotLog> BotLogs { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<EmployeeProfile> EmployeeProfiles { get; set; }

        public AppDbContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BotLog>().ToTable("bot_logs");
            modelBuilder.Entity<BotLog>().HasKey(p => p.Id);
            modelBuilder.Entity<BotLog>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<BotLog>().Property(p => p.Bot).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<BotLog>().Property(p => p.Server).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<BotLog>().Property(p => p.Subflujo).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<BotLog>().Property(p => p.Fecha).IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<BotLog>().Property(p => p.UsuarioBot).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<BotLog>().Property(p => p.Plataforma).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<BotLog>().Property(p => p.UsuarioPlataforma).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<BotLog>().Property(p => p.TipoDocumento).HasMaxLength(20);
            modelBuilder.Entity<BotLog>().Property(p => p.NroDocumento).HasMaxLength(100);
            modelBuilder.Entity<BotLog>().Property(p => p.Mensaje).IsRequired();
            modelBuilder.Entity<BotLog>().Property(p => p.Falla).IsRequired();

            modelBuilder.Entity<Client>().ToTable("clients");
            modelBuilder.Entity<Client>().HasKey(p => p.Id);
            modelBuilder.Entity<Client>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<Client>().Property(p => p.Name).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Client>().Property(p => p.LastName).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Client>().Property(p => p.Email).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Client>().Property(p => p.PhoneNumber).IsRequired().HasMaxLength(9);
            modelBuilder.Entity<Client>().Property(p => p.RegistrationDate).IsRequired().ValueGeneratedOnAdd();

            modelBuilder.Entity<Order>().ToTable("orders");
            modelBuilder.Entity<Order>().HasKey(p => p.Id);
            modelBuilder.Entity<Order>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<Order>().Property(p => p.OrderDate).IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<Order>().Property(p => p.TotalAmount).IsRequired();
            modelBuilder.Entity<Order>().Property(p => p.State).IsRequired();

            modelBuilder.Entity<Student>().ToTable("students");
            modelBuilder.Entity<Student>().HasKey(p => p.Id);
            modelBuilder.Entity<Student>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<Student>().Property(p => p.FirstName).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Student>().Property(p => p.LastName).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Student>().Property(p => p.Email).IsRequired().HasMaxLength(50);

            modelBuilder.Entity<Course>().ToTable("courses");
            modelBuilder.Entity<Course>().HasKey(p => p.Id);
            modelBuilder.Entity<Course>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<Course>().Property(p => p.Name).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Course>().Property(p => p.Code).IsRequired().HasMaxLength(20);
            modelBuilder.Entity<Course>().Property(p => p.Credits).IsRequired();

            modelBuilder.Entity<Enrollment>().ToTable("enrollments");
            modelBuilder.Entity<Enrollment>().HasKey(p => p.Id);
            modelBuilder.Entity<Enrollment>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<Enrollment>().Property(p => p.EnrollmentDate).IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<Enrollment>().Property(p => p.State).IsRequired();

            //relaciones entre tablas
            modelBuilder.Entity<Client>()
                .HasMany(p => p.Orders)
                .WithOne(p => p.Client)
                .HasForeignKey(p => p.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación muchos a muchos entre Student y Course a través de la entidad
            // de unión explícita Enrollment (equivale a dos relaciones uno a muchos).
            modelBuilder.Entity<Student>()
                .HasMany(p => p.Enrollments)
                .WithOne(p => p.Student)
                .HasForeignKey(p => p.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Course>()
                .HasMany(p => p.Enrollments)
                .WithOne(p => p.Course)
                .HasForeignKey(p => p.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Un mismo alumno no puede tener más de una inscripción al mismo curso.
            modelBuilder.Entity<Enrollment>()
                .HasIndex(p => new { p.StudentId, p.CourseId })
                .IsUnique();

            modelBuilder.Entity<Employee>().ToTable("employees");
            modelBuilder.Entity<Employee>().HasKey(p => p.Id);
            modelBuilder.Entity<Employee>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<Employee>().Property(p => p.FirstName).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Employee>().Property(p => p.LastName).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Employee>().Property(p => p.Email).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Employee>().Property(p => p.Position).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Employee>().Property(p => p.HireDate).IsRequired();

            modelBuilder.Entity<EmployeeProfile>().ToTable("employee_profiles");
            modelBuilder.Entity<EmployeeProfile>().HasKey(p => p.Id);
            modelBuilder.Entity<EmployeeProfile>().Property(p => p.Id).IsRequired().ValueGeneratedOnAdd();
            modelBuilder.Entity<EmployeeProfile>().Property(p => p.Biography).IsRequired().HasMaxLength(500);
            modelBuilder.Entity<EmployeeProfile>().Property(p => p.Address).IsRequired().HasMaxLength(200);
            modelBuilder.Entity<EmployeeProfile>().Property(p => p.PhoneNumber).IsRequired().HasMaxLength(9);
            modelBuilder.Entity<EmployeeProfile>().Property(p => p.BirthDate).IsRequired();

            // Relación uno a uno: Employee es el lado opcional (puede no tener perfil
            // aún) y EmployeeProfile es el lado obligatorio/dependiente. EF Core exige
            // que EmployeeId sea único para poder interpretar la relación como 1:1
            // (agrega el índice único automáticamente sobre esa FK).
            modelBuilder.Entity<Employee>()
                .HasOne(p => p.Profile)
                .WithOne(p => p.Employee)
                .HasForeignKey<EmployeeProfile>(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.UseSnakeCaseNamingConvention();

            // Concurrencia optimista "gratis": xmin es una columna de sistema que ya
            // existe en toda tabla de Postgres (se incrementa en cada UPDATE de la fila).
            // Mapearla como concurrency token hace que todo UPDATE/DELETE generado por EF
            // incluya "AND xmin = @valorLeido"; si otra transacción ya modificó la fila
            // entremedio, 0 filas se ven afectadas y EF lanza DbUpdateConcurrencyException
            // (el GlobalExceptionHandler la traduce a un 409 legible). No requiere columna
            // nueva ni cambios en Resource/Repository/Service — se aplica a toda entidad
            // automáticamente, incluyendo las que se agreguen a futuro.
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
