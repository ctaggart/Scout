// Sample to demonstrate simple bare-bones mapping from method/IL offset to source locations
// while controlling how PDB files are located.  
// Written by Rick Byers - http://blogs.msdn.com/rmbyers
// 8/31/2009 - First release

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Microsoft.Samples.SimplePDBReader
{
    /// <summary>
    /// A SymbolProvider gets source locations given method/IL-offset pairs.
    /// The underlying readers are cached per module, but the cache can be cleared by calling Dispose
    /// </summary>
    public class SymbolProvider : IDisposable
    {
        /// <summary>
        /// Create a new symbol provider.
        /// Note that the symbol provider will cache the symbol readers it creates
        /// </summary>
        /// <param name="searchPath">A semicolon separated list of paths to search for a PDB file</param>
        /// <param name="searchPolicy">Flags which specify where else to search</param>
        public SymbolProvider(string searchPath, SymSearchPolicies searchPolicy)
        {
            m_searchPath = searchPath;
            m_searchPolicy = searchPolicy;

            // Create a metadata dispenser and symbol binder via COM interop to use for all modules
            m_metadataDispenser = new IMetaDataDispenser();
            m_symBinder = new ISymUnmanagedBinder2();
        }

        /// <summary>
        /// Description of a location in a source file
        /// </summary>
        public class SourceLoc
        {
            public SourceLoc(string url, int startLine, int endLine, int startCol, int endCol)
            {
                Url = url;
                StartLine = startLine;
                EndLine = endLine;
                StartCol = startCol;
                EndCol = endCol;
            }

            public readonly string Url;
            public readonly int StartLine;
            public readonly int EndLine;
            public readonly int StartCol;
            public readonly int EndCol;
        }

        /// <summary>
        /// Get a string representing the source location for the given IL offset and method
        /// </summary>
        /// <param name="method">The method of interest</param>
        /// <param name="ilOffset">The offset into the IL</param>
        /// <returns>A string of the format [filepath]:[line] (eg. "C:\temp\foo.cs:123"), or null
        /// if a matching PDB couldn't be found</returns>
        /// <remarks>Thows various COMExceptions (from DIA SDK error values) if a PDB couldn't be opened/read</remarks>
        public SourceLoc GetSourceLoc(MethodBase method, int ilOffset)
        {
            // Get the symbol reader corresponding to the module of the supplied method
            string modulePath = method.Module.FullyQualifiedName;
            ISymUnmanagedReader symReader = GetSymbolReaderForFile(modulePath);
            if (symReader == null)
                return null;    // no PDBs

            ISymUnmanagedMethod symMethod = symReader.GetMethod(new SymbolToken(method.MetadataToken));

            // Get all the sequence points for the method
            int count = symMethod.GetSequencePointCount();
            ISymUnmanagedDocument[] docs = new ISymUnmanagedDocument[count];
            int[] startLines = new int[count];
            int[] ilOffsets = new int[count];
            int[] endLines = new int[count];
            int[] startCols = new int[count];
            int[] endCols = new int[count];
            int outPoints;
            symMethod.GetSequencePoints(count, out outPoints, ilOffsets, docs, startLines, startCols, endLines, endCols);

            // Find the closest sequence point to the requested offset
            // Sequence points are returned sorted by offset so we're looking for the last one with
            // an offset less than or equal to the requested offset. 
            // Note that this won't necessarily match the real source location exactly if 
            // the code was jit-compiled with optimizations.
            int i;
            for (i = 0; i < count; i++)
            {
                if (ilOffsets[i] > ilOffset)
                    break;
            }
            // Found the first mismatch, back up if it wasn't the first
            if (i > 0)
                i--;

            // Now return the source file and line number for this sequence point
            StringBuilder url = new StringBuilder(512);
            int len;
            docs[i].GetURL(url.Capacity, out len, url);

            return new SourceLoc(url.ToString(), startLines[i], endLines[i], startCols[i], endCols[i]);
        }

        /// <summary>
        /// Create a symbol reader object corresponding to the specified module (DLL/EXE)
        /// </summary>
        /// <param name="modulePath">Full path to the module of interest</param>
        /// <returns>A symbol reader object, or null if no matching PDB symbols can located</returns>
        private ISymUnmanagedReader CreateSymbolReaderForFile(string modulePath)
        {
            // First we need to get a metadata importer for the module to provide to the symbol reader
            Guid importerIID = typeof(IMetaDataImport).GUID;
            IMetaDataImport importer = m_metadataDispenser.OpenScope(modulePath, 0, ref importerIID);

            // Call ISymUnmanagedBinder2.GetReaderForFile2 to load the PDB file (if any)
            // Note that ultimately how this PDB file is located is determined by
            // IDiaDataSource::loadDataForExe.  See the DIA SDK documentation for details.
            ISymUnmanagedReader reader = null;
            int hr = m_symBinder.GetReaderForFile2(importer, modulePath, m_searchPath, m_searchPolicy, out reader);

            // If the PDB couldn't be found (very common case), then just return null
            if (hr == (int)DiaErrors.E_PDB_NOT_FOUND)
                return null;    // Note that reader may not be null!

            // Throw an exception for any other error-code
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);

            return reader;
        }

        /// <summary>
        /// Get or create a symbol reader for the specified module (caching the result)
        /// </summary>
        /// <param name="modulePath">Full path to the module of interest</param>
        /// <returns>A symbol reader for the specified module or null if none could be found</returns>
        private ISymUnmanagedReader GetSymbolReaderForFile(string modulePath)
        {
            ISymUnmanagedReader reader;
            lock (m_symReaders)
            {
                if (!m_symReaders.TryGetValue(modulePath, out reader))
                {
                    reader = CreateSymbolReaderForFile(modulePath);
                    m_symReaders.Add(modulePath, reader);
                }
            }
            return reader;
        }

        /// <summary>
        /// IDisposable implementation
        /// Note that the reader will release file handles eventually, so this isn't strictly necessary.
        /// Therefore I don't bother with a finalizer.  This method is mainly usefull to ensure determinstic
        /// file closing.
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Explicitly dispose each reader in our cache
                foreach (var reader in m_symReaders.Values)
                {
                    ((ISymUnmanagedDispose)reader).Destroy();
                }
            }
            finally
            {
                // Make sure we don't keep disposed readers around
                m_symReaders.Clear();
            }
        }

        private IMetaDataDispenser m_metadataDispenser;
        private ISymUnmanagedBinder2 m_symBinder;
        private string m_searchPath;
        private SymSearchPolicies m_searchPolicy;

        // Map from module path to symbol reader
        private Dictionary<string, ISymUnmanagedReader> m_symReaders = new Dictionary<string, ISymUnmanagedReader>();

        [Flags]
        public enum SymSearchPolicies : int
        {
            // query the registry for symbol search paths
            AllowRegistryAccess = 1,

            // access a symbol server
            AllowSymbolServerAccess = 2,

            // Look at the path specified in Debug Directory
            AllowOriginalPathAccess = 4,

            // look for PDB in the place where the exe is.
            AllowReferencePathAccess = 8,
        }

        // The most interesting values from Dia2.h in the DIA SDK
        public enum DiaErrors : int
        {
            E_PDB_USAGE = unchecked((int)0x806d0002),
            E_PDB_NOT_FOUND = unchecked((int)0x806d0005)
        }

        // Below are COM-interop definitions (to avoid having to reference an interop assembly and easily adjust them)
        // These are mostly adapted from the MDbg managed debugging sample (which also adds another layer that makes the
        // APIs a bit more managed-friendly).
        #region COM-interop definitions

        // Bare bones COM-interop definition of the IMetaDataDispenser API
        [ComImport, Guid("809c652e-7396-11d2-9771-00a0c9b4d50c"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), CoClass(typeof(CorMetaDataDispenser))]
        private interface IMetaDataDispenser
        {
            // We need to be able to call OpenScope, which is the 2nd vtable slot.
            // Thus we need this one placeholder here to occupy the first slot..
            void DefineScope_Placeholder();

            IMetaDataImport OpenScope([In, MarshalAs(UnmanagedType.LPWStr)] String szScope, [In] Int32 dwOpenFlags, [In] ref Guid riid);

            // Don't need any other methods.
        }

        [ComImport, Guid("e5cb7a31-7512-11d2-89ce-0080c792e5d8")]
        private class CorMetaDataDispenser
        {
        }

        // Since we're just blindly passing this interface through managed code to the Symbinder, we don't care about actually
        // importing the specific methods.
        // This needs to be public so that we can call Marshal.GetComInterfaceForObject() on it to get the
        // underlying metadata pointer.
        [ComImport, Guid("7DAC8207-D3AE-4c75-9B67-92801A497D44"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IMetaDataImport
        {
            // Just need a single placeholder method so that it doesn't complain about an empty interface.
            void Placeholder();
        }

        [ComImport, Guid("ACCEE350-89AF-4ccb-8B40-1C2C4C6F9434"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), CoClass(typeof(CorSymBinder))]
        private interface ISymUnmanagedBinder2
        {
            // ISymUnmanagedBinder methods 
            // Note that it's common for these to fail with E_PDB_NOT_FOUND, so we don't convert all failures to 
            // exceptions automatically
            [PreserveSig]
            int GetReaderForFile(IMetaDataImport importer,
                                      [MarshalAs(UnmanagedType.LPWStr)] String filename,
                                      [MarshalAs(UnmanagedType.LPWStr)] String SearchPath,
                                      out ISymUnmanagedReader reader);

            [PreserveSig]
            int GetReaderFromStream(IMetaDataImport importer,
                                            IStream stream,
                                            out ISymUnmanagedReader reader);

            // ISymUnmanagedBinder2 methods 
            [PreserveSig]
            int GetReaderForFile2(IMetaDataImport importer,
                                      [MarshalAs(UnmanagedType.LPWStr)] String fileName,
                                      [MarshalAs(UnmanagedType.LPWStr)] String searchPath,
                                      SymSearchPolicies searchPolicy,
                                      out ISymUnmanagedReader reader);
        }

        [ComImport, Guid("0A29FF9E-7F9C-4437-8B11-F424491E3931")]
        private class CorSymBinder
        {
        }

        [ComImport, Guid("B4CE6286-2A6B-3712-A3B7-1EE1DAD467B5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ISymUnmanagedReader
        {
            ISymUnmanagedDocument GetDocument([MarshalAs(UnmanagedType.LPWStr)] String url,
                                  Guid language,
                                  Guid languageVendor,
                                  Guid documentType);

            void GetDocuments(int cDocs,
                                   out int pcDocs,
                                   [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedDocument[] pDocs);


            SymbolToken GetUserEntryPoint();

            ISymUnmanagedMethod GetMethod(SymbolToken methodToken);

            ISymUnmanagedMethod GetMethodByVersion(SymbolToken methodToken,
                                          int version);

            void GetVariables(SymbolToken parent,
                                int cVars,
                                out int pcVars,
                                [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] /*ISymUnmanagedVariable*/ object[] vars);

            void GetGlobalVariables(int cVars,
                                        out int pcVars,
                                        [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] /*ISymUnmanagedVariable*/ object[] vars);


            ISymUnmanagedMethod GetMethodFromDocumentPosition(ISymUnmanagedDocument document,
                                                  int line,
                                                  int column);

            void GetSymAttribute(SymbolToken parent,
                                    [MarshalAs(UnmanagedType.LPWStr)] String name,
                                    int sizeBuffer,
                                    out int lengthBuffer,
                                    [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] buffer);

            void GetNamespaces(int cNameSpaces,
                                    out int pcNameSpaces,
                                    [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] /*ISymUnmanagedNamespace*/ object[] namespaces);

            void Initialize(IntPtr importer,
                           [MarshalAs(UnmanagedType.LPWStr)] String filename,
                           [MarshalAs(UnmanagedType.LPWStr)] String searchPath,
                           IStream stream);

            void UpdateSymbolStore([MarshalAs(UnmanagedType.LPWStr)] String filename,
                                         IStream stream);

            void ReplaceSymbolStore([MarshalAs(UnmanagedType.LPWStr)] String filename,
                                          IStream stream);

            void GetSymbolStoreFileName(int cchName,
                                               out int pcchName,
                                               [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);

            void GetMethodsFromDocumentPosition(ISymUnmanagedDocument document,
                                                          int line,
                                                          int column,
                                                          int cMethod,
                                                          out int pcMethod,
                                                          [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] ISymUnmanagedMethod[] pRetVal);

            void GetDocumentVersion(ISymUnmanagedDocument pDoc,
                                          out int version,
                                          out Boolean pbCurrent);

            int GetMethodVersion(ISymUnmanagedMethod pMethod);
        };

        [ComImport, Guid("969708D2-05E5-4861-A3B0-96E473CDF63F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface ISymUnmanagedDispose
        {
            void Destroy();
        }

        [ComImport, Guid("40DE4037-7C81-3E1E-B022-AE1ABFF2CA08"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ISymUnmanagedDocument
        {
            void GetURL(int cchUrl,
                           out int pcchUrl,
                           [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szUrl);

            void GetDocumentType(ref Guid pRetVal);

            void GetLanguage(ref Guid pRetVal);

            void GetLanguageVendor(ref Guid pRetVal);

            void GetCheckSumAlgorithmId(ref Guid pRetVal);

            void GetCheckSum(int cData,
                                  out int pcData,
                                  [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] data);

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
                                     [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] byte[] source);

        };

        [ComImport, Guid("B62B923C-B500-3158-A543-24F307A8B7E1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ISymUnmanagedMethod
        {
            SymbolToken GetToken();
            int GetSequencePointCount();
            ISymUnmanagedScope GetRootScope();
            ISymUnmanagedScope GetScopeFromOffset(int offset);
            int GetOffset(ISymUnmanagedDocument document,
                             int line,
                             int column);
            void GetRanges(ISymUnmanagedDocument document,
                              int line,
                              int column,
                              int cRanges,
                              out int pcRanges,
                              [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] int[] ranges);
            void GetParameters(int cParams,
                                  out int pcParams,
                                  [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] parms);
            ISymUnmanagedNamespace GetNamespace();
            void GetSourceStartEnd(ISymUnmanagedDocument[] docs,
                                      [In, Out, MarshalAs(UnmanagedType.LPArray)] int[] lines,
                                      [In, Out, MarshalAs(UnmanagedType.LPArray)] int[] columns,
                                      out Boolean retVal);
            void GetSequencePoints(int cPoints,
                                      out int pcPoints,
                                      [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] offsets,
                                      [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedDocument[] documents,
                                      [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] lines,
                                      [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] columns,
                                      [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] endLines,
                                      [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] int[] endColumns);
        }

        [ComImport, Guid("68005D0F-B8E0-3B01-84D5-A11A94154942"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ISymUnmanagedScope
        {
            ISymUnmanagedMethod GetMethod();

            ISymUnmanagedScope GetParent();

            void GetChildren(int cChildren,
                                out int pcChildren,
                                [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedScope[] children);

            int GetStartOffset();

            int GetEndOffset();

            int GetLocalCount();

            void GetLocals(int cLocals,
                              out int pcLocals,
                              [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] locals);

            void GetNamespaces(int cNameSpaces,
                                  out int pcNameSpaces,
                                  [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedNamespace[] namespaces);
        };

        [ComImport, Guid("9F60EEBE-2D9A-3F7C-BF58-80BC991C60BB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ISymUnmanagedVariable
        {
            void GetName(int cchName,
                            out int pcchName,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);

            int GetAttributes();

            void GetSignature(int cSig,
                                 out int pcSig,
                                 [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] sig);

            int GetAddressKind();

            int GetAddressField1();

            int GetAddressField2();

            int GetAddressField3();

            int GetStartOffset();

            int GetEndOffset();
        }

        [ComImport, Guid("0DFF7289-54F8-11d3-BD28-0000F80849BD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ISymUnmanagedNamespace
        {
            void GetName(int cchName,
                            out int pcchName,
                            [MarshalAs(UnmanagedType.LPWStr)] StringBuilder szName);

            void GetNamespaces(int cNameSpaces,
                                    out int pcNameSpaces,
                                    [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedNamespace[] namespaces);

            void GetVariables(int cVars,
                                 out int pcVars,
                                 [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] ISymUnmanagedVariable[] pVars);
        }

        #endregion
    }
}