using System;
using System.IO;
using System.Runtime.InteropServices;

namespace StackSharing.Lib.Utilities
{
    public static class ShortcutManager
    {
        private static string ShortcutLocation => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SendTo), "Stack.lnk");

        public static bool Exists()
        {
            return File.Exists(ShortcutLocation);
        }

        public static void Create(string executablePath)
        {
            // if (Exists()) return;

            // http://stackoverflow.com/questions/234231/creating-application-shortcut-in-a-directory
            Type t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); //Windows Script Host Shell Object
            dynamic shell = Activator.CreateInstance(t);
            
            try
            {
                var lnk = shell.CreateShortcut(ShortcutLocation);
                try
                {
                    lnk.TargetPath = executablePath;
                    //lnk.IconLocation = "shell32.dll, 122";
                    lnk.IconLocation = executablePath + ", 0";
                    lnk.Save();
                }
                finally
                {
                    Marshal.FinalReleaseComObject(lnk);
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }

        public static void Delete()
        {
            if (Exists())
                File.Delete(ShortcutLocation);
        }
    }
}
