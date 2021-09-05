using System;
using System.Collections.Generic;
using System.Text;

namespace LibCommon.Responses
{
    public class BasicResponse
    {
        public bool IsSuccess { get; set; }

        public string Message { get; set; }

        public object Result { get; set; }

    }
}
