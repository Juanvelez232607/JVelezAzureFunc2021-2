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
        private static CustomTableObj RowTemp, Entidad;
        private static ILogger GlobalLog;
        private static bool OpenRowFound;
        private static RowTemplate Row_From_JSON;
        private static string Message, requestBody;
        private static List<CustomTableObj> TableRegion;
        #endregion

        #region Funciones

        [FunctionName(nameof(InsertEntradaEmpleado))]
        public static async Task<IActionResult> InsertEntradaEmpleado(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "EmpleadosAPI")] HttpRequest req,
            [Table("EmpRegistryTable", Connection = "AzureWebJobsStorage")] CloudTable EmpRegistryTable,
            ILogger log)
        {
            if (!InicializarVariables(EmpRegistryTable, log, await new StreamReader(req.Body).ReadToEndAsync()))
                return new BadRequestObjectResult(CrearRespuesta(false, "POST Request not started: Variables not initialized, " + Message, Entidad));
            log.LogInformation("A POST Request was triggered");

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


        [FunctionName(nameof(RegistrarSalidaEmpleado))]
        public static async Task<IActionResult> RegistrarSalidaEmpleado(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "EmpleadosAPI")] HttpRequest req,
            [Table("EmpRegistryTable", Connection = "AzureWebJobsStorage")] CloudTable EmpRegistryTable,
            ILogger log)
        {
            if (!InicializarVariables(EmpRegistryTable, log, await new StreamReader(req.Body).ReadToEndAsync()))
                return new BadRequestObjectResult(CrearRespuesta(false, "PUT Request not started: Variables not initialized, " + Message, Entidad));
            log.LogInformation("A PUT Request was triggered");

            await ValNewEmpInput(requestBody);
            log.LogInformation("Validation done, result: " + OpenRowFound);
            if (OpenRowFound)
            {
                TableOperation RegistrarSalida = OperacionSalidaEmpleado();//crea operación, pero no la ejecuta
                log.LogInformation("TableOperation created");
                await EmpRegistryTable.ExecuteAsync(RegistrarSalida); //Ejecuta la operación especificada
                log.LogInformation("Entry updated");
                Message = "An entry for Employee was updated in the table via PUT";
                return new OkObjectResult(CrearRespuesta(true, Message, Entidad));
            }
            else
            {
                return new BadRequestObjectResult(CrearRespuesta(false, "PUT Request not completed: " + Message, Entidad));
            }
        }


        [FunctionName(nameof(GetAllEmpleadoMovements))]
        public static async Task<IActionResult> GetAllEmpleadoMovements(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "EmpleadosAPI")] HttpRequest req,
            [Table("EmpRegistryTable", Connection = "AzureWebJobsStorage")] CloudTable EmpRegistryTable,
            ILogger log)
        {
            if (!InicializarVariables(EmpRegistryTable, log, await new StreamReader(req.Body).ReadToEndAsync()))
                return new BadRequestObjectResult(CrearRespuesta(false, "GET Request not started: Variables not initialized, " + Message, Entidad));
            log.LogInformation("A GET Request was triggered");
            try
            {
                TableQuery<CustomTableObj> query = new TableQuery<CustomTableObj>();
                TableQuerySegment<CustomTableObj> All_ToDo = await EmpRegistryTable.ExecuteQuerySegmentedAsync(query, null); //El segundo item es para hacer cancelación si se está demorando mucho la consulta
                Message = "All Employee movements retrieved!";
                return new OkObjectResult(CrearRespuesta(true, Message, All_ToDo));
            }
            catch (Exception err)
            {
                return new BadRequestObjectResult(CrearRespuesta(false, "GET Request not completed: " + err.Message, Entidad));
            }
        }


        /// <summary>
        /// ////////////////////
        /// </summary>
        /// <param name="req"></param>
        /// <param name="EmpRegistryTable"></param>
        /// <param name="Empleado_ID"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName(nameof(GetSingleEmpleadoMovements))]
        public static async Task<IActionResult> GetSingleEmpleadoMovements(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "EmpleadosAPI/{Empleado_ID}")] HttpRequest req,
            [Table("EmpRegistryTable", Connection = "AzureWebJobsStorage")] CloudTable EmpRegistryTable,
            string Empleado_ID,
            ILogger log)
        {
            if (!InicializarVariables(EmpRegistryTable, log, await new StreamReader(req.Body).ReadToEndAsync()))
                return new BadRequestObjectResult(CrearRespuesta(false, "GET Request not started: Variables not initialized, " + Message, Entidad));
            log.LogInformation("A GET Request was triggered");
            try
            {
                await BuscarAllRowsEmpleado(Empleado_ID);
                Message = $"All Employee {Empleado_ID} movements were retrieved!";
                return new OkObjectResult(CrearRespuesta(true, Message, TableRegion));
            }
            catch (Exception err)
            {
                return new BadRequestObjectResult(CrearRespuesta(false, "GET Request not completed: " + err.Message, Entidad));
            }
        }


        [FunctionName(nameof(DeleteRegByFieldID))]
        public static async Task<IActionResult> DeleteRegByFieldID(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "EmpleadosAPI/{Empleado_ID}")] HttpRequest req,
            [Table("EmpRegistryTable", Connection = "AzureWebJobsStorage")] CloudTable EmpRegistryTable,
            string RowID,
            ILogger log)
        {
            if (!InicializarVariables(EmpRegistryTable, log, await new StreamReader(req.Body).ReadToEndAsync()))
                return new BadRequestObjectResult(CrearRespuesta(false, "DELETE Request not started: Variables not initialized, " + Message, Entidad));
            log.LogInformation("A DELETE Request was triggered");
            try
            {
                await BuscarRowByID(RowID);
                if (OpenRowFound)
                {
                    Message = $"The row {RowID} Was found and deleted!";
                    await EmpRegistryTable.ExecuteAsync(OperacionBorrarRow());
                    RowTemp = null;
                    return new OkObjectResult(CrearRespuesta(true, Message, RowTemp));
                }
                else
                {
                    return new BadRequestObjectResult(CrearRespuesta(false, "Row ID not found. DELETE Request not done", Entidad));
                }
            }
            catch (Exception err)
            {
                return new BadRequestObjectResult(CrearRespuesta(false, "DELETE Request not completed: " + err.Message, Entidad));
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
            string aux;
            if (RowTemp == null) aux = "it's null";
            else aux = RowTemp.ID_Empleado + " - " + RowTemp.TimeEntrada;
            GlobalLog.LogInformation("Row: " + aux);
            GlobalLog.LogInformation(EmpComparison);
            return null;
        }

        private static async Task<CustomTableObj> BuscarAllRowsEmpleado(string ID_Empleado)
        {
            TableQuery<CustomTableObj> query = new TableQuery<CustomTableObj>();
            TableQuerySegment<CustomTableObj> All_Rows = await TableTemp.ExecuteQuerySegmentedAsync(query, null); //El segundo item es para hacer cancelación si se está demorando mucho la consulta
            TableRegion = new List<CustomTableObj>();
            foreach (CustomTableObj Row in All_Rows)
            {
                if (string.Equals(Row.ID_Empleado, ID_Empleado)) TableRegion.Add(Row);
            }
            return null;
        }

        private static async Task<CustomTableObj> BuscarRowByID(string Row_ID)
        {
            OpenRowFound = false;
            TableQuery<CustomTableObj> query = new TableQuery<CustomTableObj>();
            TableQuerySegment<CustomTableObj> All_Rows = await TableTemp.ExecuteQuerySegmentedAsync(query, null); //El segundo item es para hacer cancelación si se está demorando mucho la consulta
            TableRegion = new List<CustomTableObj>();
            foreach (CustomTableObj Row in All_Rows)
            {
                if (string.Equals(Row.RowKey, Row_ID))
                {
                    RowTemp = Row;
                    OpenRowFound = true;
                    return null;
                }
            }
            return null;
        }


        /*
        private static async void BuscarAllRowsEmpleado(string ID_Empleado)
        {
            TableQuery<CustomTableObj> query = new TableQuery<CustomTableObj>(), output = new TableQuery<CustomTableObj>();
            TableQuerySegment<CustomTableObj> RowMatchEmpID = await TableTemp.ExecuteQuerySegmentedAsync(
                query.Where(
                    TableQuery.GenerateFilterCondition(
                        "ID_Empleado", QueryComparisons.Equal, ID_Empleado
                        )
                    )
                , null); //El segundo item es para hacer cancelación si se está demorando mucho la consulta
            TableRegion = RowMatchEmpID;
        }
        */
        private static TableOperation OperacionInsertarEntrada(ref CustomTableObj Entidad, RowTemplate Input_From_JSON)
        {
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

        private static TableOperation OperacionSalidaEmpleado()
        {
            RowTemp.TimeSalida = DateTime.Now;
            RowTemp.Tipo = "1";
            GlobalLog.LogInformation("Row object updated");
            TableOperation CrearOperacion = TableOperation.Replace(RowTemp);
            return CrearOperacion;
        }

        private static BasicResponse CrearRespuesta(bool IsSuccess, string Message, CustomTableObj FuncEntity)
        {
            return new BasicResponse { IsSuccess = IsSuccess, Message = Message, Result = FuncEntity };
        }
        private static BasicResponse CrearRespuesta(bool IsSuccess, string Message, List<CustomTableObj> FuncEntity)
        {
            return new BasicResponse { IsSuccess = IsSuccess, Message = Message, Result = FuncEntity };
        }
        private static BasicResponse CrearRespuesta(bool IsSuccess, string Message, TableQuerySegment<CustomTableObj> FuncEntity)
        {
            return new BasicResponse { IsSuccess = IsSuccess, Message = Message, Result = FuncEntity };
        }
        private static bool InicializarVariables(CloudTable EmpRegistryTable, ILogger log, string HTTP_Input)
        {
            try
            {
                #region Inicializar Variables
                TableTemp = EmpRegistryTable; //Making it global so it's accessible from other methods without using parameters
                GlobalLog = log;
                RowTemp = null;
                requestBody = HTTP_Input;
                Row_From_JSON = null;
                Message = string.Empty;
                Entidad = null;
                #endregion
                return true;
            }
            catch (Exception err)
            {
                Message = err.Message;
                return false;
            }
        }


        private static TableOperation OperacionBorrarRow()
        {
            TableOperation CrearOperacion = TableOperation.Delete(RowTemp);
            return CrearOperacion;
        }


    }
}
