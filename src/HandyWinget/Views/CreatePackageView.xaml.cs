﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Downloader;
using HandyControl.Tools;
using HandyWinget.Common;
using HandyWinget.Common.Models.Export;
using HandyWinget.Control;
using Microsoft.Win32;
using YamlDotNet.Serialization;
using Path = System.IO.Path;

namespace HandyWinget.Views
{
    /// <summary>
    /// Interaction logic for CreatePackageView.xaml
    /// </summary>
    public partial class CreatePackageView : UserControl, INotifyPropertyChanged
    {
        #region INotify
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private ObservableCollection<Installer> installers = new ObservableCollection<Installer>();
        public ObservableCollection<Installer> Installers
        {
            get { return installers; }
            set
            {
                installers = value;
                RaisePropertyChanged();
            }
        }
        public CreatePackageView()
        {
            InitializeComponent();

            DataContext = this;

            if (Helper.IsWingetInstalled())
            {
                btnValidate.IsEnabled = true;
            }
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (Installers.Count > 1)
            {
                GenerateScript();
            }
            else
            {
                GenerateScript(GenerateScriptMode.SaveToFile);
            }
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (Installers.Count > 1)
            {
                Helper.CreateInfoBar("Not Supported", "You have Multiple installer We do not support this scenario yet.", panel, Severity.Warning);
            }
            else
            {
                GenerateScript(GenerateScriptMode.CopyToClipboard);
            }
        }

        public void ClearInputs()
        {
            txtAppName.Text = string.Empty;
            txtPublisher.Text = string.Empty;
            txtId.Text = string.Empty;
            txtVersion.Text = string.Empty;
            txtDescription.Text = string.Empty;
            txtHomePage.Text = string.Empty;
            txtLicense.Text = string.Empty;
            txtLicenseUrl.Text = string.Empty;
            txtUrl.Text = string.Empty;
            txtHash.Text = string.Empty;
            prgStatus.Value = 0;
            Installers.Clear();
        }

        public void GenerateScript()
        {
            try
            {
                if (!string.IsNullOrEmpty(txtAppName.Text) && !string.IsNullOrEmpty(txtPublisher.Text) &&
                    !string.IsNullOrEmpty(txtId.Text) && !string.IsNullOrEmpty(txtVersion.Text) &&
                    !string.IsNullOrEmpty(txtLicense.Text) && !string.IsNullOrEmpty(txtUrl.Text) && txtUrl.Text.IsUrl())
                {
                    var versionBuilder = new ExportVersionModel
                    {
                        PackageIdentifier = txtId.Text,
                        PackageVersion = txtVersion.Text,
                        PackageName = txtAppName.Text,
                        Publisher = txtPublisher.Text,
                        License = txtLicense.Text,
                        LicenseUrl = txtLicenseUrl.Text,
                        ShortDescription = txtDescription.Text,
                        PackageUrl = txtHomePage.Text,
                        ManifestType = "version",
                        ManifestVersion = "1.0.0",
                        DefaultLocale = "en-US"
                    };

                    var installerBuilder = new ExportInstallerModel
                    {
                        PackageIdentifier = txtId.Text,
                        PackageVersion = txtVersion.Text,
                        ManifestType = "installer",
                        ManifestVersion = "1.0.0",
                        Installers = Installers.ToList()
                    };

                    var versionSerializer = new SerializerBuilder().Build();
                    var installerSerializer = new SerializerBuilder().Build();
                    var versionYaml = versionSerializer.Serialize(versionBuilder);
                    var installerYaml = installerSerializer.Serialize(installerBuilder);

                    var dialog = new SaveFileDialog();
                    dialog.Title = "Save Package";
                    dialog.FileName = $"{txtId.Text}.yaml";
                    dialog.DefaultExt = "yaml";
                    dialog.Filter = "Yaml File (*.yaml)|*.yaml";
                    if (dialog.ShowDialog() == true)
                    {
                        var path = Path.GetDirectoryName(dialog.FileName) + @"\" + txtVersion.Text;

                        Directory.CreateDirectory(path);
                        File.WriteAllText(path + @"\" + txtId.Text + ".yaml", versionYaml);
                        File.WriteAllText(path + @"\" + txtId.Text + ".installer.yaml", installerYaml);
                        ClearInputs();
                    }
                }
                else
                {
                    Helper.CreateInfoBar("Fill Inputs", "Required fields must be filled", panel, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Helper.CreateInfoBar("Error", ex.Message, panel, Severity.Error);
            }
        }
        public void GenerateScript(GenerateScriptMode mode)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtAppName.Text) && !string.IsNullOrEmpty(txtPublisher.Text) &&
                    !string.IsNullOrEmpty(txtId.Text) && !string.IsNullOrEmpty(txtVersion.Text) &&
                    !string.IsNullOrEmpty(txtLicense.Text) && !string.IsNullOrEmpty(txtUrl.Text) && txtUrl.Text.IsUrl())
                {
                    var builder = new YamlPackageModel
                    {
                        PackageIdentifier = txtId.Text,
                        PackageVersion = txtVersion.Text,
                        PackageName = txtAppName.Text,
                        Publisher = txtPublisher.Text,
                        License = txtLicense.Text,
                        LicenseUrl = txtLicenseUrl.Text,
                        ShortDescription = txtDescription.Text,
                        PackageUrl = txtHomePage.Text,
                        ManifestType = "singleton",
                        ManifestVersion = "1.0.0",
                        PackageLocale = "en-US",
                        Installers = Installers.ToList()
                    };

                    var serializer = new SerializerBuilder().Build();
                    var yaml = serializer.Serialize(builder);
                    switch (mode)
                    {
                        case GenerateScriptMode.CopyToClipboard:
                            Clipboard.SetText(yaml);
                            Helper.CreateInfoBar("Script Copied", "Script Copied to clipboard.", panel, Severity.Success);
                            ClearInputs();
                            break;
                        case GenerateScriptMode.SaveToFile:
                            var dialog = new SaveFileDialog();
                            dialog.Title = "Save Package";
                            dialog.FileName = $"{txtId.Text}.yaml";
                            dialog.DefaultExt = "yaml";
                            dialog.Filter = "Yaml File (*.yaml)|*.yaml";
                            if (dialog.ShowDialog() == true)
                            {
                                File.WriteAllText(dialog.FileName, yaml);
                                ClearInputs();
                            }

                            break;
                    }
                }
                else
                {
                    Helper.CreateInfoBar("Fill Inputs", "Required fields must be filled", panel, Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Helper.CreateInfoBar("Error", ex.Message, panel, Severity.Error);
            }
        }

        private async void btnGetHashWeb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(txtUrl.Text) && txtUrl.Text.IsUrl())
                {
                    prgStatus.IsIndeterminate = false;
                    btnGetHashWeb.IsEnabled = false;
                    btnGetHashLocal.IsEnabled = false;
                    txtHash.IsEnabled = false;
                    try
                    {
                        var downloader = new DownloadService();
                        downloader.DownloadProgressChanged += OnDownloadProgressChanged;
                        downloader.DownloadFileCompleted += OnDownloadFileCompleted;
                        await downloader.DownloadFileTaskAsync(txtUrl.Text, new DirectoryInfo(Consts.TempPath));
                    }
                    catch (Exception ex)
                    {
                        prgStatus.IsIndeterminate = true;
                        prgStatus.ShowError = true;
                        Helper.CreateInfoBar("Error", ex.Message, panel, Severity.Error);
                    }
                }
                else
                {
                    Helper.CreateInfoBar("Invalid", "Url field is Empty or Invalid", panel, Severity.Error);

                }
            }
            catch (Exception ex)
            {
                Helper.CreateInfoBar("Error", ex.Message, panel, Severity.Error);
            }
        }

