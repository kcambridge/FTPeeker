using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Web;

namespace FTPeeker.BLL
{
    public class GPGEncryptionManager
    {
        public string GPGPath { set; get; }
        string APPLICAITON_NAME = "";
        string logFilePath = "";
        string GPGExecuteDomain { get; set; }
        string GPGExecuteUserName { get; set; }
        string GPGExecutePassword { get; set; }

        public GPGEncryptionManager(string GPGPath)
        {
            this.GPGPath = GPGPath;
            
        }
        public GPGEncryptionManager(string GPGPath, string ApplicationName, string logFilePath, string executeDomain, string executeUsername, string executePassword)
        {
            this.GPGPath = GPGPath;
            this.APPLICAITON_NAME = ApplicationName;
            this.logFilePath = logFilePath;
            this.GPGExecuteDomain = executeDomain;
            this.GPGExecuteUserName = executeUsername;
            this.GPGExecutePassword = executePassword;
        }

        public string DecryptFile(FileInfo encryptedFile, string passphrase, string decryptedFilename)
        {
            // decrypts the file using GnuPG, saves it to the file system
            // and returns the new (decrypted) file name.

            string path = encryptedFile.DirectoryName;
            string outputFileNameFullPath = path + "\\" + decryptedFilename;
            try
            {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("cmd.exe");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.Domain = this.GPGExecuteDomain;
                psi.UserName = this.GPGExecuteUserName;
                SecureString theSecureString = new NetworkCredential("", this.GPGExecutePassword).SecurePassword;
                psi.Password = theSecureString;
                psi.WorkingDirectory = this.GPGPath;
                //@"C:\Program Files (x86)\GnuPG"

                System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
                string sCommandLine = "gpg --output " + outputFileNameFullPath + " --batch --passphrase " + passphrase + " --pinentry-mode loopback "
                    + " --decrypt " + encryptedFile.FullName;
                
                process.StandardInput.WriteLine(sCommandLine);
                process.StandardInput.Flush();
                process.StandardInput.Close();
                
                //Get the output stream
                var outputReader = process.StandardOutput;
                var errorReader = process.StandardError;
                process.WaitForExit();

                //log the result of console activity
                string displayText = "Output" + Environment.NewLine + "==============" + Environment.NewLine;
                displayText += outputReader.ReadToEnd();
                displayText += Environment.NewLine + "Error" + Environment.NewLine + "==============" +
                        Environment.NewLine;
                displayText += errorReader.ReadToEnd();
                Logger.logActivity(this.APPLICAITON_NAME, logFilePath, "Console Output = " + displayText);
                
                
                process.WaitForExit();
                process.Close();
            }
            catch (Exception ex)
            {
                Logger.logException(this.APPLICAITON_NAME, logFilePath, "Error decrypting file. "+ex.Message+"\n Stack Trace: "+ex.StackTrace);
                Console.Write(ex.StackTrace);
                throw ex;
            }
            return outputFileNameFullPath;
        }

        public string EncryptFile(FileInfo sourceFile, string recipientEmail, string encryptedFilename)
        {
            // encrypts the file using GnuPG, saves it to the file system
            // and returns the new (encrypted) file name.

            string path = sourceFile.DirectoryName;
            string outputFileNameFullPath = path + "\\" + encryptedFilename;
            try
            {
                System.Diagnostics.ProcessStartInfo psi =
                  new System.Diagnostics.ProcessStartInfo("cmd.exe");
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardInput = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.WorkingDirectory = this.GPGPath;
                psi.Domain = this.GPGExecuteDomain;
                psi.UserName = this.GPGExecuteUserName;
                SecureString theSecureString = new NetworkCredential("", this.GPGExecutePassword).SecurePassword;
                psi.Password = theSecureString;
                //psi.WorkingDirectory = @"C:\Program Files (x86)\GnuPG";

                System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
                string sCommandLine = "gpg --output " + outputFileNameFullPath + " --batch --recipient " + recipientEmail + " --encrypt " + sourceFile.FullName;

                Logger.logException(this.APPLICAITON_NAME, logFilePath, "Command = "+sCommandLine);

                process.StandardInput.WriteLine(sCommandLine);
                process.StandardInput.Flush();
                process.StandardInput.Close();

                process.WaitForExit();
                //Get the output stream
                var outputReader = process.StandardOutput;
                var errorReader = process.StandardError;
                //log the result of console activity
                string displayText = "Output" + Environment.NewLine + "==============" + Environment.NewLine;
                displayText += outputReader.ReadToEnd();
                displayText += Environment.NewLine + "Error" + Environment.NewLine + "==============" +
                        Environment.NewLine;
                displayText += errorReader.ReadToEnd();
                Logger.logException(this.APPLICAITON_NAME, logFilePath, "Console Output = " + displayText);

                
                process.Close();
            }
            catch (Exception ex)
            {
                Console.Write(ex.StackTrace);
                Logger.logException(this.APPLICAITON_NAME, logFilePath, ex.StackTrace, "Encryption Error");
                throw ex;
            }
            return outputFileNameFullPath;
        }
    }
}