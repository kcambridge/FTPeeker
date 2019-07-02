using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FTPeeker.Models.Entities;
using FTPeeker.BLL;
using RecommendWebService.Models;
using FTPeeker.Models.ViewModels;

namespace FTPeeker.Controllers
{
    public class FTPController : Controller
    {
        FTPeekerEntities dbContext = new FTPeekerEntities();

        // GET: FTP
        [HttpGet]
        public ActionResult Index()
        {
            ICollection<FTPK_FTPs> sites = dbContext.FTPK_FTPs.ToList();
    
            return View(sites);
        }

        [HttpGet]
        public ActionResult Browse(int id, string path = "")
        {
            FTPK_FTPs site = dbContext.FTPK_FTPs.Where(x => x.ID == id).FirstOrDefault();
            if (site == null)
            {
                return HttpNotFound();
            }

            SftpClientWrapper sftp = new SftpClientWrapper(site.Host, site.UserName, site.Password);
            AppResponse<List<VMDirectoryItem>> resp = sftp.getDirectoryContents(path);

            List<VMDirectoryItem> items = resp.getData();
            VMBrowse model = new VMBrowse(items);
            return View(model);
        }
    }
}