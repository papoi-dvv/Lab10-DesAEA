# Hotel.Web – Lab 10

Aplicación ASP.NET Core MVC (.NET 10) con **Entity Framework Core Database First** para administrar habitaciones y reservas de un hotel. Sin migraciones y sin procedimientos almacenados: el esquema lo define el script SQL y el modelo se genera con *scaffold*.

## Requisitos

- .NET 10 SDK
- SQL Server (probado con `localhost\SQLEXPRESS`)
- Herramienta `dotnet-ef` (solo para regenerar el modelo): `dotnet tool install --global dotnet-ef`

## Preparación

1. Ejecutar `sql/HotelDB.sql`: crea la base `HotelDB` con las tablas `TipoHabitacion`, `Habitacion`, `Huesped` y `Reserva` y sus datos de prueba. Se puede ejecutar varias veces (vuelve a crear las tablas).

   ```bash
   sqlcmd -S localhost\SQLEXPRESS -E -C -f 65001 -i Lab10/sql/HotelDB.sql
   ```

2. Ejecutar `sql/Semana10_Observaciones.sql`, que es el paso final del laboratorio:

   ```sql
   ALTER TABLE Reserva ADD Observaciones NVARCHAR(200) NULL;
   ```

   > Ojo: `HotelDB.sql` vuelve a crear las tablas, así que si se ejecuta otra vez hay que volver a ejecutar también este paso (y repetir el scaffold con `--force`).

3. Revisar la cadena de conexión en `Hotel.Web/appsettings.json` y ajustar `Server` si la instancia es otra.

4. Ejecutar la aplicación desde la carpeta del proyecto:

   ```bash
   dotnet run --project Hotel.Web
   ```

## Comando de scaffold utilizado

```bash
dotnet ef dbcontext scaffold "Server=localhost\SQLEXPRESS;Database=HotelDB;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context HotelContext
```

Para regenerar el modelo después del `ALTER TABLE` se añade `--force`:

```bash
dotnet ef dbcontext scaffold "Server=localhost\SQLEXPRESS;Database=HotelDB;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context HotelContext --force
```

En Visual Studio, con el paquete `Microsoft.EntityFrameworkCore.Tools`, el equivalente en la Consola del Administrador de paquetes es:

```powershell
Scaffold-DbContext "Server=localhost\SQLEXPRESS;Database=HotelDB;Trusted_Connection=True;TrustServerCertificate=True;" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Context HotelContext
```

> Tras cada scaffold hay que **quitar el método `OnConfiguring`** del contexto generado: la cadena vive en `appsettings.json` y el contexto se registra con `AddDbContext` en `Program.cs`.

## Estructura

| Carpeta | Contenido |
|---|---|
| `Hotel.Web/Models` | Clases generadas por el scaffold (`Habitacion`, `Huesped`, `Reserva`, `TipoHabitacion`, `HotelContext`). **No se modifican**, salvo quitar `OnConfiguring`. |
| `Hotel.Web/ViewModels` | `DisponibilidadViewModel` y `HabitacionDisponibleViewModel` (noches y total estimado). |
| `Hotel.Web/Repositorios` | `HabitacionRepositorio` y `ReservaRepositorio`: todas las consultas LINQ, las listas desplegables y los `SaveChangesAsync`. |
| `Hotel.Web/Controllers` | `HabitacionesController`, `ReservasController` (no usan `HotelContext`) y `ControladorBase` (notificaciones por `TempData`). |
| `Hotel.Web/Views` | Vistas Razor fuertemente tipadas a las entidades. |
| `Hotel.Web/wwwroot` | `css/site.css` (tema *liquid glass*), `js/site.js` (notificaciones y contadores), `js/luces.js` (fondo animado). |
| `sql` | `HotelDB.sql` y `Semana10_Observaciones.sql`. |

## Funcionalidad

- **Habitaciones:** listado de habitaciones activas con su tipo, filtro por tipo y orden por número; detalle con el historial de reservas; alta, edición y **eliminación lógica** (`Activo = 0`, nunca `DELETE`). Si el número ya existe, el error se muestra en el formulario con `ModelState.AddModelError`.
- **Disponibilidad:** filtro por GET (`ingreso` y `salida`) que lista las habitaciones activas libres con noches y total estimado. El cruce se resuelve en el repositorio con una sola consulta LINQ (`!Any`): una reserva se cruza cuando su `Estado` no es `Cancelada`, su `FechaIngreso` es menor que la salida pedida y su `FechaSalida` mayor que el ingreso pedido.
- **Reservas:** alta con listas desplegables de huéspedes y habitaciones activas, resumen en vivo (noches y total) y validaciones de que la salida sea posterior al ingreso, que el ingreso no sea anterior a hoy y que la habitación no esté reservada en ese rango. Se guarda con `Estado = "Pendiente"`.
- **Listado de reservas:** huésped, habitación, fechas, noches, total y estado, con filtro por estado y cancelación (`Estado = "Cancelada"`) de las que están `Pendiente` o `Confirmada`, confirmada en un modal.
- **Observaciones:** columna agregada al final del laboratorio; se captura en el formulario de nueva reserva (con contador de caracteres) y se muestra en el listado.
- **Interfaz:** modo oscuro con efecto vidrio, KPIs animados, notificaciones (toasts) que llegan por `TempData` tras la redirección y fondo de luces. Todo se desactiva con la preferencia del sistema "reducir movimiento".

Todas las acciones y los métodos de los repositorios son `async` (`ToListAsync`, `AnyAsync`, `SaveChangesAsync`), las lecturas usan `AsNoTracking()`, no hay SQL escrito a mano y los formularios siguen el patrón Post/Redirect/Get.

## Notas

- Las entidades generadas por scaffold no llevan DataAnnotations, así que las reglas y los mensajes (en español) se aplican en los controladores con `ModelState`. Las propiedades de navegación obligatorias se quitan del `ModelState` con `ModelState.Remove`.
- El script `HotelDB.sql` crea las reservas de prueba en fechas relativas a la fecha actual, para que el filtro de disponibilidad siempre tenga cruces que mostrar.
