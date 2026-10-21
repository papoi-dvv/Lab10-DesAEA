using Hotel.Web.Models;
using Hotel.Web.Repositorios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hotel.Web.Controllers
{
    public class HabitacionesController : ControladorBase
    {
        private readonly HabitacionRepositorio _habitaciones;

        public HabitacionesController(HabitacionRepositorio habitaciones) => _habitaciones = habitaciones;

        // GET: /Habitaciones?tipoHabitacionId=2
        public async Task<IActionResult> Index(int? tipoHabitacionId)
        {
            var habitaciones = await _habitaciones.ListarActivasAsync(tipoHabitacionId);

            await CargarTiposAsync(tipoHabitacionId);
            ViewData["TipoFiltro"] = tipoHabitacionId;
            ViewData["TotalActivas"] = await _habitaciones.ContarActivasAsync();
            ViewData["OcupadasHoy"] = await _habitaciones.ContarOcupadasHoyAsync();

            return View(habitaciones);
        }

        // GET: /Habitaciones/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var habitacion = await _habitaciones.ObtenerPorIdAsync(id);
            if (habitacion is null) return NotFound();

            ViewData["Reservas"] = await _habitaciones.ReservasPorHabitacionAsync(id);
            return View(habitacion);
        }

        // GET: /Habitaciones/Create
        public async Task<IActionResult> Create()
        {
            await CargarTiposAsync();
            return View(new Habitacion { Piso = 1 });
        }

        // POST: /Habitaciones/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Habitacion habitacion)
        {
            LimpiarNavegacion();
            Validar(habitacion);

            if (ModelState.IsValid && await _habitaciones.ExisteNumeroAsync(habitacion.Numero))
                ModelState.AddModelError(nameof(Habitacion.Numero), "Ya existe una habitación con ese número.");

            if (ModelState.IsValid)
            {
                await _habitaciones.CrearAsync(habitacion);
                Notificar(TipoNotificacion.Creado, "Habitación registrada",
                    $"La habitación {habitacion.Numero} se agregó correctamente.");
                return RedirectToAction(nameof(Index));
            }

            await CargarTiposAsync(habitacion.TipoHabitacionId);
            return View(habitacion);
        }

        // GET: /Habitaciones/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var habitacion = await _habitaciones.ObtenerPorIdAsync(id);
            if (habitacion is null) return NotFound();

            await CargarTiposAsync(habitacion.TipoHabitacionId);
            return View(habitacion);
        }

        // POST: /Habitaciones/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Habitacion habitacion)
        {
            if (id != habitacion.HabitacionId) return BadRequest();

            LimpiarNavegacion();
            Validar(habitacion);

            if (ModelState.IsValid && await _habitaciones.ExisteNumeroAsync(habitacion.Numero, id))
                ModelState.AddModelError(nameof(Habitacion.Numero), "Ya existe otra habitación con ese número.");

            if (ModelState.IsValid)
            {
                if (!await _habitaciones.ActualizarAsync(habitacion)) return NotFound();

                Notificar(TipoNotificacion.Editado, "Habitación actualizada",
                    $"Los cambios en la habitación {habitacion.Numero} se guardaron.");
                return RedirectToAction(nameof(Index));
            }

            await CargarTiposAsync(habitacion.TipoHabitacionId);
            return View(habitacion);
        }

        // GET: /Habitaciones/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var habitacion = await _habitaciones.ObtenerPorIdAsync(id);
            if (habitacion is null) return NotFound();
            return View(habitacion);
        }

        // POST: /Habitaciones/Delete/5  (eliminación lógica: Activo = 0)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var habitacion = await _habitaciones.ObtenerPorIdAsync(id);
            if (habitacion is null || !await _habitaciones.DesactivarAsync(id))
            {
                Notificar(TipoNotificacion.Error, "No se pudo dar de baja", "No se encontró la habitación indicada.");
                return RedirectToAction(nameof(Index));
            }

            Notificar(TipoNotificacion.Eliminado, "Habitación dada de baja",
                $"La habitación {habitacion.Numero} quedó inactiva (Activo = 0).");
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarTiposAsync(int? seleccionado = null)
        {
            var tipos = await _habitaciones.ListarTiposAsync();
            ViewBag.Tipos = new SelectList(tipos, nameof(TipoHabitacion.TipoHabitacionId), nameof(TipoHabitacion.Nombre), seleccionado);
            ViewBag.ListaTipos = tipos;
        }

        // Las entidades generadas por scaffold tienen propiedades de navegación
        // obligatorias; el formulario no las envía, así que se quitan del ModelState.
        private void LimpiarNavegacion()
        {
            ModelState.Remove(nameof(Habitacion.TipoHabitacion));
            ModelState.Remove(nameof(Habitacion.Reservas));

            // Las entidades generadas no llevan DataAnnotations: los mensajes del
            // enlazador salen en inglés y se reemplazan por los de Validar().
            ModelState.Remove(nameof(Habitacion.Numero));
            ModelState.Remove(nameof(Habitacion.TipoHabitacionId));
            ModelState.Remove(nameof(Habitacion.Piso));
        }

        // Reglas del formulario de habitación (mismas en Create y Edit).
        private void Validar(Habitacion habitacion)
        {
            if (string.IsNullOrWhiteSpace(habitacion.Numero))
                ModelState.AddModelError(nameof(Habitacion.Numero), "Indique el número de la habitación.");
            else if (habitacion.Numero.Trim().Length > 10)
                ModelState.AddModelError(nameof(Habitacion.Numero), "El número admite hasta 10 caracteres.");

            if (habitacion.TipoHabitacionId <= 0)
                ModelState.AddModelError(nameof(Habitacion.TipoHabitacionId), "Seleccione un tipo de habitación.");

            if (habitacion.Piso is < 1 or > 50)
                ModelState.AddModelError(nameof(Habitacion.Piso), "El piso debe estar entre 1 y 50.");
        }
    }
}
