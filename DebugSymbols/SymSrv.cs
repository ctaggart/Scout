using System;
using System.IO;
using System.Runtime.InteropServices;

using JetBrains.UI.Shell.Progress;
using JetBrains.Util;

#if RS40
using JetBrains.Application.Progress;
using JetBrains.VSIntegration.Shell;
#else
using JetBrains.Shell.Progress;
using JetBrains.Shell.VSIntegration;
#endif

namespace ReSharper.Scout.DebugSymbols
{
	internal static class SymSrv
	{
		public static string DownloadFile(string url, string fileStorePath)
		{
			Uri uri = new Uri(url);

			// Hardcoded in vsdebug.dll, hardcoded here.
			//
			fileStorePath = Path.Combine(fileStorePath, "src");

			// Convert /foo/bar/baz => foo\bar\baz
			//
			string cacheFileName = Path.Combine(fileStorePath,
				uri.PathAndQuery.Substring(1).Replace('/', Path.DirectorySeparatorChar));

			// The file was already loaded.
			//
			if (File.Exists(cacheFileName))
				return cacheFileName;

			// Mandatory for EULA dialog.
			//
			SymbolServerSetOptions(SSRVOPT_PARENTWIN, (long)VSShell.Instance.MainWindow.Handle);

#if DEBUG
			SymbolServerSetOptions(SSRVOPT_TRACE, 1);
#endif

			IntPtr fileHandle = IntPtr.Zero;
			try
			{
				IntPtr siteHandle;
				if (httpOpenFileHandle(uri.Scheme + "://" + uri.Host, uri.PathAndQuery, 0, out siteHandle, out fileHandle))
				{
					// Force folder creation
					//
					string folderPath = Path.GetDirectoryName(cacheFileName);
					if (!Directory.Exists(folderPath))
						Directory.CreateDirectory(folderPath);

					using (SyncProgressWindow progressWindow = new SyncProgressWindow())
					{
						bool  cancelled;
						ulong totalRead = 0;

						progressWindow.ExecuteTask(delegate(IProgressIndicator progress)
						{
							progress.Start(1);

							using (Stream fileStream = File.OpenWrite(cacheFileName))
							{
								byte[] buffer = new byte[8192];
								uint read;
								while (!progress.IsCanceled && httpReadFile(fileHandle, buffer, (uint)buffer.Length, out read) && read > 0)
								{
									totalRead += read;
									progress.CurrentItemText = string.Format(Properties.Resources.SymSrv_DownloadProgress, totalRead);
									progressWindow.Update();
									fileStream.Write(buffer, 0, (int)read);
								}
							}

							if (!progress.IsCanceled)
								File.SetAttributes(cacheFileName, FileAttributes.ReadOnly);

							return null;
						}, cacheFileName, out cancelled);

						return cancelled? null: cacheFileName;
					}
				}
				else
				{
					Logger.LogError("Failed to download url {0}: error {1}",
						url, Marshal.GetLastWin32Error());
				}
			}
			finally
			{
				if (fileHandle != IntPtr.Zero)
					httpCloseHandle(fileHandle);
			}

			// Failed to download.
			//
			return null;
		}

		#region Interop

		private const uint SSRVOPT_PARENTWIN = 0x000080;
		private const uint SSRVOPT_TRACE     = 0x000400;

		private const string module = "symsrv.dll";

		public delegate bool SymSrvCallbackProc(uint @event, long param1, long param2);

		[DllImport(module, SetLastError = true, CharSet=CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SymbolServerSetOptions(uint flag, long param);

		[DllImport(module, SetLastError = true, CharSet=CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool httpOpenFileHandle(string site, string file, int unused, out IntPtr siteHandle, out IntPtr fileHandle);

		[DllImport(module, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool httpReadFile(IntPtr hFile, byte[] buffer, uint dwNumberOfBytesToRead, out uint lpdwNumberOfBytesRead);

		[DllImport(module, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool httpCloseHandle(IntPtr handle);

		#endregion

	}
}