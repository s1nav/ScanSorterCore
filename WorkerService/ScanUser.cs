using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text;
using WorkerService.Enumerators;

namespace WorkerService
{
    class ScanUser
    {
        public string HomeDirectory { get; }
        public string UserName { get; }

        private readonly Settings _settings;

        public ScanUser(string userName, Settings settings)
        {
            _settings = settings;
            UserName = userName;
            var lDAPSearchURL = $"LDAP://{_settings.UsersDN}";
            var filterString = $"(&(objectCategory=person)(objectClass=user)({_settings.FolderNameMapAttr}={UserName}))";
            try
            {
                using (DirectoryEntry domain = new DirectoryEntry(lDAPSearchURL))
                {
                    using (DirectorySearcher searcher = new DirectorySearcher(domain, filterString))
                    {
                        HomeDirectory = searcher.FindOne().Properties[_settings.UsersHomeDirAttr][0].ToString();
                    }
                }
            }
            catch (Exception)
            {

            }
            //HomeDirectory = "E:\\CodingTestField\\IvanovII";
        }

    }
}
