using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Samples.Tools.Mdbg.Extensions;
using System.Runtime.InteropServices;
using Microsoft.Samples.SimplePDBReader;

namespace Scout
{
    public static class Program
    {
        //Lazy<Random> _random = new Lazy<Random>();
        static void Main(string[] args)
        {
            var hProcess = new IntPtr(new Random().Next());
            if (!Dbghelp.SymInitializeW(hProcess, null, false))
            {
                Console.Error.WriteLine("LoadSource: Dbghelp.SymInitializeW() failed, error was: " + Marshal.GetLastWin32Error());
                return;
            }

            var sp = new SymbolProvider(@"C:\Projects\Scout\packages\FSharp.Data.2.0.4\lib\net40", SymbolProvider.SymSearchPolicies.AllowReferencePathAccess);
            var sr = sp.GetSymbolReaderForFile(@"C:\Projects\Scout\packages\FSharp.Data.2.0.4\lib\net40\FSharp.Data.DesignTime.pdb");
            // http://msdn.microsoft.com/en-us/library/ms233132.aspx
            int pcDocs;
            sr.GetDocuments(0, out pcDocs, null);
            Console.WriteLine("pcDocs: " + pcDocs);

        }
    }
}
