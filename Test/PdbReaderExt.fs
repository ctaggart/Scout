[<AutoOpen>]
module SourceLink.PdbReaderExt

open SourceLink.AbstractIL.Support

type PdbDocument with
    member x.URL with get() = pdbDocumentGetURL x
    member x.DocumentType with get() = pdbDocumentGetType x
    member x.Language with get() = pdbDocumentGetLanguage x
    member x.LanguageVender with get() = pdbDocumentGetLanguageVendor x
    member x.FindClosestLine line = pdbDocumentFindClosestLine x line
//    member x.CheckSum with get() = x.symDocument.GetCheckSum() // NotImplementedException

type PdbMethod with
    member x.Token with get() = pdbMethodGetToken
    member x.SequencePoints with get() = pdbMethodGetSequencePoints x
    member x.RootScope with get() = pdbMethodGetRootScope x

type PdbMethodScope with
    member x.Children with get() = pdbScopeGetChildren x
    member x.Offsets with get() = pdbScopeGetOffsets x
    member x.Locals with get() = pdbScopeGetLocals x

type PdbVariable with
    member x.Name with get() = pdbVariableGetName x
    member x.Signature pdbVariableGetSignature = pdbVariableGetSignature x
    member x.AddressAttributes = pdbVariableGetAddressAttributes x