using NUnit.Framework;

public class DungeonGridFlagsTests
{
    [Test]
    public void Flags_RoundTripInRowMajorOrder()
    {
        bool[,] source = new bool[2, 2];
        source[0, 0] = true;
        source[1, 1] = true;

        string serialized = DungeonGridFlags.Serialize(source);
        bool[,] restored = new bool[2, 2];

        Assert.That(serialized, Is.EqualTo("1001"));
        Assert.That(DungeonGridFlags.TryRestore(serialized, restored), Is.True);
        Assert.That(restored[0, 0], Is.True);
        Assert.That(restored[1, 0], Is.False);
        Assert.That(restored[0, 1], Is.False);
        Assert.That(restored[1, 1], Is.True);
    }

    [Test]
    public void InvalidData_DoesNotPartiallyMutateTheTargetGrid()
    {
        bool[,] target = new bool[2, 2];
        target[0, 0] = true;
        target[0, 1] = true;

        Assert.That(DungeonGridFlags.TryRestore("010x", target), Is.False);
        Assert.That(DungeonGridFlags.Serialize(target), Is.EqualTo("1010"));
    }

    [Test]
    public void WrongLength_IsRejected()
    {
        Assert.That(DungeonGridFlags.TryRestore("101", new bool[2, 2]), Is.False);
    }
}
