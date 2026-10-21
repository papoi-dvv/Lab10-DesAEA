/* Paso final del laboratorio: nueva columna y regeneración del modelo.
   Después de ejecutarlo hay que volver a hacer scaffold con --force. */
USE HotelDB;
GO

IF COL_LENGTH('dbo.Reserva', 'Observaciones') IS NULL
    ALTER TABLE Reserva ADD Observaciones NVARCHAR(200) NULL;
GO
