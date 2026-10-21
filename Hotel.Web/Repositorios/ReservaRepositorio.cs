using Hotel.Web.Models;
using Hotel.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Hotel.Web.Repositorios
{
    // Consultas LINQ y SaveChangesAsync de reservas y disponibilidad.
    public class ReservaRepositorio
    {
        private readonly HotelContext _contexto;

        public ReservaRepositorio(HotelContext contexto) => _contexto = contexto;

        // Estados posibles de una reserva (los mismos que valida la base de datos).
        public static readonly string[] Estados = { "Pendiente", "Confirmada", "Cancelada", "Finalizada" };

        public async Task<List<Reserva>> ListarAsync(string? estado = null)
        {
            var consulta = _contexto.Reservas
                .AsNoTracking()
                .Include(r => r.Huesped)
                .Include(r => r.Habitacion)
                    .ThenInclude(h => h.TipoHabitacion)
                .AsQueryable();

            if (!string.IsNullOrEmpty(estado))
                consulta = consulta.Where(r => r.Estado == estado);

            return await consulta
                .OrderByDescending(r => r.FechaIngreso)
                .ThenBy(r => r.Habitacion.Numero)
                .ToListAsync();
        }

        public async Task<Reserva?> ObtenerPorIdAsync(int id) =>
            await _contexto.Reservas
                .AsNoTracking()
                .Include(r => r.Huesped)
                .Include(r => r.Habitacion)
                    .ThenInclude(h => h.TipoHabitacion)
                .FirstOrDefaultAsync(r => r.ReservaId == id);

        // Habitaciones activas libres en el rango pedido, en una sola consulta LINQ.
        // Una reserva se cruza cuando su Estado no es Cancelada, su FechaIngreso es
        // menor que la salida pedida y su FechaSalida es mayor que el ingreso pedido.
        public async Task<List<HabitacionDisponibleViewModel>> HabitacionesDisponiblesAsync(DateOnly ingreso, DateOnly salida) =>
            await _contexto.Habitacions
                .AsNoTracking()
                .Where(h => h.Activo && !h.Reservas.Any(r =>
                    r.Estado != "Cancelada" &&
                    r.FechaIngreso < salida &&
                    r.FechaSalida > ingreso))
                .OrderBy(h => h.Numero)
                .Select(h => new HabitacionDisponibleViewModel
                {
                    HabitacionId = h.HabitacionId,
                    Numero = h.Numero,
                    Tipo = h.TipoHabitacion.Nombre,
                    Piso = h.Piso,
                    Capacidad = h.TipoHabitacion.Capacidad,
                    PrecioNoche = h.TipoHabitacion.PrecioNoche
                })
                .ToListAsync();

        // ¿La habitación ya está reservada en ese rango? (misma regla de cruce)
        public async Task<bool> EstaReservadaAsync(int habitacionId, DateOnly ingreso, DateOnly salida, int? excluirReservaId = null) =>
            await _contexto.Reservas
                .AsNoTracking()
                .AnyAsync(r =>
                    r.HabitacionId == habitacionId &&
                    r.Estado != "Cancelada" &&
                    r.FechaIngreso < salida &&
                    r.FechaSalida > ingreso &&
                    (!excluirReservaId.HasValue || r.ReservaId != excluirReservaId.Value));

        // Datos de las listas desplegables de Create.
        public async Task<List<Huesped>> ListarHuespedesActivosAsync() =>
            await _contexto.Huespeds
                .AsNoTracking()
                .Where(h => h.Activo)
                .OrderBy(h => h.Apellidos)
                .ThenBy(h => h.Nombres)
                .ToListAsync();

        public async Task<List<Habitacion>> ListarHabitacionesActivasAsync() =>
            await _contexto.Habitacions
                .AsNoTracking()
                .Include(h => h.TipoHabitacion)
                .Where(h => h.Activo)
                .OrderBy(h => h.Numero)
                .ToListAsync();

        public async Task<int> CrearAsync(Reserva reserva)
        {
            reserva.Estado = "Pendiente";
            reserva.FechaRegistro = DateTime.Now;
            _contexto.Reservas.Add(reserva);
            await _contexto.SaveChangesAsync();
            return reserva.ReservaId;
        }

        // Solo se cancelan las reservas Pendiente o Confirmada.
        public async Task<bool> CancelarAsync(int id)
        {
            var reserva = await _contexto.Reservas.FirstOrDefaultAsync(r => r.ReservaId == id);
            if (reserva is null) return false;
            if (reserva.Estado != "Pendiente" && reserva.Estado != "Confirmada") return false;

            reserva.Estado = "Cancelada";
            await _contexto.SaveChangesAsync();
            return true;
        }

        // Precio por noche del tipo de la habitación (para calcular el total al guardar).
        public async Task<decimal> PrecioNocheAsync(int habitacionId) =>
            await _contexto.Habitacions
                .AsNoTracking()
                .Where(h => h.HabitacionId == habitacionId)
                .Select(h => h.TipoHabitacion.PrecioNoche)
                .FirstOrDefaultAsync();

        // Contadores del encabezado del listado de reservas.
        public async Task<Dictionary<string, int>> ContarPorEstadoAsync() =>
            await _contexto.Reservas
                .AsNoTracking()
                .GroupBy(r => r.Estado)
                .Select(g => new { Estado = g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.Estado, x => x.Total);
    }
}
