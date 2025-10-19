using ClarkAI.Core.Application.Interfaces.Repositories;
using ClarkAI.Core.Application.Interfaces.Services;
using ClarkAI.Core.Application.Service;
using ClarkAI.Core.Entity.Model;
using ClarkAI.Infrastructure;
using ClarkAI.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddDbContext<ClarkContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnections"),
npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
    maxRetryCount : 5,
    maxRetryDelay : TimeSpan.FromSeconds(120),
    errorCodesToAdd : null)));

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Services.AddCors(cors =>
    cors.AddPolicy("Clark", pol =>
    {
        pol.WithOrigins("")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    }));

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<PaystackSettings>(builder.Configuration.GetSection("Paystack"));
builder.Services.AddHttpClient<PaystackService>();
builder.Services.AddScoped<IPaystackService,PaystackService>();
builder.Services.AddScoped<IUserRepository,UserRepository>();
builder.Services.AddScoped<IPaymentRepository,PaymentRepository>();
builder.Services.AddScoped<IUnitOfWork,UnitOfWork>();
builder.Services.AddScoped<PaymentProcessingJob>();
builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer(); 

builder.Services.AddSwaggerGen(c =>
{

    //c.EnableAnnotations();

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
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

var jwtKey = builder.Configuration["Jwt:Key"];
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

        NameClaimType = "email",
        RoleClaimType = "role"
    };
});


builder.Logging.AddDebug();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = string.Empty;
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clark v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("Clark");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();






