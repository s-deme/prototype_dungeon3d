using System;
using System.Collections.Generic;

/// <summary>
/// Guards the hand-authored dungeon data before a run starts.
/// Kept independent of Unity so it can be exercised by fast EditMode tests.
/// </summary>
public static class DungeonMapValidator
{
    private static readonly (int x, int y)[] CardinalSteps =
    {
        (0, -1), (1, 0), (0, 1), (-1, 0)
    };

    public static void ValidateOrThrow(IReadOnlyList<string> rows)
    {
        if (rows == null || rows.Count == 0) throw new ArgumentException("A dungeon needs at least one row.", nameof(rows));

        int width = rows[0]?.Length ?? 0;
        if (width == 0) throw new ArgumentException("A dungeon row cannot be empty.", nameof(rows));

        int starts = 0;
        int chests = 0;
        int exits = 0;
        (int x, int y) start = (-1, -1);
        (int x, int y) exit = (-1, -1);

        for (int y = 0; y < rows.Count; y++)
        {
            string row = rows[y];
            if (row == null || row.Length != width) throw new ArgumentException("Every dungeon row must have the same width.", nameof(rows));
            for (int x = 0; x < width; x++)
            {
                char cell = row[x];
                if (cell == 'S') { starts++; start = (x, y); }
                if (cell == 'C') chests++;
                if (cell == '>') { exits++; exit = (x, y); }
                if ("#.SCPRF>".IndexOf(cell) < 0) throw new ArgumentException($"Unsupported dungeon cell '{cell}'.", nameof(rows));
            }
        }

        if (starts != 1) throw new ArgumentException("A dungeon must contain exactly one start tile.", nameof(rows));
        if (chests != 3) throw new ArgumentException("A dungeon must contain exactly three relic chests.", nameof(rows));
        if (exits != 1) throw new ArgumentException("A dungeon must contain exactly one exit.", nameof(rows));
        HashSet<(int x, int y)> reachable = FindReachable(rows, start);
        if (!reachable.Contains(exit)) throw new ArgumentException("The exit must be reachable from the start.", nameof(rows));

        for (int y = 0; y < rows.Count; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (rows[y][x] == 'C' && !reachable.Contains((x, y)))
                    throw new ArgumentException("Every relic chest must be reachable from the start.", nameof(rows));
            }
        }
    }

    private static HashSet<(int x, int y)> FindReachable(IReadOnlyList<string> rows, (int x, int y) from)
    {
        Queue<(int x, int y)> frontier = new Queue<(int x, int y)>();
        HashSet<(int x, int y)> visited = new HashSet<(int x, int y)>();
        frontier.Enqueue(from);
        visited.Add(from);
        while (frontier.Count > 0)
        {
            (int x, int y) current = frontier.Dequeue();
            foreach ((int x, int y) step in CardinalSteps)
            {
                int nextX = current.x + step.x;
                int nextY = current.y + step.y;
                if (nextY < 0 || nextY >= rows.Count || nextX < 0 || nextX >= rows[0].Length || rows[nextY][nextX] == '#') continue;
                if (visited.Add((nextX, nextY))) frontier.Enqueue((nextX, nextY));
            }
        }
        return visited;
    }
}
