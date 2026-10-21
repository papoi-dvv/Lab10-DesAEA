using Hotel.Web.Models;
using Hotel.Web.Repositorios;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// EF Core Database First: el contexto generado por scaffold se registra aquí
// (al contexto se le quitó el método OnConfiguring) y la cadena vive en appsettings.json.
builder.Services.AddDbContext<HotelContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("HotelDB")));

// Todo el acceso a datos (LINQ y SaveChangesAsync) vive en los repositorios.
builder.Services.AddScoped<HabitacionRepositorio>();
builder.Services.AddScoped<ReservaRepositorio>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Habitaciones}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
