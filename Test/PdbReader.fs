namespace SourceLink.SymbolStore

//type FsPdbReader = SourceLink.AbstractIL.Support.PdbReader

//open SourceLink.AbstractIL.Support
open System.IO
open System
open System.Text

//open ReSharper.Scout.DebugSymbols // TODO
//open Roslyn.Test.PdbUtilities
//open SourceLink.SymbolStore



// TODO split off to SrcSrv and/or PdbSourceCache
type PdbReader (sessionCookie:IntPtr, moduleCookie:int64, reader:TempPdbReader) =
    
//    let reader =
//        let dllFull = Path.GetFullPath pdb
//        let pdb = Path.ChangeExtension(dllFull, ".pdb")
//        pdbReadOpen dllFull pdb

//    let sourceServer = reader.symReader :? ISymUnmanagedSourceServerModule

    

//    interface IDisposable with
//        member x.Dispose() =
//            pdbReadClose reader
//            ()

    member x.Documents
        with get() =
//          pdbReaderGetDocuments reader
            reader.SymbolReader.GetDocuments()

    member x.GetDownloadUrl sourceFilePath =
//        let sbUrl = StringBuilder 2048
//        SrcSrv.SrcSrvGetFile(sessionCookie, moduleCookie, documentUrl, null, sbUrl, (uint32)sbUrl.Capacity) |> ignore // TODO
//        sbUrl.ToString()
        SrcSrv.GetFileUrl(sessionCookie, moduleCookie, sourceFilePath)


type SymbolCache (symbolCacheDir:string) =
    let sessionCookie = IntPtr(Random().Next())
    do SrcSrv.Init(sessionCookie, symbolCacheDir)

    member x.DownloadFile url =
        SymSrv.DownloadFile(url, symbolCacheDir)

    member x.ReadPdb fileName stream =
        
        let reader = TempPdbReader.Create stream

//        let mutable cData = 0u
//        let mutable data = IntPtr.Zero
//
//        let mutable cData = 0u
//        let mutable data = IntPtr.Zero
//        let ss = reader.SymUnmanagedSourceServerModule
//        let rv = ss.GetSourceServerData(&cData, &data)
//        if rv < 0 then failwithf "GetSourceServerData failed with %d" rv
//        printfn "got data, # of bytes: %d" cData // got data, # of bytes: 1451
//
//        let moduleCookie = int64 (Random().Next()) //(pdb.ToLower().GetHashCode() <<< 30)
//
//        let moduleFileName = "a" // anything appears to work, but must not be empty or null
//        let r = SrcSrv.SrcSrvLoadModule(sessionCookie, moduleFileName, moduleCookie, data, cData)
//        printfn "SrcSrvLoadModule: %b" r 

        let moduleCookie = SrcSrv.LoadModule(sessionCookie, fileName, reader.SymUnmanagedSourceServerModule)
        printfn "moduleCookie: %d" moduleCookie
        PdbReader(sessionCookie, moduleCookie, reader)