using ClarkAI.Core.Application.Interfaces.Repositories;
using ClarkAI.Core.Application.Interfaces.Services;
using ClarkAI.Core.Application.Service;
using ClarkAI.Infrastructure;
using ClarkAI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddDbContext<ClarkContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnections"),
npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
    maxRetryCount : 5,
    maxRetryDelay : TimeSpan.FromSeconds(60),
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

builder.Services.AddHttpClient<IPaystackService>();
builder.Services.AddScoped<IPaystackService,PaystackService>();
builder.Services.AddTransient<IUserRepository,UserRepository>();
builder.Services.AddTransient<IPaymentRepository,PaymentRepository>();
builder.Services.AddTransient<IUnitOfWork,UnitOfWork>();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
