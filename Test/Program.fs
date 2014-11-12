module Test

open System
open System.Diagnostics
open System.Runtime.InteropServices
open Microsoft.Samples.Tools.Mdbg.Extensions
//open FSharp.Data
open System.Reflection

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

let printMethods() =
    let a = Assembly.LoadFrom @"C:\Projects\scoutplugin\packages\SourceLink\lib\net45\SourceLink.dll"
    for dt in a.DefinedTypes do
        printfn "\n%s" dt.FullName
        for m in dt.GetMembers() do
            printfn "  %d %s" m.MetadataToken m.Name

        

[<EntryPoint>]
let main argv =
//    let h = Http.Request("http://www.google.com")
//    printModules()

//    printfn "%A" h.StatusCode

    printMethods()
    0
