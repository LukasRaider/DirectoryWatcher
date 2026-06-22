using DirectoryWatcher.Repositories;
using DirectoryWatcher.Services;

var builder = WebApplication.CreateBuilder(args);

// Pøidání standardních služeb pro MVC (Controllery a Views)
builder.Services.AddControllersWithViews();

// --- REGISTRACE NAŠICH SLUŽEB (Dependency Injection) ---
// AddScoped zajistí, že se instance vytvoøí znovu pro každý HTTP požadavek, což je ideální pro repozitáøe a servisy.
builder.Services.AddScoped<IStateRepository, JsonStateRepository>();
builder.Services.AddScoped<IDirectoryAnalyzerService, DirectoryAnalyzerService>();

var app = builder.Build();

// Konfigurace HTTP pipeline (Middleware)
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// Výchozí hodnota HSTS je 30 dní. Mùžete ji zmìnit pro produkèní scénáøe, viz https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Definice výchozí routy (smìrování) na HomeController a akci Index
app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();