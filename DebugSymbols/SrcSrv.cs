using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using JetBrains.Util;
#if RS40
using JetBrains.VSIntegration.Shell;
#else
using JetBrains.Shell.VSIntegration;
#endif

namespace ReSharper.Scout.DebugSymbols
{
	internal class SrcSrv
	{
		public static readonly SrcSrv Instance = new SrcSrv();

		private readonly IntPtr _cookie;

		public SrcSrv(): this((IntPtr)Guid.NewGuid().GetHashCode())
		{
		}

		public SrcSrv(IntPtr cookie)
		{
			// Session cookie. Must be an unique value.
			//
			_cookie = cookie;

			// Initialize SrcSrv.dll
			//
			SrcSrvSetOptions(1);
			SrcSrvSetParentWindow(VSShell.Instance.MainWindow.Handle);
			SrcSrvInit(cookie, VSShell.Instance.UserSettingsLocalDir);
		}

		private static bool SrcSrvCallback(uint @event, long param1, long param2)
		{
			Logger.LogMessage(LoggingLevel.NORMAL, "SrcSrvCallback: {0} {1} {2}", @event, param1, param2);
			return true;
		}

		public long LoadModule(string moduleFilePath, ISymUnmanagedSourceServerModule sourceServerModule)
		{
			if (moduleFilePath     == null) throw new ArgumentNullException("moduleFilePath");
			if (sourceServerModule == null) throw new ArgumentNullException("sourceServerModule");

			long moduleCookie = ((long)moduleFilePath.ToLower().GetHashCode()) << 30;

			if (SrcSrvIsModuleLoaded(_cookie, moduleCookie))
			{
				// Already loaded.
				//
				return moduleCookie;
			}

			IntPtr data;
			uint dataByteCount;
			sourceServerModule.GetSourceServerData(out dataByteCount, out data);

			try
			{
				return SrcSrvLoadModule(_cookie, Path.GetFileName(moduleFilePath),
					moduleCookie, data, dataByteCount)? moduleCookie: 0L;
			}
			finally
			{
				Marshal.FreeCoTaskMem(data);
			}
		}

		public string GetFileUrl(string sourceFilePath, long moduleCookie)
		{
			SrcSrvRegisterCallback(_cookie, SrcSrvCallback, moduleCookie);

			if (sourceFilePath == null) throw new ArgumentNullException("sourceFilePath");

			StringBuilder url = new StringBuilder(2048);

			return SrcSrvGetFile(_cookie, moduleCookie, sourceFilePath,
				null, url, (uint)url.Capacity)? url.ToString(): null;
		}

		#region Interop

		private const string module = "srcsrv.dll";

		[DllImport(module)]
		public static extern uint SrcSrvSetOptions(uint options);

		[DllImport(module)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SrcSrvSetParentWindow(IntPtr wnd);

		[DllImport(module, CharSet=CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SrcSrvInit(IntPtr sessionCookie, string workingFolder);

		public delegate bool SrcSrvCallbackProc(uint @event, long param1, long param2);

		[DllImport(module, CharSet=CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SrcSrvRegisterCallback(IntPtr sessionCookie, SrcSrvCallbackProc callback, long moduleCookie);

		[DllImport(module)]
		[return : MarshalAs(UnmanagedType.Bool)]
		public static extern bool SrcSrvIsModuleLoaded(IntPtr sessionCookie, long moduleCookie);

		[DllImport(module, CharSet=CharSet.Auto)]
		[return : MarshalAs(UnmanagedType.Bool)]
		public static extern bool SrcSrvLoadModule(IntPtr sessionCookie, string moduleFileName, long moduleCookie, IntPtr symbolClob, uint clobLen);

		[DllImport(module, CharSet=CharSet.Auto)]
		[return : MarshalAs(UnmanagedType.Bool)]
		public static extern bool SrcSrvGetFile(IntPtr sessionCookie, long moduleCookie, string sourceFileLocalPath, string optParams, StringBuilder buffer, uint bufferlen);

		[DllImport(module, CharSet=CharSet.Auto)]
		[return : MarshalAs(UnmanagedType.Bool)]
		public static extern bool SrcSrvGetToken(IntPtr sessionCookie, long moduleCookie, string sourceFileName, out IntPtr tokenClob, out uint clobLen);

		[DllImport(module, CharSet=CharSet.Auto)]
		[return : MarshalAs(UnmanagedType.Bool)]
		public static extern bool SrcSrvExecToken(IntPtr sessionCookie, IntPtr tokenOut, string optParams, StringBuilder buffer, uint bufferlen);

		#endregion
	}
}