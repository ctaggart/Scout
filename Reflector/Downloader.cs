extern alias VS8;
extern alias VS9;

using System;
using System.IO;
using System.Net;

using JetBrains.UI.Shell.Progress;

#if RS40
using JetBrains.Application.Progress;
using JetBrains.VSIntegration.Shell;
#else
using JetBrains.Shell.Progress;
using JetBrains.Shell.VSIntegration;
#endif

namespace ReSharper.Scout.Reflector
{
	using Properties;

	internal class Downloader
	{
		public static readonly Downloader Instance = new Downloader();

		public string DownloadReflector()
		{
			using (SyncProgressWindow progressWindow = new SyncProgressWindow())
			{
				bool cancelled;

				string path = (string)progressWindow.ExecuteTask(downloadTask,
					Resources.Reflector_DownloadTask, out cancelled );

				return cancelled ? null : path;
			}
		}

		private static object downloadTask(IProgressIndicator progress)
		{
			string tempFilePath            = Path.GetTempFileName();
#if RS40
			string reflectorFolder         = VSShell.Instance.UserSettingsLocalDir.Combine(Resources.Reflector).FullPath;
#else
			string reflectorFolder         = Path.Combine(VSShell.Instance.UserSettingsLocalDir, Resources.Reflector);
#endif
			string reflectorExecutablePath = Path.Combine(reflectorFolder, Resources.Reflector + ".exe");

			progress.Start(100);
			progress.CurrentItemText = Resources.Reflector_DownloadStarted;

			WebRequest request    = WebRequest.Create(Resources.Reflector_Url);
			WebResponse response  = request.GetResponse();
			Stream responseStream = response.GetResponseStream();

			byte[] buffer       = new byte[8192];
			long processedBytes = 0;
			long totalBytes     = response.ContentLength;

			using (Stream tempFile = File.OpenWrite(tempFilePath))
			{
				while (processedBytes < totalBytes)
				{
					if (progress.IsCanceled)
						break;

					int read = responseStream.Read(buffer, 0, buffer.Length);
					if (read < 0)
						break;

					tempFile.Write(buffer, 0, read);
					processedBytes += read;

					progress.Advance(read * 100.0 / totalBytes);
					progress.CurrentItemText = string.Format(Resources.Reflector_DownloadProgress,
						processedBytes, totalBytes);
				}
			}
	
			response.Close();

			if (processedBytes == totalBytes)
			{
				progress.CurrentItemText = Resources.Reflector_Uncompressing;
				UncompressZipFile(tempFilePath, reflectorFolder);
				return reflectorExecutablePath;
			}

			return null;
		}

		private static void uncompress8(string zipFile, string destFolder)
		{
			new VS8::Microsoft.VisualStudio.Zip.ZipFileDecompressor(zipFile)
				.UncompressToFolder(destFolder, true);
		}

		private static void uncompress9(string zipFile, string destFolder)
		{
			new VS9::Microsoft.VisualStudio.Zip.ZipFileDecompressor(zipFile)
				.UncompressToFolder(destFolder, true);
		}

		public static void UncompressZipFile(string zipFile, string destFolder)
		{
			switch (VSShell.Instance.VsVersion.Major)
			{
				case 8:
					uncompress8(zipFile, destFolder);
					break;
				case 9:
					uncompress9(zipFile, destFolder);
					break;
				default:
					throw new NotSupportedException(string.Format("VS version {0} is not supported", VSShell.Instance.VsVersion));
			}
		}

	}
}