﻿using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VSLangProj;
using Task = System.Threading.Tasks.Task;

namespace ObfuscarTools
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class RemoveObfuscarCommand
	{
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 4129;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("d98d245c-3edb-4a90-8637-fb2bb92e99e1");

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		private readonly AsyncPackage package;

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoveObfuscarCommand"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		/// <param name="commandService">Command service to add command to, not null.</param>
		private RemoveObfuscarCommand(AsyncPackage package, OleMenuCommandService commandService)
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
		public static RemoveObfuscarCommand Instance
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
			// Switch to the main thread - the call to AddCommand in RemoveObfuscarCommand's constructor requires
			// the UI thread.
			await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

			OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
			Instance = new RemoveObfuscarCommand(package, commandService);
		}

		/// <summary>
		/// This function is the callback used to execute the command when the menu item is clicked.
		/// See the constructor to see how the menu item is associated with this function using
		/// OleMenuCommandService service and MenuCommand class.
		/// </summary>
		/// <param name="sender">Event sender.</param>
		/// <param name="e">Event args.</param>
		private void Execute(object sender, EventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			ObfuscarToolsPackage pkg = this.package as ObfuscarToolsPackage;

			var project = SolutionManager.GetActiveProject();
			var projectName = project.Name;
			var vsp = project.Object as VSProject;
			if (vsp == null)
			{
				MessageBox.Show($"The project \"{projectName}\" is not a supported .NET Project.");
				return;
			}
			var vs = new DotNetProject(pkg, vsp);


			var buildConfig = vs.ActiveBuildConfig;

			var configDisplayName = $"{buildConfig.ConfigurationName}[{buildConfig.PlatformName}]";

			if (vs.IsObfuscated(buildConfig.ConfigurationName,buildConfig.PlatformName) == false)
			{
				MessageBox.Show($"The project \"{projectName}\" is not set up for obfuscation for {configDisplayName} config.");
			}
			else
			{
				var result = MessageBox.Show($"Remove obfuscation command fom Project \"{projectName}\"?", "Obfuscar Tools", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
				if (result == DialogResult.No) return;

				vs.RemoveObfuscarCommand(buildConfig.ConfigurationName,buildConfig.PlatformName);	
				MessageBox.Show($"The project \"{projectName}\" will not be obfuscated for {configDisplayName} builds now.");

			}
		}
	}
}
