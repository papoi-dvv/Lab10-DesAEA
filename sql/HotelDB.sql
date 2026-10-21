/* =====================================================================
   HotelDB – Semana 10 (EF Core Database First)
   Tablas: TipoHabitacion, Habitacion, Huesped, Reserva + datos de prueba.
   El esquema lo define este script: no se usan migraciones.
   Ejecutar con:  sqlcmd -S localhost\SQLEXPRESS -E -C -f 65001 -i HotelDB.sql
   ===================================================================== */

IF DB_ID('HotelDB') IS NULL
    CREATE DATABASE HotelDB;
GO

USE HotelDB;
GO

/* ---------- Limpieza (el script se puede ejecutar varias veces) ---------- */
IF OBJECT_ID('dbo.Reserva', 'U')         IS NOT NULL DROP TABLE dbo.Reserva;
IF OBJECT_ID('dbo.Habitacion', 'U')      IS NOT NULL DROP TABLE dbo.Habitacion;
IF OBJECT_ID('dbo.Huesped', 'U')         IS NOT NULL DROP TABLE dbo.Huesped;
IF OBJECT_ID('dbo.TipoHabitacion', 'U')  IS NOT NULL DROP TABLE dbo.TipoHabitacion;
GO

/* ---------- Tablas ---------- */
CREATE TABLE dbo.TipoHabitacion
(
    TipoHabitacionId INT IDENTITY(1,1) NOT NULL,
    Nombre           NVARCHAR(50)      NOT NULL,
    PrecioNoche      DECIMAL(10,2)     NOT NULL,
    Capacidad        INT               NOT NULL,
    CONSTRAINT PK_TipoHabitacion       PRIMARY KEY (TipoHabitacionId),
    CONSTRAINT UQ_TipoHabitacion_Nombre UNIQUE (Nombre),
    CONSTRAINT CK_TipoHabitacion_Precio CHECK (PrecioNoche > 0),
    CONSTRAINT CK_TipoHabitacion_Capacidad CHECK (Capacidad BETWEEN 1 AND 10)
);
GO

CREATE TABLE dbo.Habitacion
(
    HabitacionId     INT IDENTITY(1,1) NOT NULL,
    Numero           NVARCHAR(10)      NOT NULL,
    TipoHabitacionId INT               NOT NULL,
    Piso             INT               NOT NULL,
    Activo           BIT               NOT NULL CONSTRAINT DF_Habitacion_Activo DEFAULT (1),
    CONSTRAINT PK_Habitacion            PRIMARY KEY (HabitacionId),
    CONSTRAINT UQ_Habitacion_Numero     UNIQUE (Numero),
    CONSTRAINT FK_Habitacion_TipoHabitacion FOREIGN KEY (TipoHabitacionId)
        REFERENCES dbo.TipoHabitacion (TipoHabitacionId),
    CONSTRAINT CK_Habitacion_Piso       CHECK (Piso BETWEEN 1 AND 50)
);
GO

CREATE TABLE dbo.Huesped
(
    HuespedId INT IDENTITY(1,1) NOT NULL,
    Nombres   NVARCHAR(80)      NOT NULL,
    Apellidos NVARCHAR(80)      NOT NULL,
    Dni       CHAR(8)           NOT NULL,
    Telefono  NVARCHAR(20)      NULL,
    Email     NVARCHAR(120)     NULL,
    Activo    BIT               NOT NULL CONSTRAINT DF_Huesped_Activo DEFAULT (1),
    CONSTRAINT PK_Huesped        PRIMARY KEY (HuespedId),
    CONSTRAINT UQ_Huesped_Dni    UNIQUE (Dni)
);
GO

