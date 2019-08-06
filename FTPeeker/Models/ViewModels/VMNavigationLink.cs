using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FTPeeker.Models.ViewModels
{
    public class VMNavigationLink
    {
        public string path { get; set; }
        public string displayText { get; set; }
        public bool isFirst { get; set; }
        public bool isLast { get; set; }

        public VMNavigationLink()
        {
            this.path = "";
            this.displayText = "";
            this.isFirst = false;
            this.isLast = false;
        }

        public VMNavigationLink(string path, string displayText)
        {
            this.path = path;
            this.displayText = displayText;
            this.isFirst = false;
            this.isLast = false;
        }
    }
}