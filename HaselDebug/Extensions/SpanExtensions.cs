namespace HaselDebug.Extensions;

public static class SpanExtensions
{
    extension(ReadOnlySpan<byte> span)
    {
        public int GetSpanHashCode()
        {
            var hc = new HashCode();
            hc.AddBytes(span);
            return hc.ToHashCode();
        }

        public ReadOnlySpan<byte> UntilNullTerminator()
        {
            var idx = span.IndexOf((byte)0);
            return idx == -1 ? span : span[..idx];
        }
    }
}
