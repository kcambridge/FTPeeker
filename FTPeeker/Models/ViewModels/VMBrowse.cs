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

        public VMBrowse()
        {
            this.items = new Collection<VMDirectoryItem>();
        }
        public VMBrowse(ICollection<VMDirectoryItem> items)
        {
            this.items = items;
        }
    }
}