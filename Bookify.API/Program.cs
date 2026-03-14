using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Azure.Storage.Blobs;
using Bookify.API.Data;
using Bookify.API.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddOData(options => options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100));

// OpenAPI & Swagger (no Microsoft.OpenApi.Models – avoids conflict with Microsoft.OpenApi 2.x from ASP.NET Core 10)
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=bookify.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Azure Blob Storage (use development storage when ConnectionString not set)
var blobConnectionString = builder.Configuration.GetValue<string>("Storage:ConnectionString")
    ?? "UseDevelopmentStorage=true";
builder.Services.AddSingleton(_ => new BlobServiceClient(blobConnectionString));
builder.Services.AddScoped<Bookify.API.Services.IBlobStorageService, Bookify.API.Services.BlobStorageService>();

// Background Worker
builder.Services.AddHostedService<Bookify.API.Services.AudioMetadataWorker>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Bookify API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Auto-migrate and seed on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();

    // Ensure default dev user exists so playback endpoints work when auth is not yet configured
    var devUserId = new Guid("00000000-0000-0000-0000-000000000001");
    if (app.Environment.IsDevelopment() && !dbContext.Users.Any(u => u.Id == devUserId))
    {
        dbContext.Users.Add(new User
        {
            Id = devUserId,
            EntraId = "dev-mock-user",
            Role = "User",
            IsActive = true
        });
        dbContext.SaveChanges();
    }
}

app.Run();
