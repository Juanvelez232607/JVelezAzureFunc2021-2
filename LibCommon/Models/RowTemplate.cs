using System;
using System.Collections.Generic;
using System.Text;

namespace LibCommon.Models
{
    public class RowTemplate
    {
        #region Declaracion de variables
        public string ID_Empleado { get; set; }
        public DateTime TimeEntrada { get; set; }
        public DateTime TimeSalida { get; set; }
        public string Tipo { get; set; } //0: Entrada, 1: Salida
        public bool Consolidado { get; set; } // Falso Cada que se agregue un nuevo registro
        #endregion

        #region Constructores
        public RowTemplate(string ID_Empleado, DateTime TimeEntrada, DateTime TimeSalida, string Tipo, bool Consolidado)
        {
            this.ID_Empleado = ID_Empleado;
            this.TimeEntrada = TimeEntrada;
            this.TimeSalida = TimeSalida;
            this.Tipo = Tipo;
            this.Consolidado = Consolidado;
        }
        public RowTemplate()
        {

        }
        #endregion
    }
}
