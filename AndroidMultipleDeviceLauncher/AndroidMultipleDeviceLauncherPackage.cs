﻿using AndroidMultipleDeviceLauncher.Services;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Sentry;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Task = System.Threading.Tasks.Task;

namespace AndroidMultipleDeviceLauncher
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(AndroidMultipleDeviceLauncherPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class AndroidMultipleDeviceLauncherPackage : AsyncPackage
    {
        /// <summary>
        /// AndroidMultipleDeviceLauncherPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "2c1e8968-b9db-4335-bc7b-13be80d47c19";

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await RunMultipleDevicesCommand.InitializeAsync(this);
            await RunMultipleDevicesSettingsCommand.InitializeAsync(this);

            Settings settings = new Settings();

            string adbPath = settings.GetSetting(Settings.SettingsName, "adbPath");
            string avdPath = settings.GetSetting(Settings.SettingsName, "avdPath");

            Adb.AdbPath = string.IsNullOrEmpty(adbPath) ? @"C:\Program Files (x86)\Android\android-sdk\platform-tools\" : adbPath;
            Avd.AvdPath = string.IsNullOrEmpty(avdPath) ? @"C:\Program Files (x86)\Android\android-sdk\emulator\" : avdPath;

            SentrySdk.Init(o =>
            {
                o.Dsn = "https://f75b9ebc93000f010dec9a3ad92a5c91@o4508195271147520.ingest.de.sentry.io/4508195273375824";
                o.Debug = false;
            });
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
        }

        private void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = e.ExceptionObject as Exception;
            SentrySdk.CaptureException(exception);
            PromptToSaveUnsavedFiles();
        }

        public void PromptToSaveUnsavedFiles()
        {
            DTE2 dte = (DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE));

            Documents documents = dte.Documents;

            foreach (Document doc in documents)
            {
                if (doc.Saved == false)
                {
                    string message = $"The document '{doc.Name}' has unsaved changes. Do you want to save it?";
                    MessageBoxResult result = System.Windows.MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, message), "Save Changes", MessageBoxButton.YesNoCancel);

                    if (result == MessageBoxResult.Yes)
                    {
                        doc.Save();
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }
            }
        }

        #endregion
    }
}
