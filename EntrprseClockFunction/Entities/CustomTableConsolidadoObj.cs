using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace EntrprseClockFunction.Entities
{
    class CustomTableConsolidadoObj : TableEntity
    {

        #region Declaracion de variables

        //Tiempo consolidado por empleado:
        public int ID_Empleado { get; set; }
        public DateTime FechaActual { get; set; }
        public int TotalMinTrabajados { get; set; }

        #endregion

        #region Constructores

        public CustomTableConsolidadoObj(int iD_Empleado, DateTime fechaActual, int totalMinTrabajados)
        {
            ID_Empleado = iD_Empleado;
            FechaActual = fechaActual;
            TotalMinTrabajados = totalMinTrabajados;
        }

        public CustomTableConsolidadoObj()
        {
        }

        #endregion
    }
}
