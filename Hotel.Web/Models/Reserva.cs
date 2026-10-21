using System;
using System.Collections.Generic;

namespace Hotel.Web.Models;

public partial class Reserva
{
    public int ReservaId { get; set; }

    public int HuespedId { get; set; }

    public int HabitacionId { get; set; }

    public DateOnly FechaIngreso { get; set; }

    public DateOnly FechaSalida { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime FechaRegistro { get; set; }

    public string? Observaciones { get; set; }

    public virtual Habitacion Habitacion { get; set; } = null!;

    public virtual Huesped Huesped { get; set; } = null!;
}
