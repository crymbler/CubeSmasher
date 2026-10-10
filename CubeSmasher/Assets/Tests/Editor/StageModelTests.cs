using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// StageModel: прогресс накапливается, переход на следующую стадию увеличивает порог.
/// </summary>
public class StageModelTests
{
    [Test]
    public void InitialStage_ClampedToOne()
    {
        var stage = new StageModel(0);
        Assert.AreEqual(1, stage.CurrentStage);
    }

    [Test]
    public void AddProgress_CompletesStageAtThreshold()
    {
        var stage = new StageModel(1);
        var completed = new List<int>();
        stage.OnStageCompleted += s => completed.Add(s);

        stage.AddProgress(49);
        Assert.AreEqual(0, completed.Count, "при 49 из 50 стадия не должна закрыться");

        stage.AddProgress(1);
        Assert.AreEqual(new[] { 2 }, completed.ToArray());
        Assert.AreEqual(2, stage.CurrentStage);
    }

    [Test]
    public void NextStage_RequiresMoreDetails()
    {
        // стадия 2 требует 50 + 25 = 75 деталей
        var stage = new StageModel(2);
        var completed = new List<int>();
        stage.OnStageCompleted += s => completed.Add(s);

        stage.AddProgress(74);
        Assert.AreEqual(0, completed.Count);
        stage.AddProgress(1);
        Assert.AreEqual(new[] { 3 }, completed.ToArray());
    }
}
