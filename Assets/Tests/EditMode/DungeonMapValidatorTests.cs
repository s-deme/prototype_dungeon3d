using System;
using NUnit.Framework;

public class DungeonMapValidatorTests
{
    private static readonly string[] ValidDungeon =
    {
        "#########",
        "#S.C.C..#",
        "#.#####.#",
        "#...C..>#",
        "#########"
    };

    [Test]
    public void ValidDungeon_IsAccepted()
    {
        Assert.DoesNotThrow(() => DungeonMapValidator.ValidateOrThrow(ValidDungeon));
    }

    [Test]
    public void DungeonWithoutThreeRelics_IsRejected()
    {
        string[] invalid =
        {
            "#####",
            "#S>C#",
            "#####"
        };

        ArgumentException error = Assert.Throws<ArgumentException>(() => DungeonMapValidator.ValidateOrThrow(invalid));
        StringAssert.Contains("three relic chests", error.Message);
    }

    [Test]
    public void DungeonWithUnreachableExit_IsRejected()
    {
        string[] invalid =
        {
            "#########",
            "#S.C.C###",
            "#.#######",
            "#...C#>##",
            "#########"
        };

        ArgumentException error = Assert.Throws<ArgumentException>(() => DungeonMapValidator.ValidateOrThrow(invalid));
        StringAssert.Contains("exit must be reachable", error.Message);
    }

    [Test]
    public void AlternateDungeonLayout_IsAccepted()
    {
        string[] alternate =
        {
            "#####################",
            "#S......#.....#.....#",
            "###...#.#.###.#.###.#",
            "#..P#.#...#...#...#.#",
            "#.#...#####.#####.#.#",
            "#.#...#...#.....#.#.#",
            "#.#####.#.#####.#.#.#",
            "#.....#.#..P..#.#...#",
            "###.#.#.#####.#.###.#",
            "#...#.#.....#.#...#.#",
            "#.#.#.#####.#.###.#.#",
            "#.#...#C..#.#...#.#.#",
            "#.#####.#.#.###.#.#.#",
            "#.....#.#...#C..#.#F#",
            "###.#.#.#######.#.#.#",
            "#C..#.....R........>#",
            "#####################"
        };

        Assert.DoesNotThrow(() => DungeonMapValidator.ValidateOrThrow(alternate));
    }

    [Test]
    public void ReachableExitWithUnreachableRelic_IsRejected()
    {
        string[] invalid =
        {
            "#########",
            "#S.C.C>.#",
            "#########",
            "#C......#",
            "#########"
        };

        ArgumentException error = Assert.Throws<ArgumentException>(() => DungeonMapValidator.ValidateOrThrow(invalid));
        StringAssert.Contains("Every relic chest must be reachable", error.Message);
    }
}
