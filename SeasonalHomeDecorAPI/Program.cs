using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer;
using BusinessLogicLayer.ObjectMapper;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.Interfaces;
using Repository;
using Repository.UnitOfWork;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using BusinessLogicLayer.Hub;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure JWT
var jwtKey = builder.Configuration["Jwt:Key"];
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
var securityKey = new SymmetricSecurityKey(keyBytes);

// 2. Basic Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

// 3. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// 4. Configure Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,        // Thay đổi từ ApiKey sang Http
        Scheme = "bearer",                     // Thêm scheme
        BearerFormat = "JWT",                  // Thêm format
        Description = "Enter your Bearer token in this format: Bearer {token}"
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

// 5. Configure Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = securityKey
        };

        // Thêm logging để debug
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("Authentication failed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated successfully");
                return Task.CompletedTask;
            }
        };
    });

// 6. Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("RequireDecoratorRole", policy =>
        policy.RequireRole("Decorator"));

    options.AddPolicy("RequireCustomerRole", policy =>
        policy.RequireRole("Customer"));

    options.AddPolicy("AllRoles", policy =>
        policy.RequireRole("Admin", "Decorator", "Customer"));
});

// 7. Configure Database
builder.Services.AddDbContext<HomeDecorDBContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// 8. Configure AutoMapper
builder.Services.AddAutoMapper(typeof(HomeDecorAutoMapperProfile).Assembly);

// 9. Configure Dependency Injection
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDecorCategoryRepository, DecorCategoryRepository>();
builder.Services.AddScoped<IDecorCategoryService, DecorCategoryService>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IChatService, ChatService>();


// 10. Build the application
var app = builder.Build();

// 11. Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS phải đứng trước Authentication và Authorization
app.UseCors("AllowAll");

// Map SignalR hub
app.MapHub<ChatHub>("/chatHub");

// Thứ tự này rất quan trọng
app.UseAuthentication();    // Xác thực
app.UseAuthorization();    // Phân quyền

app.MapControllers();

app.Run();