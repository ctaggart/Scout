open System
open System.Diagnostics
open System.Runtime.InteropServices
open FSharp.Data
open System.Diagnostics.SymbolStore
open Microsoft.Samples.SimplePDBReader
open System.Text
open ReSharper.Scout.DebugSymbols
open IKVM.Reflection

//let symInit() =
//    let hProcess = new IntPtr(Random().Next())
//    if Dbghelp.SymInitializeW(hProcess, null, false) = false then
//        failwithf "SymInitializeW failed: %A"  (ComponentModel.Win32Exception(Marshal.GetLastWin32Error()))
//    hProcess

/// Windows API
//module Windows =
//    [<DllImport("kernel32", CharSet=CharSet.Unicode)>]
//    extern int AddDllDirectory(string dir)

[<ComImport; Interface>]
[<Guid("809c652e-7396-11d2-9771-00a0c9b4d50c") ; InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>]
type IMetaDataDispenser = 
    abstract DefineScope : unit -> unit // need this here to fill the first vtable slot
    abstract OpenScope : [<In ; MarshalAs(UnmanagedType.LPWStr)>] szScope : string * [<In>] dwOpenFlags:Int32 * [<In>] riid : System.Guid byref * [<Out ; MarshalAs(UnmanagedType.IUnknown)>] punk:Object byref -> unit

