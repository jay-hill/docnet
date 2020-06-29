using Docnet.Core.Bindings;
using Docnet.Core.Exceptions;
using Docnet.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
namespace Docnet.Core.Readers
{
    internal class DocReader : IDocReader
    {
        private readonly DocumentWrapper _docWrapper;
        private readonly PageDimensions _dimensions;

        public DocReader(string filePath, string password, PageDimensions dimensions)
        {
            _dimensions = dimensions;

            lock (DocLib.Lock)
            {
                _docWrapper = new DocumentWrapper(filePath, password);
            }
        }

        public DocReader(byte[] bytes, string password, PageDimensions dimensions)
        {
            _dimensions = dimensions;

            lock (DocLib.Lock)
            {
                _docWrapper = new DocumentWrapper(bytes, password);
            }
        }

        /// <inheritdoc />
        public PdfVersion GetPdfVersion()
        {
            var version = 0;

            lock (DocLib.Lock)
            {
                var success = fpdf_view.FPDF_GetFileVersion(_docWrapper.Instance, ref version) == 1;

                if (!success)
                {
                    throw new DocnetException("failed to get pdf version");
                }
            }

            return new PdfVersion(version);
        }

        /// <inheritdoc />
        public int GetPageCount()
        {
            lock (DocLib.Lock)
            {
                return fpdf_view.FPDF_GetPageCount(_docWrapper.Instance);
            }
        }

        /// <inheritdoc />
        public IPageReader GetPageReader(int pageIndex)
        {
            return new PageReader(_docWrapper, pageIndex, _dimensions);
        }

        public string GetMetaText(string tag)
        {
            // Length includes a trailing \0.
            uint length = fpdf_view.FPDF_GetMetaText(_docWrapper.Instance, tag, null, 0);

            if (length <= 2)
            {
                return string.Empty;
            }

            byte[] buffer = new byte[length];
            fpdf_view.FPDF_GetMetaText(_docWrapper.Instance, tag, buffer, length);

            return System.Text.Encoding.Unicode.GetString(buffer, 0, (int)(length - 2));
        }

        public PdfBookmarkCollection GetBookmarks()
        {
            var bookmarks = new PdfBookmarkCollection();
            var rootBookmark = FpdfBookmarkT.__CreateInstance(IntPtr.Zero);
            LoadBookmarks(bookmarks, fpdf_view.FPDFBookmark_GetFirstChild(_docWrapper.Instance, rootBookmark));
            return bookmarks;
        }

        private void LoadBookmarks(PdfBookmarkCollection bookmarks, FpdfBookmarkT bookmark)
        {
            if (bookmark.__Instance == IntPtr.Zero)
            {
                return;
            }

            var ptrs = new List<IntPtr>();

            bookmarks.Add(LoadBookmark(bookmark));

            while ((bookmark = fpdf_view.FPDFBookmark_GetNextSibling(_docWrapper.Instance, bookmark)).__Instance != IntPtr.Zero)
            {
                if (ptrs.Contains(bookmark.__Instance))
                {
                    // avoid infinite bookmark loop problem
                    break;
                }

                ptrs.Add(bookmark.__Instance);
                bookmarks.Add(LoadBookmark(bookmark));
            }
        }

        private PdfBookmark LoadBookmark(FpdfBookmarkT bookmark)
        {
            var result = new PdfBookmark
            {
                Title = GetBookmarkTitle(bookmark),
                PageIndex = (int)GetBookmarkPageIndex(bookmark)
            };

            var child = fpdf_view.FPDFBookmark_GetFirstChild(_docWrapper.Instance, bookmark);

            if (child.__Instance != IntPtr.Zero)
            {
                LoadBookmarks(result.Children, child);
            }

            return result;
        }

        private static string GetBookmarkTitle(FpdfBookmarkT bookmark)
        {
            uint length = fpdf_view.FPDFBookmark_GetTitle(bookmark, null, 0);
            var buffer = new byte[length];
            fpdf_view.FPDFBookmark_GetTitle(bookmark, buffer, length);

            string result = Encoding.Unicode.GetString(buffer);
            if (result.Length > 0 && result[result.Length - 1] == 0)
            {
                result = result.Substring(0, result.Length - 1);
            }

            return result;
        }

        private uint GetBookmarkPageIndex(FpdfBookmarkT bookmark)
        {
            var dest = fpdf_view.FPDFBookmark_GetDest(_docWrapper.Instance, bookmark);

            if (dest.__Instance != IntPtr.Zero)
            {
                return fpdf_view.FPDFDest_GetPageIndex(_docWrapper.Instance, dest);
            }

            return 0;
        }

        public void Dispose()
        {
            lock (DocLib.Lock)
            {
                _docWrapper?.Dispose();
            }
        }
    }
}