using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FTPeeker.Models.Entities;
using FTPeeker.BLL;
using FTPeeker.Models.ViewModels;
using FTPeeker.Models;
using System.Web.Script.Serialization;
using System.IO;
using System.Configuration;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Data.Entity.Validation;
using Renci.SshNet.Sftp;
using System.Threading.Tasks;
using System.Net;

namespace FTPeeker.Controllers
{
    public class HomeController : Controller
    {
        FTPeekerEntities dbContext = new FTPeekerEntities();
        string localFileDir = ConfigurationManager.AppSettings["LocalFileDir"].ToString();
        string ACTION_UPLOAD = "UPLOAD";
        string ACTION_DOWNLOAD = "DOWNLOAD";

        // GET: FTP
        [HttpGet]
        public ActionResult Index()
        {
            string userID = SecurityManager.getUserID();
            ICollection<FTPK_FTPs> sites = new Collection<FTPK_FTPs>();
            if (SecurityManager.isAdmin())
            {
                sites = dbContext.FTPK_FTPs.ToList();
            }
            else
            {
                sites = dbContext.FTPK_FTPs.Where(x => x.FTPK_User_Permissions.Any(y => y.UserID == userID)).ToList();
            }
            return View(sites);
        }

        [HttpGet]
        public ActionResult DeliverFile(int? id, string fileName)
        {
            if (!id.HasValue)
            {
                return RedirectToAction("Index");
            }

            //TODO: Need to secure this endpoint by SFTP id
            if (!SecurityManager.canRead(id.Value))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }
            string downloadPath = Server.MapPath(localFileDir);
            string localFilePath = Path.Combine(downloadPath, fileName);
            if (System.IO.File.Exists(localFilePath))
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(localFilePath);
                return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
            }
            else
            {
                return HttpNotFound();
            }

        }

        [HttpPost]
        public JsonResult FetchFile(int id, string remoteDir, string fileName)
        {
            AppResponse<String> resp = new AppResponse<String>();
            FTPK_FTPs site = dbContext.FTPK_FTPs.Where(x => x.ID == id).FirstOrDefault();
            
            if (site == null)
            {
                resp.SetFailure("Invalid FTP Site");
                return Json(resp);
            }

            if (!SecurityManager.canRead(site.ID))
            {
                resp.SetFailure("Unauthorized");
                return Json(resp);
            }


            string downloadPath = Server.MapPath(localFileDir);
            if (!Directory.Exists(downloadPath))
            {
                Directory.CreateDirectory(downloadPath);
            }
            DirectoryInfo downloadDir = new DirectoryInfo(downloadPath);
            string localFilePath = Path.Combine(downloadPath, fileName);

            try
            {
                bool useCacheFile = false;
                SftpClientWrapper sftp = initSFTP(site);
                FileInfo localFile = downloadDir.GetFiles().Where(x => x.Name == fileName).FirstOrDefault();
                if (localFile != null)
                {
                    AppResponse<SftpFile> fileResp = sftp.getRemoteFileInfo(remoteDir, fileName);
                    if (fileResp.success)
                    {
                        SftpFile remoteFile = fileResp.getData();
                        if (remoteFile.Length == localFile.Length && remoteFile.LastWriteTime <= localFile.LastWriteTime)
                        {
                            useCacheFile = true;
                            resp.SetSuccess();
                        }
                        else
                        {
                            localFile.Delete();
                        }
                    }
                    else
                    {
                        localFile.Delete();
                    }
                }

                if (!useCacheFile)
                {
                    bool res = sftp.downloadFile(remoteDir, fileName, downloadDir);
                    if (res)
                    {
                        resp.SetSuccess();
                    }
                    else
                    {
                        resp.SetFailure("Failed to download file. Please try again later.");
                    }
                }
                
            }
            catch (Exception ex)
            {
                resp.SetFailure(ex.Message);
            }
            finally
            {
                //kick off cache cleanup
                Task.Factory.StartNew(this.CleanUpCache);
            }
            return Json(resp);
        }

        [HttpGet]
        public ActionResult Download(int? id, string remoteDir, string fileName)
        {
            AppResponse<Object> resp = new AppResponse<object>();
            if (remoteDir == null)
            {
                remoteDir = "";
            }

            if (!id.HasValue)
            {
                return RedirectToAction("Index");
            }

            if (fileName == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
           
            string siteName = "";
            try
            {
                FTPK_FTPs site = dbContext.FTPK_FTPs.Where(x => x.ID == id.Value).FirstOrDefault();

                if (site == null)
                {
                    return HttpNotFound("No FTP Site with ID '" + id.Value + ", found");
                }

                if (!SecurityManager.canRead(site.ID))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
                }

                siteName = site.DisplayName;

                FTPK_Logs log = new FTPK_Logs();
                log.SiteID = id.Value;
                log.Action = ACTION_DOWNLOAD;
                log.FileName = fileName;
                log.Path = remoteDir;
                log.UserID = SecurityManager.getUserID();
                log.LogDate = DateTime.Now;
                dbContext.FTPK_Logs.Add(log);

                dbContext.SaveChanges();
                    
                resp.SetSuccess();
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name, eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine("- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName, ve.ErrorMessage);
                    }
                }
                throw;
            }
            catch (Exception ex)
            {
                resp.SetFailure(ex.Message);
                    
            }
            VMDownload model = new VMDownload(id.Value, remoteDir, fileName, siteName);
            model.response = resp;
            return View(model);
        }

        [HttpGet]
        public ActionResult Upload(int? id, string path = "")
        {
            if (!id.HasValue)
            {
                return RedirectToAction("Index");
            }
            FTPK_FTPs site = dbContext.FTPK_FTPs.Where(x => x.ID == id.Value).FirstOrDefault();
            if (site == null)
            {
                return HttpNotFound("No FTP Site with ID '"+id.Value+"' found");
            }

            if (!SecurityManager.canUpload(site.ID))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }

            VMUpload model = new VMUpload(id.Value, path, site.DisplayName);
            return View(model);
        }

        [HttpPost]
        public ActionResult Upload(VMUpload model)
        {
            if (model == null)
            {
                return HttpNotFound();
            }

            if (model.path == null)
            {
                model.path = "";
            }

            AppResponse<List<string>> resp = uploadFiles(model.files);

            if (!resp.success)
            {
                ModelState.AddModelError("files", resp.message);
                return View(model);
            }

            List<string> filesToUpload = resp.getData();

            FTPK_FTPs site = dbContext.FTPK_FTPs.Where(x => x.ID == model.id).FirstOrDefault();
            if (site == null)
            {
                return HttpNotFound();
            }

            if (!SecurityManager.canUpload(site.ID))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }

            SftpClientWrapper sftp = initSFTP(site);

            string fullPath = Server.MapPath(localFileDir);
            DirectoryInfo localDir = new DirectoryInfo(fullPath);

            int failedCnt = 0;
            foreach (string fName in filesToUpload)
            {
                try
                {
                    FTPK_Logs log = new FTPK_Logs();
                    log.SiteID = model.id;
                    log.Action = ACTION_UPLOAD;
                    log.FileName = fName;
                    log.Path = model.path;
                    log.UserID = SecurityManager.getUserID();
                    log.LogDate = DateTime.Now;
                    dbContext.FTPK_Logs.Add(log);

                    dbContext.SaveChanges();

                    resp.SetSuccess();
                }
                catch (Exception ex)
                {
                    resp.SetFailure(ex.Message);
                }

                bool success = sftp.uploadFile(model.path, fName, localDir);
                if (!success)
                {
                    ModelState.AddModelError("files", fName+" failed to upload. Please retry this file.");
                    failedCnt ++;
                }
                
            }
            AppResponse<Object> mResp = new AppResponse<object>();
            if(!ModelState.IsValid)
            {
                mResp.SetFailure(failedCnt + " of " + filesToUpload.Count() + " file(s) uploaded successfully!");

            }
            else
            {
                mResp.SetSuccess();
            }


            model.files = null;
            model.response = mResp;
            return View(model);
        }

        [HttpGet]
        public ActionResult Browse(int? id, string path = "")
        {
            if (!id.HasValue)
            {
                return RedirectToAction("Index");
            }
            
            FTPK_FTPs site = dbContext.FTPK_FTPs.Where(x => x.ID == id).FirstOrDefault();
            if (site == null)
            {
                return HttpNotFound();
            }

            if (!SecurityManager.canRead(site.ID))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }

            SftpClientWrapper sftp = initSFTP(site);

            AppResponse<List<VMDirectoryItem>> resp = sftp.getDirectoryContents(path);
            VMBrowse model = new VMBrowse();
            if (resp.success)
            {
                List<VMDirectoryItem> items = (List<VMDirectoryItem>)resp.getData();
                VMSFTPPermission permissions = SecurityManager.getPermissions(site.ID);
                model = new VMBrowse(items.OrderBy(x => x.typeSequence).ThenBy(x => x.name).ToList(), id.Value, path, getPreviousPath(path), site.DisplayName, permissions);
                model.navLinks = getNavigationTreeLinks(path);
            }
            else
            {
                model.errorMessage = "Something when wrong. "+resp.message;
            }
            return View(model);
        }

        private void CleanUpCache()
        {
            try
            {
                string downloadPath = Server.MapPath(localFileDir);
                if (!Directory.Exists(downloadPath))
                {
                    Directory.CreateDirectory(downloadPath);
                }
                DirectoryInfo downloadDir = new DirectoryInfo(downloadPath);
                var oldFiles = downloadDir.GetFiles().Where(x => x.LastWriteTime < DateTime.Now.AddHours(-24)).ToList();
                foreach (var file in oldFiles)
                {
                    file.Delete();
                }
            }
            catch (Exception)
            {
                
                //do nothing
            }
            
        }
        private AppResponse<List<string>> uploadFiles(HttpPostedFileBase[] files)
        {
            AppResponse<List<string>> resp = new AppResponse<List<string>>();
            //Uploading files
            if (files != null)
            {
                try
                {
                    List<string> filesToUpload = new List<string>();
                    foreach (var f in files)
                    {
                        if (f != null)
                        {
                            Regex rgx = new Regex("[^a-zA-Z0-9 -_.]");
                            var uploadDir = localFileDir + "\\";
                            string fullPath = Server.MapPath(localFileDir);
                            if (!Directory.Exists(fullPath))
                            {
                                Directory.CreateDirectory(fullPath);
                            }
                            
                            var filePathTemp = Path.Combine(fullPath, f.FileName);
                            var fileNameNoEx = rgx.Replace(Path.GetFileNameWithoutExtension(filePathTemp), "");

                            var fileEx = Path.GetExtension(filePathTemp);
                            string fileNameInc = "";
                            int inc = 0;
                            var filePath = Path.Combine(fullPath, fileNameNoEx + fileNameInc + fileEx);
                            string fName = fileNameNoEx +fileNameInc+ fileEx;

                            while (System.IO.File.Exists(filePath))
                            {
                                inc++;
                                fileNameInc = inc.ToString();
                                filePath = Path.Combine(fullPath, fileNameNoEx + fileNameInc + fileEx);
                                fName = fileNameNoEx +fileNameInc +fileEx;
                            }
                            f.SaveAs(filePath);
                            filesToUpload.Add(fName);
                        }

                    }

                    resp.SetSuccess();
                    resp.setData(filesToUpload);
                }
                catch (Exception ex)
                {
                    resp.SetFailure(ex.Message);
                }
                
            }

            return resp;
        }

        private List<VMNavigationLink> getNavigationTreeLinks(string currentPath)
        {
            List<VMNavigationLink> links = new List<VMNavigationLink>();
            VMNavigationLink root = new VMNavigationLink("", "home");
            links.Add(root);

            if (currentPath.Contains("/"))
            {
                string[] parts = currentPath.Split('/');
                string retPath = "";
                for (int i = 1; i <= parts.Length - 1; i++)
                {
                    if (parts[i] !="")
                    {
                        if (retPath == "")
                        {
                            retPath = "/" + parts[i];
                        }
                        else
                        {
                            retPath = retPath + "/" + parts[i];
                        }
                        VMNavigationLink thisLink = new VMNavigationLink(retPath, parts[i]);
                        links.Add(thisLink);
                    }
                    
                }
            }

            links.First().isFirst = true;
            links.Last().isLast = true;

            return links;
        }

        private SftpClientWrapper initSFTP(FTPK_FTPs site)
        {
            if (site.AuthTypeCode == VMAuthType.SSH_KEY)
            {
                string sshFilePath = Server.MapPath(site.SSHKeyPath);
                return new SftpClientWrapper(site.Host, site.UserName, site.Password, site.Port, sshFilePath, site.SSHKeyPassword);
            }
            else
            {
                return new SftpClientWrapper(site.Host, site.UserName, site.Password, site.Port);
            }
        }

        private string getPreviousPath(string currentPath)
        {
            string retPath = currentPath;
            if(currentPath.Contains("/"))
            {
                retPath = "";
                string[] parts = currentPath.Split('/');
                for (int i = 0; i <= parts.Length - 2; i++)
                {
                    if(i == 0)
                    {
                        retPath = "/"+parts[i];
                    }
                    else
                    {
                        retPath = retPath + "/" + parts[i];
                    }
                    
                }
            }
            return retPath;
        }
    }
}