CREATE TABLE dbo.Reserva
(
    ReservaId     INT IDENTITY(1,1) NOT NULL,
    HuespedId     INT               NOT NULL,
    HabitacionId  INT               NOT NULL,
    FechaIngreso  DATE              NOT NULL,
    FechaSalida   DATE              NOT NULL,
    Estado        NVARCHAR(20)      NOT NULL CONSTRAINT DF_Reserva_Estado DEFAULT ('Pendiente'),
    FechaRegistro DATETIME2(0)      NOT NULL CONSTRAINT DF_Reserva_FechaRegistro DEFAULT (SYSDATETIME()),
    CONSTRAINT PK_Reserva            PRIMARY KEY (ReservaId),
    CONSTRAINT FK_Reserva_Huesped    FOREIGN KEY (HuespedId)    REFERENCES dbo.Huesped (HuespedId),
    CONSTRAINT FK_Reserva_Habitacion FOREIGN KEY (HabitacionId) REFERENCES dbo.Habitacion (HabitacionId),
    CONSTRAINT CK_Reserva_Fechas     CHECK (FechaSalida > FechaIngreso),
    CONSTRAINT CK_Reserva_Estado     CHECK (Estado IN ('Pendiente', 'Confirmada', 'Cancelada', 'Finalizada'))
);
GO

CREATE INDEX IX_Reserva_Habitacion_Fechas ON dbo.Reserva (HabitacionId, FechaIngreso, FechaSalida);
GO

/* ---------- Datos de prueba ---------- */
INSERT INTO dbo.TipoHabitacion (Nombre, PrecioNoche, Capacidad) VALUES
    (N'Simple',       120.00, 1),
    (N'Doble',        190.00, 2),
    (N'Matrimonial',  230.00, 2),
    (N'Suite',        420.00, 4);
GO

INSERT INTO dbo.Habitacion (Numero, TipoHabitacionId, Piso, Activo) VALUES
    (N'101', 1, 1, 1),
    (N'102', 1, 1, 1),
    (N'103', 2, 1, 1),
    (N'201', 2, 2, 1),
    (N'202', 3, 2, 1),
    (N'203', 3, 2, 1),
    (N'301', 4, 3, 1),
    (N'302', 4, 3, 1),
    (N'104', 1, 1, 0);   -- dada de baja (eliminación lógica)
GO

INSERT INTO dbo.Huesped (Nombres, Apellidos, Dni, Telefono, Email, Activo) VALUES
    (N'Edwin William', N'Arévalo Sermeño', '70123456', N'987654321', N'edwin.arevalo@tecsup.edu.pe', 1),
    (N'María Fernanda', N'Quispe Rojas',   '45678912', N'912345678', N'maria.quispe@correo.com',     1),
    (N'Carlos Alberto', N'Núñez Vargas',   '10293847', N'945612378', N'carlos.nunez@correo.com',     1),
    (N'Lucía',          N'Paredes Soto',   '72635481', N'998877665', N'lucia.paredes@correo.com',    1),
    (N'Jorge',          N'Mendoza Flores', '41122334', NULL,         NULL,                           1);
GO

-- Reservas de prueba relativas a la fecha actual, para que el filtro de
-- disponibilidad siempre tenga cruces que mostrar.
DECLARE @hoy DATE = CAST(GETDATE() AS DATE);

INSERT INTO dbo.Reserva (HuespedId, HabitacionId, FechaIngreso, FechaSalida, Estado) VALUES
    (1, 1, DATEADD(DAY,  1, @hoy), DATEADD(DAY,  4, @hoy), N'Confirmada'),
    (2, 3, DATEADD(DAY,  2, @hoy), DATEADD(DAY,  5, @hoy), N'Pendiente'),
    (3, 5, DATEADD(DAY,  0, @hoy), DATEADD(DAY,  3, @hoy), N'Confirmada'),
    (4, 7, DATEADD(DAY,  6, @hoy), DATEADD(DAY,  9, @hoy), N'Pendiente'),
    (5, 2, DATEADD(DAY,  1, @hoy), DATEADD(DAY,  2, @hoy), N'Cancelada'),
    (1, 4, DATEADD(DAY, -8, @hoy), DATEADD(DAY, -5, @hoy), N'Finalizada');
GO

SELECT 'TipoHabitacion' AS Tabla, COUNT(*) AS Filas FROM dbo.TipoHabitacion
UNION ALL SELECT 'Habitacion', COUNT(*) FROM dbo.Habitacion
UNION ALL SELECT 'Huesped',    COUNT(*) FROM dbo.Huesped
UNION ALL SELECT 'Reserva',    COUNT(*) FROM dbo.Reserva;
GO
