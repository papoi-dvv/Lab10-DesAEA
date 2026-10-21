using Hotel.Web.Models;
using Hotel.Web.Repositorios;
using Hotel.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hotel.Web.Controllers
{
    public class ReservasController : ControladorBase
    {
        private readonly ReservaRepositorio _reservas;

        public ReservasController(ReservaRepositorio reservas) => _reservas = reservas;

        // GET: /Reservas?estado=Pendiente
        public async Task<IActionResult> Index(string? estado)
        {
            if (!string.IsNullOrEmpty(estado) && !ReservaRepositorio.Estados.Contains(estado))
                estado = null;

            var reservas = await _reservas.ListarAsync(estado);

            ViewData["Estado"] = estado;
            ViewData["Conteos"] = await _reservas.ContarPorEstadoAsync();
            return View(reservas);
        }

        // GET: /Reservas/Disponibles?ingreso=2026-10-21&salida=2026-10-24
        public async Task<IActionResult> Disponibles(DateOnly? ingreso, DateOnly? salida)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var modelo = new DisponibilidadViewModel
            {
                Ingreso = ingreso ?? hoy,
                Salida = salida ?? hoy.AddDays(1)
            };

            if (modelo.Salida <= modelo.Ingreso)
            {
                ModelState.AddModelError(nameof(modelo.Salida), "La fecha de salida debe ser posterior a la de ingreso.");
                return View(modelo);
            }

            modelo.Buscado = true;
            modelo.Habitaciones = await _reservas.HabitacionesDisponiblesAsync(modelo.Ingreso!.Value, modelo.Salida!.Value);
            return View(modelo);
        }

        // GET: /Reservas/Create  (opcionalmente con los datos que llegan de Disponibles)
        public async Task<IActionResult> Create(int? habitacionId, DateOnly? ingreso, DateOnly? salida)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);
            var reserva = new Reserva
            {
                HabitacionId = habitacionId ?? 0,
                FechaIngreso = ingreso ?? hoy,
                FechaSalida = salida ?? hoy.AddDays(1),
                Estado = "Pendiente"
            };

            await CargarListasAsync(reserva);
            return View(reserva);
        }

        // POST: /Reservas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Reserva reserva)
        {
            LimpiarNavegacion();

            var hoy = DateOnly.FromDateTime(DateTime.Today);

            if (reserva.FechaIngreso == default)
                ModelState.AddModelError(nameof(Reserva.FechaIngreso), "Indique la fecha de ingreso.");

            if (reserva.FechaSalida == default)
                ModelState.AddModelError(nameof(Reserva.FechaSalida), "Indique la fecha de salida.");

            if (reserva.FechaIngreso != default && reserva.FechaSalida != default &&
                reserva.FechaSalida <= reserva.FechaIngreso)
                ModelState.AddModelError(nameof(Reserva.FechaSalida), "La fecha de salida debe ser posterior a la de ingreso.");

            if (reserva.FechaIngreso != default && reserva.FechaIngreso < hoy)
                ModelState.AddModelError(nameof(Reserva.FechaIngreso), "La fecha de ingreso no puede ser anterior a hoy.");

            if (reserva.HuespedId <= 0)
                ModelState.AddModelError(nameof(Reserva.HuespedId), "Seleccione un huésped.");

            if (reserva.HabitacionId <= 0)
                ModelState.AddModelError(nameof(Reserva.HabitacionId), "Seleccione una habitación.");

            if (ModelState.IsValid &&
                await _reservas.EstaReservadaAsync(reserva.HabitacionId, reserva.FechaIngreso, reserva.FechaSalida))
            {
                ModelState.AddModelError(nameof(Reserva.HabitacionId),
                    "La habitación ya está reservada en ese rango de fechas.");
            }

            if (ModelState.IsValid)
            {
                // Columna agregada al final del laboratorio (NVARCHAR(200) NULL).
                reserva.Observaciones = string.IsNullOrWhiteSpace(reserva.Observaciones)
                    ? null
                    : reserva.Observaciones.Trim();

                await _reservas.CrearAsync(reserva);

                var noches = reserva.FechaSalida.DayNumber - reserva.FechaIngreso.DayNumber;
                var total = noches * await _reservas.PrecioNocheAsync(reserva.HabitacionId);

                Notificar(TipoNotificacion.Reserva, "Reserva registrada",
                    $"{noches} noche(s) por S/ {total:N2}. Queda en estado Pendiente.");
                return RedirectToAction(nameof(Index));
            }

            await CargarListasAsync(reserva);
            return View(reserva);
        }

        // POST: /Reservas/Cancelar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            if (await _reservas.CancelarAsync(id))
                Notificar(TipoNotificacion.Cancelado, "Reserva cancelada",
                    $"La reserva #{id} pasó al estado Cancelada.");
            else
                Notificar(TipoNotificacion.Advertencia, "No se pudo cancelar",
                    "Solo se cancelan las reservas en estado Pendiente o Confirmada.");

            return RedirectToAction(nameof(Index));
        }

        private async Task CargarListasAsync(Reserva reserva)
        {
            var huespedes = await _reservas.ListarHuespedesActivosAsync();
            var habitaciones = await _reservas.ListarHabitacionesActivasAsync();

            ViewBag.Huespedes = new SelectList(
                huespedes.Select(h => new { h.HuespedId, Nombre = $"{h.Apellidos}, {h.Nombres} ({h.Dni})" }),
                "HuespedId", "Nombre", reserva.HuespedId);

            ViewBag.Habitaciones = new SelectList(
                habitaciones.Select(h => new
                {
                    h.HabitacionId,
                    Nombre = $"{h.Numero} · {h.TipoHabitacion.Nombre} · S/ {h.TipoHabitacion.PrecioNoche:N2} por noche"
                }),
                "HabitacionId", "Nombre", reserva.HabitacionId);

            // Precios por habitación para calcular el total en el navegador.
            ViewBag.Precios = habitaciones.ToDictionary(
                h => h.HabitacionId,
                h => h.TipoHabitacion.PrecioNoche);
        }

        // Propiedades de navegación obligatorias de la entidad generada por scaffold.
        private void LimpiarNavegacion()
        {
            ModelState.Remove(nameof(Reserva.Huesped));
            ModelState.Remove(nameof(Reserva.Habitacion));
            ModelState.Remove(nameof(Reserva.Estado));

            // Las entidades generadas no llevan DataAnnotations: los mensajes que
            // produce el enlazador salen en inglés, así que se reemplazan por los
            // mensajes propios que agregan las validaciones de más arriba.
            ModelState.Remove(nameof(Reserva.HuespedId));
            ModelState.Remove(nameof(Reserva.HabitacionId));
            ModelState.Remove(nameof(Reserva.FechaIngreso));
            ModelState.Remove(nameof(Reserva.FechaSalida));
        }
    }
}
