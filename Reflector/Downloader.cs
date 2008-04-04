extern alias VS8;
extern alias VS9;

using System;
using System.IO;
using System.Net;

using JetBrains.Shell.Progress;
using JetBrains.Shell.VSIntegration;
using JetBrains.UI.Shell.Progress;

namespace ReSharper.Scout.Reflector
{
	using Properties;

	internal class Downloader
	{
		public static Downloader Instance = new Downloader();

		public string DownloadReflector()
		{
			using (SyncProgressWindow progressWindow = new SyncProgressWindow())
			{
				bool cancelled;

				string path = (string)progressWindow.ExecuteTask(DownloadTask,
					Resources.Reflector_DownloadTask, out cancelled );

				return cancelled ? null : path;
			}
		}

		private object DownloadTask(IProgressIndicator progress)
		{
			Settings settings              = Settings.Default;
			string tempFilePath            = Path.GetTempFileName();
			string reflectorFolder         = Path.Combine(VSShell.Instance.UserSettingsLocalDir, settings.Reflector);
			string reflectorExecutablePath = Path.Combine(reflectorFolder, settings.Reflector + ".exe");

			progress.Start(100);
			progress.CurrentItemText = Resources.Reflector_DownloadStarted;

			WebRequest request    = WebRequest.Create(settings.ReflectorUrl);
			WebResponse response  = request.GetResponse();
			Stream responseStream = response.GetResponseStream();

			byte[] buffer       = new byte[1024];
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

		private static void Uncompress8(string zipFile, string destFolder)
		{
			new VS8::Microsoft.VisualStudio.Zip.ZipFileDecompressor(zipFile)
				.UncompressToFolder(destFolder, true);
		}

		private static void Uncompress9(string zipFile, string destFolder)
		{
			new VS9::Microsoft.VisualStudio.Zip.ZipFileDecompressor(zipFile)
				.UncompressToFolder(destFolder, true);
		}

		public static void UncompressZipFile(string zipFile, string destFolder)
		{
			switch (VSShell.Instance.VsVersion.Major)
			{
				case 8:
					Uncompress8(zipFile, destFolder);
					break;
				case 9:
					Uncompress9(zipFile, destFolder);
					break;
				default:
					throw new NotSupportedException(string.Format("VS version {0} is not supported", VSShell.Instance.VsVersion));
			}
		}

	}
}