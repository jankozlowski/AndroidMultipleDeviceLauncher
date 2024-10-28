using AndroidMultipleDeviceLauncher.Models;
using AndroidMultipleDeviceLauncher.Services;
using EnvDTE;
using EnvDTE80;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Logging;
using Microsoft.VisualStudio.RpcContracts.Build;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Xml;
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
        public const int CommandId2 = 0x0300;
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

            var menuCommandID2 = new CommandID(CommandSet, CommandId2);
            var menuItem2 = new MenuCommand(this.Execute, menuCommandID2);
            commandService.AddCommand(menuItem2);
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

        private Adb adb = new Adb();
        private Avd avd = new Avd();
        private Settings settings = new Settings();

        private void Execute(object sender, EventArgs e)
        {
            List<Device> selectedDevices = SelectedDevicesSingelton.GetInstance().SelectedDevices;

            if (selectedDevices == null || selectedDevices.Count == 0)
            {
                string message = "No device selected";
                System.Windows.MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, message), "No device", MessageBoxButton.OK);

                return;
            }

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            using (var loadingDialog = new LoadingDialog(cancellationTokenSource))
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        RunDevices(selectedDevices);

                        if (cancellationTokenSource.IsCancellationRequested)
                            return;

                        UpdateSingeltoneAdbNames();

                        EnvDTE.Project project = GetStartupProject();

                        if (project == null)
                        {
                            string message = $"No startup project found, set android project as startup project";
                            MessageBoxResult result = System.Windows.MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, message), "No startup project found", MessageBoxButton.OK);
                            loadingDialog.Dispose();
                            return;
                        }

                        loadingDialog.SetMessage("Waiting for all devices to boot");
                        CheckIfAllDevicesBooted(cancellationTokenSource);

                        if (cancellationTokenSource.IsCancellationRequested)
                            return;

                        loadingDialog.SetMessage("Building project");
                        BuildSolution(cancellationTokenSource);

                        if (cancellationTokenSource.IsCancellationRequested)
                            return;

                        loadingDialog.SetMessage("Geting project data");
                        ProjectConfiguration configuration = GetProjectConfiguration(project);

                        if (cancellationTokenSource.IsCancellationRequested)
                            return;

                        loadingDialog.SetMessage("Installing app on devices");

                        if (string.IsNullOrEmpty(configuration.FullOutputPath) || !File.Exists(configuration.FullOutputPath))
                        {
                            string message = $"No apk file found in path {configuration.FullOutputPath}, build project to create apk file";
                            MessageBoxResult result = System.Windows.MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, message), "No apk found", MessageBoxButton.OK);
                            loadingDialog.Dispose();
                            return;
                        }

                        InstallAppOnDevices(configuration.FullOutputPath);

                        if (cancellationTokenSource.IsCancellationRequested)
                            return;

                        loadingDialog.SetMessage("Geting start activity");
                        configuration.ActivityName = GetActivityName(configuration.PackageName);

                        if (cancellationTokenSource.IsCancellationRequested)
                            return;

                        loadingDialog.SetMessage("Starting app on devices");
                        RunAppOnDevices(configuration.PackageName, configuration.ActivityName);

                        if (cancellationTokenSource.IsCancellationRequested)
                            return;

                        loadingDialog.Dispose();
                        return;
                    }
                    catch (CancelationException ex)
                    {
                        loadingDialog.Dispose();
                        return;
                    }
                    catch (Exception ex)
                    {
                        loadingDialog.Dispose();
                        System.Windows.MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, ex.Message), "Error", MessageBoxButton.OK);
                        return;
                    }

                });

                loadingDialog.SetMessage("Running selected devices");
                loadingDialog.ShowDialog();
            }

        }

        private void UpdateSingeltoneAdbNames()
        {
            List<Device> selectedDevices = SelectedDevicesSingelton.GetInstance().SelectedDevices;
            List<Device> connectedDevices = adb.GetConnectedDevices();
            foreach (Device device in connectedDevices)
            {
                var selectedDevice = selectedDevices.Where(d => d.AvdName.Equals(device.AvdName)).FirstOrDefault();
                if (selectedDevice != null)
                {
                    var adbName = adb.AdbCommandWithResult($"-s {device.Id} emu avd name");
                    var result = SplitByLine(adbName);
                    selectedDevice.AdbName = result[0].Trim();
                }
            }
        }

        private void RunDevices(List<Device> selectedDevices)
        {
            List<Device> runingDevices = adb.GetConnectedDevices();
            List<Device> allAvds = avd.GetAvdEmulators();

            foreach (var device in selectedDevices)
            {
                if (device.IsEmulator)
                {
                    if (runingDevices.FirstOrDefault(d => d.AdbName.Equals(device.AdbName)) != null)
                        continue;

                    if (allAvds.FirstOrDefault(d => d.AvdName.Equals(device.AvdName)) != null)
                    {
                        avd.AvdCommand($"-avd {device.AvdName}"); //run avd
                        System.Threading.Thread.Sleep(4000);
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

            System.Threading.Thread.Sleep(1000);
        }

        private void CheckIfAllDevicesBooted(CancellationTokenSource cancellationTokenSource)
        {
            bool AllBooted = false;
            List<Device> devices = adb.GetConnectedSelectedDevices();

            while (!AllBooted)
            {
                List<bool> results = new List<bool>();

                foreach (Device device in devices)
                {
                    string bootStatus = adb.AdbCommandWithResult($"-s {device.Id} shell getprop sys.boot_completed");
                    bool booted = bootStatus.Contains("1") ? true : false;
                    results.Add(booted);
                }

                AllBooted = results.All(item => item);
                System.Threading.Thread.Sleep(1000);

                if (cancellationTokenSource.IsCancellationRequested)
                    throw new CancelationException();
            }
        }

        private void InstallAppOnDevices(string apkPath)
        {
            List<Device> devices = adb.GetConnectedSelectedDevices();
            foreach (Device device in devices)
            {
                adb.AdbCommandWithResult($@"-s {device.Id} install {apkPath}");
            }
        }

        private void RunAppOnDevices(string packageName, string activityName)
        {
            List<Device> devices = adb.GetConnectedSelectedDevices();
            foreach (Device device in devices)
            {
                adb.AdbCommand($"-s {device.Id}  shell am start -n {packageName}{activityName}");
            }
        }

        private ProjectConfiguration GetProjectConfiguration(EnvDTE.Project project)
        {
            ConfigurationManager configManager = project.ConfigurationManager;
            Configuration activeConfig = configManager.ActiveConfiguration;

            string configuration = activeConfig.ConfigurationName;
            string platform = activeConfig.PlatformName;
            string outputPath = activeConfig.Properties.Item("OutputPath").Value.ToString();

            string projectPath = project.FullName.Substring(0, project.FullName.LastIndexOf("\\"));
            string buildPath = Path.Combine(projectPath, outputPath);

            var files = Directory.GetFiles(buildPath, "*.apk").ToList();
            string fullPath = files.Where(s => s.ToLower().Contains("signed")).FirstOrDefault();

            var csproj = Directory.GetFiles(projectPath, "*.csproj").First();
            string androidId = GetPackageName(csproj);

            ProjectConfiguration projectConfiguration = new ProjectConfiguration()
            {
                Configuration = configuration,
                Platform = platform,
                OutputPath = outputPath,
                FullOutputPath = fullPath,
                PackageName = androidId
            };

            return projectConfiguration;
        }

        private string GetActivityName(string packageName)
        {
            List<Device> devices = adb.GetConnectedSelectedDevices();

            string adbResult = adb.AdbCommandWithResult($"-s {devices.First().Id} shell cmd package resolve-activity --brief {packageName}");
            string[] lineResult = SplitByLine(adbResult);
            string activityName = lineResult[1].Substring(lineResult[1].LastIndexOf("/"));

            return activityName;
        }

        private string[] SplitByLine(string text)
        {
            string[] lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            return lines;
        }

        private string GetPackageName(string csProjectPath)
        {
            if (string.IsNullOrEmpty(csProjectPath))
                return string.Empty;

            XmlDocument doc = new XmlDocument();
            doc.Load(csProjectPath);

            string result = string.Empty;
            string appId = string.Empty;
            string assemblyName = string.Empty;

            var ApplicationIdTag = doc.GetElementsByTagName("ApplicationId");
            if (ApplicationIdTag.Count > 0)
            {
                appId = ApplicationIdTag[0].InnerText;
            }

            var AssrmblyNameTag = doc.GetElementsByTagName("AssemblyName");
            if (AssrmblyNameTag.Count > 0)
            {
                assemblyName = AssrmblyNameTag[0].InnerText;
            }

            result = string.IsNullOrEmpty(appId) ? assemblyName : appId;

            return result;
        }

        private bool BuildSolution(CancellationTokenSource cancellationTokenSource)
        {
            string buildCheckBoxString = settings.GetSetting(Settings.SettingsName, "buildSolution");
            bool buildCheckBoxBool = false;
            bool.TryParse(buildCheckBoxString, out buildCheckBoxBool);
            if (!buildCheckBoxBool)
                return true;

            string projectPath = GetStartupProjectPath();

            if (string.IsNullOrEmpty(projectPath))
                return false;

            BuildAction(projectPath, "Restore");

            CleanSolution(projectPath, cancellationTokenSource);

            var buildResult = BuildAction(projectPath, "Build");

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                System.Threading.Thread.Sleep(500);
                if (buildResult.OverallResult == BuildResultCode.Success)
                    return true;

                if (buildResult.OverallResult == BuildResultCode.Failure)
                {
                    System.Windows.MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Build failed"), "Error", MessageBoxButton.OK);
                    return false;
                }
            }

            if (cancellationTokenSource.IsCancellationRequested)
                throw new CancelationException();

            return true;
        }

        private void CleanSolution(string projectPath, CancellationTokenSource cancellationTokenSource)
        {
            var buildResult = BuildAction(projectPath, "Clean");

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                System.Threading.Thread.Sleep(500);
                if (buildResult.OverallResult == BuildResultCode.Success)
                    return;

                if (buildResult.OverallResult == BuildResultCode.Failure)
                    return;
            }

            if (cancellationTokenSource.IsCancellationRequested)
                throw new CancelationException();
        }

        private Microsoft.Build.Execution.BuildResult BuildAction(string projectPath, string buildAction)
        {
            var projectCollection = ProjectCollection.GlobalProjectCollection;
            var existingProject = projectCollection.LoadedProjects.FirstOrDefault(p => p.FullPath.Equals(projectPath, StringComparison.OrdinalIgnoreCase));

            if (existingProject != null)
            {
                projectCollection.UnloadProject(existingProject);
            }

            Microsoft.Build.Evaluation.Project project = new Microsoft.Build.Evaluation.Project(projectPath);
            var globalProperties = project.GlobalProperties;

            var buildRequest = new BuildRequestData(projectPath, globalProperties, null, new[] { buildAction }, null);
            var buildParameters = new BuildParameters(project.ProjectCollection);
            /*  string logPath = projectPath.Substring(0, projectPath.LastIndexOf("\\"));
              FileLogger fl = new FileLogger() { Parameters = $@"logfile={logPath}\log.txt" };
              buildParameters.Loggers = new List<Microsoft.Build.Framework.ILogger> { fl }.AsEnumerable();*/
            return BuildManager.DefaultBuildManager.Build(buildParameters, buildRequest);
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

        private class ProjectConfiguration
        {
            public string Configuration { get; set; }

            public string Platform { get; set; }

            public string OutputPath { get; set; }

            public string FullOutputPath { get; set; }

            public string PackageName { get; set; }

            public string ActivityName { get; set; }
        }

    }

}
