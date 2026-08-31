using System;

/// <summary>
/// Version-tolerant, Unity-independent representation of an in-progress run.
/// It owns the wire format only; the game validates map-specific constraints
/// after a snapshot is read.
/// </summary>
public sealed class DungeonRunSaveData
{
    private const int MinimumStatCount = 8;
    private const int CurrentStatCount = 15;

    public int PlayerX { get; set; }
    public int PlayerY { get; set; }
    public int Health { get; set; }
    public int Relics { get; set; }
    public int Potions { get; set; }
    public int Focus { get; set; }
    public int Steps { get; set; }
    public int Gold { get; set; }
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public int EnemiesDefeated { get; set; }
    public int DifficultyIndex { get; set; } = 1;
    public int MapIndex { get; set; }
    public int RandomState { get; set; }
    public int DailyRunId { get; set; }
    public string ExploredFlags { get; set; }
    public string OpenedFlags { get; set; }
    public string DefeatedEncounterFlags { get; set; }

    public string Serialize()
    {
        int[] stats =
        {
            PlayerX, PlayerY, Health, Relics, Potions, Focus, Steps, Gold,
            Level, Experience, EnemiesDefeated, DifficultyIndex, MapIndex,
            RandomState, DailyRunId
        };
        return string.Join("|", string.Join(",", stats), ExploredFlags, OpenedFlags, DefeatedEncounterFlags);
    }

    public static bool TryDeserialize(string serialized, out DungeonRunSaveData snapshot, out string failure)
    {
        snapshot = null;
        failure = "保存データの形式が壊れています。";
        if (string.IsNullOrEmpty(serialized)) return false;

        string[] segments = serialized.Split('|');
        if (segments.Length != 4) return false;

        string[] statStrings = segments[0].Split(',');
        if (!IsSupportedStatCount(statStrings.Length))
        {
            failure = "保存データの項目数が一致しません。";
            return false;
        }

        int[] values = new int[statStrings.Length];
        for (int i = 0; i < values.Length; i++)
        {
            if (int.TryParse(statStrings[i], out values[i])) continue;
            failure = "保存データに不正な数値が含まれています。";
            return false;
        }

        snapshot = new DungeonRunSaveData
        {
            PlayerX = values[0],
            PlayerY = values[1],
            Health = values[2],
            Relics = values[3],
            Potions = values[4],
            Focus = values[5],
            Steps = values[6],
            Gold = values[7],
            Level = statStrings.Length >= 10 ? values[8] : 1,
            Experience = statStrings.Length >= 10 ? values[9] : 0,
            EnemiesDefeated = statStrings.Length >= 11 ? values[10] : 0,
            DifficultyIndex = statStrings.Length >= 12 ? values[11] : 1,
            MapIndex = statStrings.Length >= 14 ? values[12] : 0,
            RandomState = statStrings.Length >= 14 ? values[13] : 2026 + values[6],
            DailyRunId = statStrings.Length >= CurrentStatCount ? values[14] : 0,
            ExploredFlags = segments[1],
            OpenedFlags = segments[2],
            DefeatedEncounterFlags = segments[3]
        };
        failure = null;
        return true;
    }

    private static bool IsSupportedStatCount(int count)
    {
        return count == MinimumStatCount || count == 10 || count == 11 || count == 12 || count == 14 || count == CurrentStatCount;
    }
}
