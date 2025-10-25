using TSoftApiClient.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(); // MVC desteği
builder.Services.AddControllers();

// Swagger/OpenAPI yapılandırması
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "T-Soft API Client",
        Version = "v1",
        Description = "T-Soft REST1 API ile entegrasyon için ASP.NET Core API",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Geliştirici",
            Email = "info@example.com"
        }
    });

    // XML yorumları ekle (opsiyonel)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// HttpClient yapılandırması
builder.Services.AddHttpClient<TSoftApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// TSoftApiService'i DI container'a ekle
builder.Services.AddScoped<TSoftApiService>();

// CORS yapılandırması (gerekirse)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "T-Soft API Client v1");
        options.RoutePrefix = "api/docs"; // Swagger'ı /api/docs'da aç
    });
}

app.UseStaticFiles(); // CSS, JS dosyaları için

app.UseCors("AllowAll");

app.UseRouting();

app.UseAuthorization();

// MVC Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// API Controllers
app.MapControllers();

// Basit test endpoint'i
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    message = "T-Soft API Client is running!"
}))
.WithName("HealthCheck");

app.Run();
