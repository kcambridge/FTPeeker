using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FTPeeker.Models
{
    public class AppResponse<T>
    {
        public string responseCode { get; set; }
        public string message { get; set; }
        public bool success { get; set; }
        private T data;

        public static string SUCCESS = "OK";
        public static string PARIAL_SUCCESS = "PARTOK";
        public static string FAIL = "FAIL";
        public static string LOGIN_FAIL = "LOGINFAIL";
        public static string NOT_FOUND = "NOTFOUND";
        public static string AUTH_FAIL = "AUTHFAIL";
        public static string INVALID_JSON_FORMAT = "INVJSON";
        public static string INVALID_PARAMETERS = "INVPARAM";
        
        public AppResponse()
        {
            this.responseCode = "";
            this.message = "";
            this.success = false;
        }

        public AppResponse(string responseCode, string message, bool success)
        {
            this.responseCode = responseCode;
            this.message = message;
            this.success = success;
        }

        public void SetSuccess(string message)
        {
            this.success = true;
            this.responseCode = SUCCESS;
            this.message = message;
        }

        public void SetSuccess()
        {
            this.success = true;
            this.responseCode = SUCCESS;
            this.message = "Success!";
        }

        public void SetFailure(string message)
        {
            this.success = false;
            this.responseCode = FAIL;
            this.message = message;
        }

        public void SetResponse(string responseCode, string message, bool success)
        {
            this.success = success;
            this.responseCode = responseCode;
            this.message = message;
        }

        public T getData()
        {
            return data;
        }

        public void setData(T data)
        {
            this.data = data;
        }
        
    }
}