        private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            DispatcherHelper.RunOnMainThread(() =>
            {
                string fileName = ((DownloadPackage) e.UserState).FileName;
                prgStatus.Value = 0;
                txtHash.Text = CryptographyHelper.GenerateSHA256FromFile(fileName);
                btnGetHashWeb.IsEnabled = true;
                btnGetHashLocal.IsEnabled = true;
                txtHash.IsEnabled = true;

                File.Delete(fileName);
            });
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DispatcherHelper.RunOnMainThread(() =>
            {
                prgStatus.Value = (int) e.ProgressPercentage;
            });
        }

        private void btnGetHashLocal_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Open Setup File";
            dialog.Filter = "All Files (*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                txtHash.Text = CryptographyHelper.GenerateSHA256FromFile(dialog.FileName);
            }
        }

        private void btnValidate_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "Open Yaml File";
            dialog.Filter = "Yaml File (*.yaml)|*.yaml";
            if (dialog.ShowDialog() == true)
            {
                string command = $"/K winget validate {dialog.FileName}";
                Process.Start("cmd.exe", command);
            }
        }

        private void txtPublisher_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtId.Text = $"{txtAppName.Text}.{txtPublisher.Text}";
        }

        private void btnAddInstaller_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtUrl.Text) || !string.IsNullOrEmpty(txtHash.Text))
            {
                var arch = (cmbArchitecture.SelectedItem as ComboBoxItem).Content.ToString();
                var item = new Installer
                {
                    Architecture = arch,
                    InstallerUrl = txtUrl.Text,
                    InstallerSha256 = txtHash.Text
                };

                if (!Installers.Contains(item, new GenericCompare<Installer>(x => x.Architecture)))
                {
                    Installers.Add(item);
                }
                else
                {
                    Helper.CreateInfoBar("Error", $"{arch} Architecture already exist.", panel, Severity.Error);
                }
            }
            else
            {
                Helper.CreateInfoBar("Error", "Installer Url and Installer Sha256 must be filled", panel, Severity.Error);
            }
        }

        private void btnRemoveInstaller_Click(object sender, RoutedEventArgs e)
        {
            var item = lstInstaller.SelectedItem as Installer;
            if (item != null)
            {
                Installers.Remove(item);
            }
            else
            {
                Helper.CreateInfoBar("Error", "Please Select Installer from list", panel, Severity.Error);
            }
        }
    }
}
