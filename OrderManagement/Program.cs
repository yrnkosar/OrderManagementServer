using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrderManagement.Data;
using OrderManagement.Repositories;
using OrderManagement.Services;
using System.Text;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();

        // JWT Authentication yapýlandýrmasý
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
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],  // JWT Issuer (Yayýncý)
                    ValidAudience = builder.Configuration["Jwt:Audience"],  // JWT Audience (Hedef Kitle)
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])) // JWT Secret Key (Gizli Anahtar)
                };
            });

        // Authorization için rol tabanlý yetkilendirme ekleyelim
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        });

        // DbContext ve diðer servisleri ekleyelim
        builder.Services.AddDbContext<OrderManagementContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Repositories ve Services'i DI'ye ekleyelim
        builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
        builder.Services.AddScoped<ICustomerService, CustomerService>();
        builder.Services.AddScoped<LoginService>();

        // Swagger UI için JWT Authorization ekleyelim
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
                Description = "Please enter JWT with Bearer into field"
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

        // Swagger'ý yalnýzca geliþtirme ortamýnda çalýþtýralým
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Authentication ve Authorization middleware'lerini ekleyelim
        app.UseAuthentication();
        app.UseAuthorization();

        // Diðer middleware'ler
        app.UseHttpsRedirection();

        app.MapControllers();

        app.Run();
    }
}
