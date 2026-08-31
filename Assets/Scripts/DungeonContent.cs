using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hand-authored dungeon definitions. Keeping content data outside the
/// MonoBehaviour makes map edits reviewable and independently testable.
/// </summary>
public sealed class DungeonDefinition
{
    public IReadOnlyList<string> Layout { get; }
    public IReadOnlyList<Vector2Int> EncounterTiles { get; }

    public DungeonDefinition(string[] layout, Vector2Int[] encounterTiles)
    {
        if (layout == null) throw new ArgumentNullException(nameof(layout));
        if (encounterTiles == null) throw new ArgumentNullException(nameof(encounterTiles));

        // Definitions are shared by every run, so callers only receive immutable views.
        Layout = Array.AsReadOnly((string[])layout.Clone());
        EncounterTiles = Array.AsReadOnly((Vector2Int[])encounterTiles.Clone());
    }
}

/// <summary>
/// Hand-authored dungeon definitions and their encounter locations.
/// A definition keeps the two pieces of map data together so they cannot drift
/// out of sync when a new dungeon is added.
/// </summary>
public static class DungeonContent
{
    private static readonly DungeonDefinition[] definitions =
    {
        new DungeonDefinition(
            new[]
            {
                "#####################", "#S....#.......#.....#", "#.###.#.#####.#.###.#", "#..P#.#.....#.#...#.#",
                "###.#.#####.#.###.#.#", "#...#.....#.#.....#.#", "#.#######.#.#######.#", "#.....#...#.R...#...#",
                "#####.#.#######.#.###", "#...#.#.....#...#.P.#", "#.#.#.#####.#.#####.#", "#.#...#C..#.#.....#.#",
                "#.#####.#.#.#####.#.#", "#.....#.#...#C..#.#F#", "###.#.#.#######.#.#.#", "#C..#..............>#",
                "#####################"
            },
            new[] { new Vector2Int(5, 5), new Vector2Int(8, 7), new Vector2Int(3, 13), new Vector2Int(16, 15) }),
        new DungeonDefinition(
            new[]
            {
                "#####################", "#S......#.....#.....#", "#####.#.#.###.#.###.#", "#..P#.#...#...#...#.#",
                "#.#.#.#####.#####.#.#", "#.#...#...#.....#.#.#", "#.#####.#.#####.#.#.#", "#.....#.#..P..#.#...#",
                "###.#.#.#####.#.###.#", "#...#.#.....#.#...#.#", "#.#.#.#####.#.###.#.#", "#.#...#C..#.#...#.#.#",
                "#.#####.#.#.###.#.#.#", "#.....#.#...#C..#.#F#", "###.#.#.#######.#.#.#", "#C..#.....R........>#",
                "#####################"
            },
            new[] { new Vector2Int(5, 5), new Vector2Int(10, 7), new Vector2Int(15, 11), new Vector2Int(17, 13) }),
        new DungeonDefinition(
            new[]
            {
                "#####################", "#S......#.....#.....#", "###...#.#.###.#.###.#", "#..P#.#...#...#...#.#",
                "#.#...#####.#####.#.#", "#.#...#...#.....#.#.#", "#.#####.#.#####.#.#.#", "#.....#.#..P..#.#...#",
                "###.#.#.#####.#.###.#", "#...#.#.....#.#...#.#", "#.#.#.#####.#.###.#.#", "#.#...#C..#.#...#.#.#",
                "#.#####.#.#.###.#.#.#", "#.....#.#...#C..#.#F#", "###.#.#.#######.#.#.#", "#C..#.....R........>#",
                "#####################"
            },
            new[] { new Vector2Int(5, 5), new Vector2Int(10, 7), new Vector2Int(15, 11), new Vector2Int(17, 13) })
    };

    public static int Count => definitions.Length;

    public static DungeonDefinition Get(int index)
    {
        if (index < 0 || index >= definitions.Length) throw new ArgumentOutOfRangeException(nameof(index));
        return definitions[index];
    }

    public static void ValidateOrThrow()
    {
        if (definitions.Length == 0) throw new InvalidOperationException("At least one dungeon definition is required.");

        foreach (DungeonDefinition definition in definitions)
        {
            DungeonMapValidator.ValidateOrThrow(definition.Layout);
            foreach (Vector2Int encounter in definition.EncounterTiles)
            {
                if (encounter.y < 0 || encounter.y >= definition.Layout.Count || encounter.x < 0 || encounter.x >= definition.Layout[0].Length || definition.Layout[encounter.y][encounter.x] == '#')
                    throw new InvalidOperationException("Encounter tiles must be walkable.");
            }
        }
    }
}
