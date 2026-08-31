using System;

/// <summary>
/// Converts rectangular dungeon state grids to and from their compact save-data
/// representation. Restoration validates the whole payload before changing the
/// target grid, so a malformed save cannot leave it partially updated.
/// </summary>
public static class DungeonGridFlags
{
    public static string Serialize(bool[,] flags)
    {
        if (flags == null) throw new ArgumentNullException(nameof(flags));

        int width = flags.GetLength(0);
        int height = flags.GetLength(1);
        char[] data = new char[width * height];
        int index = 0;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                data[index++] = flags[x, y] ? '1' : '0';
        return new string(data);
    }

    public static bool TryRestore(string data, bool[,] flags)
    {
        if (data == null || flags == null || data.Length != flags.Length) return false;

        for (int index = 0; index < data.Length; index++)
            if (data[index] != '0' && data[index] != '1') return false;

        int width = flags.GetLength(0);
        int height = flags.GetLength(1);
        int dataIndex = 0;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                flags[x, y] = data[dataIndex++] == '1';
        return true;
    }
}
