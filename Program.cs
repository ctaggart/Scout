using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Samples.Tools.Mdbg.Extensions;
using System.Runtime.InteropServices;

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

        }
    }
}
