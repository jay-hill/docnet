﻿using System;
using System.Linq;
using Docnet.Core.Exceptions;
using Docnet.Core.Models;
using Docnet.Tests.Integration.Utils;
using Xunit;

namespace Docnet.Tests.Integration
{
    [Collection("Lib collection")]
    public sealed class DocReaderTests
    {
        private readonly LibFixture _fixture;

        public DocReaderTests(LibFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void GetDocReader_WhenCalledWithNullFilePath_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => _fixture.Lib.GetDocReader((string)null, null, new PageDimensions(10, 10)));
        }

        [Fact]
        public void GetDocReader_WhenCalledWithNullBytes_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => _fixture.Lib.GetDocReader((byte[])null, null, new PageDimensions(10, 10)));
        }

        [Theory]
        [InlineData(Input.FromBytes)]
        [InlineData(Input.FromFile)]
        public void GetDocReader_WhenCalledWithInvalidFileData_ShouldThrow(Input type)
        {
            Assert.Throws<DocnetException>(() => _fixture.GetDocReader(type, "Docs/protected_0.pdf", null, 10, 10));
        }

        [Theory]
        [InlineData(Input.FromBytes, 0, 0)]
        [InlineData(Input.FromFile, 0, 0)]
        [InlineData(Input.FromBytes, 2, 1)]
        [InlineData(Input.FromFile, 2, 1)]
        [InlineData(Input.FromBytes, -1, 3)]
        [InlineData(Input.FromFile, -1, 3)]
        [InlineData(Input.FromBytes, 0, -10)]
        [InlineData(Input.FromFile, 0, -10)]
        [InlineData(Input.FromBytes, -20, -10)]
        [InlineData(Input.FromFile, -20, -10)]
        public void GetDocReader_WhenCalledWithInvalidDimensions_ShouldThrow(Input type, int dimOne, int dimTwo)
        {
            Assert.Throws<ArgumentException>(() => _fixture.GetDocReader(type, "Docs/simple_0.pdf", null, dimOne, dimTwo));
        }

        [Theory]
        [InlineData(Input.FromFile, "Docs/simple_0.pdf", null, 19)]
        [InlineData(Input.FromBytes, "Docs/simple_0.pdf", null, 19)]
        [InlineData(Input.FromFile, "Docs/simple_1.pdf", null, 5)]
        [InlineData(Input.FromBytes, "Docs/simple_1.pdf", null, 5)]
        [InlineData(Input.FromFile, "Docs/simple_2.pdf", null, 10)]
        [InlineData(Input.FromBytes, "Docs/simple_2.pdf", null, 10)]
        [InlineData(Input.FromFile, "Docs/simple_3.pdf", null, 2)]
        [InlineData(Input.FromBytes, "Docs/simple_3.pdf", null, 2)]
        [InlineData(Input.FromFile, "Docs/protected_0.pdf", "password", 3)]
        [InlineData(Input.FromBytes, "Docs/protected_0.pdf", "password", 3)]
        public void GetPageCount_WhenCalled_ShouldReturnCorrectResults(Input type, string filePath, string password, int expectedCount)
        {
            using (var reader = _fixture.GetDocReader(type, filePath, password, 10, 10))
            {
                var count = reader.GetPageCount();

                Assert.Equal(expectedCount, count);
            }
        }

        [Theory]
        [InlineData(Input.FromFile, "Docs/simple_0.pdf", null, 17)]
        [InlineData(Input.FromBytes, "Docs/simple_0.pdf", null, 17)]
        [InlineData(Input.FromFile, "Docs/simple_1.pdf", null, 13)]
        [InlineData(Input.FromBytes, "Docs/simple_1.pdf", null, 13)]
        [InlineData(Input.FromFile, "Docs/simple_2.pdf", null, 12)]
        [InlineData(Input.FromBytes, "Docs/simple_2.pdf", null, 12)]
        [InlineData(Input.FromFile, "Docs/simple_3.pdf", null, 13)]
        [InlineData(Input.FromBytes, "Docs/simple_3.pdf", null, 13)]
        [InlineData(Input.FromFile, "Docs/protected_0.pdf", "password", 17)]
        [InlineData(Input.FromBytes, "Docs/protected_0.pdf", "password", 17)]
        public void GetPdfVersion_WhenCalled_ShouldReturnCorrectResults(Input type, string filePath, string password, int expectedVersion)
        {
            using (var reader = _fixture.GetDocReader(type, filePath, password, 10, 10))
            {
                var version = reader.GetPdfVersion();

                Assert.Equal(expectedVersion, version.Number);
            }
        }

        [Theory]
        [InlineData(Input.FromFile, "Docs/simple_0.pdf", null, "Title", "PDF - Wikipedia")]
        [InlineData(Input.FromBytes, "Docs/simple_0.pdf", null, "Title", "PDF - Wikipedia")]
        [InlineData(Input.FromFile, "Docs/simple_1.pdf", null, "Title", "PDF")]
        [InlineData(Input.FromBytes, "Docs/simple_1.pdf", null, "Title", "PDF")]
        [InlineData(Input.FromFile, "Docs/simple_2.pdf", null, "Title", "")]
        [InlineData(Input.FromBytes, "Docs/simple_2.pdf", null, "Title", "")]
        [InlineData(Input.FromFile, "Docs/simple_3.pdf", null, "Title", "")]
        [InlineData(Input.FromBytes, "Docs/simple_3.pdf", null, "Title", "")]
        [InlineData(Input.FromFile, "Docs/protected_0.pdf", "password", "Title", "Secret (disambiguation) - Wikipedia")]
        [InlineData(Input.FromBytes, "Docs/protected_0.pdf", "password", "Title", "Secret (disambiguation) - Wikipedia")]
        public void GetMetaText_WhenCalled_ShouldReturnCorrectResults(Input type, string filePath, string password, string tag, string expectedValue)
        {
            using (var reader = _fixture.GetDocReader(type, filePath, password, 10, 10))
            {
                var value = reader.GetMetaText(tag);
                Assert.Equal(expectedValue, value);
            }
        }

        [Theory]
        [InlineData(Input.FromFile, "Docs/simple_0.pdf", null, new string[] { })]
        [InlineData(Input.FromBytes, "Docs/simple_0.pdf", null, new string[] { })]
        [InlineData(Input.FromFile, "Docs/simple_1.pdf", null, new string[] { "Introduction to the Pdf995 Suite", "Sample Word document with links", "Excel Chart and Spreadsheet" })]
        [InlineData(Input.FromBytes, "Docs/simple_1.pdf", null, new string[] { "Introduction to the Pdf995 Suite", "Sample Word document with links", "Excel Chart and Spreadsheet" })]
        [InlineData(Input.FromFile, "Docs/simple_2.pdf", null, new string[] { "Template" })]
        [InlineData(Input.FromBytes, "Docs/simple_2.pdf", null, new string[] { "Template" })]
        [InlineData(Input.FromFile, "Docs/simple_3.pdf", null, new string[] { })]
        [InlineData(Input.FromBytes, "Docs/simple_3.pdf", null, new string[] { })]
        [InlineData(Input.FromFile, "Docs/protected_0.pdf", "password", new string[] { })]
        [InlineData(Input.FromBytes, "Docs/protected_0.pdf", "password", new string[] { })]
        public void GetBookmarks_WhenCalled_ShouldReturnCorrectResults(Input type, string filePath, string password, string[] titles)
        {
            using (var reader = _fixture.GetDocReader(type, filePath, password, 10, 10))
            {
                var bookmarks = reader.GetBookmarks();
                Assert.Equal(titles.Length, bookmarks.Count);

                foreach (string bookmarkTitle in bookmarks.Select(n => n.Title))
                {
                    Assert.Contains(bookmarkTitle, titles);
                }
            }
        }
    }
}