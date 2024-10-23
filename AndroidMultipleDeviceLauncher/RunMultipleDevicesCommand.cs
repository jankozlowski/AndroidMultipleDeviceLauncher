using AndroidMultipleDeviceLauncher.Models;
using AndroidMultipleDeviceLauncher.Services;
using EnvDTE;
using EnvDTE80;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Locator;
using Microsoft.Build.Logging;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Threading;
using VSLangProj;
using Task = System.Threading.Tasks.Task;

namespace AndroidMultipleDeviceLauncher
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class RunMultipleDevicesCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("7e48a3b2-e661-464b-9ac0-b56d87191a71");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunMultipleDevicesCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private RunMultipleDevicesCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static RunMultipleDevicesCommand Instance
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in RunMultipleDevicesCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new RunMultipleDevicesCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>

        Adb adb = new Adb();
        Avd avd = new Avd();

        private void Execute(object sender, EventArgs e)
        {
            List<Device> selectedDevices = SelectedDevicesSingelton.GetInstance().SelectedDevices;

            if (selectedDevices == null || selectedDevices.Count == 0)
            {
                string message = "No device selected";
                System.Windows.MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, message), "No device", MessageBoxButton.OK);

                return;
            }

            //add loading

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                List<Device> runingDevices = adb.GetConnectedDevices();
                List<Device> allAvds = avd.GetAvdEmulators();

                foreach (var device in selectedDevices)
                {
                    if (device.IsEmulator)
                    {
                        if (runingDevices.FirstOrDefault(d => d.Name.Equals(device.Name)) != null)
                            continue;

                        if (allAvds.FirstOrDefault(d => d.Name.Equals(device.Name)) != null)
                        {
                            avd.AvdCommand($"-avd {device.Name}"); //run avd
                        }
                        else
                        {
                            SelectedDevicesSingelton.GetInstance().SelectedDevices.Remove(device);
                        }
                    }

                    if (!device.IsEmulator)
                    {
                        if (runingDevices.FirstOrDefault(d => d.Id == device.Id) != null)
                            continue;

                        SelectedDevicesSingelton.GetInstance().SelectedDevices.Remove(device);
                    }
                }

                BuildSolution();

                EnvDTE.Project project = GetStartupProject();

                if (project == null)
                {
                    string message = $"No startup project found, set android project as startup project";
                    MessageBoxResult result = System.Windows.MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, message), "No startup project found", MessageBoxButton.OK);
                    return;
                }

                ProjectConfiguration configuration = GetProjectConfiguration(project);

                CheckIfAllDevicesBooted();
                InstallAppOnDevices(configuration.FullOutputPath);
                // RunAppOnDevices();
                //test on two themes
            });

        }

        private void CheckIfAllDevicesBooted()
        {
            bool AllBooted = true;
            List<Device> devices = adb.GetConnectedDevices();

            while (!AllBooted)
            {
                List<bool> results = new List<bool>();

                foreach (Device device in devices)
                {
                    string bootStatus = adb.AdbCommandWithResult($"adb -s {device.Id} shell getprop sys.boot_completed");
                    bool booted = bootStatus.Equals("1") ? true : false;
                    results.Add(booted);
                }

                AllBooted = results.All(item => item);
                System.Threading.Thread.Sleep(1000);
            }
        }

        private void InstallAppOnDevices(string apkPath)
        {
            List<Device> devices = adb.GetConnectedDevices();
            foreach (Device device in devices)
            {
                adb.AdbCommand($"adb -s {device.Id} install {apkPath}");
            }
        }

        private void RunAppOnDevices()
        {
            List<Device> devices = adb.GetConnectedDevices();
            foreach (Device device in devices)
            {
                adb.AdbCommand($"adb -s {device.Id}  shell am start -n < package_name >/< activity_name ");
            }
        }

        private ProjectConfiguration GetProjectConfiguration(EnvDTE.Project project)
        {
            ConfigurationManager configManager = project.ConfigurationManager;
            Configuration activeConfig = configManager.ActiveConfiguration;

            string configuration = activeConfig.ConfigurationName;
            string platform = activeConfig.PlatformName;
            string outputPath = activeConfig.Properties.Item("OutputPath").Value.ToString();
            string packageName = project.Properties.Item("PackageName").Value.ToString();

            string projectPath = project.FullName.Substring(0, project.FullName.LastIndexOf("\\"));
            string buildPath = Path.Combine(projectPath, outputPath);

            var files = Directory.GetFiles(buildPath, "*.apk").OrderBy(s => s).ToList();
            string fullPath = files.First();

            ProjectConfiguration projectConfiguration = new ProjectConfiguration()
            {
                Configuration = configuration,
                Platform = platform,
                OutputPath = outputPath,
                FullOutputPath = fullPath,
                //package name
                //activity name
            };

            return projectConfiguration;
        }

        private class ProjectConfiguration
        {
            public string Configuration { get; set; }
            public string Platform { get; set; }

            public string OutputPath { get; set; }

            public string FullOutputPath { get; set; }

        }

        private bool BuildSolution()
        {
            string projectPath = GetCurrentSolution();

            if (string.IsNullOrEmpty(projectPath))
                return false;

            var projectCollection = new ProjectCollection();
            var globalProperties = projectCollection.GlobalProperties;

            FileLogger fl = new FileLogger() { Parameters = @"logfile=C:\logs\log.txt" };

            var buildRequest = new BuildRequestData(projectPath, globalProperties, null, new[] { "Build" }, null);
            var buildParameters = new BuildParameters(projectCollection);
            buildParameters.Loggers = new List<Microsoft.Build.Framework.ILogger> { fl }.AsEnumerable();

            var buildResult = Microsoft.Build.Execution.BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);

            if (buildResult.OverallResult == BuildResultCode.Success)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private string GetCurrentSolution()
        {
            IVsSolution solution = (IVsSolution)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(IVsSolution));
            solution.GetSolutionInfo(out string solutionDirectory, out string solutionName, out string solutionDirectory2);

            return solutionName;
        }

        private EnvDTE.Project GetStartupProject()
        {
            string startupProjectPath = GetStartupProjectPath();
            if (string.IsNullOrEmpty(startupProjectPath))
                return null;

            DTE2 dte = (DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE));


            EnvDTE.Solution solution = dte.Solution;

            EnvDTE.Project targetProject = null;
            foreach (EnvDTE.Project project in solution.Projects)
            {
                if (string.Equals(project.FullName, startupProjectPath, StringComparison.OrdinalIgnoreCase))
                {
                    targetProject = project;
                    break;
                }
            }

            return targetProject;
        }

        private string GetStartupProjectPath()
        {
            DTE2 dte = (DTE2)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE));

            SolutionBuild sb = dte.Solution.SolutionBuild;
            string startupProjectPath = string.Empty;
            foreach (String s in (Array)sb.StartupProjects)
            {
                startupProjectPath = s;
            }
            return Path.Combine(Environment.CurrentDirectory, startupProjectPath);
        }
    

        private void CleanAndRunDevices()
        {

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




    }

}
