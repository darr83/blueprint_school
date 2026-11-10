using BlueprintSchool.Simulation.Core;

namespace BlueprintSchool.Simulation.Simulation;

public enum NotificationLevel
{
    Info,
    Warning,
    Critical,
}

/// <summary>A message for the player, linked to the person or room it is about.</summary>
public sealed record Notification(
    int Id,
    DateTime Time,
    NotificationLevel Level,
    string Text,
    int? PersonId = null,
    string? RoomId = null) : IGameEvent;

/// <summary>Keeps the most recent notifications and publishes each one on the event bus.</summary>
public sealed class NotificationLog(EventBus events, int capacity = 200)
{
    private readonly LinkedList<Notification> _items = new();
    private int _nextId = 1;

    public IEnumerable<Notification> Recent => _items;

    public int Count => _items.Count;

    public int NextId => _nextId;

    /// <summary>Puts back saved notifications (newest first) without announcing them again.</summary>
    public void Restore(IEnumerable<Notification> items, int nextId)
    {
        _items.Clear();
        foreach (var n in items)
        {
            _items.AddLast(n);
        }

        _nextId = Math.Max(nextId, _items.Count == 0 ? 1 : _items.Max(n => n.Id) + 1);
    }

    public Notification Add(DateTime time, NotificationLevel level, string text, int? personId = null, string? spaceId = null)
    {
        var n = new Notification(_nextId++, time, level, text, personId, spaceId);
        _items.AddFirst(n);
        while (_items.Count > capacity)
        {
            _items.RemoveLast();
        }

        events.Publish(n);
        return n;
    }
}
