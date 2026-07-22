using System.Text;
using BlogSamples.AsyncParallel.GitHistoryAnalysis;

namespace BlogSamples.Tests.AsyncParallel.GitHistoryAnalysis;

public sealed class GitLogStreamParserTests
{
    [Fact]
    public async Task ParseAsync_PreservaPathsEspeciaisIdsOpacosERenames()
    {
        var objectId = new string('a', 64);
        var fields = new[]
        {
            GitLogStreamParser.CommitMarker, objectId, "pai-1 pai-2",
            "Ana", "ana@example.com", "2026-07-21T10:00:00-03:00",
            "Beto", "beto@example.com", "2026-07-21T10:01:00-03:00",
            "G", "Corrige autorização", "Reviewed-by: Lia"
        };
        var tokens = fields.Concat(new[]
        {
            "10\t2\tpasta/arquivo com espaço.cs",
            "-\t-\tassets/binário.dat",
            "3\t1\t", "origem\tcom-tab.cs", "destino\ncom-quebra.cs"
        });
        await using var stream = CreateStream(tokens);

        var records = new List<FileChangeRecord>();
        await foreach (var record in new GitLogStreamParser().ParseAsync(stream))
        {
            records.Add(record);
        }

        Assert.Equal(3, records.Count);
        Assert.Equal(objectId, records[0].Commit.ObjectId);
        Assert.Equal("pasta/arquivo com espaço.cs", records[0].Path);
        Assert.True(records[1].IsBinary);
        Assert.Equal("origem\tcom-tab.cs", records[2].PreviousPath);
        Assert.Equal("destino\ncom-quebra.cs", records[2].Path);
    }

    [Fact]
    public async Task ParseAsync_RejeitaRenameIncompleto()
    {
        var tokens = new[]
        {
            GitLogStreamParser.CommitMarker, "id", "", "Ana", "ana@example.com",
            "2026-07-21T10:00:00Z", "Ana", "ana@example.com",
            "2026-07-21T10:00:00Z", "N", "Assunto", "", "1\t1\t", "origem.cs"
        };
        await using var stream = CreateStream(tokens);

        await Assert.ThrowsAsync<InvalidDataException>(async () =>
        {
            await foreach (var _ in new GitLogStreamParser().ParseAsync(stream))
            {
            }
        });
    }

    private static MemoryStream CreateStream(IEnumerable<string> tokens)
    {
        var bytes = Encoding.UTF8.GetBytes(string.Join('\0', tokens) + '\0');
        return new MemoryStream(bytes);
    }
}