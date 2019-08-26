using FTPeeker.Models.ViewModels;
using FTPeeker.Models;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using System.Text;
using Renci.SshNet.Sftp;

namespace FTPeeker.BLL
{
    public class SftpClientWrapper
    {
        string host;
		string username;
		string password;

		SftpClient sftp = null;

		public SftpClientWrapper(string host, string username, string password, int port) {
			this.host = host;
			this.username = username;
			this.password = password;

            AuthenticationMethod authMethod;
            authMethod = new PasswordAuthenticationMethod(username, password);
			ConnectionInfo conInfo = new ConnectionInfo(host,port, username, authMethod);
			this.sftp = new SftpClient(conInfo);
		}

        public SftpClientWrapper(string host, string username, string password, int port, string SSHKeyPath, string sshKeyFilePass)
        {
            this.host = host;
            this.username = username;
            this.password = password;

            AuthenticationMethod authMethod;

            string key = File.ReadAllText(SSHKeyPath);
            //Regex removeSubjectRegex = new Regex("Subject:.*[\r\n]+", RegexOptions.IgnoreCase);
            //key = removeSubjectRegex.Replace(key, "");
            MemoryStream buf = new MemoryStream(Encoding.UTF8.GetBytes(key));
            PrivateKeyFile pkf = new PrivateKeyFile(buf, sshKeyFilePass);
            authMethod = new PrivateKeyAuthenticationMethod(username, pkf);
           
            ConnectionInfo conInfo = new ConnectionInfo(host, port, username, authMethod);
            this.sftp = new SftpClient(conInfo);
        }

        public AppResponse<List<VMDirectoryItem>> getDirectoryContents(string remoteDirectory)
        {
            AppResponse<List<VMDirectoryItem>> resp = new AppResponse<List<VMDirectoryItem>>();
            try
            {
                sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(30);
                sftp.Connect();
                sftp.ChangeDirectory("/");
                
                List<VMDirectoryItem> items = new List<VMDirectoryItem>();
                var dirObjects = sftp.ListDirectory(remoteDirectory);

                foreach (var dir in dirObjects)
                {
                    string d = dir.FullName;
                    if (!d.EndsWith("/.") && !d.Contains(".."))
                    {
                        string typeCode = VMDirectoryItemType.FILE;
                        int typeSequence = 1;
                        if (dir.IsDirectory)
                        {
                            typeCode =VMDirectoryItemType.DIRECTORY;
                            typeSequence = 0;
                        }
                        string[] parts = d.Split('/');
                        string val = parts[parts.Length - 1];
                        items.Add(new VMDirectoryItem(dir, remoteDirectory, typeCode, typeSequence));
                    }
                    
                }
                resp.SetSuccess();
                resp.setData(items);
            }
            catch (Exception ex)
            {
                resp.SetFailure(ex.Message);
            }
            
            return resp;
        }

