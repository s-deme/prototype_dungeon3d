using NUnit.Framework;

public class DungeonRunSaveDataTests
{
    [Test]
    public void CurrentSnapshot_RoundTripsAllFields()
    {
        DungeonRunSaveData source = new DungeonRunSaveData
        {
            PlayerX = 3,
            PlayerY = 4,
            Health = 9,
            Relics = 2,
            Potions = 1,
            Focus = 3,
            Steps = 51,
            Gold = 75,
            Level = 4,
            Experience = 6,
            EnemiesDefeated = 3,
            DifficultyIndex = 2,
            MapIndex = 1,
            RandomState = 4711,
            DailyRunId = 20260831,
            ExploredFlags = "1010",
            OpenedFlags = "1000",
            DefeatedEncounterFlags = "0010"
        };

        Assert.That(DungeonRunSaveData.TryDeserialize(source.Serialize(), out DungeonRunSaveData restored, out string failure), Is.True, failure);
        Assert.That(restored.PlayerX, Is.EqualTo(source.PlayerX));
        Assert.That(restored.Level, Is.EqualTo(source.Level));
        Assert.That(restored.MapIndex, Is.EqualTo(source.MapIndex));
        Assert.That(restored.RandomState, Is.EqualTo(source.RandomState));
        Assert.That(restored.DailyRunId, Is.EqualTo(source.DailyRunId));
        Assert.That(restored.ExploredFlags, Is.EqualTo(source.ExploredFlags));
    }

    [Test]
    public void LegacyEightStatSnapshot_UsesCompatibleDefaults()
    {
        const string legacy = "1,2,10,0,1,2,8,20|111|000|010";

        Assert.That(DungeonRunSaveData.TryDeserialize(legacy, out DungeonRunSaveData restored, out string failure), Is.True, failure);
        Assert.That(restored.Level, Is.EqualTo(1));
        Assert.That(restored.DifficultyIndex, Is.EqualTo(1));
        Assert.That(restored.MapIndex, Is.EqualTo(0));
        Assert.That(restored.RandomState, Is.EqualTo(2034));
    }

    [Test]
    public void MalformedSnapshot_ReturnsARecoveryMessage()
    {
        Assert.That(DungeonRunSaveData.TryDeserialize("1,2,broken|1|0|0", out _, out string failure), Is.False);
        Assert.That(failure, Is.EqualTo("保存データに不正な数値が含まれています。"));
    }
}
