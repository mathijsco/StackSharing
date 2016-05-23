using StackSharing.WinApp.UI;
using System;
using System.Windows.Forms;

namespace StackSharing.WinApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(params string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var config = AssemblyConfig.Load();

            bool doConfig = args.Length == 0,
                doUpload = args.Length > 0;

            if (doUpload && config.IsDefault)
            {
                if (MessageBox.Show("You have not configured your stack account yet.\r\nPlease do so before uploading.", "Warning", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                    return;
                doConfig = true;
            }

            if (doConfig && new ConfigurationForm(config).ShowDialog() != DialogResult.OK)
                return;

            if (doUpload)
                Application.Run(new UploadForm(config, args));
        }
    }
}
