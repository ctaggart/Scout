module Program2

open System.IO
open SourceLink
open SourceLink.SymbolStore

let printPdbDocuments() =
    let a = @"..\..\..\packages\SourceLink\lib\net45\SourceLink.pdb"
    use s = File.OpenRead a

    let sc = SymbolCache @"C:\tmp\cache"
    let r = sc.ReadPdb(s, a)

    for d in r.Documents do
        printfn "\n%s" d.URL
        printfn "  %s" (Hex.encode (d.GetCheckSum()))
        let url = r.GetDownloadUrl d.URL
        printfn "  %s" url
        let downloadedFile = sc.DownloadFile url
        printfn "  %s" downloadedFile
    ()

[<EntryPoint>]
let main argv =
    printPdbDocuments()
    0