open System
open System.Diagnostics
open System.Runtime.InteropServices
open Microsoft.Samples.Tools.Mdbg.Extensions
open FSharp.Data

let symInit() =
    let hProcess = new IntPtr(Random().Next())
    if Dbghelp.SymInitializeW(hProcess, null, false) = false then
        failwithf "SymInitializeW failed: %A"  (ComponentModel.Win32Exception(Marshal.GetLastWin32Error()))
    hProcess

// http://msdn.microsoft.com/en-us/library/system.diagnostics.processmodule.baseaddress.aspx
let printModules() =
//    let p = Process.GetCurrentProcess()
//    for m in p.Modules do // native modules, not assemblies
//        printfn "%s" m.ModuleName

    let ad = AppDomain.CurrentDomain
//    ad.SetupInformation.
    for a in ad.GetAssemblies() do
        printfn "%s" a.FullName

[<EntryPoint>]
let main argv =
    let h = Http.Request("http://www.google.com")
    printModules()

//    printfn "%A" h.StatusCode
    0
