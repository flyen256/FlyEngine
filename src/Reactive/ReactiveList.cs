namespace Flyeng.Reactive;

public class ReactiveList<T>
{
    private List<T> _values = new();
    public IReadOnlyList<T> Values => _values;

    public Action<T>? OnAdd;
    public Action<T>? OnRemove;

    public void Add(T item)
    {
        _values.Add(item);
        OnAdd?.Invoke(item);
    }

    public void Remove(T item)
    {
        if (!_values.Contains(item))
            return;
        _values.Remove(item);
        OnRemove?.Invoke(item);
    }
}
