using Hotel.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Web.Repositorios
{
    // Todas las consultas LINQ y los SaveChangesAsync de habitaciones.
    // El controlador nunca toca HotelContext.
    public class HabitacionRepositorio
    {
        private readonly HotelContext _contexto;

        public HabitacionRepositorio(HotelContext contexto) => _contexto = contexto;

        // Habitaciones activas con su tipo; filtro opcional por tipo y orden por número.
        public async Task<List<Habitacion>> ListarActivasAsync(int? tipoHabitacionId = null)
        {
            var consulta = _contexto.Habitacions
                .AsNoTracking()
                .Include(h => h.TipoHabitacion)
                .Where(h => h.Activo);

            if (tipoHabitacionId.HasValue)
                consulta = consulta.Where(h => h.TipoHabitacionId == tipoHabitacionId.Value);

            return await consulta
                .OrderBy(h => h.Numero)
                .ToListAsync();
        }

        // Para Details y Delete (solo lectura).
        public async Task<Habitacion?> ObtenerPorIdAsync(int id) =>
            await _contexto.Habitacions
                .AsNoTracking()
                .Include(h => h.TipoHabitacion)
                .FirstOrDefaultAsync(h => h.HabitacionId == id);

        // Para Edit y para la baja lógica: la entidad se devuelve con seguimiento.
        public async Task<Habitacion?> ObtenerParaEditarAsync(int id) =>
            await _contexto.Habitacions.FirstOrDefaultAsync(h => h.HabitacionId == id);

        // El número de habitación es único; se excluye la propia fila al editar.
        public async Task<bool> ExisteNumeroAsync(string numero, int? excluirId = null) =>
            await _contexto.Habitacions
                .AsNoTracking()
                .AnyAsync(h => h.Numero == numero && (!excluirId.HasValue || h.HabitacionId != excluirId.Value));

        // Datos de la lista desplegable de tipos de habitación.
        public async Task<List<TipoHabitacion>> ListarTiposAsync() =>
            await _contexto.TipoHabitacions
                .AsNoTracking()
                .OrderBy(t => t.Nombre)
                .ToListAsync();

        // Reservas de una habitación (historial del detalle).
        public async Task<List<Reserva>> ReservasPorHabitacionAsync(int habitacionId) =>
            await _contexto.Reservas
                .AsNoTracking()
                .Include(r => r.Huesped)
                .Where(r => r.HabitacionId == habitacionId)
                .OrderByDescending(r => r.FechaIngreso)
                .ToListAsync();

        public async Task<int> CrearAsync(Habitacion habitacion)
        {
            habitacion.Activo = true;
            _contexto.Habitacions.Add(habitacion);
            await _contexto.SaveChangesAsync();
            return habitacion.HabitacionId;
        }

        public async Task<bool> ActualizarAsync(Habitacion habitacion)
        {
            var actual = await ObtenerParaEditarAsync(habitacion.HabitacionId);
            if (actual is null) return false;

            actual.Numero = habitacion.Numero;
            actual.TipoHabitacionId = habitacion.TipoHabitacionId;
            actual.Piso = habitacion.Piso;

            await _contexto.SaveChangesAsync();
            return true;
        }

        // Eliminación lógica: Activo = 0, nunca un DELETE físico.
        public async Task<bool> DesactivarAsync(int id)
        {
            var habitacion = await ObtenerParaEditarAsync(id);
            if (habitacion is null) return false;

            habitacion.Activo = false;
            await _contexto.SaveChangesAsync();
            return true;
        }

        // Contadores del encabezado del listado.
        public async Task<int> ContarActivasAsync() =>
            await _contexto.Habitacions.AsNoTracking().CountAsync(h => h.Activo);

        public async Task<int> ContarOcupadasHoyAsync()
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            return await _contexto.Reservas
                .AsNoTracking()
                .Where(r => r.Estado != "Cancelada" && r.FechaIngreso <= hoy && r.FechaSalida > hoy)
                .Select(r => r.HabitacionId)
                .Distinct()
                .CountAsync();
        }
    }
}
