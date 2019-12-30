using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FTPeeker.Models.ViewModels
{
    public class VMSFTPPermission
    {
        public static string READ = "READ";
        public static string ADMIN = "ADMIN";
        public static string DELETE = "DELETE";
        public static string UPLOAD = "UPLOAD";
        public static string EDIT = "EDIT";


        public bool canRead { get; set; }
        public bool canUpload { get; set; }
        public bool canEdit { get; set; }

        public VMSFTPPermission()
        {
            this.canEdit = false;
            this.canUpload = false;
            this.canRead = false;
        }

        public VMSFTPPermission(bool canRead, bool canUpload, bool canEdit)
        {
            this.canEdit = canEdit;
            this.canUpload = canUpload;
            this.canRead = canRead;
        }
    
    }
}