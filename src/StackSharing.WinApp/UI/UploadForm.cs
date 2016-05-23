using StackSharing.Lib;
using StackSharing.Lib.Models;
using StackSharing.Lib.Utilities;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using StackSharing.WinApp.Properties;

namespace StackSharing.WinApp.UI
{
    public partial class UploadForm : Form
    {
        private readonly string[] _paths;
        private readonly StackClient _client;
        private readonly CancellationTokenSource _cancelationTokenSource;
        private SharedOnlineItem _sharedOnlineItem;
        private bool _isUploading = true;
        private bool _propertiesApplied = false;
        private bool _okPressed = false;
        private readonly string _friendlyShareName;


        public UploadForm(AssemblyConfig config, string[] paths)
        {
            InitializeComponent();
            this.Icon = Resources.icon;

            _friendlyShareName = paths.Length > 1 ? Path.GetFileName(Path.GetDirectoryName(paths[0])) : Path.GetFileName(paths[0]);
            this.Text = string.Format(this.Text, _friendlyShareName);
            _cancelationTokenSource = new CancellationTokenSource();
            _client = new StackClient(config, _cancelationTokenSource.Token);
            _paths = paths;

            this.Load += (sender, e) => StartWork();
        }


        private async Task StartWork()
        {
            progressBar.Style = ProgressBarStyle.Marquee;

            try
            {
                lblTitle.Text = "Creating share...";
                var friendlyName = _friendlyShareName.Length > 20 ? _friendlyShareName.Substring(0, 20) : _friendlyShareName;
                var rootFolder = await _client.CreateFolder(string.Format("Share/{0:yyyyMMddhhmmss}-{1}", DateTime.UtcNow, friendlyName), null);

                var format = "Uploading file {0}/{1}...";
                lblTitle.Text = string.Format(format, 0, _paths.Length);
                progressBar.Style = ProgressBarStyle.Blocks;
                var status = _client.UploadFiles(rootFolder, _paths);
                while (!status.Task.IsFaulted && !status.Task.IsCanceled && !status.Task.IsCompleted)
                {
                    lblTitle.Text = string.Format(format, status.CurrentFileNo, status.TotalFiles);
                    progressBar.Value = (int) (status.FileProgress*100);
                    await Task.Delay(100);
                }
                // Task is canceled or something, so close the UI.
                if (!status.Task.IsCompleted)
                {
                    this.Close();
                    return;
                }

                if (status.Task.IsFaulted)
                {
                    lblTitle.Text = @"Error";
                    txtShareUrl.Text = status.Task.Exception.InnerException.Message;
                    return;
                }

                var onlineItem = status.Result;
                progressBar.Style = ProgressBarStyle.Marquee;

                lblTitle.Text = "Creating share...";
                _sharedOnlineItem = await _client.ShareItem(onlineItem);
                txtShareUrl.Text = _sharedOnlineItem.ShareUrl.ToString();

                lblTitle.Text = "Upload completed";

                if (_okPressed)
                    await SetShareProperties();
            }
            catch (TaskCanceledException)
            {
                this.Close();
            }
            catch (Exception ex)
            {
                lblTitle.Text = @"Error";
                txtShareUrl.Text = ex.Message;
            }
            finally
            {
                _isUploading = false;
                progressBar.Style = ProgressBarStyle.Blocks;
                progressBar.Value = 100;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _cancelationTokenSource.Cancel();

            if (!_isUploading)
                this.Close();
        }

        private async void btnOk_Click(object sender, EventArgs e)
        {
            if (txtPassword.Text != txtPassword2.Text)
            {
                MessageBox.Show("The two spefied passwords do not match.\r\nPlease try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.Text = null;
                txtPassword2.Text = null;
                txtPassword.Focus();
                return;
            }

            _okPressed = true;
            txtPassword.Enabled = false;
            txtPassword2.Enabled = false;
            btnRandomPassword.Enabled = false;
            chkExpire.Enabled = false;
            btnOk.Enabled = false;

            if (_isUploading) return;

            if (!_propertiesApplied)
                await SetShareProperties();

            this.Close();
        }

        private void btnRandomPassword_Click(object sender, EventArgs e)
        {
            txtPassword.Text = PasswordGenerator.Generate();
            txtPassword.UseSystemPasswordChar = false;
            txtPassword2.Enabled = false;
            txtPassword2.Text = txtPassword.Text;
        }

        private async Task SetShareProperties()
        {
            progressBar.Style = ProgressBarStyle.Marquee;
            try
            {
                if (chkExpire.Checked)
                {
                    lblTitle.Text = "Setting expiry...";
                    await _client.SetExpiryDate(_sharedOnlineItem, DateTime.Now.AddDays(14));
                }

                if (!string.IsNullOrEmpty(txtPassword.Text))
                {
                    lblTitle.Text = "Setting password...";
                    await _client.SetPassword(_sharedOnlineItem, txtPassword.Text);
                }
            }
            catch (TaskCanceledException)
            {
                this.Close();
            }
            catch (Exception ex)
            {
                lblTitle.Text = @"Error";
                txtShareUrl.Text = ex.Message;
            }
            finally
            {
                lblTitle.Text = @"Finished";
                progressBar.Style = ProgressBarStyle.Blocks;
                btnOk.Enabled = true;
                btnCancel.Enabled = false;
                _propertiesApplied = true;
            }
        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {
            if (!txtPassword.UseSystemPasswordChar)
            {
                txtPassword.UseSystemPasswordChar = true;
                txtPassword2.Enabled = true;
                txtPassword2.Text = null;
            }
        }
    }
}
