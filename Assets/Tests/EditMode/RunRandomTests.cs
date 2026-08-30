using NUnit.Framework;

public class RunRandomTests
{
    [Test]
    public void RestoredState_ContinuesWithTheSameSequence()
    {
        RunRandom original = new RunRandom(7391);
        original.Next(1000);
        original.NextDouble();
        int savedState = original.State;

        int expectedInteger = original.Next(1000);
        double expectedFraction = original.NextDouble();

        RunRandom restored = new RunRandom(savedState);
        Assert.That(restored.Next(1000), Is.EqualTo(expectedInteger));
        Assert.That(restored.NextDouble(), Is.EqualTo(expectedFraction));
    }

    [Test]
    public void ZeroSeed_IsNormalizedAndStillProducesValues()
    {
        RunRandom random = new RunRandom(0);
        Assert.That(random.Next(7), Is.InRange(0, 6));
        Assert.That(random.NextDouble(), Is.InRange(0d, 1d));
    }
}