        public AppResponse<SftpFile> getRemoteFileInfo(string remoteDir, string remoteFileName)
        {
            AppResponse<SftpFile> resp = new AppResponse<SftpFile>();
            try
            {
                sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(30);
                sftp.Connect();
                SftpFile remoteFile = sftp.ListDirectory(remoteDir).Where(x => x.Name == remoteFileName).FirstOrDefault();
                if (remoteFile != null)
                {
                    resp.SetSuccess();
                    resp.setData(remoteFile);
                }
                else
                {
                    resp.SetFailure("File not found");
                    resp.responseCode = AppResponse<SftpFile>.NOT_FOUND;
                }
            }
            catch (Exception ex)
            {
                resp.SetFailure(ex.Message);
            }
            finally
            {
                sftp.Disconnect();
            }
            return resp;
        }

        
		public bool downloadFile(string remoteDirectory, string remoteFileName, DirectoryInfo targetDirectory) {
            bool success = true;

            Directory.CreateDirectory(targetDirectory.FullName);
            sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(30);
            sftp.Connect();


            var remoteFiles = sftp.ListDirectory(remoteDirectory);

            //for each file in FTP directory
            if (remoteFiles.Count() > 0)
            {
                var remoteFile = remoteFiles.Where(x => x.Name == remoteFileName).OrderByDescending(x => x.Name).First();
                if (remoteFile != null)
                {
                    bool retry = false;
                    bool continueDownload = false;
                    int retryCnt = 0;

                    //if file name matches
                    if (remoteFile.Name.Contains(remoteFileName))
                    {
                        Console.WriteLine("Downloading: " + remoteFile.Name);
                        FileInfo localFile = new FileInfo(Path.Combine(targetDirectory.FullName, remoteFile.Name));
                        //delete file if already exists in local directory
                        if (localFile.Exists)
                        {
                            Console.WriteLine("Deleting local file: " + localFile.FullName);
                            localFile.Delete();
                        }

                        do
                        {
                            //download file from ftp
                            Stream destinationStream;

                            if (continueDownload)
                            {
                                Console.WriteLine("Resuming download...");
                                destinationStream = new FileStream(localFile.FullName, FileMode.Append);
                            }
                            else
                            {
                                Console.WriteLine("Starting download...");
                                destinationStream = new FileStream(localFile.FullName, FileMode.Create);
                            }

                            using (destinationStream)
                            using (var sourceStream = sftp.Open(remoteFile.FullName, FileMode.Open))
                            {

                                sourceStream.Seek(destinationStream.Length, SeekOrigin.Begin);
                                byte[] buffer = new byte[81920];
                                int read;
                                ulong total = (ulong)destinationStream.Length;
                                while ((read = sourceStream.Read(buffer, 0, buffer.Length)) != 0)
                                {
                                    destinationStream.Write(buffer, 0, read);

                                    // report progress
                                    //total = total + (ulong)read;
                                    //printActionDel(total);
                                }

                                localFile = new FileInfo(localFile.FullName);
                            }

                            if (localFile.Exists)
                            {
                                if (remoteFile.Length == localFile.Length)
                                {
                                    Console.WriteLine("Download complete!");
                                    continueDownload = false;
                                    retry = false;
                                    success = true;
                                    break;
                                }
                                else if (localFile.Length > remoteFile.Length)
                                {
                                    Console.WriteLine("Invalid Download. Disconnecting...");
                                    sftp.Disconnect();
                                    success = false;
                                    continueDownload = false;
                                    retry = false;
                                    break;
                                }
                                else if (localFile.Length < remoteFile.Length)
                                {
                                    Console.WriteLine("Continuing...");
                                    continueDownload = true;
                                    retry = false;
                                    sftp.Disconnect();
                                    continue;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Retrying...");
                                retry = true;
                                retryCnt++;
                            }


                        } while (retry && retryCnt < 3);

                    }
                }
                else
                {
                    Console.WriteLine("Empty file: '" + remoteFileName + "'");
                    throw new FileNotFoundException();
                }
            }
            else
            {
                Console.WriteLine("File '"+remoteFileName+"' not found.");
            }
            
            Console.WriteLine("Disconnecting...");
            sftp.Disconnect();
            return success;
		}

		public bool uploadFile(string remoteDirectory, string fileName, DirectoryInfo localDirectory) {
			Directory.CreateDirectory(localDirectory.FullName);

			sftp.Connect();
			sftp.ChangeDirectory(remoteDirectory.ToString());

			FileInfo file = new FileInfo(Path.Combine(localDirectory.FullName, fileName));
			using (FileStream fileStream = new FileStream(file.FullName, FileMode.Open)) {
				sftp.UploadFile(fileStream, fileName, null);
			}

			var remoteFiles = sftp.ListDirectory(remoteDirectory);
			foreach (var remoteFile in remoteFiles) {
				if (remoteFile.Name.Contains(fileName)) {
					if (remoteFile.Length == file.Length) {
                        sftp.Disconnect();
						return true;
					} else {
                        sftp.Disconnect();
						return false;
					}
				}
			}
            sftp.Disconnect();
			return false;
		}

    }
}