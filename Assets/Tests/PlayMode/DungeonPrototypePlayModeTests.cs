using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class DungeonPrototypePlayModeTests
{
    private static readonly string[] StringKeys =
    {
        "arcane-depths-save-v1",
        "arcane-depths-save-backup-v1"
    };

    private static readonly string[] IntKeys =
    {
        "arcane-depths-save-schema",
        "arcane-depths-save-backup-schema",
        "arcane-depths-best-score",
        "arcane-depths-total-runs",
        "arcane-depths-cleared-runs",
        "arcane-depths-achievements",
        "arcane-depths-cleared-map-mask",
        "arcane-depths-total-gold",
        "arcane-depths-total-defeated",
        "arcane-depths-total-steps",
        "arcane-depths-fastest-clear"
    };

    private readonly Dictionary<string, string> savedStrings = new Dictionary<string, string>();
    private readonly Dictionary<string, int> savedInts = new Dictionary<string, int>();

    [SetUp]
    public void PreservePlayerProfile()
    {
        foreach (string key in StringKeys)
            if (PlayerPrefs.HasKey(key)) savedStrings[key] = PlayerPrefs.GetString(key);
        foreach (string key in IntKeys)
            if (PlayerPrefs.HasKey(key)) savedInts[key] = PlayerPrefs.GetInt(key);

        PlayerPrefs.DeleteKey("arcane-depths-save-v1");
        PlayerPrefs.DeleteKey("arcane-depths-save-schema");
        PlayerPrefs.DeleteKey("arcane-depths-save-backup-v1");
        PlayerPrefs.DeleteKey("arcane-depths-save-backup-schema");
        PlayerPrefs.Save();
    }

    [TearDown]
    public void RestorePlayerProfile()
    {
        foreach (string key in StringKeys) PlayerPrefs.DeleteKey(key);
        foreach (string key in IntKeys) PlayerPrefs.DeleteKey(key);
        foreach (KeyValuePair<string, string> entry in savedStrings) PlayerPrefs.SetString(entry.Key, entry.Value);
        foreach (KeyValuePair<string, int> entry in savedInts) PlayerPrefs.SetInt(entry.Key, entry.Value);
        PlayerPrefs.Save();
    }

    [UnityTest]
    public IEnumerator DungeonPrototype_StartsAndOpensTheTutorial()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync("DungeonPrototype", LoadSceneMode.Single);
        while (!load.isDone) yield return null;
        yield return null;

        DungeonPrototype prototype = Object.FindFirstObjectByType<DungeonPrototype>();
        Assert.That(prototype, Is.Not.Null);
        Assert.That(GetPrivateField(prototype, "mode").ToString(), Is.EqualTo("Title"));

        InvokePrivate(prototype, "StartNewRun");
        yield return null;

        Assert.That(GetPrivateField(prototype, "mode").ToString(), Is.EqualTo("Exploring"));
        Assert.That((bool)GetPrivateField(prototype, "showTutorial"), Is.True);
        Assert.That((Vector2Int)GetPrivateField(prototype, "facing"), Is.EqualTo(Vector2Int.right));

        Vector2Int initialPosition = (Vector2Int)GetPrivateField(prototype, "player");
        InvokePrivate(prototype, "TryMove", Vector2Int.right);
        Assert.That((Vector2Int)GetPrivateField(prototype, "player"), Is.EqualTo(initialPosition + Vector2Int.right));
        Assert.That((Vector2Int)GetPrivateField(prototype, "facing"), Is.EqualTo(Vector2Int.right));

        string[] resetFlags = { "opened", "defeatedEncounters" };
        foreach (string name in resetFlags) ((bool[,])GetPrivateField(prototype, name))[1, 1] = true;
        InvokePrivate(prototype, "ResetRun", false);
        foreach (string name in resetFlags)
            foreach (bool flag in (bool[,])GetPrivateField(prototype, name)) Assert.That(flag, Is.False, name);

        string savedRun = PlayerPrefs.GetString("arcane-depths-save-v1");
        Assert.That(savedRun, Is.Not.Empty);
        InvokePrivate(prototype, "StartDailyRun");
        Assert.That((bool)GetPrivateField(prototype, "restartAsDaily"), Is.True);
        Assert.That((bool)GetPrivateField(prototype, "showRestartConfirm"), Is.True);
        Assert.That(PlayerPrefs.GetString("arcane-depths-save-v1"), Is.EqualTo(savedRun));
        InvokePrivate(prototype, "StartNewRun");
        Assert.That((bool)GetPrivateField(prototype, "restartAsDaily"), Is.False);
        Assert.That((bool)GetPrivateField(prototype, "showRestartConfirm"), Is.True);
        Assert.That(PlayerPrefs.GetString("arcane-depths-save-v1"), Is.EqualTo(savedRun));

        var originalDungeon = (System.Array)GetPrivateField(prototype, "dungeon");
        var originalPlayer = (Vector2Int)GetPrivateField(prototype, "player");
        System.Type cellType = originalDungeon.GetType().GetElementType();
        System.Array testDungeon = System.Array.CreateInstance(cellType, originalDungeon.GetLength(0), originalDungeon.GetLength(1));
        object floor = System.Enum.Parse(cellType, "Floor");
        for (int y = 1; y <= 2; y++)
            for (int x = 1; x <= 2; x++) testDungeon.SetValue(floor, x, y);
        try
        {
            SetPrivateField(prototype, "dungeon", testDungeon);
            SetPrivateField(prototype, "player", new Vector2Int(1, 1));
            object[] arguments = { new Vector2Int(2, 2), default(Vector2Int) };
            Assert.That((bool)InvokePrivate(prototype, "TryFindNextStep", arguments), Is.True);
            Assert.That((Vector2Int)arguments[1], Is.EqualTo(new Vector2Int(1, 2)));
        }
        finally
        {
            SetPrivateField(prototype, "dungeon", originalDungeon);
            SetPrivateField(prototype, "player", originalPlayer);
        }
    }

    [Test]
    public void DailySeed_IsStableForTheSameCalendarDay()
    {
        int first = (int)InvokePrivateStatic(typeof(DungeonPrototype), "DailySeed", 20260831);
        int second = (int)InvokePrivateStatic(typeof(DungeonPrototype), "DailySeed", 20260831);
        int differentDay = (int)InvokePrivateStatic(typeof(DungeonPrototype), "DailySeed", 20260901);

        Assert.That(first, Is.EqualTo(second));
        Assert.That(first, Is.Not.EqualTo(0));
        Assert.That(differentDay, Is.Not.EqualTo(first));
    }

    private static object GetPrivateField(object target, string name)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing private field: " + name);
        return field.GetValue(target);
    }

    private static void SetPrivateField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, "Missing private field: " + name);
        field.SetValue(target, value);
    }

    private static object InvokePrivate(object target, string name, params object[] arguments)
    {
        MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "Missing private method: " + name);
        return method.Invoke(target, arguments);
    }

    private static object InvokePrivateStatic(System.Type type, string name, params object[] arguments)
    {
        MethodInfo method = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "Missing private static method: " + name);
        return method.Invoke(null, arguments);
    }
}
