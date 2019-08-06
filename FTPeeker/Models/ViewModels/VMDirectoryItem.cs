using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FTPeeker.Models.ViewModels
{
    public class VMDirectoryItem
    {
        public string name { get; set; }
        public string path { get; set; }
        public string typeCode { get; set; }
        public string directory { get; set; }
        public int typeSequence { get; set; }
        public DateTime lastModified { get; set; }
        public long size { get; set; }

        public VMDirectoryItem()
        {
            this.name = "";
            this.typeCode = "";
            this.typeSequence = 1;
            this.path = "";
            this.size = 0;
            this.lastModified = DateTime.MinValue;
            this.directory = "";
        }

        public VMDirectoryItem(string name,string path,string directory, string typeCode, int typeSequence = 1)
        {
            this.name = name;
            this.path = path;
            this.typeCode = typeCode;
            this.typeSequence = typeSequence;
            this.size = 0;
            this.lastModified = DateTime.MinValue;
            this.directory = directory;
            
            
        }

        public VMDirectoryItem(SftpFile file,string directory,string typeCode, int typeSequence = 1)
        {
            string[] parts = file.FullName.Split('/');
            string val = parts[parts.Length - 1];

            this.name = val;
            this.path = file.FullName;
            this.typeCode = typeCode;
            this.typeSequence = typeSequence;
            this.lastModified = file.LastWriteTime;
            this.size = file.Length;
            this.directory = directory;
        }

        public bool isUpFolder()
        {
            if (this.typeCode == VMDirectoryItemType.DIRECTORY_UP)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isFolder()
        {
            if (this.typeCode == VMDirectoryItemType.DIRECTORY)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool isFile()
        {
            if (this.typeCode == VMDirectoryItemType.FILE)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}