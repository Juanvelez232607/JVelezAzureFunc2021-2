using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace EntrprseClockFunction.Functions
{
    public static class Timer_Func
    {
        [FunctionName("Timer_Func")]
        public static void Run([TimerTrigger("0 * * * * *")]TimerInfo myTimer,  //Once per minute
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
