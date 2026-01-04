namespace HaselDebug.Extensions;

public static class StringExtensions
{
    extension(string input)
    {
        public string SplitCamelCase()
        {
            return string.Concat(input.Select(c => char.IsUpper(c) ? " " + c : c.ToString())).Replace("< ", "<").Trim();
        }
    }
}
