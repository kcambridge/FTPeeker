using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;

namespace FTPeeker.Models.ViewModels
{
    public class VMBrowse
    {
        public ICollection<VMDirectoryItem> items { get; set; }
        public string previousPath { get; set; }
        public string path { get; set; }
        public int id { get; set; }
        public string errorMessage { get; set; }
        public string siteName { get; set; }
        public ICollection<VMNavigationLink> navLinks { get; set; }
        public VMSFTPPermission permissions { get; set; }

        public VMBrowse()
        {
            this.items = new Collection<VMDirectoryItem>();
            this.id = -1;
            this.path = "";
            this.previousPath = "";
            this.errorMessage = "";
            this.siteName = "";
            this.navLinks = new Collection<VMNavigationLink>();
            this.permissions = new VMSFTPPermission();
        }
        public VMBrowse(ICollection<VMDirectoryItem> items, int id, string path, string previousPath, string siteName, VMSFTPPermission permissions)
        {
            this.items = items;
            this.id = id;
            this.path = path;
            this.previousPath = previousPath;
            this.errorMessage = "";
            this.siteName = siteName;
            this.navLinks = new Collection<VMNavigationLink>();
            this.permissions = permissions;
        }
    }
}