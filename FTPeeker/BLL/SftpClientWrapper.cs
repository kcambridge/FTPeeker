using FTPeeker.Models.ViewModels;
using RecommendWebService.Models;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace FTPeeker.BLL
{
    public class SftpClientWrapper
    {
        string host;
		string username;
		string password;

		SftpClient sftp = null;

		public SftpClientWrapper(string host, string username, string password) {
			this.host = host;
			this.username = username;
			this.password = password;

			AuthenticationMethod authMethod = new PasswordAuthenticationMethod(username, password);
			ConnectionInfo conInfo = new ConnectionInfo(host, username, authMethod);
			this.sftp = new SftpClient(conInfo);
		}

        public SftpClientWrapper(string host, string username, string password, string keyFilePath, string keyFilePass)
        {
			this.host = host;
			this.username = username;
			this.password = password;

			PrivateKeyFile pkf = new PrivateKeyFile(keyFilePath, keyFilePass);
			AuthenticationMethod authMethod = new PrivateKeyAuthenticationMethod(username, pkf);
			ConnectionInfo conInfo = new ConnectionInfo(host, username, authMethod);
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
                var dirs = sftp.ListDirectory(remoteDirectory).Select(s => s.FullName);
                List<VMDirectoryItem> items = new List<VMDirectoryItem>();
                foreach (var d in dirs)
                {
                    items.Add(new VMDirectoryItem(d));
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

        public bool downloadFiles(string remoteDirectory, string remoteFileName, DirectoryInfo targetDirectory)
        {
            bool success = true;
            Directory.CreateDirectory(targetDirectory.FullName);
            Console.WriteLine("Connecting");
            sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(30);
            sftp.Connect();
            

            var remoteFiles = sftp.ListDirectory(remoteDirectory);

            //for each file in FTP directory
            foreach (var remoteFile in remoteFiles.OrderBy(x => x.Name))
            {
                bool retry = false;
                bool continueDownload = false;
                int retryCnt = 0;

                //if file name matches
                if (remoteFile.Name.Contains(remoteFileName))
                {
                    Console.WriteLine("Downloading: "+remoteFile.Name);
                    FileInfo localFile = new FileInfo(Path.Combine(targetDirectory.FullName, remoteFile.Name));
                    //delete file if already exists in local directory
                    if (localFile.Exists)
                    {
                        Console.WriteLine("Deleting local file: "+localFile.FullName);
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
            Console.WriteLine("Disconnecting...");
            sftp.Disconnect();
            return success;
            //throw new FileNotFoundException();
        }

        /// <summary>
        ///  Downloads the most recent version of a file, based on the dated file name
        /// </summary>
        /// <param name="remoteDirectory">Sub directory on SFTP server</param>
        /// <param name="remoteFileName">Name or part of the name of the file to be downloaded</param>
        /// <param name="targetDirectory">Directory where the file is to be saved</param>
        /// <returns></returns>
		public bool downloadFile(string remoteDirectory, string remoteFileName, DirectoryInfo targetDirectory) {
            bool success = true;
            Directory.CreateDirectory(targetDirectory.FullName);
            Console.WriteLine("Connecting...");
            sftp.ConnectionInfo.Timeout = TimeSpan.FromMinutes(30);
            sftp.Connect();


            var remoteFiles = sftp.ListDirectory(remoteDirectory);

            //for each file in FTP directory
            if (remoteFiles.Count() > 0)
            {
                var remoteFile = remoteFiles.Where(x => x.FullName.Contains(remoteFileName)).OrderByDescending(x => x.Name).First();
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

		public bool uploadFile(string remoteDirectory, string remoteFileName, DirectoryInfo targetDirectory, string targetFileName) {
			Directory.CreateDirectory(targetDirectory.FullName);

			sftp.Connect();
			sftp.ChangeDirectory(remoteDirectory.ToString());

			FileInfo file = new FileInfo(Path.Combine(targetDirectory.FullName, targetFileName));
			using (FileStream fileStream = new FileStream(file.FullName, FileMode.Open)) {
				sftp.UploadFile(fileStream, targetFileName, null);
			}

			var remoteFiles = sftp.ListDirectory(remoteDirectory);
			foreach (var remoteFile in remoteFiles) {
				if (remoteFile.Name.Contains(remoteFileName)) {
					if (remoteFile.Length == file.Length) {
						return true;
					} else {
						return false;
					}
				}
			}

			return false;
		}

		public void uploadAllFilesInDirectory(DirectoryInfo remoteDirectory, DirectoryInfo sourceDirectory, string keyFilePath, string keyFilePass) {
			PrivateKeyFile pkf = new PrivateKeyFile(keyFilePath, keyFilePass);
			AuthenticationMethod authMethod = new PrivateKeyAuthenticationMethod(username, pkf);
			ConnectionInfo conInfo = new ConnectionInfo(host, username, authMethod);
			SftpClient sftp = new SftpClient(conInfo);

			sftp.Connect();
			sftp.ChangeDirectory(remoteDirectory.ToString());

			foreach (FileInfo file in sourceDirectory.GetFiles()) {
				using (FileStream fileStream = new FileStream(file.FullName, FileMode.Open)) {

					sftp.UploadFile(fileStream, file.Name, null);
				}
			}
		}

    }
}