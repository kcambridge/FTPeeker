using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FTPeeker.Models.ViewModels
{
    public class VMUpload
    {
        public int id { get; set; }
        public string path { get; set; }
        [DataType(DataType.Upload)]
        public HttpPostedFileBase[] files { get; set; }
        public string siteName { get; set; }
        
        public AppResponse<Object> response { get; set; }

        public VMUpload()
        {
            this.id = -1;
            this.path = "";
            this.response = null;
            this.siteName = "";
        }

        public VMUpload(int id, string path, string siteName)
        {
            this.id = id;
            this.path = path;
            this.response = null;
            this.siteName = siteName;
        }
    }
}