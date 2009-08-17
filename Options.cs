using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

using EnvDTE;

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

		private static T GetDebuggerOption<T>(Settings name)
		{
			if (!UseDebuggerSettings)
				return GetOption(name, default(T));

			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(ReSharper.VsShell.GetVsRegistryKey("Debugger")))
			{
				object o = key == null? null: key.GetValue(name.ToString());
				return (T)(o != null? Convert.ChangeType(o, typeof(T)): default(T));
			}
		}

		public static string SymbolCacheDir
		{
			set { SetOption(Settings.SymbolCacheDir, value, null); }
			get
			{
			    string cacheDir = GetDebuggerOption<string>(Settings.SymbolCacheDir) ??
			               ReSharper.GetUserSettingsFolder("src");

                if (cacheDir.Trim().Length == 0)
                    cacheDir = Path.GetTempPath() + "\\Symbols";

                if (!Directory.Exists(cacheDir))
                    Directory.CreateDirectory(cacheDir);
                else
                {
                    string pubSymbolsDir = cacheDir + "\\MicrosoftPublicSymbols";
                    if (Directory.Exists(pubSymbolsDir))
                        cacheDir = pubSymbolsDir;
                }
                return cacheDir;
			}
		}

		public static string SymbolPath
		{
			set { SetOption(Settings.SymbolPath, value, null); }
			get { return GetDebuggerOption<string>(Settings.SymbolPath)
				?? Environment.GetEnvironmentVariable("_NT_SYMBOL_PATH"); }
		}

		public static bool UseDebuggerSettings
		{
			set { SetOption(Settings.UseDebuggerSettings, value, true); }
			get { return GetOption(Settings.UseDebuggerSettings, true); }
		}

		#endregion

		private static readonly string _myRegistryKeyPath = string.Join("\\",
			new string[] { ReSharper.VsShell.ProductRegistryKey, "Plugins", AssemblyInfo.Product, AssemblyInfo.MajorVersion });

		private static T GetOption<T>(Settings name, T defaultValue)
		{
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_myRegistryKeyPath))
			{
				object o = key == null? null: key.GetValue(name.ToString());
				return o == null? defaultValue: typeof(T).IsEnum?
					(T)Enum.Parse(typeof(T), o.ToString()):
					(T)Convert.ChangeType(o, typeof(T));
			}
		}

		private static void SetOption<T>(Settings name, T value, T defaultValue)
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
			set { SetOption(Settings.UsePdbFiles, value, true); }
			get { return GetOption(Settings.UsePdbFiles, true); }
		}

		public static bool UseReflector
		{
			set { SetOption(Settings.UseReflector, value, true); }
			get { return GetOption(Settings.UseReflector, true); }
		}

		#region Reflector settings

		public static bool ReuseAnyReflectorInstance
		{
			set { SetOption(Settings.ReuseAnyReflectorInstance, value, false); }
			get { return GetOption(Settings.ReuseAnyReflectorInstance, false); }
		}

		public static ReflectorConfiguration ReflectorConfiguration
		{
			set { SetOption(Settings.ReflectorConfiguration, value, ReflectorConfiguration.Default); }
			get { return GetOption(Settings.ReflectorConfiguration, ReflectorConfiguration.Default); }
		}

		public static string ReflectorCustomConfiguration
		{
			set { SetOption(Settings.ReflectorCustomConfiguration, value, null); }
			get { return GetOption<string>(Settings.ReflectorCustomConfiguration, null); }
		}

		public static string ReflectorPath
		{
			set { SetOption(Settings.ReflectorPath, value, null); }
			get
			{
				string value = GetOption<string>(Settings.ReflectorPath, null);
				if (string.IsNullOrEmpty(value) || !File.Exists(value))
				{
					string reflectorExecutableName = Resources.Reflector + ".exe";

					value = SearchRegistry(reflectorExecutableName);
                    if (!string.IsNullOrEmpty(value) && File.Exists(value))
						return ReflectorPath = value;

					// Search for a running instance.
					//
					foreach (Process process in ReSharper.Dte.Debugger.LocalProcesses)
					{
						if (process.Name.EndsWith(reflectorExecutableName, StringComparison.OrdinalIgnoreCase))
							return ReflectorPath = process.Name;
					}

					// Finally download it.
					//
					if (MessageBox.Show(ReSharper.VsShell.MainWindow, Resources.Options_ConfirmReflectorDownload,
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

		private static string SearchRegistry(string executableName)
		{
			string[] pathOfInterest = new string[]
			{
				"code", // URL:Code Identifier Protocol
				"reflectorfile",
				"assembly",
				"dllfile",
				"exefile",
				"Applications\\" + executableName
			};

			string value = null;
			foreach (string path in pathOfInterest)
			{
				RegistryKey key = Registry.ClassesRoot.OpenSubKey(path)
					?? Registry.CurrentUser.OpenSubKey(path);

				if (key == null)
					continue;

				value = SerachRecursively(key, executableName);
				key.Close();

				if (value != null)
					break;
			}

			return value;
		}

		private static string SerachRecursively(RegistryKey key, string executableName)
		{
			foreach (string subKeyName in key.GetSubKeyNames())
			{
				using (RegistryKey subKey = key.OpenSubKey(subKeyName))
				{
					if (subKey == null)
						continue;

					string cmd = (string)subKey.GetValue(null);
					if (!string.IsNullOrEmpty(cmd))
						foreach (string s in cmd.Split('"'))
							if (s.EndsWith(executableName, StringComparison.OrdinalIgnoreCase))
								return s;

					string value = SerachRecursively(subKey, executableName);
					if (value != null)
						return value;
				}
			}

			return null;
		}

		#endregion
	}
}