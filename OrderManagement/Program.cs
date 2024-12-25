using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrderManagement.Data;
using OrderManagement.Repositories;
using OrderManagement.Services;
using Microsoft.AspNetCore.SignalR;
using System.Text;
using OrderManagement.Hubs;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();

        // CORS Policy Definition
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        // JWT Authentication
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                };
            });

        // Authorization Policy for Admin role
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        });

        // DbContext and other services
        builder.Services.AddDbContext<OrderManagementContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Register Repositories and Services
        builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
        builder.Services.AddScoped<ICustomerService, CustomerService>();
        builder.Services.AddScoped<LoginService>();
        builder.Services.AddScoped<IProductService, ProductService>();
        builder.Services.AddScoped<IProductRepository, ProductRepository>();
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddScoped<IOrderService, OrderService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<ILogService, LogService>(); // LogService Registration
        builder.Services.AddScoped<ILogRepository, LogRepository>(); // LogRepository Registration

        // Add SignalR
        builder.Services.AddSignalR();  // SignalR servisini ekleyin

        // Swagger UI for JWT Authorization
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order Management API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                Description = "Please enter JWT with Bearer into the field"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                }
            });
        });

        var app = builder.Build();

        // Swagger should only be available in development environment
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // CORS Middleware
        app.UseCors("AllowAll");

        // Authentication and Authorization Middleware
        app.UseAuthentication();
        app.UseAuthorization();

        // Other middleware
        app.UseHttpsRedirection();

        // Map SignalR Hub (add this line)
        app.MapHub<OrderHub>("/orderHub"); // Hub'ý buraya ekliyoruz

        // Map controllers
        app.MapControllers();

        // Run the application
        app.Run();
    }
}
