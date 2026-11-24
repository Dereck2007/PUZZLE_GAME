using MathPuzzle.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// =========================================
// CONFIGURACIÓN DE SERVICIOS
// =========================================

// Agregar los servicios de MVC.
builder.Services.AddControllersWithViews();

// Habilitar almacenamiento de sesiones en memoria.
builder.Services.AddDistributedMemoryCache();

// Configurar las sesiones de los jugadores.
builder.Services.AddSession(options =>
{
    // Tiempo máximo de inactividad.
    options.IdleTimeout = TimeSpan.FromMinutes(30);

    // Configuración de la cookie.
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

var app = builder.Build();

// =========================================
// CONFIGURACIÓN DE LA APLICACIÓN
// =========================================

// Manejo de errores en producción.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Redireccionar a HTTPS.
app.UseHttpsRedirection();

// Habilitar archivos estáticos.
app.UseStaticFiles();

// Habilitar las rutas.
app.UseRouting();

// =========================================
// CONFIGURACIÓN DE SESIONES
// =========================================

// Habilitar sesiones de ASP.NET Core.
app.UseSession();

// Bloquear el procesamiento simultáneo
// de solicitudes de una misma sesión.
app.UseMiddleware<BloqueoSesionMiddleware>();

// =========================================
// AUTORIZACIÓN Y RUTAS
// =========================================

// Habilitar autorización.
app.UseAuthorization();

// Configurar la ruta principal.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// Iniciar la aplicación.
app.Run();