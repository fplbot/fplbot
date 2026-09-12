namespace FplBot.Domain;

public class EventCollection
{
    private readonly HashSet<FplEvent> _events = new();

    public IReadOnlyCollection<FplEvent> Current => _events;

    private EventCollection(){ }

    public static EventCollection Empty()
    {
        return new EventCollection();
    }

    public void Add(FplEvent fplEvent)
    {
        if (fplEvent == FplEvent.All)
        {
            _events.Clear();
            _events.Add(FplEvent.All);
            return;
        }

        if (_events.Contains(FplEvent.All))
        {
            return;
        }

        _events.Add(fplEvent);
    }

    public void Add(IEnumerable<FplEvent> fplEvents)
    {
        foreach (var fplEvent in fplEvents)
        {
            Add(fplEvent);
        }
    }

    public bool Contains(FplEvent fplEvent)
    {
        return _events.Contains(FplEvent.All) || _events.Contains(fplEvent);
    }

    public void Remove(IEnumerable<FplEvent> fplEvents)
    {
        foreach (var fplEvent in fplEvents)
        {
            Remove(fplEvent);
        }
    }

    public void Remove(FplEvent fplEvent)
    {
        if (_events.Contains(FplEvent.All))
        {
            _events.Remove(FplEvent.All);
            foreach (var value in Enum.GetValues<FplEvent>())
            {
                if (value != FplEvent.All && value != fplEvent)
                {
                    _events.Add(value);
                }
            }
            return;
        }

        _events.Remove(fplEvent);
    }



}
