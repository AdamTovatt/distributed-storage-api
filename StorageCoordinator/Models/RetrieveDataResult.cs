using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StorageCoordinator.Models
{
    public class RetrieveDataResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public HttpStatusCode StatusCode { get; set; }

        public RetrieveDataResult(bool success, string message, HttpStatusCode httpStatusCode)
        {
            Success = success;
            Message = message;
            StatusCode = httpStatusCode;
        }
    }
}
