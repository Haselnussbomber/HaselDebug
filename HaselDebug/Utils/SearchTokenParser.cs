namespace HaselDebug.Utils;

public readonly record struct SearchToken(string Key, string Value, bool IsExclude);

public static class SearchTokenParser
{
    public static List<SearchToken> Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [];

        var results = new List<SearchToken>();
        var span = input.AsSpan();
        var i = 0;

        while (i < span.Length)
        {
            // 1. Skip whitespace
            while (i < span.Length && char.IsWhiteSpace(span[i]))
                i++;

            if (i >= span.Length)
                break;

            // 2. Check for exclusion
            var isExclude = false;
            if (span[i] == '-')
            {
                isExclude = true;
                i++;
                if (i >= span.Length)
                    break;

            }

            // 3. Extract Key (if exists)
            var key = string.Empty;
            var potentialKeyStart = i;
            var colonPos = -1;

            // Look ahead for a colon before the first space or quote
            for (var j = i; j < span.Length; j++)
            {
                if (char.IsWhiteSpace(span[j]) || span[j] == '"')
                    break;

                if (span[j] == ':')
                {
                    colonPos = j;
                    break;
                }
            }

            if (colonPos != -1)
            {
                key = span[potentialKeyStart..colonPos].ToString();
                i = colonPos + 1; // Move past the colon
            }

            // 4. Extract Value (handling quotes)
            if (i < span.Length && span[i] == '"')
            {
                // Skip opening quote
                i++;
                var valueStart = i;
                while (i < span.Length && span[i] != '"')
                    i++;

                // We take whatever is there, even if i == span.Length (unclosed quote)
                results.Add(new SearchToken(key, span[valueStart..i].ToString(), isExclude));

                // Skip closing quote
                if (i < span.Length)
                    i++;
            }
            else
            {
                var valueStart = i;
                while (i < span.Length && !char.IsWhiteSpace(span[i]))
                    i++;

                results.Add(new SearchToken(key, span[valueStart..i].ToString(), isExclude));
            }
        }

        return results;
    }
}
