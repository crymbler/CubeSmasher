using System;
using CubeSmasher.Infrastructure;
using NUnit.Framework;

/// <summary>
/// ServiceLocator: регистрация по типу, повторная регистрация не заменяет сервис, Clear очищает.
/// </summary>
public class ServiceLocatorTests
{
    private interface IThing { }
    private class ThingA : IThing { }
    private class ThingB : IThing { }

    [SetUp]
    public void SetUp() => ServiceLocator.Instance.Clear();

    [TearDown]
    public void TearDown() => ServiceLocator.Instance.Clear();

    [Test]
    public void RegisterAndGet_ReturnsSameInstance()
    {
        var a = new ThingA();
        ServiceLocator.Instance.Register<IThing>(a);
        Assert.AreSame(a, ServiceLocator.Instance.Get<IThing>());
    }

    [Test]
    public void SecondRegister_KeepsFirstService()
    {
        var first = new ThingA();
        ServiceLocator.Instance.Register<IThing>(first);
        ServiceLocator.Instance.Register<IThing>(new ThingB());
        Assert.AreSame(first, ServiceLocator.Instance.Get<IThing>());
    }

    [Test]
    public void Get_ThrowsWhenMissing()
    {
        Assert.Throws<InvalidOperationException>(() => ServiceLocator.Instance.Get<IThing>());
    }

    [Test]
    public void TryGet_ReturnsFalseWhenMissing()
    {
        Assert.IsFalse(ServiceLocator.Instance.TryGet(out IThing _));
    }

    [Test]
    public void Clear_RemovesAllServices()
    {
        ServiceLocator.Instance.Register<IThing>(new ThingA());
        ServiceLocator.Instance.Clear();
        Assert.IsFalse(ServiceLocator.Instance.TryGet(out IThing _));
    }
}
