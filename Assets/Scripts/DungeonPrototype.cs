using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A self-contained, asset-free 2D dungeon-crawler prototype.
/// Attach this to any GameObject in an otherwise empty scene and press Play.
/// </summary>
public class DungeonPrototype : MonoBehaviour
{
    private enum Cell { Wall, Floor, Start, Chest, Potion, Shrine, Fountain, Stairs }
    private enum Mode { Title, Exploring, Combat, Shrine, Paused, Victory, Defeat }
    private enum Difficulty { Wanderer, Adventurer, Abyssal }

    // # = wall, . = corridor, S = start, C = relic cache, F = healing spring, > = exit
    private readonly string[] layout =
    {
        "#####################",
        "#S....#.......#.....#",
        "#.###.#.#####.#.###.#",
        "#..P#.#.....#.#...#.#",
        "###.#.#####.#.###.#.#",
        "#...#.....#.#.....#.#",
        "#.#######.#.#######.#",
        "#.....#...#.R...#...#",
        "#####.#.#######.#.###",
        "#...#.#.....#...#.P.#",
        "#.#.#.#####.#.#####.#",
        "#.#...#C..#.#.....#.#",
        "#.#####.#.#.#####.#.#",
        "#.....#.#...#C..#.#F#",
        "###.#.#.#######.#.#.#",
        "#C..#..............>#",
        "#####################"
    };

    private Cell[,] dungeon;
    private bool[,] explored;
    private bool[,] opened;
    private bool[,] defeatedEncounters;
    private Vector2Int player;
    private Vector2Int start;
    private int width;
    private int height;
    private int health = 12;
    private const int MaxHealth = 12;
    private const int MaxFocus = 3;
    private const string SaveKey = "arcane-depths-save-v1";
    private const string SoundSettingKey = "arcane-depths-sound";
    private const string UiScaleSettingKey = "arcane-depths-ui-scale";
    private const string GuidanceSettingKey = "arcane-depths-guidance";
    private const string ContrastSettingKey = "arcane-depths-high-contrast";
    private const string DifficultySettingKey = "arcane-depths-difficulty";
    private const string BestScoreKey = "arcane-depths-best-score";
    private const string TotalRunsKey = "arcane-depths-total-runs";
    private const string ClearedRunsKey = "arcane-depths-cleared-runs";
    private const string AchievementKey = "arcane-depths-achievements";
    private const int MoonbearerAchievement = 1;
    private const int CartographerAchievement = 2;
    private const int VeteranAchievement = 4;
    private int relics;
    private int potions;
    private int focus;
    private int level;
    private int experience;
    private int bestScore;
    private int totalRuns;
    private int clearedRuns;
    private int achievements;
    private int enemiesDefeated;
    private int steps;
    private int gold;
    private int enemyHealth;
    private int enemyMaxHealth;
    private string enemyName;
    private string enemyIntent;
    private Mode mode;
    private Mode modeBeforePause;
    private bool showHelp;
    private bool showSettings;
    private bool showProfile;
    private bool showRestartConfirm;
    private bool showTutorial;
    private int tutorialStep;
    private bool soundEnabled;
    private bool guidanceEnabled;
    private bool highContrast;
    private bool compactLayout;
    private float uiScale;
    private Difficulty selectedDifficulty;
    private Difficulty runDifficulty;
    private string message;
    private string eventTitle;
    private readonly Queue<string> journal = new Queue<string>();
    private System.Random rng;

    private Texture2D pixel;
    private AudioSource audioSource;
    private GUIStyle titleStyle;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle smallStyle;
    private GUIStyle centerStyle;
    private GUIStyle buttonStyle;

