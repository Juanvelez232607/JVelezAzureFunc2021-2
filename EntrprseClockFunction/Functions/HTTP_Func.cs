using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;
using EntrprseClockFunction.Entities;
using LibCommon.Models;
using System.Collections.Generic;
using LibCommon.Responses;

namespace EntrprseClockFunction.Functions
{
    public static class HTTP_Func
    {
        #region MyVars
        private static CloudTable TableTemp;
        private static CustomTableObj RowTemp;
        private static ILogger GlobalLog;
        private static bool OpenRowFound;
        private static RowTemplate Row_From_JSON;
        private static string Message;
        #endregion

        #region Funciones

        [FunctionName(nameof(InsertEntradaEmpleado))]
        public static async Task<IActionResult> InsertEntradaEmpleado(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "EmpleadosAPI")] HttpRequest req,
            [Table("EmpRegistryTable", Connection = "AzureWebJobsStorage")] CloudTable EmpRegistryTable,
            ILogger log)
        {
            TableTemp = EmpRegistryTable; //Making it global so it's accessible from other methods without using parameters
            GlobalLog = log;
            RowTemp = null;
            
            log.LogInformation("A POST Request was triggered");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            Row_From_JSON = null;
            Message = string.Empty;
            CustomTableObj Entidad = null;

            await ValNewEmpInput(requestBody);
            log.LogInformation("Validation done: " + OpenRowFound);
            if (!OpenRowFound)
            {
                TableOperation InsertarFila = OperacionInsertarEntrada(ref Entidad, Row_From_JSON);//crea operación, pero no la ejecuta
                log.LogInformation("TableOperation created");
                await EmpRegistryTable.ExecuteAsync(InsertarFila); //Ejecuta la operación especificada
                log.LogInformation("Entry stored");
                Message = "New entry for Employee added to the table via POST";
                return new OkObjectResult(CrearRespuesta(true, Message, Entidad));
            }
            else
            {
                return new BadRequestObjectResult(CrearRespuesta(false, "POST Request not completed: " + Message, Entidad));
            }
        }

        #endregion

        private static async Task<bool> ValNewEmpInput(string HTTPInput)
        {
            try
            {
                Row_From_JSON = JsonConvert.DeserializeObject<RowTemplate>(HTTPInput);
                Int16.Parse(Row_From_JSON.ID_Empleado).ToString();
                await BuscarRowAbierta(Row_From_JSON.ID_Empleado);
                if (!OpenRowFound) Message = "Employee is not inside";
                else Message = "Employee inside already";
            }
            catch (Exception err)
            {
                Message = err.Message;
                return false;
            }
            return false;
        }

        //devuelve la entrada que tenga abierta el empleado, o null si no tiene entrada abierta
        private static async Task<CustomTableObj> BuscarRowAbierta(string ID_Empleado)
        {
            int c = 0, c_ID = 0, c_Tipo = 0;
            string EmpComparison = "\nlista de comparaciones de ID:\n Tabla - Json\n";
            OpenRowFound = false;
            TableQuery<CustomTableObj> query = new TableQuery<CustomTableObj>();
            TableQuerySegment<CustomTableObj> All_Rows = await TableTemp.ExecuteQuerySegmentedAsync(query, null); //El segundo item es para hacer cancelación si se está demorando mucho la consulta
            foreach (CustomTableObj Row in All_Rows)
            {
                c++;
                EmpComparison = EmpComparison + Row.ID_Empleado + " - " + ID_Empleado + "\n";
                if (string.Equals(Row.ID_Empleado, ID_Empleado)) c_ID++;
                if (string.Equals(Row.Tipo, "0")) c_Tipo++;
                if (string.Equals(Row.ID_Empleado, ID_Empleado) && string.Equals(Row.Tipo, "0"))
                {
                    RowTemp = Row;
                    OpenRowFound = true;
                    break;
                }
            }
            GlobalLog.LogInformation("Total Rows analyzed: " + c);
            GlobalLog.LogInformation("Total ID_Empleado matches: " + c_ID);
            GlobalLog.LogInformation("Total Tipo Matches: " + c_Tipo);
            GlobalLog.LogInformation("RowFound?: " + OpenRowFound);
            GlobalLog.LogInformation("Row: " + RowTemp);
            GlobalLog.LogInformation(EmpComparison);
            return null;
        }


        private static TableOperation OperacionInsertarEntrada(ref CustomTableObj Entidad, RowTemplate Input_From_JSON)
        {
            //Entidad = new CustomTableObj(Input_From_JSON.ID_Empleado);
            Entidad = new CustomTableObj
            {
                ID_Empleado = Input_From_JSON.ID_Empleado,
                TimeEntrada = DateTime.UtcNow,
                TimeSalida = DateTime.UtcNow,
                Tipo = "0",
                Consolidado = false,
                ETag = "*",
                PartitionKey = "TODO",
                RowKey = Guid.NewGuid().ToString()
            };

            TableOperation CrearOperacion = TableOperation.Insert(Entidad);
            return CrearOperacion;
        }

        private static BasicResponse CrearRespuesta(bool IsSuccess, string Message, CustomTableObj FuncEntity)
        {
            return new BasicResponse { IsSuccess = IsSuccess, Message = Message, Result = FuncEntity };
        }
        private static BasicResponse CrearRespuesta(bool IsSuccess, string Message, TableQuerySegment<CustomTableObj> FuncEntity)
        {
            return new BasicResponse { IsSuccess = IsSuccess, Message = Message, Result = FuncEntity };
        }





    }
}
