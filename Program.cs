
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Configuration;
using TmsApi.Services;
using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;
using TmsApi.Persistence;
using Tms.Api.Filters;
using Asp.Versioning;
using TmsApi.Middleware;





public partial class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers(options =>
        {
            options.Filters.Add<AuditLogFilter>();
        });

        // builder.Services.AddSingleton<EnrollmentWorker>();
        builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
        builder.Services.AddRouting();
        builder.Services
            .AddAuthentication("Training")
            .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

        builder.Services.AddAuthorization();

        builder.Services.AddOptions<PaymentOptions>()
             .BindConfiguration("Payments")
             .ValidateDataAnnotations()
             .ValidateOnStart();

        builder.Services.AddProblemDetails();

        builder.Services.AddOpenApi();
        builder.Services.AddOpenApi("v1", options =>
        {
            options.ShouldInclude = description =>
            description.GroupName == "v1";
        });
        builder.Services.AddOpenApi("v2", options =>
        {
            options.ShouldInclude = description =>
            description.GroupName == "v2";
        });
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        builder.Services.AddDbContext<TmsDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging());

        builder.Services.AddScoped<ICourseService, CourseService>();
        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }
        else
        {
            app.UseExceptionHandler();
        }

        app.MapScalarApiReference(options =>
        {
            options.WithTitle("TMS API Reference")
            .WithTheme(ScalarTheme.DeepSpace)
            .WithDefaultHttpClient(ScalarTarget.CSharp,
            ScalarClient.HttpClient);
            // Tell Scalar to pull both documents into its sidebar dropdown
            options
            .AddDocument("v1", "API Version 1.0")
            .AddDocument("v2", "API Version 2.0");
        });

        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseExceptionHandler("/error");
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseStatusCodePages();


        app.MapGet("/api/assessments/results", () => Results.Ok(new
        {
            courseCode = "CS-101",
            studentId = "S-001",
            letterGrade = "A"
        })).RequireAuthorization();


        // app.MapGet("/api/enrollments/worker-smoke", (EnrollmentWorker worker) =>
        // {
        //     worker.ProcessBatch();
        //     return Results.Ok("processed");
        // });

        app.MapGet("/api/error", () =>
        {
            throw new TmsDatabaseException("Simulated database failure for ProblemDetails testing");
        });

        app.UseMiddleware<V1DeprecationMiddleware>();


        app.MapControllers();

        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();

            // Apply migrations
            context.Database.Migrate();

            // Seed only if empty
            if (!context.Students.Any())
            {
                var students = new List<Student>
        {
            new() { RegistrationNumber = "TMS-2026-0001", Name = "Alice Smith", GPA = 3.8m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0002", Name = "Bob Jones", GPA = 2.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0003", Name = "Charlie Brown", GPA = 3.4m, IsActive = false },
            new() { RegistrationNumber = "TMS-2026-0004", Name = "Diana Prince", GPA = 3.9m, IsActive = true },
            new() { RegistrationNumber = "TMS-2026-0005", Name = "Evan Wright", GPA = 2.5m, IsActive = true }
        };

                context.Students.AddRange(students);

                var courses = new List<Course>
        {
            new() { Code = "CS-101", Title = "Introduction to Computer Science", MaxCapacity = 30 },
            new() { Code = "CS-201", Title = "Data Structures and Algorithms", MaxCapacity = 25 },
            new() { Code = "MAT-101", Title = "Calculus I", MaxCapacity = 40 }
        };

                context.Courses.AddRange(courses);

                context.SaveChanges(); // Important: generates IDs

                var enrollments = new List<Enrollment>
        {
            new() { StudentId = students[0].Id, CourseId = courses[0].Id, Grade = 4.0m },
            new() { StudentId = students[0].Id, CourseId = courses[1].Id, Grade = 3.6m },
            new() { StudentId = students[1].Id, CourseId = courses[0].Id, Grade = 2.8m },
            new() { StudentId = students[3].Id, CourseId = courses[1].Id, Grade = 3.9m }
        };

                context.Enrollments.AddRange(enrollments);
                context.SaveChanges();
            }
        }

        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
            await DataSeeder.SeedAsync(context);
        }
        app.Run();
    }
}


// Payment Options class
public class PaymentOptions
{
    [System.ComponentModel.DataAnnotations.Required]
    public required string GatewayUrl { get; set; }
    
    [System.ComponentModel.DataAnnotations.Range(100, 100000)]
    public decimal MaxDepositBirr { get; set; }
}
