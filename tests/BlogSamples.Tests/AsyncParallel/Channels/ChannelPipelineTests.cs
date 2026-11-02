using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using BlogSamples.AsyncParallel.Channels;

namespace BlogSamples.Tests.AsyncParallel.Channels;

public sealed class ChannelPipelineTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task NormalCompletionProcessesEveryItem(int consumerCount)
    {
        var processed = new ConcurrentQueue<int>();

        await PipelineChannels.ExecutarAsync(
            ProduceAsync(100),
            (item, _) =>
            {
                processed.Enqueue(item);
                return ValueTask.CompletedTask;
            },
            capacidade: 1,
            consumidores: consumerCount).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(Enumerable.Range(0, 100), processed.Order());
        if (consumerCount == 1)
        {
            Assert.Equal(Enumerable.Range(0, 100), processed);
        }
    }

    [Fact]
    public async Task ConsumerFailureStopsProducerWithoutDeadlock()
    {
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PipelineChannels.ExecutarAsync(
                ProduceAsync(1000),
                (item, _) => item == 2
                    ? ValueTask.FromException(new InvalidOperationException("consumer failed"))
                    : ValueTask.CompletedTask,
                capacidade: 1,
                consumidores: 1).WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.Equal("consumer failed", failure.Message);
    }

    [Fact]
    public async Task ProducerFailureStopsConsumers()
    {
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PipelineChannels.ExecutarAsync(
                ProduceWithFailureAsync(),
                (_, _) => ValueTask.CompletedTask,
                capacidade: 1).WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.Equal("producer failed", failure.Message);
    }

    [Fact]
    public async Task CancellationStopsBlockedPipeline()
    {
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var execution = PipelineChannels.ExecutarAsync(
            ProduceAsync(1000),
            async (_, token) =>
            {
                started.TrySetResult();
                await Task.Delay(Timeout.Infinite, token);
            },
            capacidade: 1,
            consumidores: 1,
            cancellationToken: cancellation.Token);

        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            execution.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Theory]
    [InlineData(BoundedChannelFullMode.Wait, false, 10, 20, 0)]
    [InlineData(BoundedChannelFullMode.DropWrite, true, 10, 20, 30)]
    [InlineData(BoundedChannelFullMode.DropNewest, true, 10, 30, 20)]
    [InlineData(BoundedChannelFullMode.DropOldest, true, 20, 30, 10)]
    public void FullModesHaveExplicitDiscardBehavior(
        BoundedChannelFullMode mode, bool accepted, int first, int second, int discarded)
    {
        var drops = new List<int>();
        var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(2)
        {
            FullMode = mode,
            SingleWriter = true,
            SingleReader = true
        }, item => drops.Add(item));

        Assert.True(channel.Writer.TryWrite(10));
        Assert.True(channel.Writer.TryWrite(20));
        Assert.Equal(accepted, channel.Writer.TryWrite(30));
        Assert.True(channel.Reader.TryRead(out var actualFirst));
        Assert.True(channel.Reader.TryRead(out var actualSecond));
        Assert.Equal(first, actualFirst);
        Assert.Equal(second, actualSecond);
        Assert.Equal(discarded == 0 ? Array.Empty<int>() : [discarded], drops);
    }

    [Fact]
    public async Task CompletionWithErrorAllowsReadingBufferedItem()
    {
        var channel = Channel.CreateBounded<int>(1);
        Assert.True(channel.Writer.TryWrite(42));
        Assert.True(channel.Writer.TryComplete(new InvalidOperationException("closed")));

        Assert.Equal(42, await channel.Reader.ReadAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => channel.Reader.Completion);
        Assert.False(channel.Writer.TryWrite(43));
    }

    private static async IAsyncEnumerable<int> ProduceAsync(
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var item = 0; item < count; item++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<int> ProduceWithFailureAsync()
    {
        yield return 1;
        await Task.Yield();
        throw new InvalidOperationException("producer failed");
    }
}
