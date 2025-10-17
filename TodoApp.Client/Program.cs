using TodoApp.Client.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// 1. Get the base URL from configuration using the builder.Configuration object
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];

// Basic check to ensure the configuration was found
if (string.IsNullOrEmpty(apiBaseUrl))
{
    throw new InvalidOperationException("ApiSettings:BaseUrl is not configured. Please check TodoApp.Client/appsettings.json.");
}

// 2. Configure HttpClient with the BaseAddress
builder.Services.AddHttpClient<ApiConsumerService>(client =>
{
    // FIX: This correctly sets the BaseAddress using the value retrieved from appsettings.json.
    client.BaseAddress = new Uri(apiBaseUrl);
});
//builder.Services.AddScoped<ApiConsumerService>();

var app = builder.Build();

// Configure the HTTP request pipeline. (Standard Razor Pages/MVC setup)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
