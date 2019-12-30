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
            ICollection<FTPK_FTPs> sites = dbContext.FTPK_FTPs.ToList();
            return View(sites);
        }

        [HttpGet]
        public ActionResult DeliverFile(string fileName)
        {
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


            string downloadPath = Server.MapPath(localFileDir);
            if (!Directory.Exists(downloadPath))
            {
                Directory.CreateDirectory(downloadPath);
            }
            DirectoryInfo downloadDir = new DirectoryInfo(downloadPath);
            string localFilePath = Path.Combine(downloadPath, fileName);


            //TODO: need to check if file size matches remote file here. Use local file if size the same.
            foreach (var file in downloadDir.GetFiles().Where(x => x.Name == fileName))
            {
                file.Delete();
            }
            

            try
            {
                SftpClientWrapper sftp = new SftpClientWrapper(site.Host, site.UserName, site.Password, site.Port);
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
            catch (Exception ex)
            {

                resp.SetFailure(ex.Message);
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
            if (id.HasValue && fileName != "")
            {
                string siteName = "";
                try
                {
                    FTPK_FTPs site = dbContext.FTPK_FTPs.Where(x => x.ID == id.Value).FirstOrDefault();

                    if (site == null)
                    {
                        return HttpNotFound("No FTP Site with ID '" + id.Value + ", found");
                    }

                    siteName = site.DisplayName;

                    FTPK_Logs log = new FTPK_Logs();
                    log.SiteID = id.Value;
                    log.Action = ACTION_DOWNLOAD;
                    log.FileName = fileName;
                    log.Path = remoteDir;
                    log.UserID = User.Identity.Name.ToString();
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
            else
            {
                return HttpNotFound();
            }
        }

        [HttpGet]
        public ActionResult Upload(int? id, string path = "")
        {
            if (id.HasValue)
            {
                FTPK_FTPs site = dbContext.FTPK_FTPs.Where(x => x.ID == id.Value).FirstOrDefault();
                if (site == null)
                {
                    return HttpNotFound("No FTP Site with ID '"+id.Value+"' found");
                }
                VMUpload model = new VMUpload(id.Value, path, site.DisplayName);
                return View(model);
            }
            else
            {
                return HttpNotFound();
            }
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

            AppResponse<List<string>> resp = uploadBackupFiles(model.files);

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
           
            SftpClientWrapper sftp = new SftpClientWrapper(site.Host, site.UserName, site.Password, site.Port);
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
                    log.UserID = User.Identity.Name.ToString();
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
                return HttpNotFound();
            }
            FTPK_FTPs site = dbContext.FTPK_FTPs.Where(x => x.ID == id).FirstOrDefault();
            if (site == null)
            {
                return HttpNotFound();
            }

            SftpClientWrapper sftp = new SftpClientWrapper(site.Host,site.UserName, site.Password,site.Port);
            AppResponse<List<VMDirectoryItem>> resp = sftp.getDirectoryContents(path);
            VMBrowse model = new VMBrowse();
            if (resp.success)
            {
                List<VMDirectoryItem> items = (List<VMDirectoryItem>)resp.getData();
                model = new VMBrowse(items.OrderBy(x => x.typeSequence).ThenBy(x => x.name).ToList(), id.Value, path, getPreviousPath(path), site.DisplayName);
                model.navLinks = getNavigationTreeLinks(path);
            }
            else
            {
                model.errorMessage = "Something when wrong. "+resp.message;
            }
            return View(model);
        }

        private AppResponse<List<string>> uploadBackupFiles(HttpPostedFileBase[] files)
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
                            Regex rgx = new Regex("[^a-zA-Z0-9 -_]");
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