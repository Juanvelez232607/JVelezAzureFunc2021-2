using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace EntrprseClockFunction.Entities
{
    public class CustomTableObj : TableEntity
    {

        #region Declaracion de variables
        public string ID_Empleado { get; set; }
        public DateTime TimeEntrada { get; set; }
        public DateTime TimeSalida { get; set; }
        public string Tipo { get; set; } //0: Entrada, 1: Salida
        public bool Consolidado { get; set; } // Falso Cada que se agregue un nuevo registro
       
        #endregion
    }
}
