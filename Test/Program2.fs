module Program

open System.IO
open SourceLink
open SourceLink.SymbolStore

let printPdbDocuments() =
    let a = @"..\..\..\packages\SourceLink\lib\net45\SourceLink.pdb"
    use s = File.OpenRead a

    let sc = SymbolCache @"C:\tmp\cache"
    let r = sc.ReadPdb(s, a)

    for d in r.Documents do
        printfn "\npdb original source file path: %s" d.URL
        printfn "it had an md5 checksum of: %s" (d.GetCheckSum() |> Hex.encode)
        let url = r.GetDownloadUrl d.URL
        printfn "has download url if source indexed: %s" url
        let downloadedFile = sc.DownloadFile url
        printfn "downloaded the file to the cache %s" downloadedFile
        printfn "downloaded file has md5 of: %s" (Crypto.hashMD5 downloadedFile |> Hex.encode)

[<EntryPoint>]
let main argv =
    printPdbDocuments()
    0