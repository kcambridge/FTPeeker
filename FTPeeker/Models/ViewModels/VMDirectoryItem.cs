using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FTPeeker.Models.ViewModels
{
    public class VMDirectoryItem
    {
        public string name { get; set; }

        public VMDirectoryItem()
        {
            this.name = "";
        }

        public VMDirectoryItem(string name)
        {
            this.name = name;
        }
    }
}