extern alias VS8;
extern alias VS9;

using System;
using System.IO;
using System.Net;

#if RS30
using JetBrains.Shell.Progress;
#else
using JetBrains.Application.Progress;
#endif

namespace ReSharper.Scout.Reflector
{
	using Properties;

	internal class Downloader
	{
		public static readonly Downloader Instance = new Downloader();

		public string DownloadReflector()
		{
			string path      = null;
			bool   succeeded = ReSharper.ExecuteTask(Resources.Reflector_DownloadTask, true,
				delegate(IProgressIndicator indicator)
				{
					path = (string)DownloadTask(indicator);
				});
			return succeeded? path: null;
		}

		private static object DownloadTask(IProgressIndicator progress)
		{
			string tempFilePath            = Path.GetTempFileName();
			string reflectorFolder         = ReSharper.GetUserSettingsFolder(Resources.Reflector);
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
		    Version vsVersion = ReSharper.VsVersion;

		    if (vsVersion.Major < 9)
		    {
		        Uncompress8(zipFile, destFolder);
		    }
		    else
            {
		        Uncompress9(zipFile, destFolder);
		    }
		}
	}
}