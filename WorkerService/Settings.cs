using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace WorkerService
{
    public class Settings
    {
        public string RootPath { get; set; }
        public string GarbagePath { get; set; }
        public string[] WatchingFilter { get; set; }
        public string UsersSubDir { get; set; } = "Scan";
        public string UsersDN { get; set; }
        public string FolderNameMapAttr { get; set; } = "sAMAccountName";
        public string UsersHomeDirAttr { get; set; } = "homeDirectory";
        public string LogPath { get; set; }
        public bool UseGarbage { get; set; } = true;


        public bool Valid()
        {
            var result = true;

            if (!PathAvailable(RootPath))
                result = false;

            if (UseGarbage)
                if(!PathAvailable(GarbagePath))
                    result = false;

            if (!PathAvailable(LogPath))
                result = false;

            return result;
        }

        private bool PathAvailable(string path)
        {
            var probe = $"{path}\\probe.tmp";
            if(!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
            //try
            //{
            //    using (FileStream fs = new FileStream(probe, FileMode.OpenOrCreate))
            //    {
            //        File.Delete(probe);
            //        return fs.CanWrite;
            //    }
            //}
            //catch (Exception)
            //{
            //    return false;
            //}
        }

    }
}
