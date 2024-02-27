using System.Text.RegularExpressions;

namespace WildcardsUtilities.Tests;

public class FilesFilteringTests
{
    [Theory]
    [InlineData("*.txt", @"^[/]?[^/]*\.txt$")]
    [InlineData("/i*.??html", @"^[/]?i[^/]*\.[^/]?[^/]?html$")]
    [InlineData("!*.*", @"^[/]?[^/]*\.[^/]*$")]
    public void ToRegex_should_work(string filter, string expectedRegex)
    {
        FilesFiltering
            .ToRegex(filter)
            .ToString()
            .Should()
            .Be(expectedRegex);
    }

    [Theory]
    [InlineData("*.txt", "/file.txt", true)]
    [InlineData("*.txt", "file.txtx", false)]
    [InlineData("/i*.??html", "index.cshtml", true)]
    [InlineData("/i*.??html", "/index.xhtml", true)]
    [InlineData("/i*.??html", "index.html", true)]
    [InlineData("/i*.??html", "index.htm", false)]
    [InlineData("/i*.??html", "index.abchtml", false)]
    [InlineData("!*.*", "ciao", true)]
    [InlineData("!*.*", "file.txt", false)]
    [InlineData("!*.*", ".gitignore", false)]
    public void Path_should_or_should_not_match_filter(string filter, string path, bool expected)
    {
        if (filter.StartsWith("!"))
            expected = !expected;

        FilesFiltering
            .ToRegex(filter)
            .IsMatch(path)
            .Should()
            .Be(expected);
    }

    public static readonly IEnumerable<object[]> GetFiles_should_work_DATA =
    [
        [
            new string[]
            {
                "foo/*.*",
                "!foo/I*.*"
            },

            new string[]
            {
                "foo/file.cs",
                "foo/file.fs",
                "foo/test.txt",
                "foo/test_0.txt",
            }
        ],
        [
            new string[]
            {
                "foo/*.*"
            },

            new string[]
            {
                "foo/file.cs",
                "foo/file.fs",
                "foo/test.txt",
                "foo/test_0.txt",
                "foo/Index.cshtml.css"
            }
        ],
        [
            new string[]
            {
                "foo/*.*",
                "!foo/I*.*",
                "**/test_?.txt",
            },

            new string[]
            {
                "foo/file.cs",
                "foo/file.fs",
                "foo/test.txt",
                "foo/test_0.txt",
                "Hello/World/test_2.txt",
                "Hxhxhx/test_2.txt",
                "test_0.txt",
                "test_1.txt",
                "test_2.txt"
            }
        ],
        [
            new string[]
            {
                "foo/*.*",
                "!foo/I*.*",
                "**/test_?.txt",
            },

            new string[]
            {
                "foo/file.cs",
                "foo/file.fs",
                "foo/test.txt",
                "foo/test_0.txt",
                "Hello/World/test_2.txt",
                "Hxhxhx/test_2.txt",
                "test_0.txt",
                "test_1.txt",
                "test_2.txt"
            }
        ],
        [
            new string[]
            {
                "Hello/**/archive?*",
                "!H*/**/*fhwe*",
                "**/*hx*/t*_*.txt"
            },

            new string[]
            {
                "Hello/World/archive.zip",
                "Hello/World/archive_yfewq78fgue.zip",
                "Hxhxhx/test_2.txt"
            }
        ],
        [

            new string[]
            {
                "Hello/**/World/*.html"
            },

            new string[]
            {
                "Hello/Brother/Hi/World/World.html",
                "Hello/World/index.html"
            }
        ],
        [

            new string[]
            {
                "Hello/**/World/*.html",
                "!Hello/**/*/World/*.html"
            },

            new string[]
            {
                "Hello/World/index.html"
            }
        ],
        [

            new string[]
            {
                "**/prefix*",
                "!**/prefix?",
                "!H*/prefix*H*"
            },

            new string[]
            {
                "Hello/prefixUEWGIUFG"
            }
        ]
    ];

    [Theory]
    [MemberData(nameof(GetFiles_should_work_DATA))]
    public void GetFiles_should_work(string[] filters, string[] expected)
    {
        var root = $"{Environment.CurrentDirectory}/TestRootFolder";

        FilesFiltering
            .GetFiles(filters, $"{Environment.CurrentDirectory}/TestRootFolder")
            .Select(file => Path.GetRelativePath(root, file.FullName).Replace('\\', '/'))
            .Should()
            .BeEquivalentTo(expected);
    }
}