namespace WildcardsUtilities;

public static class FilesFiltering
{
    public static IEnumerable<FileInfo> GetFiles(string[] filters, string root)
    {
        ArgumentNullException.ThrowIfNull(filters);
        ArgumentException.ThrowIfNullOrWhiteSpace(root);

        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"The specified '{nameof(root)}' is not a directory.");

        if (filters.Length == 0)
            return [];

        var splittedFilters = filters.SelectMany(filter =>
        {
            var excludes = filter.StartsWith('!');
            filter = excludes ? filter[1..] : filter;

            var splittedFilter =
            (
                Items: filter.Split('/', StringSplitOptions.RemoveEmptyEntries),
                Excludes: excludes
            );

            IList<(string[] Items, bool Excludes)> results = [splittedFilter];

            if (splittedFilter.Items[0] == "**")
                results.Add(splittedFilter with { Items = splittedFilter.Items[1..] });

            return results;
        });

        var fileFilters =
        (
            from s in splittedFilters
            where s.Items.Length == 1
            let negation = s.Excludes ? "!" : string.Empty
            select negation + s.Items[0]
        )
        .Distinct();

        var inclusiveFileFilters = fileFilters.Where(f => !f.StartsWith('!'));

        var exclusiveFileRegexes =
            from f in fileFilters
            where f.StartsWith('!')
            select ToRegex(f);

        var splittedFolderFilters = splittedFilters.Where(s => s.Items.Length > 1);

        var folderFiltersWithRegexes =
            from s in splittedFolderFilters
            let negation = s.Excludes ? "!" : string.Empty
            let joinStartIndex = s.Items[0] == "**" ? 0 : 1
            let newFilter = '/' + string.Join('/', s.Items, joinStartIndex, s.Items.Length - joinStartIndex)
            select
            (
                Regex: ToRegex(s.Items[0]),
                NewFilter: negation + newFilter
            );

        var inclusiveFolderFilters =
        (
            from s in splittedFolderFilters
            where !s.Excludes
            select s.Items[0]
        )
        .Distinct();

        var filesInFolders = inclusiveFolderFilters
            .SelectMany(filter => GetFilesByFolderFilter(root, filter, folderFiltersWithRegexes));

        var files = inclusiveFileFilters
            .SelectMany(filter => GetFilesByFileFilter(root, filter, exclusiveFileRegexes));

        return files.Concat(filesInFolders).DistinctBy(f => f.FullName);
    }

    internal static string[] GetNewFilters
    (
        string dirName,
        IEnumerable<(Regex Regex, string NewFilter)> folderFiltersWithRegexes
    )
    =>
    (
        from f in folderFiltersWithRegexes
        where f.Regex.IsMatch(dirName)
        select f.NewFilter
    )
    .ToArray();

    internal static IEnumerable<FileInfo> GetFilesByFolderFilter
    (
        string root,
        string folderFilter,
        IEnumerable<(Regex Regex, string NewFilter)> folderFiltersWithRegexes
    )
    {
        if (HasWildcards(folderFilter))
        {
            var regex = ToRegex(folderFilter);

            var dirs =
                from path in Directory.EnumerateDirectories(root)
                let dir = new DirectoryInfo(path)
                where regex.IsMatch(dir.Name)
                select dir;

            return dirs.SelectMany
            (
                dir =>
                    GetFiles(GetNewFilters(dir.Name, folderFiltersWithRegexes), dir.FullName)
            );
        }

        var directory = new DirectoryInfo($"{root}/{folderFilter}");

        return directory.Exists ?
            GetFiles(GetNewFilters(directory.Name, folderFiltersWithRegexes), directory.FullName) :
            [];
    }

    internal static IEnumerable<FileInfo> GetFilesByFileFilter
    (
        string root,
        string fileFilter,
        IEnumerable<Regex> exclusiveRegexes
    )
    {
        if (HasWildcards(fileFilter))
        {
            var regex = ToRegex(fileFilter);

            return
                from path in Directory.EnumerateFiles(root)
                let file = new FileInfo(path)
                where regex.IsMatch(file.Name) && !exclusiveRegexes.AnyMatch(file.Name)
                select file;
        }

        var includedFile = new FileInfo($"{root}/{fileFilter}");

        return includedFile.Exists && !exclusiveRegexes.AnyMatch(includedFile.Name) ?
            [includedFile] : [];
    }

    internal static bool HasWildcards(string s) =>
        s.Contains('*') || s.Contains('?');

    // Returns true if any provided regex matches with the specified input, otherwise false.
    internal static bool AnyMatch(this IEnumerable<Regex> regexes, string input) =>
        regexes.Any(regex => regex.IsMatch(input));

    // Converts a file/folder filter to a regex.
    internal static Regex ToRegex(string filter)
    {
        // Removes the unnecessary initial !
        if (filter.StartsWith('!'))
            filter = filter[1..];

        // Removes the unnecessary initial /
        if (filter.StartsWith('/'))
            filter = filter[1..];

        var regexFilter = Regex
            .Escape(filter)  // Escapes the filter to exclude special regex character for future matching operations.
            .Replace(@"\?", "[^/]?")  // Replaces the escaped ? character with its regex version.
            .Replace(@"\*", "[^/]*");  // Replaces the escaped * character with its regex version.

        // ^ at the start and $ at the end indicate that the filter should be matched for the whole input.
        // [/]? indicates that paths with or without the initial / will be matched anyway.
        return new($"^[/]?{regexFilter}$");
    }
}
