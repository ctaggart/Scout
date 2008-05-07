using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

using EnvDTE;
#if RS40
using JetBrains.VSIntegration.Shell;
#else
using JetBrains.Shell.VSIntegration;
#endif

namespace ReSharper.Scout
{
	using Properties;

	internal enum ReflectorConfiguration
	{
		Default,
		PerSolution,
		Custom,
	}

	internal static class Options
	{
		enum Settings
		{
			UsePdbFiles,
			UseReflector,

			// Source server
			//
			UseDebuggerSettings,
			SymbolCacheDir,
			SymbolPath,

			// Reflector
			//
			ReflectorPath,
			ReflectorConfiguration,
			ReflectorCustomConfiguration,
			ReuseAnyReflectorInstance,
		}

		#region Source server settings

		private static T getDebuggerOption<T>(Settings name)
		{
			if (!UseDebuggerSettings)
				return getOption(name, default(T));

			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(VSShell.Instance.GetVsRegistryKey("Debugger")))
			{
				object o = key == null? null: key.GetValue(name.ToString());
				return (T)(o != null? Convert.ChangeType(o, typeof(T)): default(T));
			}
		}

		public static string SymbolCacheDir
		{
			set { setOption(Settings.SymbolCacheDir, value, null); }
			get
			{
				return getDebuggerOption<string>(Settings.SymbolCacheDir) ??
					Path.Combine(VSShell.Instance.UserSettingsLocalDir, "src");
			}
		}

		public static string SymbolPath
		{
			set { setOption(Settings.SymbolPath, value, null); }
			get { return getDebuggerOption<string>(Settings.SymbolPath)
				?? Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH"); }
		}

		public static bool UseDebuggerSettings
		{
			set { setOption(Settings.UseDebuggerSettings, value, true); }
			get { return getOption(Settings.UseDebuggerSettings, true); }
		}

		#endregion

		private static readonly string _myRegistryKeyPath = string.Join("\\",
			new string[] { VSShell.Instance.ProductRegistryKey, "Plugins", AssemblyInfo.Product, AssemblyInfo.MajorVersion });

		private static T getOption<T>(Settings name, T defaultValue)
		{
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_myRegistryKeyPath))
			{
				object o = key == null? null: key.GetValue(name.ToString());
				return o == null? defaultValue: typeof(T).IsEnum?
					(T)Enum.Parse(typeof(T), o.ToString()):
					(T)Convert.ChangeType(o, typeof(T));
			}
		}

		private static void setOption<T>(Settings name, T value, T defaultValue)
		{
			using (RegistryKey key = Registry.CurrentUser.CreateSubKey(_myRegistryKeyPath))
			{
				if (Equals(value, defaultValue))
				{
					if (key.GetValue(name.ToString()) != null)
						key.DeleteValue(name.ToString());
				}
				else
					key.SetValue(name.ToString(), value);
			}
		}

		public static bool UsePdbFiles
		{
			set { setOption(Settings.UsePdbFiles, value, true); }
			get { return getOption(Settings.UsePdbFiles, true); }
		}

		public static bool UseReflector
		{
			set { setOption(Settings.UseReflector, value, true); }
			get { return getOption(Settings.UseReflector, true); }
		}

		#region Reflector settings

		public static bool ReuseAnyReflectorInstance
		{
			set { setOption(Settings.ReuseAnyReflectorInstance, value, false); }
			get { return getOption(Settings.ReuseAnyReflectorInstance, false); }
		}

		public static ReflectorConfiguration ReflectorConfiguration
		{
			set { setOption(Settings.ReflectorConfiguration, value, ReflectorConfiguration.Default); }
			get { return getOption(Settings.ReflectorConfiguration, ReflectorConfiguration.Default); }
		}

		public static string ReflectorCustomConfiguration
		{
			set { setOption(Settings.ReflectorCustomConfiguration, value, null); }
			get { return getOption<string>(Settings.ReflectorCustomConfiguration, null); }
		}

		public static string ReflectorPath
		{
			set { setOption(Settings.ReflectorPath, value, null); }
			get
			{
				string value = getOption<string>(Settings.ReflectorPath, null);
				if (string.IsNullOrEmpty(value))
				{
					string reflectorExecutableName = Properties.Settings.Default.Reflector + ".exe";

					// Query the shell association key.
					//
					using (RegistryKey dllFile = Registry.ClassesRoot.OpenSubKey("dllfile\\shell"))
					{
						if (dllFile != null)
							foreach (string subKey in dllFile.GetSubKeyNames())
								using (RegistryKey commandKey = dllFile.OpenSubKey(subKey + "\\command"))
									if (commandKey != null)
									{
										string cmd = (string) commandKey.GetValue(null);
										if (!string.IsNullOrEmpty(cmd))
											foreach (string s in cmd.Split('"'))
												if (s.EndsWith(reflectorExecutableName, StringComparison.OrdinalIgnoreCase))
													return ReflectorPath = s;
									}
					}

					// Search for a running instance.
					//
					foreach (Process process in VSShell.Instance.ApplicationObject.Debugger.LocalProcesses)
					{
						if (process.Name.EndsWith(reflectorExecutableName, StringComparison.OrdinalIgnoreCase))
							return ReflectorPath = process.Name;
					}

					// Finally download it.
					//
					if (MessageBox.Show(VSShell.Instance.MainWindow, Resources.Options_ConfirmReflectorDownload,
						AssemblyInfo.Product, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
					{
						return ReflectorPath = Reflector.Downloader.Instance.DownloadReflector();
					}
					
					// Suppress this question when the user denies it.
					//
					UseReflector = false;
				}

				return value;
			}
		}

		#endregion

	}
}