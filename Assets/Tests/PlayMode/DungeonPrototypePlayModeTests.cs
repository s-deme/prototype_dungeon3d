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

    private static void InvokePrivate(object target, string name)
    {
        MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "Missing private method: " + name);
        method.Invoke(target, null);
    }

    private static object InvokePrivateStatic(System.Type type, string name, params object[] arguments)
    {
        MethodInfo method = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "Missing private static method: " + name);
        return method.Invoke(null, arguments);
    }
}
