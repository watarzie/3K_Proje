using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using MediatR;
using FluentValidation;
using _3K.Core.Interfaces;
using _3K.Application.Behaviors;
using _3K.Infrastructure.Data;
using _3K.Infrastructure.Repositories;
using _3K.Infrastructure.Services;

// Npgsql'in DateTime davranışı için eski uyumluluğu aç (Unspecified DateTime hatasını çözer)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ======= DbContext =======
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// ======= Repository & UnitOfWork =======
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProjeRepository, ProjeRepository>();

// ======= Services =======
builder.Services.AddScoped<ICekiService, CekiService>();
builder.Services.AddScoped<ISandikService, SandikService>();
builder.Services.AddScoped<IUrunService, UrunService>();
builder.Services.AddScoped<IStokService, StokService>();
builder.Services.AddScoped<IFBTransferService, FBTransferService>();
builder.Services.AddScoped<IHareketService, HareketService>();
builder.Services.AddScoped<IRevizyonService, RevizyonService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDurumHesaplaService, DurumHesaplaService>();
builder.Services.AddScoped<ILookupService, _3K.Infrastructure.Services.LookupService>();
builder.Services.AddScoped<IRolService, _3K.Infrastructure.Services.RolService>();

// ======= Current User Service (Pipeline Behavior için) =======
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ======= MediatR + Pipeline Behaviors =======
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(_3K.Application.Features.SandikIslemleri.Commands.FiiliSandikDegistirCommand).Assembly);
    // Pipeline sırası: UnhandledException (en dış) → Authorization → Validation → Handler
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// ======= FluentValidation =======
builder.Services.AddValidatorsFromAssembly(typeof(_3K.Application.Features.SandikIslemleri.Commands.FiiliSandikDegistirCommand).Assembly);

// ======= JWT Authentication =======
var jwtKey = builder.Configuration["JwtSettings:SecretKey"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? throw new InvalidOperationException("JWT SecretKey yapılandırılmamış.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "3K_API",
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "3K_Client",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ======= Controllers =======
builder.Services.AddControllers();

// ======= Dosya yükleme boyut limiti (50 MB) =======
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
});

// ======= Swagger =======
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "3K Sevkiyat / Çeki Takip API",
        Version = "v1",
        Description = "Sevkiyat yönetimi, çeki takibi, sandık operasyonları ve stok yönetimi API'si"
    });

    // JWT Auth tanımı — Http scheme kullanılıyor, Swagger otomatik "Bearer " ekler
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Login'den dönen token'ı buraya yapıştırın (Bearer öneki otomatik eklenir)",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
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
            Array.Empty<string>()
        }
    });
});

// ======= CORS =======
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// ======= Middleware Pipeline =======
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "3K API v1"));
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
