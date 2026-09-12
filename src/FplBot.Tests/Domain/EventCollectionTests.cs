using FplBot.Domain;

namespace FplBot.Tests.Domain;

public class EventCollectionTests
{
    [Fact]
    public void Add_SingleEvent_IsInCurrent()
    {
        var events = EventCollection.Empty();
        events.Add(FplEvent.Standings);
        Assert.Single(events.Current, FplEvent.Standings);
    }

    [Fact]
    public void Add_All_CollapsesToJustAll()
    {
        var events = EventCollection.Empty();
        events.Add(FplEvent.Standings);
        events.Add(FplEvent.All);
        Assert.Single(events.Current, FplEvent.All);
    }

    [Fact]
    public void Add_SpecificEvent_WhenAlreadySubscribedToAll_IsNoOp()
    {
        var events = EventCollection.Empty();
        events.Add(FplEvent.All);
        events.Add(FplEvent.Standings);
        Assert.Single(events.Current, FplEvent.All);
    }

    [Fact]
    public void Remove_SpecificEvent_WhenSubscribedToAll_ExpandsToEverythingExceptThat()
    {
        var events = EventCollection.Empty();
        events.Add(FplEvent.All);
        events.Remove(FplEvent.Standings);

        var expected = Enum.GetValues<FplEvent>().Where(e => e != FplEvent.All && e != FplEvent.Standings).ToHashSet();
        Assert.Equal(expected, events.Current.ToHashSet());
    }

    [Fact]
    public void Remove_SpecificEvent_WhenNotSubscribedToAll_JustRemovesIt()
    {
        var events = EventCollection.Empty();
        events.Add(FplEvent.Standings);
        events.Add(FplEvent.Deadlines);
        events.Remove(FplEvent.Standings);
        Assert.Single(events.Current, FplEvent.Deadlines);
    }

    [Fact]
    public void Contains_WhenSubscribedToAll_IsTrueForAnyEvent()
    {
        var events = EventCollection.Empty();
        events.Add(FplEvent.All);
        Assert.True(events.Contains(FplEvent.Standings));
        Assert.True(events.Contains(FplEvent.Deadlines));
    }

    [Fact]
    public void Contains_WhenNotSubscribed_IsFalse()
    {
        var events = EventCollection.Empty();
        events.Add(FplEvent.Standings);
        Assert.False(events.Contains(FplEvent.Deadlines));
    }

    [Fact]
    public void Add_Many_AddsEachEvent()
    {
        var events = EventCollection.Empty();
        events.Add([FplEvent.Standings, FplEvent.Deadlines]);
        Assert.True(events.Contains(FplEvent.Standings));
        Assert.True(events.Contains(FplEvent.Deadlines));
    }

    [Fact]
    public void Add_ManyContainingAll_CollapsesToJustAll()
    {
        var events = EventCollection.Empty();
        events.Add([FplEvent.Standings, FplEvent.All, FplEvent.Deadlines]);
        Assert.Single(events.Current, FplEvent.All);
    }

    [Fact]
    public void CreateSingle_StartsWithThatEvent()
    {
        var events = EventCollection.CreateSingle(FplEvent.Standings);
        Assert.Single(events.Current, FplEvent.Standings);
    }

    [Fact]
    public void CreateMany_StartsWithThoseEvents()
    {
        var events = EventCollection.CreateMany([FplEvent.Standings, FplEvent.Deadlines]);
        Assert.True(events.Contains(FplEvent.Standings));
        Assert.True(events.Contains(FplEvent.Deadlines));
    }
}
