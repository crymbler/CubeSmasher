using System.Collections.Generic;
using CubeSmasher.Infrastructure;
using NUnit.Framework;

/// <summary>
/// Subscriptions: подписка при Track, отписка при Dispose в обратном порядке.
/// </summary>
public class SubscriptionsTests
{
    [Test]
    public void Track_RunsSubscribeImmediately()
    {
        var log = new List<string>();
        using (var subs = new Subscriptions())
        {
            subs.Track(() => log.Add("sub"), () => log.Add("unsub"));
            Assert.AreEqual(new[] { "sub" }, log.ToArray());
        }
    }

    [Test]
    public void Dispose_UnsubscribesInReverseOrder()
    {
        var log = new List<string>();
        var subs = new Subscriptions();
        subs.Track(() => log.Add("a+"), () => log.Add("a-"));
        subs.Track(() => log.Add("b+"), () => log.Add("b-"));
        subs.Dispose();
        Assert.AreEqual(new[] { "a+", "b+", "b-", "a-" }, log.ToArray());
    }

    [Test]
    public void Dispose_TwiceDoesNotUnsubscribeAgain()
    {
        int count = 0;
        var subs = new Subscriptions();
        subs.Track(() => { }, () => count++);
        subs.Dispose();
        subs.Dispose();
        Assert.AreEqual(1, count);
    }
}
