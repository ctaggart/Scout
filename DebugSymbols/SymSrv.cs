using System;
using System.IO;
using System.Runtime.InteropServices;

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

			IntPtr fileHandle = IntPtr.Zero;
			IntPtr siteHandle = IntPtr.Zero;
			try
			{
				if (httpOpenFileHandle(uri.Scheme + "://" + uri.Host, uri.PathAndQuery, 0, out siteHandle, out fileHandle))
				{
					// Force folder creation
					//
					string folderPath = Path.GetDirectoryName(cacheFileName);
					if (!Directory.Exists(folderPath))
						Directory.CreateDirectory(folderPath);

					using (Stream fileStream = File.OpenWrite(cacheFileName))
					{
						byte[] buffer = new byte[8192];
						uint read;
						while (httpReadFile(fileHandle, buffer, (uint)buffer.Length, out read) && read > 0)
							fileStream.Write(buffer, 0, (int) read);
					}

					return cacheFileName;
				}
			}
			finally
			{
				if (fileHandle != IntPtr.Zero)
					httpCloseHandle(fileHandle);
				if (siteHandle != IntPtr.Zero)
					httpCloseHandle(siteHandle);
			}

			// Failed to download.
			//
			return null;
		}

		#region Interop

		private const string module = "symsrv.dll";

		[DllImport(module)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SymbolServerSetOptions(uint options, long param);

		[DllImport(module)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool httpOpenFileHandle(string site, string file, int unused, out IntPtr siteHandle, out IntPtr fileHandle);

		[DllImport(module)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool httpReadFile(IntPtr hFile, byte[] buffer, uint dwNumberOfBytesToRead, out uint lpdwNumberOfBytesRead);

		[DllImport(module)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool httpCloseHandle(IntPtr handle);

		#endregion

	}
}