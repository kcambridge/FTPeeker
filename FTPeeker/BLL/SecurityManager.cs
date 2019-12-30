using FTPeeker.Models.Entities;
using FTPeeker.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace FTPeeker.BLL
{
    public static class SecurityManager
    {
        public static bool canRead(int FTPID)
        {
            string[] validPermissions = { VMSFTPPermission.READ, VMSFTPPermission.ADMIN};
            return hasAccess(FTPID, validPermissions);
        }

        public static bool canUpload(int FTPID)
        {
            
            string[] validPermissions = { VMSFTPPermission.UPLOAD, VMSFTPPermission.ADMIN };
            return hasAccess(FTPID, validPermissions);
        }

        public static bool canDelete(int FTPID)
        {

            string[] validPermissions = { VMSFTPPermission.DELETE, VMSFTPPermission.ADMIN };
            return hasAccess(FTPID, validPermissions);
        }

        public static bool canEdit(int FTPID)
        {

            string[] validPermissions = { VMSFTPPermission.EDIT, VMSFTPPermission.ADMIN };
            return hasAccess(FTPID, validPermissions);
        }

        private static bool hasAccess(int FTPID, string[] validPermissions)
        {
            bool hasAccess = false;
            using (FTPeekerEntities dbContext = new FTPeekerEntities())
            {
                string userID = getUserID();
                var permissions = dbContext.FTPK_User_Permissions.Where(x => x.UserID == userID).ToList();

                if (permissions.Where(x => x.PermissionCode == VMSFTPPermission.ADMIN).Count() > 0)
                {
                    hasAccess =  true;
                }
                else if (permissions.Where(x => x.FTPK_FTPs.Any(y => y.ID == FTPID) && validPermissions.Contains(x.PermissionCode)).Count() > 0)
                {
                    hasAccess =  true;
                }
            }
            return hasAccess;
        }

        

        public static bool isAdmin()
        {
            bool isAdmin = false;
            using (FTPeekerEntities dbContext = new FTPeekerEntities())
            {
                string userID = getUserID();
                if (dbContext.FTPK_User_Permissions.Where(x => x.UserID == userID && x.PermissionCode == VMSFTPPermission.ADMIN).Count() > 0)
                {
                    isAdmin = true;
                }
            }
            return isAdmin;
        }

        public static string getUserID()
        {

            string[] userIDParts = HttpContext.Current.User.Identity.Name.Split('\\');
            if (userIDParts.Length >= 2)
            {
                return HttpContext.Current.User.Identity.Name.Split('\\')[1];
            }
            else
            {
                return HttpContext.Current.User.Identity.Name;
            }
            
        }

        public static VMSFTPPermission getPermissions(int FTPID)
        {
            VMSFTPPermission perm = new VMSFTPPermission();
            perm.canRead = canRead(FTPID);
            perm.canUpload = canUpload(FTPID);
            perm.canEdit = canEdit(FTPID);
            return perm;
        }


    }
}