// -----------------------------------------------------------------------
// Artigo: Git History na Prática: Debug e Auditoria em Escala
// URL: https://zocate.li/posts/2026/git-history-debug-auditoria-escala/
// Leitura incremental de campos terminados por NUL, sem ReadToEndAsync.
// -----------------------------------------------------------------------

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace BlogSamples.AsyncParallel.GitHistoryAnalysis;

internal static class NullDelimitedTokenReader
{
    public static async IAsyncEnumerable<string> ReadAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
        var token = new ArrayBufferWriter<byte>();

        try
        {
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
            {
                var start = 0;
                for (var index = 0; index < bytesRead; index++)
                {
                    if (buffer[index] != 0)
                    {
                        continue;
                    }

                    token.Write(buffer.AsSpan(start, index - start));
                    yield return Encoding.UTF8.GetString(token.WrittenSpan);
                    token.Clear();
                    start = index + 1;
                }

                token.Write(buffer.AsSpan(start, bytesRead - start));
            }

            if (token.WrittenCount > 0)
            {
                yield return Encoding.UTF8.GetString(token.WrittenSpan);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