[<ComImport; Interface>]
[<Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44"); InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>]
[<CLSCompliant(true)>]
type IMetadataImport =
    abstract Placeholder : unit -> unit

// derived from ilsupp.fs
//let pdbReadOpen (dll:string) (pdb:string) =
//    let mutable IID_IMetaDataImport = Guid "7DAC8207-D3AE-4c75-9B67-92801A497D44"
//    let mdd = Activator.CreateInstance (Type.GetTypeFromProgID "CLRMetaData.CorMetaDataDispenser") :?> IMetaDataDispenser
//    let mutable o : Object = new Object()
//    mdd.OpenScope(dll, 0, &IID_IMetaDataImport, &o) ;
//    let pMdd = Marshal.GetComInterfaceForObject(o, typeof<IMetadataImport>)
//    let symBinder = SymBinder() // ISymWrapper.dll, Windows only
//    let symReader = symBinder.GetReader(pMdd, dll, pdb)
//    if IntPtr.Zero <> pMdd then
//        Marshal.Release pMdd |> ignore
//    symReader :?> ISymbolReader



//let printPdbInfo() =
//    let dll = @"C:\Projects\Scout\packages\FSharp.Data.2.0.4\lib\net40\FSharp.Data.dll"
//    let pdb = @"C:\Projects\Scout\packages\FSharp.Data.2.0.4\lib\net40\FSharp.Data.pdb"
//    let r = pdbReadOpen dll pdb
//    for d in r.GetDocuments() do
//        printfn "\n%A\n%A\n%A\n%A" d.URL d.LanguageVendor d.Language d.DocumentType
//    ()   

let printUrlAndDownload() =
    // path for srcsrv.dll
//    try
////        let dir = @"C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE" //typeof<LibGit2SharpException>.Assembly.Location |> Path.GetDirectoryName
//        let dir = @"C:\Program Files (x86)\Windows Kits\8.1\Debuggers\x64\"
//        let i = dir |> Windows.AddDllDirectory
//        if i = 0 then
//            failwithf "AddDllDirectory %A failed: %A" dir (ComponentModel.Win32Exception(Marshal.GetLastWin32Error()))
//    with
//        | :? EntryPointNotFoundException -> failwithf "AddDllDirectory not found. Install KB2533623 update. http://support.microsoft.com/kb/2533623"

    let dir = @"C:\Projects\Scout\packages\FSharp.Data.2.0.4\lib\net40"
    let dll = @"C:\Projects\Scout\packages\FSharp.Data.2.0.4\lib\net40\FSharp.Data.dll"
//    let pdb = @"C:\Projects\Scout\packages\FSharp.Data.2.0.4\lib\net40\FSharp.Data.pdb"
    let sp = SymbolProvider(dir, SymbolProvider.SymSearchPolicies.AllowReferencePathAccess)
    let sr = sp.GetSymbolReaderForFile dll
    let mutable cDocs = 0
    sr.GetDocuments(0, &cDocs, null)
    printfn "pcDocs: %d" cDocs
    let mutable docs = Array.zeroCreate<SymbolProvider.ISymUnmanagedDocument> cDocs
    sr.GetDocuments(cDocs, &cDocs, docs)
    for d in docs do
        
        let mutable cUrl = 0
        d.GetURL(0, &cUrl, null)
        let sbUrl = StringBuilder cUrl
        d.GetURL(cUrl, &cUrl, sbUrl)
        let url = sbUrl.ToString()
        printfn "url: %s" url

        let mutable cChecksum = 0
        d.GetCheckSum(0, &cChecksum, null)
        let checksum = Array.zeroCreate<byte> cChecksum
        d.GetCheckSum(cChecksum, &cChecksum, checksum)
        printfn "url: %A" checksum

//    let a = sr :? ISymUnmanagedSourceServerModule
    printfn "is ISymUnmanagedSourceServerModule %b" (sr :? ISymUnmanagedSourceServerModule)
    let ss = sr :?> ISymUnmanagedSourceServerModule
    let mutable cData = 0u
    let mutable data = IntPtr.Zero
    let rv = ss.GetSourceServerData(&cData, &data)
    if rv < 0 then failwithf "GetSourceServerData failed with %d" rv
    printfn "got data, # of bytes: %d" cData

    let sn = IntPtr(Random().Next())
    let dllCookie = (int64) (dll.ToLower().GetHashCode() <<< 30)

    let b = SrcSrv.SrcSrvInit(sn, @"C:\temp")
    let moduleCookie = SrcSrv.SrcSrvLoadModule(sn, dll, dllCookie, data, cData)
    let sbUrl = StringBuilder 2048
    let a = SrcSrv.SrcSrvSetOptions 1u
    
    let gotUrl = SrcSrv.SrcSrvGetFile(sn, dllCookie, @"C:\Git\FSharp.Data SourceLink\src\Net\Http.fs", null, sbUrl, (uint32)sbUrl.Capacity)
    let url = sbUrl.ToString()
    printfn "gotUrl: %b" gotUrl
    printfn "url: %s" url
    let b = SymSrv.DownloadFile(url, @"C:\temp")
    ()

let printAssemblyInfo() =
    // http://weblog.ikvm.net/PermaLink.aspx?guid=d28a08bf-0476-41be-a923-60842fdaf8b0
    let all =
        BindingFlags.Public 
        ||| BindingFlags.NonPublic
        ||| BindingFlags.Instance
        ||| BindingFlags.Static
        ||| BindingFlags.DeclaredOnly
    use universe = new Universe()
    universe.add_AssemblyResolve(
        ResolveEventHandler(fun _ args ->
            universe.CreateMissingAssembly args.Name))
    let writeMembers (members:MemberInfo[]) =
        for m in members do
            printfn "  %d %A" m.MetadataToken m

    let dll = @"C:\Projects\Scout\packages\FSharp.Data.2.0.4\lib\net40\FSharp.Data.dll"
    let a = universe.LoadFile dll
//    a.ManifestModule.
    for t in a.GetTypes() do
        printfn "  %X %A" t.MetadataToken t
        for m in t.GetMembers all do
            printfn "    %x %A" m.MetadataToken m
//        for m in t.GetFields all do
//            printfn "    %X %A" m.MetadataToken m
    ()

[<EntryPoint>]
let main argv =
//    printPdbInfo()
    printUrlAndDownload()
//    printAssemblyInfo()
    0
