using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.ObjectMapper;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Repository.Interfaces;
using Repository.UnitOfWork;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using BusinessLogicLayer.Hub;
using BusinessLogicLayer.Services;
using Repository.Repositories;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Design;
using Nest;
using Net.payOS;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
IConfiguration configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

// 1. Configure JWT
var jwtKey = builder.Configuration["Jwt:Key"];
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
var securityKey = new SymmetricSecurityKey(keyBytes);

// 2. Basic Services
builder.Services.AddControllers()
       .AddJsonOptions(options =>
       {
           options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
           options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
           options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
           options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // Convert number to text in enum
       });

builder.Services.AddEndpointsApiExplorer();

PayOS payOS = new PayOS(configuration["Environment:PAYOS_CLIENT_ID"] ?? throw new Exception("Cannot find environment"),
                    configuration["Environment:PAYOS_API_KEY"] ?? throw new Exception("Cannot find environment"),
                    configuration["Environment:PAYOS_CHECKSUM_KEY"] ?? throw new Exception("Cannot find environment"));
builder.Services.AddSingleton(payOS);

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
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
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

// 9. Configure Cloudinary
builder.Services.AddScoped<ICloudinaryService>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var cloudinaryConfig = configuration.GetSection("Cloudinary");
    return new CloudinaryService(
        cloudinaryConfig["CloudName"],
        cloudinaryConfig["ApiKey"],
        cloudinaryConfig["ApiSecret"]
    );
});

builder.Services.AddSingleton<IElasticClient>(sp =>
{
    var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
        .DefaultIndex("decorservice_index"); // default index
    return new ElasticClient(settings);
});

// 10. Configure Dependency Injection
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAccountManagementService, AccountManagementService>();
builder.Services.AddScoped<IAccountProfileService, AccountProfileService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDecorCategoryService, DecorCategoryService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IProviderService, ProviderService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ITicketTypeService, TicketTypeService>();
builder.Services.AddScoped<ISupportService, SupportService>();
builder.Services.AddScoped<IDataSeeder, DataSeeder>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<FcmService>();
builder.Services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();
builder.Services.AddScoped<IFollowService, FollowService>();
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IDecorServiceService, DecorServiceService>();
builder.Services.AddScoped<IElasticClientService, ElasticClientService>();
builder.Services.AddScoped<IBookingService, BookingService>();

// 11. Build the application
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    await seeder.SeedAdminAsync();
}

// 12. Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS must be configured before Authentication and Authorization
app.UseCors("AllowAll");

// Map SignalR hub
app.MapHub<ChatHub>("/chatHub");
app.MapHub<NotificationHub>("/notificationHub");

// The order here is important
app.UseAuthentication();    // Authentication
app.UseAuthorization();     // Authorization

app.MapControllers();

app.Run();