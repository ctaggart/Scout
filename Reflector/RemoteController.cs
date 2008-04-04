using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using JetBrains.Util;

namespace ReSharper.Scout.Reflector
{
	internal class RemoteController
	{
		public static readonly RemoteController Instance = new RemoteController();

		private Process _reflectorProcess;

		// 30 sec timeout to start Reflector.exe.
		// Should be available in the options page or not?
		//
		private const long MaximumTimeout = 30000;

		public bool Available
		{
			get { return sendCopyDataMessage("Available\n4.0.0.0"); }
		}

		public bool Select(string value)
		{
			Logger.LogMessage(LoggingLevel.VERBOSE, "Select {0}", value);
			return sendCopyDataMessage("Select\n" + value);
		}

		public bool LoadAssembly(string fileName)
		{
			if (_reflectorProcess == null || _reflectorProcess.HasExited)
			{
				if (_reflectorProcess != null)
					_reflectorProcess.Close();

				string path = Options.ReflectorPath;
				if (string.IsNullOrEmpty(path))
					return false;

				_reflectorProcess = Process.Start(path);
				_reflectorProcess.WaitForInputIdle();
			}

			Logger.LogMessage(LoggingLevel.VERBOSE, "LoadAssembly {0}", fileName);
			return sendCopyDataMessage("LoadAssembly\n" + fileName);
		}

		public bool UnloadAssembly(string fileName)
		{
			Logger.LogMessage(LoggingLevel.VERBOSE, "UnloadAssembly {0}", fileName);
			return sendCopyDataMessage("UnloadAssembly\n" + fileName);
		}

		private bool sendCopyDataMessage(string message)
		{
			if (_reflectorProcess == null || _reflectorProcess.HasExited)
				return false;

			Stopwatch timeout = Stopwatch.StartNew();
			IntPtr handle = _reflectorProcess.MainWindowHandle;
			while (handle == IntPtr.Zero && !_reflectorProcess.HasExited && _reflectorProcess.Responding)
			{
				if (timeout.ElapsedMilliseconds > MaximumTimeout)
					return false;

				System.Threading.Thread.Sleep(200);
				_reflectorProcess.Refresh();
				handle = _reflectorProcess.MainWindowHandle;
			}

			if (handle == IntPtr.Zero)
				return false;

			// Reflector may ask some questions, so bring it to the top.
			//
			SwitchToThisWindow(handle, true);

			char[] chars = message.ToCharArray();
			CopyDataStruct data = new CopyDataStruct();
			data.Padding = IntPtr.Zero;
			data.Size = chars.Length * 2;
			data.Buffer = Marshal.AllocHGlobal(data.Size);
			Marshal.Copy(chars, 0, data.Buffer, chars.Length);
			bool result = SendMessage(handle, WM_COPYDATA, IntPtr.Zero, ref data);
			Marshal.FreeHGlobal(data.Buffer);
			return result;
		}

		#region Interop

		private const int WM_COPYDATA = 0x004A;
		private const string module = "user32.dll";

		[DllImport(module)]
		private static extern bool SendMessage(IntPtr wnd, int msg, IntPtr wparam, ref CopyDataStruct lparam);

		[DllImport(module)]
		private static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

		[StructLayout(LayoutKind.Sequential)]
		private struct CopyDataStruct
		{
			public IntPtr Padding;
			public int    Size;
			public IntPtr Buffer;
		}

		#endregion
	}
}