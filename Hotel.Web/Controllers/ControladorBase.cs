using Microsoft.AspNetCore.Mvc;

namespace Hotel.Web.Controllers
{
    // Tipos de notificación que entiende site.js (clase CSS e icono de cada toast).
    public static class TipoNotificacion
    {
        public const string Creado = "creado";
        public const string Editado = "editado";
        public const string Eliminado = "eliminado";
        public const string Reserva = "reserva";
        public const string Cancelado = "cancelado";
        public const string Advertencia = "advertencia";
        public const string Error = "error";
    }

    public abstract class ControladorBase : Controller
    {
        // El resultado se guarda en TempData para mostrarlo tras la redirección (Post/Redirect/Get).
        protected void Notificar(string tipo, string titulo, string mensaje)
        {
            TempData["Tipo"] = tipo;
            TempData["Titulo"] = titulo;
            TempData["Mensaje"] = mensaje;
        }
    }
}
