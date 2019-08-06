using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FTPeeker.Models.ViewModels
{
    public class VMDownload
    {
        public string remoteDir { get; set; }
        public int id { get; set; }
        public string fileName { get; set; }
        public AppResponse<Object> response { get; set; }
        public string siteName { get; set; }

        public VMDownload()
        {
            this.remoteDir = "";
            this.id = -1;
            this.fileName = "";
            this.siteName = "";
        }
        public VMDownload(int id , string path, string fileName, string siteName)
        {
            this.remoteDir = path;
            this.id = id;
            this.fileName = fileName;
            this.siteName = siteName;
        }
    }
}