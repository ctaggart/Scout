using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
//using Microsoft.Samples.Debugging.CorDebug;
//using Microsoft.Samples.Debugging.MdbgEngine;

namespace Microsoft.Samples.Tools.Mdbg.Extensions
{
//    public unsafe sealed partial class Extension : CommandBase
//    {
//        const int MAX_PATH = 260;

//        static bool initialized;
//        static IntPtr hProcess;
//        static Dictionary<string, ulong> moduleCache = new Dictionary<string, ulong>();

//        static void LoadSourceCmdInit()
//        {
//            if (initialized)
//            {
//                return;
//            }

//            hProcess = Process.GetCurrentProcess().Handle;

//            // BEGIN: copy workaround from srctool.cpp

//            // hackamatic - VS does not create 'src' directory when extracting files.
//            // Since this is the most commonly used debugger, we will be compatible with
//            // it.  The way to do that is to temporarally set up for a flat directory,
//            // then call SymSetHomeDirectory.  Because of the flat dir option, it will
//            // not use the 'src' dir.  Remmember to unset the option after doing this
//            // or all file extractions will be to a flat directory.

//            Dbghelp.SYMOPT flat = Dbghelp.SYMOPT.SYMOPT_FLAT_DIRECTORY;
//            Dbghelp.SYMOPT result = Dbghelp.SymSetOptions(flat);
//            if (result != flat)
//            {
//                WriteError("LoadSource: Dbghelp.SymSetOptions() failed, error was: " + Marshal.GetLastWin32Error());
//                return;
//            }
//            string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\SourceServer";
//            IntPtr hh = Dbghelp.SymSetHomeDirectoryW(hProcess, localApplicationData);
//            if (hh == IntPtr.Zero)
//            {
//                WriteError("LoadSource: Dbghelp.SymSetHomeDirectoryW() failed, error was: " + Marshal.GetLastWin32Error());
//                return;
//            }

//            // END: copy workaround from srctool.cpp

//            Dbghelp.SYMOPT options = Dbghelp.SYMOPT.SYMOPT_DEBUG | Dbghelp.SYMOPT.SYMOPT_LOAD_ANYTHING;
//            Dbghelp.SYMOPT result2 = Dbghelp.SymSetOptions(options);
//            if (result2 != options)
//            {
//                WriteError("LoadSource: Dbghelp.SymSetOptions() failed, error was: " + Marshal.GetLastWin32Error());
//                return;
//            }

//            bool success = Dbghelp.SymInitializeW(hProcess, null, false);
//            if (!success)
//            {
//                WriteError("LoadSource: Dbghelp.SymInitializeW() failed, error was: " + Marshal.GetLastWin32Error());
//                return;
//            }

//            initialized = true;
//        }

//        [
//CommandDescription(
//                    CommandName = "ls",
//                    ShortHelp = "Load Sources from source server",
//                    MinimumAbbrev = 2,
//                    LongHelp = @"
//Usage: ls [frames]
//    load sources using the SRC* source server
//")]
//        public static void LoadSourceCmd(string arguments)
//        {
//            MDbgFrame currentFrame = CommandBase.Debugger.Processes.Active.Threads.Active.CurrentFrame;
//            string sourceFile = currentFrame.SourcePosition.Path;
//            string fileLocation = CommandBase.Shell.FileLocator.GetFileLocation(sourceFile);
//            if (!File.Exists(fileLocation))
//            {
//                fileLocation = GetPathFromSourceServer(arguments, currentFrame, sourceFile);
//                if (File.Exists(fileLocation))
//                {
//                    WriteOutput("LoadSource: source server associated file path: " + sourceFile + " with path: " + fileLocation);
//                    CommandBase.Shell.FileLocator.Associate(sourceFile, fileLocation);
//                }
//            }
//            IMDbgCommand show;
//            string args;
//            CommandBase.Shell.Commands.ParseCommand("sh", out show, out args);
//            show.Execute(string.Empty);
//        }

//        static string GetPathFromSourceServer(string arguments, MDbgFrame currentFrame, string sourceFile)
//        {
//            LoadSourceCmdInit();
//            if (!initialized)
//            {
//                WriteError("LoadSource: failed to initialize");
//                return null;
//            }

//            bool spew = false;
//            ArgParser parser = new ArgParser(arguments, "d");
//            if (parser.OptionPassed("d"))
//            {
//                spew = true;
//            }

//            bool success;
//            if (spew)
//            {
//                success = Dbghelp.SymRegisterCallbackW64(hProcess, (h, ac, cd, uc) =>
//                {
//                    if (ac == Dbghelp.CBA.CBA_DEFERRED_SYMBOL_LOAD_CANCEL)
//                    {
//                        if (spew) WriteOutput("LoadSource: CBA_DEFERRED_SYMBOL_LOAD_CANCEL");
//                        return false;
//                    }
//                    else if (ac == Dbghelp.CBA.CBA_EVENT)
//                    {
//                        Dbghelp.IMAGEHLP_CBA_EVENT foo = (Dbghelp.IMAGEHLP_CBA_EVENT)Marshal.PtrToStructure((IntPtr)cd, typeof(Dbghelp.IMAGEHLP_CBA_EVENT));
//                        if (spew) WriteOutput("LoadSource: CBA_EVENT: " + foo.desc);
//                    }
//                    else
//                    {
//                        if (spew) WriteOutput("LoadSource: Dbghelp.SymRegisterCallbackW64() hProcess: 0x" + string.Format("{0:X}", (ulong)h) + " ac: " + ac);
//                    }
//                    return true;
//                }, 0);
//                if (!success)
//                {
//                    WriteError("LoadSource: Dbghelp.SymRegisterCallbackW64() failed, error was: " + Marshal.GetLastWin32Error());
//                    return null;
//                }
//            }

//            MDbgModule module = currentFrame.Function.Module;
//            string symbolFile = module.SymbolFilename;
//            ulong baseAddress;
//            if (!moduleCache.TryGetValue(symbolFile, out baseAddress))
//            {
//                CorModule corModule = module.CorModule;
//                baseAddress = (ulong)corModule.BaseAddress;
//                uint size = (uint)corModule.Size;
//                string moduleName = Path.GetFileNameWithoutExtension(sourceFile);
//                if (spew) WriteOutput("LoadSource: dll file: " + corModule.Name + " size: " + size + " baseAddress: 0x" + string.Format("{0:X}", baseAddress) + " pdb file: " + symbolFile + " source file: " + sourceFile);
//                baseAddress = Dbghelp.SymLoadModuleExW(hProcess, IntPtr.Zero, symbolFile, moduleName, baseAddress, size, (Dbghelp.MODLOAD_DATA*)null, 0);
//                if (baseAddress == 0)
//                {
//                    WriteError("LoadSource: Dbghelp.SymLoadModuleExW() failed, error was: " + Marshal.GetLastWin32Error());
//                    return null;
//                }
//                if (spew) WriteOutput("LoadSource: returned baseAddress: 0x" + string.Format("{0:X}", baseAddress));
//                moduleCache.Add(symbolFile, baseAddress);
//            }

//            StringBuilder path = new StringBuilder(MAX_PATH * 8);
//            success = Dbghelp.SymGetSourceFileW(hProcess, baseAddress, IntPtr.Zero, sourceFile, path, (uint)path.Capacity);
//            if (!success)
//            {
//                WriteError("LoadSource: Dbghelp.SymGetSourceFileW() failed, error was: " + Marshal.GetLastWin32Error());
//                return null;
//            }
//            return path.ToString();
//        }

//        static bool LoadSourceCmdTemp(ulong baseAddress, string sourceFile, bool spew)
//        {
//            Dbghelp.IMAGEHLP_MODULE64 moduleInfo = new Dbghelp.IMAGEHLP_MODULE64();
//            moduleInfo.SizeOfStruct = (uint)Marshal.SizeOf(moduleInfo);
//            bool success = Dbghelp.SymGetModuleInfoW64(hProcess, baseAddress, ref moduleInfo);
//            if (!success)
//            {
//                WriteError("LoadSource: Dbghelp.SymGetModuleInfoW64() failed, error was: " + Marshal.GetLastWin32Error());
//                return false;
//            }
//            if (spew) WriteOutput("LoadSource: Dbghelp.SymGetModuleInfoW64() returned baseAddress: 0x" + string.Format("{0:X}", moduleInfo.BaseOfImage) + " DbgUnmatched: " + moduleInfo.DbgUnmatched + " GlobalSymbols: " + moduleInfo.GlobalSymbols + " LoadedPdbName: " + moduleInfo.LoadedPdbName);

//            success = Dbghelp.SymEnumSourceFiles(hProcess, baseAddress, null, (sf, context) =>
//            {
//                if (sourceFile == sf.FileName)
//                {
//                    if (spew) WriteOutput("LoadSource: Dbghelp.SymEnumSourceFiles() fileName: " + sf.FileName + " modBase: 0x" + string.Format("{0:X}", sf.ModBase));
//                    IntPtr token;
//                    uint size2;
//                    bool success2 = Dbghelp.SymGetSourceFileTokenW(hProcess, sf.ModBase, sf.FileName, out token, out size2);
//                    if (!success2)
//                    {
//                        WriteError("LoadSource: Dbghelp.SymGetSourceFileTokenW() failed, error was: " + Marshal.GetLastWin32Error());
//                        return false;
//                    }
//                    if (spew) WriteOutput("LoadSource: Dbghelp.SymGetSourceFileTokenW() token: " + token + " size: " + size2);
//                }
//                return true;
//            }, IntPtr.Zero);
//            if (!success)
//            {
//                WriteError("LoadSource: Dbghelp.SymEnumSourceFiles() failed, error was: " + Marshal.GetLastWin32Error());
//                return false;
//            }
//            return true;
//        }

