using System;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;
using System.Text;

namespace ReSharper.Scout.DebugSymbols
{
	[
		ComImport,
		Guid("40DE4037-7C81-3E1E-B022-AE1ABFF2CA08"),
		InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
		ComVisible(false)
	]
	internal interface ISymUnmanagedDocument
	{
		void GetURL(int cchUrl,
		            out int pcchUrl,
		            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder szUrl);

		void GetDocumentType(ref Guid pRetVal);

		void GetLanguage(ref Guid pRetVal);

		void GetLanguageVendor(ref Guid pRetVal);

		void GetCheckSumAlgorithmId(ref Guid pRetVal);

		void GetCheckSum(int cData,
		                 out int pcData,
		                 byte[] date);

		void FindClosestLine(int line,
		                     out int pRetVal);

		void HasEmbeddedSource(out Boolean pRetVal);

		void GetSourceLength(out int pRetVal);

		void GetSourceRange(int startLine,
		                    int startColumn,
		                    int endLine,
		                    int endColumn,
		                    int cSourceBytes,
		                    out int pcSourceBytes,
		                    byte[] source);
	};

	[
		ComImport,
		Guid("997DD0CC-A76F-4c82-8D79-EA87559D27AD"),
		InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
		ComVisible(false)
	]
	internal interface ISymUnmanagedSourceServerModule
	{
		[PreserveSig]
		int GetSourceServerData(out uint dataByteCount, out IntPtr data);
	};

	[
		ComImport,
		Guid("B4CE6286-2A6B-3712-A3B7-1EE1DAD467B5"),
		InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
		ComVisible(false)
	]
	internal interface ISymUnmanagedReader
	{
		void GetDocument([MarshalAs(UnmanagedType.LPWStr)] String url,
		                 Guid language,
		                 Guid languageVendor,
		                 Guid documentType,
		                 [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedDocument retVal);

		void GetDocuments(int cDocs,
		                  out int pcDocs,
		                  [In, Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedDocument[] pDocs);


		void GetUserEntryPoint(out SymbolToken entryPoint);

		[PreserveSig]
		int GetMethod(SymbolToken methodToken,
		             [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedMethod retVal);
	}

	[
		ComImport,
		Guid("B62B923C-B500-3158-A543-24F307A8B7E1"),
		InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
		ComVisible(false)
	]
	internal interface ISymUnmanagedMethod
	{
		void GetToken(out SymbolToken pToken);
		void GetSequencePointCount(out int retVal);
		void GetRootScope([MarshalAs(UnmanagedType.Interface)] out object retVal);
		void GetScopeFromOffset(int offset, [MarshalAs(UnmanagedType.Interface)] out object retVal);

		void GetOffset(ISymUnmanagedDocument document,
		               int line,
		               int column,
		               out int retVal);

		void GetRanges(ISymUnmanagedDocument document,
		               int line,
		               int column,
		               int cRanges,
		               out int pcRanges,
		               [In, Out, MarshalAs(UnmanagedType.LPArray)] int[] ranges);

		void GetParameters(int cParams,
		                   out int pcParams,
		                   [In, Out, MarshalAs(UnmanagedType.LPArray)] object[] parms);

		void GetNamespace([MarshalAs(UnmanagedType.Interface)] out object retVal);

		void GetSourceStartEnd(ISymUnmanagedDocument[] docs,
		                       [In, Out, MarshalAs(UnmanagedType.LPArray)] int[] lines,
		                       [In, Out, MarshalAs(UnmanagedType.LPArray)] int[] columns,
		                       out Boolean retVal);

		void GetSequencePoints(int cPoints,
		                       out int pcPoints,
		                       [In, Out, MarshalAs(UnmanagedType.LPArray)] int[] offsets,
		                       [In, Out, MarshalAs(UnmanagedType.LPArray)] ISymUnmanagedDocument[] documents,
		                       [In, Out, MarshalAs(UnmanagedType.LPArray)] int[] lines,
		                       [In, Out, MarshalAs(UnmanagedType.LPArray)] int[] columns,
		                       [In, Out, MarshalAs(UnmanagedType.LPArray)] int[] endLines,
		                       [In, Out, MarshalAs(UnmanagedType.LPArray)] int[] endColumns);
	}

	[
		ComImport,
		Guid("ACCEE350-89AF-4ccb-8B40-1C2C4C6F9434"),
		InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
		ComVisible(false)
	]
	internal interface ISymUnmanagedBinder2
	{
		[PreserveSig]
		int GetReaderForFile([MarshalAs(UnmanagedType.Interface)] object importer,
		                     [MarshalAs(UnmanagedType.LPWStr)] string name,
		                     [MarshalAs(UnmanagedType.LPWStr)] string path,
		                     [MarshalAs(UnmanagedType.Interface)] out object reader);

		[PreserveSig]
		int GetReaderFromStream([MarshalAs(UnmanagedType.Interface)] object importer,
		                        IntPtr stream,
		                        [MarshalAs(UnmanagedType.Interface)] out object reader);

		[PreserveSig]
		int GetReaderForFile2([MarshalAs(UnmanagedType.IUnknown)] object importer, [MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] string path, int policy, [MarshalAs(UnmanagedType.Interface)] out ISymUnmanagedReader reader);
	}
}