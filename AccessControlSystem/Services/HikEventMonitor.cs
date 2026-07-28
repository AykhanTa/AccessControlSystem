using System.Collections.Concurrent;

namespace AccessControlSystem.Services;

/// <summary>
/// Cihazdan gələn BÜTÜN event POST-larını (saxlanmasa belə) yaddaşda izləyir —
/// diaqnostika üçün: cihaz hələ backlog tökür, yoxsa canlı oxutma gəlir?
/// </summary>
public static class HikEventMonitor
{
    private static long _total;
    private static readonly ConcurrentQueue<object> _recent = new();
    private const int Cap = 50;

    public static void Record(string? accessNumber, bool granted, DateTimeOffset? deviceTime, int? minorType)
    {
        Interlocked.Increment(ref _total);
        _recent.Enqueue(new
        {
            receivedAt = DateTime.Now,
            accessNumber,
            granted,
            deviceTime,
            minorType
        });
        while (_recent.Count > Cap && _recent.TryDequeue(out _)) { }
    }

    public static object Snapshot() => new
    {
        totalReceived = Interlocked.Read(ref _total),
        recent = _recent.Reverse().ToArray()   // ən təzə əvvəldə
    };
}
