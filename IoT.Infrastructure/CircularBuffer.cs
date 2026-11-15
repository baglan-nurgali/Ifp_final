namespace IoT.Infrastructure;

public sealed class CircularBuffer<T>
{
    private readonly T?[] _items;
    private int _next, _count;
    private readonly object _gate = new();
    public CircularBuffer(int capacity) { if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity)); _items = new T[capacity]; }
    public int Capacity => _items.Length;
    public int Count { get { lock (_gate) return _count; } }
    public void Add(T item) { lock (_gate) { _items[_next] = item; _next = (_next + 1) % Capacity; _count = Math.Min(_count + 1, Capacity); } }
    public IReadOnlyList<T> Snapshot(int count = int.MaxValue) { lock (_gate) { var n = Math.Min(Math.Max(count,0),_count); var start = (_next - n + Capacity) % Capacity; var result = new T[n]; for (var i=0;i<n;i++) result[i] = _items[(start+i)%Capacity]!; return result; } }
}
