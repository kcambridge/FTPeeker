using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace FTPeeker.BLL
{
    public class Logger
    {
        public static bool logException(string applicationName, Exception ex, string logFileDir, string details = "", string logHeadding = "", string logType = "error")
        {
            bool success = false;
            try
            {
                if (!Directory.Exists(logFileDir))
                {
                    Directory.CreateDirectory(logFileDir);
                }

                string fileName = applicationName + "_" + logType + "_log.txt";
                if (logFileDir.Substring(logFileDir.Length - 1) != "\\")
                {
                    fileName = "\\" + fileName;
                }

                string filePath = logFileDir + fileName;

                string logDetils = "\n\n"+logHeadding + "Exception encounted on " + DateTime.Now.ToString("MM/dd/yy HH:mm:ss") + "\nMessage: " + ex.Message;
                if (details != "")
                {
                    logDetils += "\nAdditional Details:\n" + details;
                }
                logDetils += "\nStackTrace: " + ex.StackTrace + "\n";
                using (StreamWriter sw = File.AppendText(filePath))
                {
                    sw.Write(logDetils);
                }
                success = true;
            }
            catch (Exception)
            {

                success = false;
            }
            return success;
        }

        public static bool logException(string applicationName, string logFileDir, string details = "", string logHeadding = "", string logType = "error")
        {
            bool success = false;
            try
            {
                if (!Directory.Exists(logFileDir))
                {
                    Directory.CreateDirectory(logFileDir);
                }

                string fileName = applicationName + "_" + logType + "_log.txt";
                if (logFileDir.Substring(logFileDir.Length - 1) != "\\")
                {
                    fileName = "\\" + fileName;
                }

                string filePath = logFileDir + fileName;

                string logDetils = "\n\n"+logHeadding + " : Error encounted on " + DateTime.Now.ToString("MM/dd/yy HH:mm:ss");
                if (details != "")
                {
                    logDetils += "\nAdditional Details:\n" + details;
                }

                using (StreamWriter sw = File.AppendText(filePath))
                {
                    sw.Write(logDetils);
                }
                success = true;
            }
            catch (Exception)
            {

                success = false;
            }
            return success;
        }

        public static bool logActivity(string applicationName, string logFileDir, string details = "", string logHeadding = "", string logType = "activity")
        {
            bool success = false;
            try
            {
                if (!Directory.Exists(logFileDir))
                {
                    Directory.CreateDirectory(logFileDir);
                }

                string fileName = applicationName + "_" + logType + "_log.txt";
                if (logFileDir.Substring(logFileDir.Length - 1) != "\\")
                {
                    fileName = "\\" + fileName;
                }

                string filePath = logFileDir + fileName;

                string logDetils = "\n\n" + logHeadding + " on " + DateTime.Now.ToString("MM/dd/yy HH:mm:ss");
                if (details != "")
                {
                    logDetils += "\nDetails:\n\n\n" + details;
                }
                using (StreamWriter sw = File.AppendText(filePath))
                {
                    sw.Write(logDetils);
                }
                success = true;
            }
            catch (Exception)
            {

                success = false;
            }
            return success;
        }
    }
}