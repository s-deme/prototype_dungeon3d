using NUnit.Framework;
using UnityEngine;

public class DungeonContentTests
{
    [Test]
    public void EveryDefinition_HasValidEncounterTiles()
    {
        Assert.That(DungeonContent.Count, Is.GreaterThan(0));
        Assert.DoesNotThrow(DungeonContent.ValidateOrThrow);

        for (int mapIndex = 0; mapIndex < DungeonContent.Count; mapIndex++)
        {
            DungeonDefinition definition = DungeonContent.Get(mapIndex);

            foreach (Vector2Int encounter in definition.EncounterTiles)
            {
                Assert.That(encounter.y, Is.InRange(0, definition.Layout.Count - 1));
                Assert.That(encounter.x, Is.InRange(0, definition.Layout[0].Length - 1));
                Assert.That(definition.Layout[encounter.y][encounter.x], Is.Not.EqualTo('#'));
            }
        }
    }
}
