using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ReSharper.Scout.DebugSymbols
{
    public class SrcSrv
    {
        //public static readonly SrcSrv Instance = new SrcSrv();

        private readonly IntPtr _cookie;

        public SrcSrv()//: this(System.Diagnostics.Process.GetCurrentProcess().Handle)
        {
        }

        public SrcSrv(IntPtr cookie)
        {
            // Session cookie. Must be an unique value.
            _cookie = cookie;

            // Initialize SrcSrv.dll
            //
            SrcSrvSetOptions(1);
            //SrcSrvSetParentWindow(ReSharper.VsShell.MainWindow.Handle); // TODO 
            //SrcSrvInit(cookie, Options.SymbolCacheDir);
            SrcSrvInit(cookie, @"C:\tmp\cache");
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

            if (sourceServerModule.GetSourceServerData(out dataByteCount, out data) < 0)
            {
                // VS2005 fails on .pdb files produced by Phoenix compiler.
                // https://connect.microsoft.com/Phoenix/
                //
                return 0L;
            }

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
            if (sourceFilePath == null) throw new ArgumentNullException("sourceFilePath");

            StringBuilder url = new StringBuilder(2048);

            return SrcSrvGetFile(_cookie, moduleCookie, sourceFilePath,
                null, url, (uint)url.Capacity)? url.ToString(): null;
        }

        private const string module = "srcsrv.dll";

        [DllImport(module, SetLastError = true)]
        public static extern uint SrcSrvSetOptions(uint options);

        [DllImport(module, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SrcSrvSetParentWindow(IntPtr wnd);

        [DllImport(module, SetLastError = true, CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SrcSrvInit(IntPtr sessionCookie, string workingFolder);

        public delegate bool SrcSrvCallbackProc(uint @event, long param1, long param2);

        [DllImport(module, SetLastError = true, CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SrcSrvRegisterCallback(IntPtr sessionCookie, SrcSrvCallbackProc callback, long moduleCookie);

        [DllImport(module, SetLastError = true)]
        [return : MarshalAs(UnmanagedType.Bool)]
        public static extern bool SrcSrvIsModuleLoaded(IntPtr sessionCookie, long moduleCookie);

        [DllImport(module, SetLastError = true, CharSet=CharSet.Auto)]
        [return : MarshalAs(UnmanagedType.Bool)]
        public static extern bool SrcSrvLoadModule(IntPtr sessionCookie, string moduleFileName, long moduleCookie, IntPtr symbolClob, uint clobLen);

        [DllImport(module, SetLastError = true, CharSet=CharSet.Auto)]
        [return : MarshalAs(UnmanagedType.Bool)]
        public static extern bool SrcSrvGetFile(IntPtr sessionCookie, long moduleCookie, string sourceFileLocalPath, string optParams, StringBuilder buffer, uint bufferlen);

        [DllImport(module, SetLastError = true, CharSet=CharSet.Auto)]
        [return : MarshalAs(UnmanagedType.Bool)]
        public static extern bool SrcSrvGetToken(IntPtr sessionCookie, long moduleCookie, string sourceFileName, out IntPtr tokenClob, out uint clobLen);

        [DllImport(module, SetLastError = true, CharSet=CharSet.Auto)]
        [return : MarshalAs(UnmanagedType.Bool)]
        public static extern bool SrcSrvExecToken(IntPtr sessionCookie, IntPtr tokenOut, string optParams, StringBuilder buffer, uint bufferlen);
    }
}