    // Every visible colour is defined by one theme. This prevents low-contrast one-off UI states.
    private Color ink;
    private Color panel;
    private Color panelLine;
    private Color floor;
    private Color wall;
    private Color unknown;
    private Color wallAccent;
    private Color cyan;
    private Color amber;
    private Color pink;
    private Color text;
    private Color mutedText;
    private Color buttonText;
    private Color disabledButton;
    private Color inactiveIndicator;
    private Color potionColor;
    private Color shrineColor;
    private Color lockedExitColor;
    private Texture2D buttonNormalTexture;
    private Texture2D buttonHoverTexture;
    private Texture2D buttonActiveTexture;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = 0.35f;
        DungeonMapValidator.ValidateOrThrow(layout);
        BuildDungeon();
        soundEnabled = PlayerPrefs.GetInt(SoundSettingKey, 1) == 1;
        uiScale = Mathf.Clamp(PlayerPrefs.GetInt(UiScaleSettingKey, 100) / 100f, 0.9f, 1.4f);
        guidanceEnabled = PlayerPrefs.GetInt(GuidanceSettingKey, 1) == 1;
        highContrast = PlayerPrefs.GetInt(ContrastSettingKey, 0) == 1;
        selectedDifficulty = (Difficulty)Mathf.Clamp(PlayerPrefs.GetInt(DifficultySettingKey, (int)Difficulty.Adventurer), 0, 2);
        runDifficulty = selectedDifficulty;
        ApplyTheme();
        LoadProfile();
        mode = Mode.Title;
    }

    private void BuildDungeon()
    {
        height = layout.Length;
        width = layout[0].Length;
        dungeon = new Cell[width, height];
        explored = new bool[width, height];
        opened = new bool[width, height];
        defeatedEncounters = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                switch (layout[y][x])
                {
                    case '#': dungeon[x, y] = Cell.Wall; break;
                    case 'S': dungeon[x, y] = Cell.Start; start = new Vector2Int(x, y); break;
                    case 'C': dungeon[x, y] = Cell.Chest; break;
                    case 'P': dungeon[x, y] = Cell.Potion; break;
                    case 'R': dungeon[x, y] = Cell.Shrine; break;
                    case 'F': dungeon[x, y] = Cell.Fountain; break;
                    case '>': dungeon[x, y] = Cell.Stairs; break;
                    default: dungeon[x, y] = Cell.Floor; break;
                }
            }
        }
    }

    private void ResetRun()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        totalRuns++;
        SaveProfile();
        runDifficulty = selectedDifficulty;
        player = start;
        health = MaxHealth;
        relics = 0;
        potions = 0;
        focus = 1;
        level = 1;
        experience = 0;
        enemiesDefeated = 0;
        steps = 0;
        gold = 0;
        mode = Mode.Exploring;
        message = DifficultyDescription(runDifficulty) + " 3つの月の欠片を探せ。";
        eventTitle = "探索開始";
        journal.Clear();
        rng = new System.Random(2026);
        Array.Clear(explored, 0, explored.Length);
        Array.Clear(opened, 0, opened.Length);
        Array.Clear(defeatedEncounters, 0, defeatedEncounters.Length);
        RevealAroundPlayer();
        AddJournal("迷宮の入口 — 松明の灯りが揺れている。");
        SaveRun();
    }

    private void ApplyTheme()
    {
        if (highContrast)
        {
            ink = new Color(0.005f, 0.009f, 0.015f, 1f);
            panel = new Color(0.025f, 0.055f, 0.09f, 1f);
            panelLine = Color.white;
            floor = new Color(0.16f, 0.27f, 0.35f, 1f);
            wall = new Color(0.004f, 0.008f, 0.014f, 1f);
            unknown = new Color(0.012f, 0.018f, 0.028f, 1f);
            wallAccent = new Color(0.45f, 0.62f, 0.72f, 1f);
            cyan = new Color(0.35f, 0.96f, 1f, 1f);
            amber = new Color(1f, 0.88f, 0.18f, 1f);
            pink = new Color(1f, 0.47f, 0.62f, 1f);
            text = Color.white;
            mutedText = new Color(0.84f, 0.91f, 0.96f, 1f);
            buttonText = new Color(0.004f, 0.012f, 0.02f, 1f);
            disabledButton = new Color(0.31f, 0.39f, 0.46f, 1f);
            inactiveIndicator = new Color(0.82f, 0.89f, 0.94f, 1f);
            potionColor = new Color(0.64f, 0.86f, 1f, 1f);
            shrineColor = new Color(0.9f, 0.75f, 1f, 1f);
            lockedExitColor = Color.white;
        }
        else
        {
            ink = new Color(0.018f, 0.03f, 0.052f, 1f);
            panel = new Color(0.055f, 0.09f, 0.145f, 0.99f);
            panelLine = new Color(0.35f, 0.66f, 0.83f, 1f);
            floor = new Color(0.13f, 0.22f, 0.29f, 1f);
            wall = new Color(0.018f, 0.037f, 0.064f, 1f);
            unknown = new Color(0.007f, 0.014f, 0.025f, 1f);
            wallAccent = new Color(0.23f, 0.39f, 0.50f, 1f);
            cyan = new Color(0.44f, 0.96f, 1f, 1f);
            amber = new Color(1f, 0.79f, 0.32f, 1f);
            pink = new Color(1f, 0.42f, 0.60f, 1f);
            text = new Color(0.96f, 0.98f, 1f, 1f);
            mutedText = new Color(0.77f, 0.85f, 0.91f, 1f);
            buttonText = new Color(0.012f, 0.039f, 0.068f, 1f);
            disabledButton = new Color(0.22f, 0.30f, 0.38f, 1f);
            inactiveIndicator = new Color(0.54f, 0.66f, 0.75f, 1f);
            potionColor = new Color(0.48f, 0.74f, 1f, 1f);
            shrineColor = new Color(0.81f, 0.65f, 1f, 1f);
            lockedExitColor = mutedText;
        }

        CreateStyles();
    }

    private void CreateStyles()
    {
        titleStyle = MakeStyle(31, FontStyle.Bold, cyan, TextAnchor.MiddleLeft);
        headerStyle = MakeStyle(16, FontStyle.Bold, amber, TextAnchor.MiddleLeft);
        labelStyle = MakeStyle(15, FontStyle.Bold, text, TextAnchor.UpperLeft);
        smallStyle = MakeStyle(13, FontStyle.Bold, mutedText, TextAnchor.UpperLeft);
        centerStyle = MakeStyle(17, FontStyle.Bold, text, TextAnchor.MiddleCenter);
        buttonStyle = MakeStyle(15, FontStyle.Bold, buttonText, TextAnchor.MiddleCenter);
        ReplaceButtonTextures();
        buttonStyle.normal.background = buttonNormalTexture;
        buttonStyle.hover.background = buttonHoverTexture;
        buttonStyle.active.background = buttonActiveTexture;
        buttonStyle.hover.textColor = buttonText;
        buttonStyle.active.textColor = buttonText;
        buttonStyle.padding = new RectOffset(8, 8, 5, 5);
    }

    private void ReplaceButtonTextures()
    {
        if (buttonNormalTexture != null) Destroy(buttonNormalTexture);
        if (buttonHoverTexture != null) Destroy(buttonHoverTexture);
        if (buttonActiveTexture != null) Destroy(buttonActiveTexture);
        buttonNormalTexture = MakeTexture(amber);
        buttonHoverTexture = MakeTexture(cyan);
        buttonActiveTexture = MakeTexture(pink);
    }

    private GUIStyle MakeStyle(int size, FontStyle fontStyle, Color color, TextAnchor anchor)
    {
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(size * uiScale),
            fontStyle = fontStyle,
            normal = { textColor = color },
            alignment = anchor,
            wordWrap = true,
            richText = true
        };
    }

    private Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void Update()
    {
        if (showRestartConfirm)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) showRestartConfirm = false;
            return;
        }

        if (showProfile)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F3)) showProfile = false;
            return;
        }

        if (showHelp)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F1)) showHelp = false;
            return;
        }

        if (showTutorial)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) AdvanceTutorial();
            return;
        }

        if (Input.GetKeyDown(KeyCode.F2)) showSettings = !showSettings;
        if (showSettings) return;

        if (mode == Mode.Title)
        {
            if (Input.GetKeyDown(KeyCode.F1)) showHelp = !showHelp;
            if (Input.GetKeyDown(KeyCode.F3)) showProfile = !showProfile;
            if (Input.GetKeyDown(KeyCode.Return)) StartNewRun();
            if (Input.GetKeyDown(KeyCode.L) && PlayerPrefs.HasKey(SaveKey)) ContinueRun();
            return;
        }

        if (Input.GetKeyDown(KeyCode.F1)) showHelp = !showHelp;
        if (Input.GetKeyDown(KeyCode.F3)) showProfile = !showProfile;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (mode == Mode.Paused) mode = modeBeforePause;
            else if (mode == Mode.Exploring || mode == Mode.Combat)
            {
                modeBeforePause = mode;
                mode = Mode.Paused;
            }
        }

        if (showHelp || mode == Mode.Paused) return;

        if (mode == Mode.Exploring)
        {
            // The layout is stored top-to-bottom, so screen-up decrements its y index.
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) TryMove(Vector2Int.down);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) TryMove(Vector2Int.up);
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) TryMove(Vector2Int.left);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) TryMove(Vector2Int.right);
            if (Input.GetKeyDown(KeyCode.E)) UsePotion();
        }
        else if (mode == Mode.Combat)
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.Space)) Attack();
            if (Input.GetKeyDown(KeyCode.Q)) ArcaneStrike();
            if (Input.GetKeyDown(KeyCode.G)) Guard();
            if (Input.GetKeyDown(KeyCode.R)) TryFlee();
            if (Input.GetKeyDown(KeyCode.E)) UsePotion(true);
        }
        else if (mode == Mode.Shrine)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectBlessing(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectBlessing(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectBlessing(2);
        }
        else if ((mode == Mode.Victory || mode == Mode.Defeat) && Input.GetKeyDown(KeyCode.Return))
        {
            ResetRun();
        }
    }

    private void TryMove(Vector2Int direction)
    {
        Vector2Int destination = player + direction;
        if (!InBounds(destination) || dungeon[destination.x, destination.y] == Cell.Wall)
        {
            eventTitle = "行き止まり";
            message = "冷たい石壁が行く手を阻む。";
            PlayTone(120f, .055f, .10f);
            return;
        }

        player = destination;
        steps++;
        PlayTone(235f, .035f, .055f);
        RevealAroundPlayer();
        CheckCartographerAchievement();
        ResolveCell();
        // Combat is resumed from the previous safe auto-save, never into a half-resolved enemy turn.
        if (mode == Mode.Exploring) SaveRun();
    }

    private void StartNewRun()
    {
        ResetRun();
        tutorialStep = 0;
        showTutorial = true;
    }

    private void ContinueRun()
    {
        if (!TryLoadRun()) ResetRun();
    }

    private string DifficultyName(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Wanderer: return "旅人";
            case Difficulty.Abyssal: return "深淵";
            default: return "冒険者";
        }
    }

    private string DifficultyDescription(Difficulty difficulty)
    {
        switch (difficulty)
        {
            case Difficulty.Wanderer: return "旅人の加護により、敵の攻撃は弱まっている。";
            case Difficulty.Abyssal: return "深淵が牙を研いでいる。敵の攻撃に注意せよ。";
            default: return "古代の迷宮に足を踏み入れた。";
        }
    }

    private float ScoreMultiplier()
    {
        switch (runDifficulty)
        {
            case Difficulty.Wanderer: return .85f;
            case Difficulty.Abyssal: return 1.25f;
            default: return 1f;
        }
    }

    private void ResolveCell()
    {
        Cell current = dungeon[player.x, player.y];
        if (current == Cell.Chest && !opened[player.x, player.y])
        {
            opened[player.x, player.y] = true;
            relics++;
            int foundGold = 14 + rng.Next(17);
            gold += foundGold;
            bool leveledUp = GainExperience(5);
            eventTitle = "月の欠片を発見";
            message = "銀の小箱に眠る欠片を回収した。出口の封印が弱まる。" + (leveledUp ? " レベルが上がった！" : "");
            AddJournal($"欠片 {relics}/3 と {foundGold} ゴールドを獲得。");
            PlayTone(730f, .18f, .14f);
            return;
        }

        if (current == Cell.Potion && !opened[player.x, player.y])
        {
            opened[player.x, player.y] = true;
            potions++;
            eventTitle = "回復薬を発見";
            message = "青い小瓶を鞄にしまった。探索中なら E キーで使用できる。";
            AddJournal("回復薬を1本入手した。");
            PlayTone(520f, .12f, .09f);
            return;
        }

        if (current == Cell.Fountain)
        {
            int healed = MaxHealth - health;
            health = MaxHealth;
            eventTitle = "星明かりの泉";
            message = healed > 0 ? $"澄んだ水が傷を癒した。体力が {healed} 回復。" : "水面にはあなたの顔だけが映っている。";
            AddJournal("星明かりの泉で休息した。");
            PlayTone(640f, .16f, .10f);
            return;
        }

        if (current == Cell.Shrine && !opened[player.x, player.y])
        {
            eventTitle = "星見の祠";
            message = "三つの光が、あなたの選択を待っている。";
            mode = Mode.Shrine;
            PlayTone(470f, .18f, .12f);
            return;
        }

        if (current == Cell.Stairs)
        {
            if (relics >= 3)
            {
                mode = Mode.Victory;
                PlayerPrefs.DeleteKey(SaveKey);
                PlayerPrefs.Save();
                UnlockAchievement(MoonbearerAchievement, "月の帰還者");
                FinalizeRun(true);
                eventTitle = "迷宮踏破";
                message = "3つの月の欠片が門を開いた。あなたは地上の月光へ帰還する。";
            }
            else
            {
                eventTitle = "封じられた出口";
                message = $"石扉には月の印。あと {3 - relics} 個の欠片が必要だ。";
            }
            return;
        }

        // Deterministic encounter spaces make the prototype easy to test and replay.
        if (!defeatedEncounters[player.x, player.y] && IsEncounterTile(player))
        {
            defeatedEncounters[player.x, player.y] = true;
            StartCombat();
            return;
        }

        eventTitle = "探索中";
        message = "遠くで何かが石床を引きずる音がする。";
    }

    private bool IsEncounterTile(Vector2Int p)
    {
        return (p.x == 5 && p.y == 5) || (p.x == 8 && p.y == 7) || (p.x == 3 && p.y == 13) || (p.x == 16 && p.y == 15);
    }

    private void StartCombat()
    {
        mode = Mode.Combat;
        bool stronger = relics >= 2;
        enemyName = stronger ? "深淵の番犬" : (relics == 1 ? "石化コウモリ" : "迷い骨");
        enemyMaxHealth = (stronger ? 11 : (relics == 1 ? 8 : 6)) + (level - 1) * 2;
        enemyHealth = enemyMaxHealth;
        enemyIntent = stronger ? "重い牙で噛み砕こうとしている" : "鋭い一撃を狙っている";
        eventTitle = "敵襲 — " + enemyName;
        message = "影が松明の灯りを横切った。構えろ。";
        AddJournal(enemyName + " が襲いかかってきた！");
        PlayTone(145f, .2f, .15f);
    }

    private void UsePotion(bool inCombat = false)
    {
        if (potions <= 0)
        {
            eventTitle = "回復薬がない";
            message = "空のベルトを探った。迷宮内の青い薬瓶を見つけよう。";
            return;
        }
        if (health >= MaxHealth)
        {
            eventTitle = "まだ使えない";
            message = "体力は満タンだ。薬は必要なときまで温存しよう。";
            return;
        }

        int healed = Mathf.Min(6, MaxHealth - health);
        potions--;
        health += healed;
        eventTitle = "回復薬を使用";
        message = $"苦い薬が傷を塞いだ。体力が {healed} 回復。";
        AddJournal("回復薬を使った。");
        PlayTone(590f, .16f, .11f);
        if (inCombat) EnemyTurn(false, $"回復薬で体力を {healed} 回復した。");
        else SaveRun();
    }

    private void Attack()
    {
        int damage = 3 + rng.Next(3) + level - 1;
        enemyHealth -= damage;
        focus = Mathf.Min(MaxFocus, focus + 1);
        PlayTone(300f, .06f, .09f);
        if (enemyHealth <= 0)
        {
            FinishCombat();
            return;
        }

        EnemyTurn(false, $"斬撃で {damage} ダメージ。集中力が1上がった。");
    }

    private void SelectBlessing(int choice)
    {
        opened[player.x, player.y] = true;
        mode = Mode.Exploring;
        if (choice == 0)
        {
            health = MaxHealth;
            eventTitle = "生命の祝福";
            message = "温かな光が体を満たし、体力が全回復した。";
        }
        else if (choice == 1)
        {
            potions += 2;
            focus = MaxFocus;
            eventTitle = "備蓄の祝福";
            message = "回復薬を2本得て、集中力が満ちた。";
        }
        else
        {
            gold += 35;
            eventTitle = "富の祝福";
            message = "古い硬貨が足元に降り注いだ。35 ゴールドを得た。";
        }
        AddJournal("星見の祠の祝福を受けた。");
        PlayTone(760f, .22f, .15f);
        SaveRun();
    }

    private void ArcaneStrike()
    {
        if (focus <= 0)
        {
            eventTitle = "集中力不足";
            message = "秘術斬りには集中力が必要だ。通常攻撃で集中力を溜めよう。";
            return;
        }

        focus--;
        int damage = 6 + rng.Next(4) + level - 1;
        enemyHealth -= damage;
        PlayTone(780f, .14f, .14f);
        if (enemyHealth <= 0)
        {
            FinishCombat();
            return;
        }

        EnemyTurn(false, $"秘術斬りが炸裂、{damage} ダメージ。青い残光が消えた。");
    }

    private void FinishCombat()
    {
        int reward = 7 + rng.Next(11);
        gold += reward;
        mode = Mode.Exploring;
        enemiesDefeated++;
        if (enemiesDefeated >= 4) UnlockAchievement(VeteranAchievement, "迷宮の征服者");
        bool leveledUp = GainExperience(8 + relics * 2);
        eventTitle = "勝利";
        message = $"{enemyName} を倒した。{reward} ゴールドを拾った。" + (leveledUp ? " レベルアップ！ 体力と集中力が回復した。" : "");
        AddJournal(enemyName + " を打ち倒した。");
        SaveRun();
    }

    private void Guard()
    {
        EnemyTurn(true, "盾を掲げ、次の一撃に備えた。");
    }

    private void TryFlee()
    {
        if (rng.NextDouble() < 0.52)
        {
            mode = Mode.Exploring;
            defeatedEncounters[player.x, player.y] = false;
            eventTitle = "離脱成功";
            message = "暗がりへ身を滑らせ、敵を撒いた。";
            AddJournal(enemyName + " から逃走した。");
            SaveRun();
        }
        else
        {
            EnemyTurn(false, "退路を探したが、敵に回り込まれた！");
        }
    }

    private void EnemyTurn(bool guarded, string prefix)
    {
        int hit = 2 + rng.Next(3);
        if (runDifficulty == Difficulty.Wanderer) hit = Mathf.Max(1, hit - 1);
        if (runDifficulty == Difficulty.Abyssal) hit++;
        if (guarded) hit = Mathf.Max(1, hit - 2);
        health -= hit;
        PlayTone(165f, .08f, .11f);
        enemyIntent = rng.NextDouble() < 0.45 ? "様子をうかがい、次の一撃を狙っている" : "容赦なく前へ踏み込んでくる";
        message = prefix + $" {enemyName} の反撃、{hit} ダメージ。";
        if (health <= 0)
        {
            health = 0;
            mode = Mode.Defeat;
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            FinalizeRun(false);
            eventTitle = "冒険の終わり";
            message = "灯りが消え、迷宮は再び静寂に包まれた。";
            AddJournal("あなたは迷宮に倒れた。");
        }
    }

    private void RevealAroundPlayer()
    {
        for (int y = -2; y <= 2; y++)
        {
            for (int x = -2; x <= 2; x++)
            {
                Vector2Int p = player + new Vector2Int(x, y);
                if (InBounds(p) && Mathf.Abs(x) + Mathf.Abs(y) <= 3) explored[p.x, p.y] = true;
            }
        }
    }

    private void CheckCartographerAchievement()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (dungeon[x, y] != Cell.Wall && !explored[x, y]) return;
            }
        }
        UnlockAchievement(CartographerAchievement, "迷宮の地図師");
    }

    private bool InBounds(Vector2Int p) => p.x >= 0 && p.x < width && p.y >= 0 && p.y < height;

    private void AddJournal(string text)
    {
        journal.Enqueue(text);
        while (journal.Count > 3) journal.Dequeue();
    }

    private void FollowGuidance()
    {
        if (!TryGetObjective(out Vector2Int target, out string targetName) || target == player)
        {
            eventTitle = "導き";
            message = targetName + "はここにある。";
            return;
        }

        if (!TryFindNextStep(target, out Vector2Int next))
        {
            eventTitle = "導きが届かない";
            message = "地図を広げても、次の一歩を見つけられない。";
            return;
        }

        TryMove(next - player);
    }

    private void RequestRestart()
    {
        showRestartConfirm = true;
    }

    private void OnGUI()
    {
        float scale = Mathf.Clamp(Screen.height / 900f, 0.72f, 1.15f) * uiScale;
        float sideWidth = 300f * scale;
        float margin = 24f * scale;
        compactLayout = Screen.width < 820f * scale || Screen.width / Mathf.Max(1f, Screen.height) < 1.08f;
        Rect mapRect;
        Rect sideRect;
        if (compactLayout)
        {
            float mapWidth = Mathf.Min(Screen.width - margin * 2f, (Screen.height - margin * 3f) * .61f * width / height);
            mapRect = new Rect((Screen.width - mapWidth) * .5f, margin, mapWidth, mapWidth * height / width);
            sideRect = new Rect(margin, mapRect.yMax + margin, Screen.width - margin * 2f, Mathf.Max(132f * scale, Screen.height - mapRect.yMax - margin * 2f));
        }
        else
        {
            // mapSize is its width; calculate the height constraint using the map's aspect ratio.
            float mapSize = Mathf.Min((Screen.height - margin * 2f) * width / height, Screen.width - sideWidth - margin * 3f);
            mapRect = new Rect(margin, (Screen.height - mapSize) * .5f, mapSize, mapSize * height / width);
            sideRect = new Rect(mapRect.xMax + margin, mapRect.y, sideWidth, mapRect.height);
        }

        DrawBackground();
        if (mode == Mode.Title)
        {
            DrawTitleScreen(scale);
            if (showHelp) DrawHelpOverlay(scale);
            if (showProfile) DrawProfileOverlay(scale);
            if (showSettings) DrawSettingsOverlay(scale);
            return;
        }
        DrawMap(mapRect);
        DrawSidePanel(sideRect, scale);
        if (mode == Mode.Exploring) DrawTouchControls(mapRect, scale);

        if (mode == Mode.Combat) DrawCombatOverlay(scale);
        if (mode == Mode.Shrine) DrawShrineOverlay(scale);
        if (mode == Mode.Victory || mode == Mode.Defeat) DrawEndingOverlay(scale);
        if (mode == Mode.Paused) DrawPauseOverlay(scale);
        if (showHelp) DrawHelpOverlay(scale);
        if (showProfile) DrawProfileOverlay(scale);
        if (showSettings) DrawSettingsOverlay(scale);
        if (showTutorial) DrawTutorialOverlay(scale);
        if (showRestartConfirm) DrawRestartConfirmOverlay(scale);
    }

    private void DrawBackground()
    {
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), ink);
        DrawRect(new Rect(0, 0, Screen.width, 5), cyan);
    }

    private void DrawMap(Rect rect)
    {
        DrawPanel(new Rect(rect.x - 8, rect.y - 8, rect.width + 16, rect.height + 16));
        float cell = rect.width / width;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Rect cellRect = new Rect(rect.x + x * cell, rect.y + y * cell, cell + 0.5f, cell + 0.5f);
                bool seen = explored[x, y];
                Cell value = dungeon[x, y];
                Color color = !seen ? unknown : (value == Cell.Wall ? wall : floor);
                DrawRect(cellRect, color);
                if (seen && value == Cell.Wall)
                {
                    DrawRect(new Rect(cellRect.x + cell * .18f, cellRect.y + cell * .18f, cell * .64f, cell * .64f), wallAccent);
                }
                if (!seen || value == Cell.Wall) continue;

                if (value == Cell.Chest && !opened[x, y]) DrawGlyph(cellRect, "◆", amber, cell * 0.8f);
                if (value == Cell.Potion && !opened[x, y]) DrawGlyph(cellRect, "●", potionColor, cell * 0.66f);
                if (value == Cell.Shrine && !opened[x, y]) DrawGlyph(cellRect, "✧", shrineColor, cell * 0.82f);
                if (value == Cell.Fountain) DrawGlyph(cellRect, "✦", cyan, cell * 0.8f);
                if (value == Cell.Stairs) DrawGlyph(cellRect, "⌄", relics >= 3 ? cyan : lockedExitColor, cell * 0.9f);
            }
        }

        if (guidanceEnabled && mode == Mode.Exploring && TryGetObjective(out Vector2Int target, out string unused) && target != player && TryFindNextStep(target, out Vector2Int nextStep))
        {
            Rect marker = new Rect(rect.x + nextStep.x * cell, rect.y + nextStep.y * cell, cell, cell);
            Vector2Int delta = nextStep - player;
            string arrow = delta.x > 0 ? "→" : delta.x < 0 ? "←" : delta.y > 0 ? "↓" : "↑";
            DrawGlyph(marker, arrow, cyan, cell * .8f);
        }

        Rect hero = new Rect(rect.x + player.x * cell + cell * .2f, rect.y + player.y * cell + cell * .2f, cell * .6f, cell * .6f);
        DrawRect(hero, pink);
        DrawRect(new Rect(hero.x + hero.width * .25f, hero.y + hero.height * .25f, hero.width * .5f, hero.height * .5f), Color.white);
        GUI.Label(new Rect(rect.x, rect.y - 29, rect.width, 23), guidanceEnabled ? "迷宮地図  /  矢印が導きの次の一歩" : "迷宮地図  /  訪れた場所だけが記録される", smallStyle);
    }

    private void DrawTitleScreen(float scale)
    {
        float w = Mathf.Min(570 * scale, Screen.width - 40);
        float h = Mathf.Min(485 * scale, Screen.height - 32);
        Rect box = new Rect((Screen.width - w) * .5f, (Screen.height - h) * .5f, w, h);
        DrawPanel(box);
        GUI.Label(new Rect(box.x, box.y + 28 * scale, box.width, 45 * scale), "秘術の深淵", MakeStyle(36, FontStyle.Bold, cyan, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(box.x, box.y + 77 * scale, box.width, 23 * scale), "二次元ダンジョン探索", MakeStyle(14, FontStyle.Bold, amber, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(box.x + 62 * scale, box.y + 115 * scale, box.width - 124 * scale, 44 * scale), "月の欠片を3つ探し、迷宮の最深部から生還せよ。", MakeStyle(16, FontStyle.Bold, text, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(box.x, box.y + 161 * scale, box.width, 18 * scale), "難易度を選択", headerStyle);
        float difficultyW = (box.width - 72 * scale) / 3f;
        float difficultyX = box.x + 24 * scale;
        DrawDifficultyButton(new Rect(difficultyX, box.y + 187 * scale, difficultyW, 48 * scale), Difficulty.Wanderer, "旅人\n敵の攻撃 -1");
        DrawDifficultyButton(new Rect(difficultyX + difficultyW + 12 * scale, box.y + 187 * scale, difficultyW, 48 * scale), Difficulty.Adventurer, "冒険者\n標準");
        DrawDifficultyButton(new Rect(difficultyX + (difficultyW + 12 * scale) * 2, box.y + 187 * scale, difficultyW, 48 * scale), Difficulty.Abyssal, "深淵\n敵の攻撃 +1");
        GUI.Label(new Rect(box.x, box.y + 242 * scale, box.width, 18 * scale), AchievementSummary(), smallStyle);
        float buttonW = box.width * .56f;
        float buttonX = box.x + (box.width - buttonW) * .5f;
        if (GUI.Button(new Rect(buttonX, box.y + 270 * scale, buttonW, 38 * scale), $"{DifficultyName(selectedDifficulty)}で新しい探索 [Enter]", buttonStyle)) StartNewRun();

        bool canContinue = PlayerPrefs.HasKey(SaveKey);
        Rect continueButton = new Rect(buttonX, box.y + 317 * scale, buttonW, 38 * scale);
        if (canContinue)
        {
            if (GUI.Button(continueButton, "続きから [L]", buttonStyle)) ContinueRun();
        }
        else DrawDisabledButton(continueButton, "続きから（保存データなし）");
        float utilityW = (box.width - 68 * scale) / 3f;
        float utilityY = box.y + 371 * scale;
        if (GUI.Button(new Rect(box.x + 22 * scale, utilityY, utilityW, 29 * scale), "ガイド [F1]", buttonStyle)) showHelp = true;
        if (GUI.Button(new Rect(box.x + 34 * scale + utilityW, utilityY, utilityW, 29 * scale), "記録 [F3]", buttonStyle)) showProfile = true;
        if (GUI.Button(new Rect(box.x + 46 * scale + utilityW * 2, utilityY, utilityW, 29 * scale), "設定 [F2]", buttonStyle)) showSettings = true;
        GUI.Label(new Rect(box.x, box.yMax - 45 * scale, box.width, 20 * scale), $"記録　探索 {totalRuns} 回　踏破 {clearedRuns} 回　最高記録 {bestScore}　勲章 {AchievementCount()}/3", smallStyle);
        GUI.Label(new Rect(box.x, box.yMax - 23 * scale, box.width, 20 * scale), "探索の進行は自動的に保存されます", smallStyle);
    }

    private void DrawDifficultyButton(Rect rect, Difficulty difficulty, string body)
    {
        string marker = selectedDifficulty == difficulty ? "● " : "○ ";
        if (GUI.Button(rect, marker + body, buttonStyle))
        {
            selectedDifficulty = difficulty;
            PlayerPrefs.SetInt(DifficultySettingKey, (int)selectedDifficulty);
            PlayerPrefs.Save();
        }
    }

    private void DrawDisabledButton(Rect rect, string buttonTextValue)
    {
        DrawRect(rect, disabledButton);
        GUI.Label(rect, buttonTextValue, MakeStyle(15, FontStyle.Bold, mutedText, TextAnchor.MiddleCenter));
    }

    private void DrawTouchControls(Rect mapRect, float scale)
    {
        float button = 34 * scale;
        float gap = 4 * scale;
        float dpadWidth = button * 3 + gap * 2;
        float w = dpadWidth + 164 * scale;
        float h = button * 2 + gap + 41 * scale;
        Rect box = new Rect(mapRect.x + 10 * scale, mapRect.yMax - h - 10 * scale, w, h);
        DrawPanel(box);
        GUI.Label(new Rect(box.x, box.y + 4 * scale, box.width, 18 * scale), "タップで移動", MakeStyle(10, FontStyle.Bold, cyan, TextAnchor.MiddleCenter));
        float y = box.y + 23 * scale;
        float x = box.x + 9 * scale;
        if (GUI.Button(new Rect(x + button + gap, y, button, button), "↑", buttonStyle)) TryMove(Vector2Int.down);
        if (GUI.Button(new Rect(x, y + button + gap, button, button), "←", buttonStyle)) TryMove(Vector2Int.left);
        if (GUI.Button(new Rect(x + button + gap, y + button + gap, button, button), "↓", buttonStyle)) TryMove(Vector2Int.up);
        if (GUI.Button(new Rect(x + (button + gap) * 2, y + button + gap, button, button), "→", buttonStyle)) TryMove(Vector2Int.right);

        float potionX = box.x + dpadWidth + 18 * scale;
        if (GUI.Button(new Rect(potionX, box.y + 20 * scale, 66 * scale, 48 * scale), "薬\n[E]", buttonStyle)) UsePotion();
        if (GUI.Button(new Rect(potionX + 72 * scale, box.y + 20 * scale, 66 * scale, 48 * scale), "導き\n[1歩]", buttonStyle)) FollowGuidance();
    }

    private void DrawSidePanel(Rect rect, float scale)
    {
        DrawPanel(rect);
        float x = rect.x + 18 * scale;
        float widthLocal = rect.width - 36 * scale;
        float y = rect.y + 16 * scale;
        if (compactLayout)
        {
            GUI.Label(new Rect(x, y, widthLocal, 22 * scale), $"体力 {health}/{MaxHealth}　等級 {level}　◆ {relics}/3　薬 {potions}", headerStyle); y += 27 * scale;
            GUI.Label(new Rect(x, y, widthLocal, 24 * scale), "導き: " + ObjectiveHint(), smallStyle); y += 29 * scale;
            GUI.Label(new Rect(x, y, widthLocal, 33 * scale), eventTitle.ToUpperInvariant() + " — " + message, smallStyle);
            float buttonY = rect.yMax - 38 * scale;
            float buttonW = (widthLocal - 18 * scale) / 4f;
            if (GUI.Button(new Rect(x, buttonY, buttonW, 30 * scale), "導き", buttonStyle)) FollowGuidance();
            if (GUI.Button(new Rect(x + (buttonW + 6 * scale), buttonY, buttonW, 30 * scale), "ガイド", buttonStyle)) showHelp = true;
            if (GUI.Button(new Rect(x + (buttonW + 6 * scale) * 2, buttonY, buttonW, 30 * scale), "設定", buttonStyle)) showSettings = true;
            if (GUI.Button(new Rect(x + (buttonW + 6 * scale) * 3, buttonY, buttonW, 30 * scale), "中断", buttonStyle))
            {
                modeBeforePause = mode;
                mode = Mode.Paused;
            }
            return;
        }
        GUI.Label(new Rect(x, y, widthLocal, 40 * scale), "秘術の深淵", titleStyle); y += 42 * scale;
        GUI.Label(new Rect(x, y, widthLocal, 20 * scale), "二次元ダンジョン探索", smallStyle); y += 39 * scale;
        DrawDivider(x, y, widthLocal); y += 14 * scale;
        GUI.Label(new Rect(x, y, widthLocal, 22 * scale), "冒険者", headerStyle); y += 26 * scale;
        GUI.Label(new Rect(x, y, widthLocal, 20 * scale), $"体力   {HealthBar()}  {health}/{MaxHealth}", labelStyle); y += 28 * scale;
        GUI.Label(new Rect(x, y, widthLocal, 20 * scale), $"レベル {level}   経験値 {experience}/{ExperienceToNext()}", labelStyle); y += 24 * scale;
        GUI.Label(new Rect(x, y, widthLocal, 20 * scale), $"月の欠片   {relics} / 3", labelStyle); y += 24 * scale;
        GUI.Label(new Rect(x, y, widthLocal, 20 * scale), $"集中力   {FocusBar()}  {focus}/{MaxFocus}", labelStyle); y += 24 * scale;
        GUI.Label(new Rect(x, y, widthLocal, 20 * scale), $"回復薬   {potions} 本   [E]で使用", labelStyle); y += 24 * scale;
        GUI.Label(new Rect(x, y, widthLocal, 20 * scale), $"所持金　{gold}　　歩数 {steps}", labelStyle); y += 24 * scale;
        GUI.Label(new Rect(x, y, widthLocal, 18 * scale), $"難易度   {DifficultyName(runDifficulty)}", smallStyle); y += 28 * scale;
        GUI.Label(new Rect(x, y, widthLocal, 20 * scale), "導き", headerStyle); y += 21 * scale;
        GUI.Label(new Rect(x, y, widthLocal, 30 * scale), ObjectiveHint(), smallStyle); y += 36 * scale;
        DrawDivider(x, y, widthLocal); y += 14 * scale;
        GUI.Label(new Rect(x, y, widthLocal, 20 * scale), eventTitle.ToUpperInvariant(), headerStyle); y += 25 * scale;
        GUI.Label(new Rect(x, y, widthLocal, 75 * scale), message, labelStyle); y += 84 * scale;
        DrawDivider(x, y, widthLocal); y += 14 * scale;
        GUI.Label(new Rect(x, y, widthLocal, 20 * scale), "冒険の記録", headerStyle); y += 24 * scale;
        foreach (string entry in journal)
        {
            GUI.Label(new Rect(x, y, widthLocal, 33 * scale), "› " + entry, smallStyle);
            y += 34 * scale;
        }

        float controlY = rect.yMax - 117 * scale;
        DrawDivider(x, controlY - 10 * scale, widthLocal);
        GUI.Label(new Rect(x, controlY, widthLocal, 20 * scale), mode == Mode.Exploring ? "移動  WASD / 矢印キー" : "戦闘中", headerStyle);
        if (mode == Mode.Exploring)
        {
            GUI.Label(new Rect(x, controlY + 25 * scale, widthLocal, 55 * scale), "◆ 月の欠片を3つ集めて出口へ\n● 薬瓶を拾い、Eで体力を回復\nEsc: ポーズ　F1: ガイド　F2: 設定　F3: 記録", smallStyle);
        }
    }

    private string HealthBar()
    {
        int filled = Mathf.CeilToInt(health / (float)MaxHealth * 8);
        return "<color=#" + ColorUtility.ToHtmlStringRGB(pink) + ">" + new string('■', filled) + "</color><color=#" + ColorUtility.ToHtmlStringRGB(inactiveIndicator) + ">" + new string('■', 8 - filled) + "</color>";
    }

    private string FocusBar()
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(cyan) + ">" + new string('◆', focus) + "</color><color=#" + ColorUtility.ToHtmlStringRGB(inactiveIndicator) + ">" + new string('◇', MaxFocus - focus) + "</color>";
    }

    private int ExperienceToNext() => 16 + level * 9;

    private bool GainExperience(int amount)
    {
        experience += amount;
        bool leveledUp = false;
        while (experience >= ExperienceToNext())
        {
            experience -= ExperienceToNext();
            level++;
            health = MaxHealth;
            focus = MaxFocus;
            leveledUp = true;
            AddJournal($"レベル {level} に到達。体力と集中力が回復した。");
        }
        return leveledUp;
    }

    private string ObjectiveHint()
    {
        if (!TryGetObjective(out Vector2Int target, out string targetName)) return "目的を達成した。出口へ向かおう。";
        if (target == player) return targetName + "はここにある。";
        if (!TryFindNextStep(target, out Vector2Int next)) return "濃い霧が道を隠している。";

        Vector2Int delta = next - player;
        string direction = delta.x > 0 ? "東 →" : delta.x < 0 ? "← 西" : delta.y > 0 ? "南 ↓" : "↑ 北";
        return $"{targetName}へ: 次は {direction}";
    }

    private bool TryGetObjective(out Vector2Int target, out string targetName)
    {
        target = player;
        targetName = relics >= 3 ? "出口" : "月の欠片";
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        queue.Enqueue(player);
        visited.Add(player);
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            Cell cell = dungeon[current.x, current.y];
            if ((relics >= 3 && cell == Cell.Stairs) || (relics < 3 && cell == Cell.Chest && !opened[current.x, current.y]))
            {
                target = current;
                return true;
            }

            foreach (Vector2Int direction in CardinalDirections())
            {
                Vector2Int candidate = current + direction;
                if (InBounds(candidate) && dungeon[candidate.x, candidate.y] != Cell.Wall && visited.Add(candidate)) queue.Enqueue(candidate);
            }
        }
        return false;
    }

    private bool TryFindNextStep(Vector2Int target, out Vector2Int next)
    {
        next = player;
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> previous = new Dictionary<Vector2Int, Vector2Int>();
        queue.Enqueue(player);
        previous[player] = player;
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == target)
            {
                Vector2Int step = target;
                while (previous[step] != player) step = previous[step];
                next = step;
                return true;
            }

            foreach (Vector2Int direction in CardinalDirections())
            {
                Vector2Int candidate = current + direction;
                if (InBounds(candidate) && dungeon[candidate.x, candidate.y] != Cell.Wall && !previous.ContainsKey(candidate))
                {
                    previous[candidate] = current;
                    queue.Enqueue(candidate);
                }
            }
        }
        return false;
    }

    private IEnumerable<Vector2Int> CardinalDirections()
    {
        yield return Vector2Int.up;
        yield return Vector2Int.right;
        yield return Vector2Int.down;
        yield return Vector2Int.left;
    }

    private void SaveRun()
    {
        if (mode != Mode.Exploring) return;
        string stats = $"{player.x},{player.y},{health},{relics},{potions},{focus},{steps},{gold},{level},{experience},{enemiesDefeated},{(int)runDifficulty}";
        PlayerPrefs.SetString(SaveKey, stats + "|" + SerializeFlags(explored) + "|" + SerializeFlags(opened) + "|" + SerializeFlags(defeatedEncounters));
        PlayerPrefs.Save();
    }

    private bool TryLoadRun()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return false;
        string[] segments = PlayerPrefs.GetString(SaveKey).Split('|');
        if (segments.Length != 4) return false;
        string[] stats = segments[0].Split(',');
        if (stats.Length != 8 && stats.Length != 10 && stats.Length != 11 && stats.Length != 12) return false;

        int[] values = new int[stats.Length];
        for (int i = 0; i < values.Length; i++) if (!int.TryParse(stats[i], out values[i])) return false;
        Vector2Int savedPlayer = new Vector2Int(values[0], values[1]);
        if (!InBounds(savedPlayer) || dungeon[savedPlayer.x, savedPlayer.y] == Cell.Wall) return false;
        if (!RestoreFlags(segments[1], explored) || !RestoreFlags(segments[2], opened) || !RestoreFlags(segments[3], defeatedEncounters)) return false;

        player = savedPlayer;
        health = Mathf.Clamp(values[2], 1, MaxHealth);
        relics = Mathf.Clamp(values[3], 0, 3);
        potions = Mathf.Max(0, values[4]);
        focus = Mathf.Clamp(values[5], 0, MaxFocus);
        steps = Mathf.Max(0, values[6]);
        gold = Mathf.Max(0, values[7]);
        level = stats.Length >= 10 ? Mathf.Max(1, values[8]) : 1;
        experience = stats.Length >= 10 ? Mathf.Clamp(values[9], 0, ExperienceToNext() - 1) : 0;
        enemiesDefeated = stats.Length >= 11 ? Mathf.Max(0, values[10]) : 0;
        runDifficulty = stats.Length >= 12 ? (Difficulty)Mathf.Clamp(values[11], 0, 2) : Difficulty.Adventurer;
        selectedDifficulty = runDifficulty;
        mode = Mode.Exploring;
        rng = new System.Random(2026 + steps);
        eventTitle = "探索を再開";
        message = "前回の足跡から、迷宮の探索を再開した。";
        AddJournal("自動保存した探索を復元した。");
        RevealAroundPlayer();
        return true;
    }

    private string SerializeFlags(bool[,] flags)
    {
        char[] data = new char[width * height];
        int index = 0;
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) data[index++] = flags[x, y] ? '1' : '0';
        return new string(data);
    }

    private bool RestoreFlags(string data, bool[,] flags)
    {
        if (data.Length != width * height) return false;
        int index = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char value = data[index++];
                if (value != '0' && value != '1') return false;
                flags[x, y] = value == '1';
            }
        }
        return true;
    }

    private void DrawCombatOverlay(float scale)
    {
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0.02f, 0.74f));
        float w = Mathf.Min(550 * scale, Screen.width - 40);
        float h = 325 * scale;
        Rect box = new Rect((Screen.width - w) * .5f, (Screen.height - h) * .5f, w, h);
        DrawPanel(box);
        GUI.Label(new Rect(box.x, box.y + 20 * scale, box.width, 34 * scale), "戦闘開始", centerStyle);
        GUI.Label(new Rect(box.x, box.y + 61 * scale, box.width, 30 * scale), enemyName, MakeStyle(23, FontStyle.Bold, pink, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(box.x, box.y + 98 * scale, box.width, 24 * scale), $"敵の体力  {enemyHealth} / {enemyMaxHealth}　　集中力 {focus}/{MaxFocus}", MakeStyle(16, FontStyle.Bold, text, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(box.x + 30 * scale, box.y + 126 * scale, box.width - 60 * scale, 23 * scale), "敵の気配: " + enemyIntent, MakeStyle(13, FontStyle.Bold, amber, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(box.x + 30 * scale, box.y + 151 * scale, box.width - 60 * scale, 38 * scale), message, MakeStyle(14, FontStyle.Bold, mutedText, TextAnchor.MiddleCenter));
        float buttonY = box.yMax - 67 * scale;
        float bw = (box.width - 120 * scale) / 5f;
        if (GUI.Button(new Rect(box.x + 20 * scale, buttonY, bw, 38 * scale), "攻撃 [A]", buttonStyle)) Attack();
        if (GUI.Button(new Rect(box.x + 40 * scale + bw, buttonY, bw, 38 * scale), "秘術 [Q]", buttonStyle)) ArcaneStrike();
        if (GUI.Button(new Rect(box.x + 60 * scale + bw * 2, buttonY, bw, 38 * scale), "防御 [G]", buttonStyle)) Guard();
        if (GUI.Button(new Rect(box.x + 80 * scale + bw * 3, buttonY, bw, 38 * scale), "薬 [E]", buttonStyle)) UsePotion(true);
        if (GUI.Button(new Rect(box.x + 100 * scale + bw * 4, buttonY, bw, 38 * scale), "逃走 [R]", buttonStyle)) TryFlee();
    }

    private void DrawShrineOverlay(float scale)
    {
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.02f, 0, 0.05f, 0.75f));
        float w = Mathf.Min(560 * scale, Screen.width - 40);
        Rect box = new Rect((Screen.width - w) * .5f, Screen.height * .27f, w, 278 * scale);
        DrawPanel(box);
        GUI.Label(new Rect(box.x, box.y + 19 * scale, box.width, 32 * scale), "星見の祠", MakeStyle(22, FontStyle.Bold, shrineColor, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(box.x + 36 * scale, box.y + 56 * scale, box.width - 72 * scale, 34 * scale), "一つだけ、星の祝福を選べ。", MakeStyle(15, FontStyle.Bold, text, TextAnchor.MiddleCenter));
        float buttonW = (box.width - 80 * scale) / 3f;
        float x = box.x + 20 * scale;
        float y = box.y + 113 * scale;
        if (GUI.Button(new Rect(x, y, buttonW, 82 * scale), "生命 [1]\n体力を全回復", buttonStyle)) SelectBlessing(0);
        if (GUI.Button(new Rect(x + buttonW + 20 * scale, y, buttonW, 82 * scale), "備蓄 [2]\n薬2本 + 集中力", buttonStyle)) SelectBlessing(1);
        if (GUI.Button(new Rect(x + (buttonW + 20 * scale) * 2, y, buttonW, 82 * scale), "富 [3]\n35 ゴールド", buttonStyle)) SelectBlessing(2);
        GUI.Label(new Rect(box.x, box.yMax - 32 * scale, box.width, 18 * scale), "祠は一度しか力を貸さない", smallStyle);
    }

    private void DrawEndingOverlay(float scale)
    {
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0.02f, 0.79f));
        float w = Mathf.Min(520 * scale, Screen.width - 40);
        Rect box = new Rect((Screen.width - w) * .5f, Screen.height * .28f, w, 280 * scale);
        DrawPanel(box);
        string heading = mode == Mode.Victory ? "迷宮踏破" : "力尽きた";
        Color headingColor = mode == Mode.Victory ? cyan : pink;
        GUI.Label(new Rect(box.x, box.y + 28 * scale, box.width, 38 * scale), heading, MakeStyle(25, FontStyle.Bold, headingColor, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(box.x + 36 * scale, box.y + 85 * scale, box.width - 72 * scale, 65 * scale), message, MakeStyle(16, FontStyle.Bold, text, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(box.x, box.y + 163 * scale, box.width, 22 * scale), $"スコア {RunScore()}　|　最高記録 {bestScore}", smallStyle);
        GUI.Label(new Rect(box.x, box.y + 187 * scale, box.width, 22 * scale), $"歩数 {steps}　|　月の欠片 {relics}/3　|　所持金 {gold} ゴールド", smallStyle);
        if (GUI.Button(new Rect(box.x + box.width * .25f, box.yMax - 48 * scale, box.width * .5f, 32 * scale), "もう一度挑戦 [Enter]", buttonStyle)) ResetRun();
    }

    private void DrawPauseOverlay(float scale)
    {
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0.02f, 0.72f));
        float w = Mathf.Min(390 * scale, Screen.width - 40);
        Rect box = new Rect((Screen.width - w) * .5f, Screen.height * .3f, w, 210 * scale);
        DrawPanel(box);
        GUI.Label(new Rect(box.x, box.y + 26 * scale, box.width, 34 * scale), "一時停止", MakeStyle(24, FontStyle.Bold, cyan, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(box.x + 32 * scale, box.y + 72 * scale, box.width - 64 * scale, 37 * scale), "迷宮の時間は止まっている。\n準備ができたら探索を再開しよう。", MakeStyle(15, FontStyle.Bold, text, TextAnchor.MiddleCenter));
        if (GUI.Button(new Rect(box.x + 28 * scale, box.yMax - 58 * scale, box.width * .52f, 33 * scale), "再開 [Esc]", buttonStyle)) mode = modeBeforePause;
        if (GUI.Button(new Rect(box.x + box.width * .58f, box.yMax - 58 * scale, box.width * .34f, 33 * scale), "探索を破棄", buttonStyle)) RequestRestart();
    }

    private void DrawHelpOverlay(float scale)
    {
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0.02f, 0.76f));
        float w = Mathf.Min(520 * scale, Screen.width - 40);
        Rect box = new Rect((Screen.width - w) * .5f, Screen.height * .22f, w, 330 * scale);
        DrawPanel(box);
        GUI.Label(new Rect(box.x, box.y + 22 * scale, box.width, 34 * scale), "冒険者の手引き", MakeStyle(22, FontStyle.Bold, cyan, TextAnchor.MiddleCenter));
        string guide = "探索　WASD / 矢印キー / 画面パッドで1マス移動\n導き　画面の「導き」で目的地へ1マス進む（設定で非表示可）\n目的　金色の◆（月の欠片）を3つ回収して出口へ\n回復　水色の✦は全回復、青い●は回復薬（Eで使用）\n祠　　紫の✧では3つの祝福から1つを選べる\n戦闘　A / スペース: 攻撃　Q: 秘術　E: 薬　G: 防御　R: 逃走\n　　　通常攻撃で集中力を溜め、秘術斬りに使う\n中断　Esc: ポーズ　F1: ガイド　F2: 設定　F3: 記録";
        GUI.Label(new Rect(box.x + 38 * scale, box.y + 75 * scale, box.width - 76 * scale, 170 * scale), guide, MakeStyle(15, FontStyle.Bold, text, TextAnchor.UpperLeft));
        if (GUI.Button(new Rect(box.x + box.width * .3f, box.yMax - 51 * scale, box.width * .4f, 32 * scale), "閉じる [F1]", buttonStyle)) showHelp = false;
    }

    private void DrawProfileOverlay(float scale)
    {
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0.02f, 0.78f));
        float w = Mathf.Min(510 * scale, Screen.width - 40);
        float h = Mathf.Min(350 * scale, Screen.height - 32);
        Rect box = new Rect((Screen.width - w) * .5f, (Screen.height - h) * .5f, w, h);
        DrawPanel(box);
        GUI.Label(new Rect(box.x, box.y + 22 * scale, box.width, 32 * scale), "冒険の記録", MakeStyle(22, FontStyle.Bold, cyan, TextAnchor.MiddleCenter));
        int clearRate = totalRuns == 0 ? 0 : Mathf.RoundToInt(clearedRuns * 100f / totalRuns);
        GUI.Label(new Rect(box.x + 34 * scale, box.y + 72 * scale, box.width - 68 * scale, 24 * scale), $"探索 {totalRuns} 回　踏破 {clearedRuns} 回　踏破率 {clearRate}%　最高記録 {bestScore}", MakeStyle(14, FontStyle.Bold, text, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(box.x + 42 * scale, box.y + 119 * scale, box.width - 84 * scale, 34 * scale), "勲章は冒険の節目で永久に記録されます。", smallStyle);
        string moon = (achievements & MoonbearerAchievement) != 0 ? "✓ 月の帰還者 — 迷宮を踏破する" : "○ 月の帰還者 — 迷宮を踏破する";
        string map = (achievements & CartographerAchievement) != 0 ? "✓ 迷宮の地図師 — すべての通路を記録する" : "○ 迷宮の地図師 — すべての通路を記録する";
        string veteran = (achievements & VeteranAchievement) != 0 ? "✓ 迷宮の征服者 — 4体の敵を倒す" : "○ 迷宮の征服者 — 4体の敵を倒す";
        GUI.Label(new Rect(box.x + 42 * scale, box.y + 165 * scale, box.width - 84 * scale, 24 * scale), moon, labelStyle);
        GUI.Label(new Rect(box.x + 42 * scale, box.y + 195 * scale, box.width - 84 * scale, 24 * scale), map, labelStyle);
        GUI.Label(new Rect(box.x + 42 * scale, box.y + 225 * scale, box.width - 84 * scale, 24 * scale), veteran, labelStyle);
        if (GUI.Button(new Rect(box.x + box.width * .3f, box.yMax - 47 * scale, box.width * .4f, 32 * scale), "閉じる [F3]", buttonStyle)) showProfile = false;
    }

    private void DrawRestartConfirmOverlay(float scale)
    {
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0.02f, 0.82f));
        float w = Mathf.Min(430 * scale, Screen.width - 40);
        Rect box = new Rect((Screen.width - w) * .5f, (Screen.height - 205 * scale) * .5f, w, 205 * scale);
        DrawPanel(box);
        GUI.Label(new Rect(box.x, box.y + 24 * scale, box.width, 32 * scale), "探索を破棄しますか？", MakeStyle(21, FontStyle.Bold, pink, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(box.x + 35 * scale, box.y + 72 * scale, box.width - 70 * scale, 42 * scale), "現在の自動保存を削除して、新しい探索を開始します。", MakeStyle(15, FontStyle.Bold, text, TextAnchor.MiddleCenter));
        if (GUI.Button(new Rect(box.x + box.width * .12f, box.yMax - 54 * scale, box.width * .35f, 33 * scale), "続ける [Esc]", buttonStyle)) showRestartConfirm = false;
        if (GUI.Button(new Rect(box.x + box.width * .53f, box.yMax - 54 * scale, box.width * .35f, 33 * scale), "破棄して開始", buttonStyle))
        {
            showRestartConfirm = false;
            ResetRun();
            tutorialStep = 0;
            showTutorial = true;
        }
    }

    private void DrawSettingsOverlay(float scale)
    {
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0.02f, 0.78f));
        float w = Mathf.Min(430 * scale, Screen.width - 40);
        float h = Mathf.Min(370 * scale, Screen.height - 32);
        Rect box = new Rect((Screen.width - w) * .5f, (Screen.height - h) * .5f, w, h);
        DrawPanel(box);
        GUI.Label(new Rect(box.x, box.y + 22 * scale, box.width, 30 * scale), "設定", MakeStyle(22, FontStyle.Bold, cyan, TextAnchor.MiddleCenter));
        float rowX = box.x + 35 * scale;
        float rowW = box.width - 70 * scale;
        GUI.Label(new Rect(rowX, box.y + 79 * scale, rowW * .5f, 25 * scale), "サウンド", labelStyle);
        if (GUI.Button(new Rect(box.x + box.width * .57f, box.y + 73 * scale, box.width * .28f, 32 * scale), soundEnabled ? "有効" : "無効", buttonStyle))
        {
            soundEnabled = !soundEnabled;
            SaveSettings();
        }
        GUI.Label(new Rect(rowX, box.y + 129 * scale, rowW * .5f, 25 * scale), "表示サイズ", labelStyle);
        if (GUI.Button(new Rect(box.x + box.width * .53f, box.y + 123 * scale, 35 * scale, 32 * scale), "−", buttonStyle))
        {
            uiScale = Mathf.Clamp(uiScale - .1f, .9f, 1.4f);
            ApplyTheme();
            SaveSettings();
        }
        GUI.Label(new Rect(box.x + box.width * .62f, box.y + 128 * scale, 44 * scale, 22 * scale), $"{Mathf.RoundToInt(uiScale * 100)}%", MakeStyle(13, FontStyle.Bold, text, TextAnchor.MiddleCenter));
        if (GUI.Button(new Rect(box.x + box.width * .76f, box.y + 123 * scale, 35 * scale, 32 * scale), "+", buttonStyle))
        {
            uiScale = Mathf.Clamp(uiScale + .1f, .9f, 1.4f);
            ApplyTheme();
            SaveSettings();
        }
        GUI.Label(new Rect(rowX, box.y + 177 * scale, rowW * .5f, 25 * scale), "地図の導き", labelStyle);
        if (GUI.Button(new Rect(box.x + box.width * .57f, box.y + 171 * scale, box.width * .28f, 32 * scale), guidanceEnabled ? "有効" : "無効", buttonStyle))
        {
            guidanceEnabled = !guidanceEnabled;
            SaveSettings();
        }
        GUI.Label(new Rect(rowX, box.y + 225 * scale, rowW * .5f, 25 * scale), "高コントラスト", labelStyle);
        if (GUI.Button(new Rect(box.x + box.width * .57f, box.y + 219 * scale, box.width * .28f, 32 * scale), highContrast ? "有効" : "無効", buttonStyle))
        {
            highContrast = !highContrast;
            ApplyTheme();
            SaveSettings();
        }
        GUI.Label(new Rect(box.x, box.yMax - 72 * scale, box.width, 20 * scale), "設定は次回起動時にも保持されます", smallStyle);
        if (GUI.Button(new Rect(box.x + box.width * .3f, box.yMax - 48 * scale, box.width * .4f, 32 * scale), "閉じる [F2]", buttonStyle)) showSettings = false;
    }

    private void DrawTutorialOverlay(float scale)
    {
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0.02f, 0.79f));
        float w = Mathf.Min(520 * scale, Screen.width - 40);
        Rect box = new Rect((Screen.width - w) * .5f, Screen.height * .25f, w, 305 * scale);
        DrawPanel(box);
        string heading;
        string body;
        if (tutorialStep == 0)
        {
            heading = "1 / 3　迷宮を歩く";
            body = "WASD、矢印キー、または画面上の矢印で移動します。\n地図は一度訪れた場所だけを記録します。\n壁にぶつかってもダメージは受けません。";
        }
        else if (tutorialStep == 1)
        {
            heading = "2 / 3　月の欠片を集める";
            body = "金色の◆から月の欠片を3つ回収してください。\n青い●は回復薬です。体力が減ったら E で使えます。\n画面右の「導き」が次の目的地を示します。";
        }
        else
        {
            heading = "3 / 3　戦い抜く";
            body = "敵と出会ったら、Aで通常攻撃、Qで秘術斬り。\n通常攻撃で集中力を溜め、強力な秘術に使いましょう。\n準備ができたら、迷宮へ。";
        }
        GUI.Label(new Rect(box.x, box.y + 30 * scale, box.width, 30 * scale), heading, MakeStyle(20, FontStyle.Bold, cyan, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(box.x + 42 * scale, box.y + 88 * scale, box.width - 84 * scale, 110 * scale), body, MakeStyle(15, FontStyle.Bold, text, TextAnchor.MiddleCenter));
        string action = tutorialStep < 2 ? "次へ [Enter]" : "探索を始める [Enter]";
        if (GUI.Button(new Rect(box.x + box.width * .26f, box.yMax - 58 * scale, box.width * .48f, 34 * scale), action, buttonStyle)) AdvanceTutorial();
        if (GUI.Button(new Rect(box.x + box.width * .75f, box.y + 15 * scale, box.width * .17f, 26 * scale), "スキップ", buttonStyle)) showTutorial = false;
    }

    private void AdvanceTutorial()
    {
        tutorialStep++;
        if (tutorialStep >= 3) showTutorial = false;
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt(SoundSettingKey, soundEnabled ? 1 : 0);
        PlayerPrefs.SetInt(UiScaleSettingKey, Mathf.RoundToInt(uiScale * 100));
        PlayerPrefs.SetInt(GuidanceSettingKey, guidanceEnabled ? 1 : 0);
        PlayerPrefs.SetInt(ContrastSettingKey, highContrast ? 1 : 0);
        PlayerPrefs.SetInt(DifficultySettingKey, (int)selectedDifficulty);
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveRun();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) SaveRun();
    }

    private void OnApplicationQuit()
    {
        SaveRun();
    }

    private int RunScore()
    {
        int rawScore = relics * 200 + gold + health * 15 + level * 35 + (mode == Mode.Victory ? 250 : 0);
        return Mathf.RoundToInt(rawScore * ScoreMultiplier());
    }

    private void LoadProfile()
    {
        bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        totalRuns = PlayerPrefs.GetInt(TotalRunsKey, 0);
        clearedRuns = PlayerPrefs.GetInt(ClearedRunsKey, 0);
        achievements = PlayerPrefs.GetInt(AchievementKey, 0);
    }

    private void SaveProfile()
    {
        PlayerPrefs.SetInt(BestScoreKey, bestScore);
        PlayerPrefs.SetInt(TotalRunsKey, totalRuns);
        PlayerPrefs.SetInt(ClearedRunsKey, clearedRuns);
        PlayerPrefs.SetInt(AchievementKey, achievements);
        PlayerPrefs.Save();
    }

    private void FinalizeRun(bool cleared)
    {
        if (cleared) clearedRuns++;
        bestScore = Mathf.Max(bestScore, RunScore());
        SaveProfile();
    }

    private void UnlockAchievement(int achievement, string name)
    {
        if ((achievements & achievement) != 0) return;
        achievements |= achievement;
        AddJournal("勲章獲得: " + name);
        SaveProfile();
    }

    private int AchievementCount()
    {
        int count = 0;
        if ((achievements & MoonbearerAchievement) != 0) count++;
        if ((achievements & CartographerAchievement) != 0) count++;
        if ((achievements & VeteranAchievement) != 0) count++;
        return count;
    }

    private string AchievementSummary()
    {
        string moon = (achievements & MoonbearerAchievement) != 0 ? "月の帰還者" : "???";
        string map = (achievements & CartographerAchievement) != 0 ? "地図師" : "???";
        string veteran = (achievements & VeteranAchievement) != 0 ? "征服者" : "???";
        return "勲章: " + moon + "　" + map + "　" + veteran;
    }

    private void PlayTone(float frequency, float duration, float volume)
    {
        if (!soundEnabled || audioSource == null) return;
        const int sampleRate = 22050;
        int samples = Mathf.CeilToInt(sampleRate * duration);
        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float envelope = 1f - i / (float)samples;
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope;
        }
        AudioClip clip = AudioClip.Create("ui-tone", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        audioSource.PlayOneShot(clip, volume);
        Destroy(clip, duration + .1f);
    }

    private void DrawPanel(Rect rect)
    {
        DrawRect(rect, panel);
        DrawRect(new Rect(rect.x, rect.y, rect.width, 1), panelLine);
        DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), panelLine);
        DrawRect(new Rect(rect.x, rect.y, 1, rect.height), panelLine);
        DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), panelLine);
    }

    private void DrawDivider(float x, float y, float widthLocal) => DrawRect(new Rect(x, y, widthLocal, 1), panelLine);
    private void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, pixel);
        GUI.color = previous;
    }

    private void DrawGlyph(Rect rect, string glyph, Color color, float size)
    {
        GUIStyle glyphStyle = MakeStyle(Mathf.RoundToInt(size), FontStyle.Bold, color, TextAnchor.MiddleCenter);
        GUI.Label(rect, glyph, glyphStyle);
    }
}