        public unsafe static class Dbghelp
        {
            const int MAX_PATH = 260;
            const string DBGHELP = "Dbghelp.dll";

            [DllImport(DBGHELP, CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool SymInitializeW(IntPtr hProcess, string UserSearchPath, bool fInvadeProcess);

            [DllImport(DBGHELP, CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern ulong SymLoadModuleExW(IntPtr hProcess, IntPtr hFile, string ImageName, string ModuleName, ulong BaseOfDll, uint DllSize, MODLOAD_DATA* Data, uint Flags);

            [DllImport(DBGHELP, CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool SymRegisterCallbackW64(IntPtr hProcess, SymRegisterCallbackProcW64 CallbackFunction, ulong UserContext);

            [DllImport(DBGHELP, CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool SymGetSourceFileW(IntPtr hProcess, ulong Base, IntPtr Params, string FileSpec, StringBuilder FilePath, uint Size);

            [DllImport(DBGHELP, CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool SymGetSourceFileTokenW(IntPtr hProcess, ulong Base, string FileSpec, out IntPtr Token, out uint Size);

            [DllImport(DBGHELP, CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool SymGetSourceFileFromTokenW(IntPtr hProcess, IntPtr Token, IntPtr Params, StringBuilder FilePath, uint Size);

            [DllImport(DBGHELP, CharSet = CharSet.Ansi, SetLastError = true)]
            public static extern bool SymEnumSourceFiles(IntPtr hProcess, ulong ModBase, string Mask, SymEnumSourceFilesProc EnumSymbolsCallback, IntPtr UserContext);

            [DllImport(DBGHELP, CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool SymGetLineFromAddrW64(IntPtr hProcess, ulong dwAddr, out uint pdwDisplacement, out IMAGEHLP_LINE64 Line);

            [DllImport(DBGHELP, CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool SymEnumerateModulesW64(IntPtr hProcess, SymEnumModules EnumModules, IntPtr UserContext);

            [DllImport(DBGHELP, CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr SymSetHomeDirectoryW(IntPtr hProcess, string dir);

            [DllImport(DBGHELP, CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern SYMOPT SymSetOptions(SYMOPT SymOptions);

            [DllImport(DBGHELP, CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern bool SymGetModuleInfoW64(IntPtr hProcess, ulong dwAddr, ref IMAGEHLP_MODULE64 ModuleInfo);

            public delegate bool SymRegisterCallbackProcW64(IntPtr hProcess, CBA ActionCode, ulong CallbackData, ulong UserContext);
            public delegate bool SymEnumSourceFilesProc(SOURCEFILE pSourceFile, IntPtr UserContext);
            public delegate bool SymEnumModules(string ModuleName, ulong BaseOfDll, IntPtr UserContext);

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public struct SOURCEFILE
            {
                public ulong ModBase;
                public string FileName;
            };

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct IMAGEHLP_LINE64
            {
                public uint SizeOfStruct;
                public IntPtr Key;
                public uint LineNumber;
                public string FileName;
                public ulong Address;
            };

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct IMAGEHLP_MODULE64
            {
                public uint SizeOfStruct;
                public ulong BaseOfImage;
                public uint ImageSize;
                public uint TimeDateStamp;
                public uint CheckSum;
                public uint NumSyms;
                public SYM_TYPE SymType;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
                public string ModuleName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
                public string ImageName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
                public string LoadedImageName;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
                public string LoadedPdbName;
                public uint CVSig;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH * 3)]
                public string CVData;
                public uint PdbSig;
                public Guid PdbSig70;
                public uint PdbAge;
                public bool PdbUnmatched;
                public bool DbgUnmatched;
                public bool LineNumbers;
                public bool GlobalSymbols;
                public bool TypeInfo;
                public bool SourceIndexed;
                public bool Publics;
            };

            public enum SYM_TYPE
            {
                SymNone = 0,
                SymCoff,
                SymCv,
                SymPdb,
                SymExport,
                SymDeferred,
                SymSym, // .sym file
                SymDia,
                SymVirtual,
                NumSymTypes
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct MODLOAD_DATA
            {
                public uint ssize;
                public DBHHEADER ssig;
                public IntPtr data;
                public uint size;
                public uint flags;
            };

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct IMAGEHLP_CBA_EVENT
            {
                public uint severity;
                public uint code;
                public string desc;
                public IntPtr object1;
            }

            public enum DBHHEADER : uint
            {
                DBHHEADER_DEBUGDIRS = 0x1, // The data member is a buffer that contains an array of IMAGE_DEBUG_DIRECTORY structures.
                DBHHEADER_CVMISC = 0x2, // The data member is a buffer that contains an array of MODLOAD_CVMISC structures.
            }

            public enum CBA : uint
            {
                CBA_DEBUG_INFO = 0x10000000, // Display verbose information. // The CallbackData parameter is a pointer to a string.
                CBA_DEFERRED_SYMBOL_LOAD_CANCEL = 0x00000007, // Deferred symbol loading has started. To cancel // The symbol load, return TRUE. // The CallbackData parameter should be ignored. 
                CBA_DEFERRED_SYMBOL_LOAD_COMPLETE = 0x00000002, // Deferred symbol load has completed. // The CallbackData parameter is a pointer to a IMAGEHLP_DEFERRED_SYMBOL_LOAD64 structure. 
                CBA_DEFERRED_SYMBOL_LOAD_FAILURE = 0x00000003, // Deferred symbol load has failed. // The CallbackData parameter is a pointer to a IMAGEHLP_DEFERRED_SYMBOL_LOAD64 structure. // The symbol handler will attempt to load // The symbols again if // The callback function sets // The FileName member of this structure. 
                CBA_DEFERRED_SYMBOL_LOAD_PARTIAL = 0x00000020, // Deferred symbol load has partially completed. // The symbol loader is unable to read // The image header from ei// Ther // The image file or // The specified module. // The CallbackData parameter is a pointer to a IMAGEHLP_DEFERRED_SYMBOL_LOAD64 structure. // The symbol handler will attempt to load // The symbols again if // The callback function sets // The FileName member of this structure. // DbgHelp 5.1: This value is not supported. 
                CBA_DEFERRED_SYMBOL_LOAD_START = 0x00000001, // Deferred symbol load has started. // The CallbackData parameter is a pointer to a IMAGEHLP_DEFERRED_SYMBOL_LOAD64 structure. 
                CBA_DUPLICATE_SYMBOL = 0x00000005, // Duplicate symbols were found. This reason is used only in COFF or CodeView format. // The CallbackData parameter is a pointer to a IMAGEHLP_DUPLICATE_SYMBOL64 structure. To specify which symbol to use, set // The SelectedSymbol member of this structure. 
                CBA_EVENT = 0x00000010, // Display verbose information. If you do not handle this event, // The information is resent through // The ,CBA_DEBUG_INFO event. // The CallbackData parameter is a pointer to a IMAGEHLP_,CBA_EVENT structure. 
                CBA_READ_MEMORY = 0x00000006, // The loaded image has been read. // The CallbackData parameter is a pointer to a IMAGEHLP_,CBA_READ_MEMORY structure. // The callback function should read // The number of bytes specified by // The bytes member into // The buffer specified by // The buf member, and update // The bytesread member accordingly. 
                CBA_SET_OPTIONS = 0x00000008, // Symbol options have been updated. To retrieve // The current options, call // The SymGetOptions function. // The CallbackData parameter should be ignored. 
                CBA_SRCSRV_EVENT = 0x40000000, // Display verbose information for source server. If you do not handle this event, // The information is resent through // The ,CBA_DEBUG_INFO event. // The CallbackData parameter is a pointer to a IMAGEHLP_,CBA_EVENT structure. // DbgHelp 6.6 and earlier: This value is not supported. 
                CBA_SRCSRV_INFO = 0x20000000, // Display verbose information for source server. // The CallbackData parameter is a pointer to a string. // DbgHelp 6.6 and earlier: This value is not supported. 
                CBA_SYMBOLS_UNLOADED = 0x00000004, // Symbols have been unloaded. // The CallbackData parameter should be ignored. 
            }

            [Flags]
            public enum SYMOPT : uint
            {
                SYMOPT_ALLOW_ABSOLUTE_SYMBOLS = 0x00000800,// Enables the use of symbols that are stored with absolute addresses. Most symbols are stored as RVAs from the base of the module. DbgHelp translates them to absolute addresses. There are symbols that are stored as an absolute address. These have very specialized purposes and are typically not used. //DbgHelp 5.1 and earlier: This value is not supported. 
                SYMOPT_ALLOW_ZERO_ADDRESS = 0x01000000,// Enables the use of symbols that do not have an address. By default, DbgHelp filters out symbols that do not have an address.
                SYMOPT_AUTO_PUBLICS = 0x00010000,// Do not search the public symbols when searching for symbols by address, or when enumerating symbols, unless they were not found in the global symbols or within the current scope. This option has no effect with SYMOPT_PUBLICS_ONLY. //DbgHelp 5.1 and earlier: This value is not supported. 
                SYMOPT_CASE_INSENSITIVE = 0x00000001,// All symbol searches are insensitive to case.
                SYMOPT_DEBUG = 0x80000000,// Pass debug output through OutputDebugString or the SymRegisterCallbackProc64 callback function.
                SYMOPT_DEFERRED_LOADS = 0x00000004,// Symbols are not loaded until a reference is made requiring the symbols be loaded. This is the fastest, most efficient way to use the symbol handler.
                SYMOPT_DISABLE_SYMSRV_AUTODETECT = 0x02000000,// Disables the auto-detection of symbol server stores in the symbol path, even without the SRV* designation, maintaining compatibility with previous behavior. //DbgHelp 6.6 and earlier: This value is not supported. 
                SYMOPT_EXACT_SYMBOLS = 0x00000400,// Do not load an unmatched .pdb file. Do not load export symbols if all else fails.
                SYMOPT_FAIL_CRITICAL_ERRORS = 0x00000200,// Do not display system dialog boxes when there is a media failure such as no media in a drive. Instead, the failure happens silently.
                SYMOPT_FAVOR_COMPRESSED = 0x00800000,// If there is both an uncompressed and a compressed file available, favor the compressed file. This option is good for slow connections.
                SYMOPT_FLAT_DIRECTORY = 0x00400000,// Symbols are stored in the root directory of the default downstream store. //DbgHelp 6.1 and earlier: This value is not supported. 
                SYMOPT_IGNORE_CVREC = 0x00000080,// Ignore path information in the CodeView record of the image header when loading a .pdb file
                SYMOPT_IGNORE_IMAGEDIR = 0x00200000,// Ignore the image directory. //DbgHelp 6.1 and earlier: This value is not supported. 
                SYMOPT_IGNORE_NT_SYMPATH = 0x00001000,// Do not use the path specified by _NT_SYMBOL_PATH if the user calls SymSetSearchPath without a valid path. //DbgHelp 5.1: This value is not supported. 
                SYMOPT_INCLUDE_32BIT_MODULES = 0x00002000,// When debugging on 64-bit Windows, include any 32-bit modules.
                SYMOPT_LOAD_ANYTHING = 0x00000040,// Disable checks to ensure a file (.exe, .dbg., or .pdb) is the correct file. Instead, load the first file located. 
                SYMOPT_LOAD_LINES = 0x00000010,// Loads line number information. 
                SYMOPT_NO_CPP = 0x00000008,// All C++ decorated symbols containing the symbol separator "::" are replaced by "__". This option exists for debuggers that cannot handle parsing real C++ symbol names. 
                SYMOPT_NO_IMAGE_SEARCH = 0x00020000,// Do not search the image for the symbol path when loading the symbols for a module if the module header cannot be read. //DbgHelp 5.1: This value is not supported. 
                SYMOPT_NO_PROMPTS = 0x00080000,// Prevents prompting for validation from the symbol server.
                SYMOPT_NO_PUBLICS = 0x00008000,// Do not search the publics table for symbols. This option should have little effect because there are copies of the public symbols in the globals table. //DbgHelp 5.1: This value is not supported. 
                SYMOPT_NO_UNQUALIFIED_LOADS = 0x00000100,// Prevents symbols from being loaded when the caller examines symbols across multiple modules. Examine only the module whose symbols have already been loaded.
                SYMOPT_OVERWRITE = 0x00100000,// Overwrite the downlevel store from the symbol store. //DbgHelp 6.1 and earlier: This value is not supported. 
                SYMOPT_PUBLICS_ONLY = 0x00004000,// Do not use private symbols. The version of DbgHelp that shipped with Windows 2000 and earlier supported only public symbols; this option provides compatibility with this limitation. //DbgHelp 5.1: This value is not supported. 
                SYMOPT_SECURE = 0x00040000,// DbgHelp will not load any symbol server other than SymSrv. SymSrv will not use the downstream store specified in _NT_SYMBOL_PATH. After this flag has been set, it cannot be cleared. //DbgHelp 6.0 and 6.1: This flag can be cleared. //DbgHelp 5.1: This value is not supported. 
                SYMOPT_UNDNAME = 0x00000002,// All symbols are presented in undecorated form. //This option has no effect on global or local symbols because they are stored undecorated. This option applies only to public symbols.
            }
        }
    //}
}
