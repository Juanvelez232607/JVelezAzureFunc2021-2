using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace EntrprseClockFunction.Functions
{
    public static class Timer_Func
    {
        // correr un proceso automático cada N minutos que consolide la información de tiempo trabajado.
        ////Cada que consolide un trabajo coloca el valor consolidado en el detalle verdadero
        ///
        ///Una misma fecha puede tener múltiples entradas y salidas de cada empleado.
        ///Se decidió permitir múltiples entradas en la tabla por cada empleado en una sola fecha

        [FunctionName("Timer_Func")]
        public static void Run([TimerTrigger("0 * * * * *")]TimerInfo myTimer,  //Once per minute
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
