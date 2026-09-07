# Arcane Depths — Unity first-person dungeon prototype

`Assets/Scenes/DungeonPrototype.unity` を Unity 6000.3.22f1 で開き、Play を押すだけで遊べます。タイトル画面から新規開始、または保存済みの探索を再開してください。Windows向け配布名は **Arcane Depths** です。

ゲーム内の画面表示・ボタン・状態ラベルは日本語に統一しています。キー名（WASD、Enter、Esc、Fキーなど）は実際の入力に対応する表記として残しています。

## 操作

- `WASD` または矢印キー: 移動（最後に進もうとした方角へ視点が向きます）
- 画面上の移動パッド: タップ／マウス操作で移動
- ゲームパッド: 左スティック／十字キーで移動、A=攻撃、B=逃走、X=秘術、Y=防御、LB=薬
- `E`: 回復薬を使用（探索中・戦闘中）
- 戦闘: `A` / Space = 攻撃、`Q` = 秘術斬り、`G` = 防御、`R` = 逃走
- `Esc`: ポーズ、`F1`: 操作ガイド、`F2`: 設定
- `F3`: 冒険の記録と勲章
- 「導き」ボタン: 次の目的地に向けて安全な1マスを進む

月の欠片を3つ回収して、樹海の出口まで到達してください。一人称の樹海ビューを進み、右側の踏破地図で現在地と方角を確認できます。地図上では、桃色の矢印が冒険者、金色の◆が欠片、水色の✦が回復の泉です。

実装は外部アセット不要の `OnGUI` ベースで、以下を含みます。

- 難易度選択（旅人／冒険者／深淵）、タイトル画面、新規開始／続きから、探索中の自動保存
- 一人称の樹海ビュー、2Dグリッド迷宮、探索済みオートマップ、タップ対応の移動パッド
- 壁の衝突、宝箱、回復の泉、星見の祠、封印された出口
- 回復薬の拾得・使用、集中力を消費する秘術斬り、経験値とレベル
- 目的地までの次の一手を示す「導き」、地図上の経路マーカー、スキップ可能なチュートリアル
- 決定論的なエンカウント、敵の意図表示、ターン制戦闘
- HP、戦利品、冒険ログ、勝敗画面、ポーズ、効果音
- 3種類の迷宮レイアウト、マップごとに変化する遭遇地点、保存可能なラン乱数、日付シードの「今日の挑戦」
- UIサイズ・高コントラスト・導き、効果音／環境音の個別音量、全画面／ウィンドウ・解像度・垂直同期設定
- キーボード再割り当て、ゲームパッド、44px以上を基準にしたタップ操作
- スコア／踏破率／永続的な勲章、誤操作を防ぐ探索破棄確認、保存バックアップと復旧画面、異常終了後の再開案内

## 開発・検証

迷宮データは起動時に `DungeonMapValidator` で検査されます。開始地点・3つの月の欠片・出口・到達可能性の条件を満たさない編集は、すぐに明示的なエラーになります。

EditMode／PlayModeテストは Unity の **Window > General > Test Runner** から実行できます。迷宮データ、保存形式、ラン乱数の継続、実際のscene起動と基本操作を検証します。

ゲームロジックは `Assets/Scripts/DungeonPrototype.cs`、迷宮データの静的検証は `Assets/Scripts/DungeonMapValidator.cs` に分離しています。これらは Unity 6 向けの `ArcaneDepths.Runtime` アセンブリとしてビルドされ、EditMode テストから明示参照されます。手作業で地図を変更したときは、Play Mode と EditMode テストの両方を実行してください。

ヘッドレスで両方のテストスイートを実行する例です。リポジトリのルートで、同じプロジェクトをUnity Editorで開いていない状態から実行してください。

```powershell
$unity = '<Unity.exeへの絶対パス>'
New-Item -ItemType Directory -Force -Path TestResults | Out-Null

& $unity -batchmode -nographics -quit -projectPath (Get-Location) -runTests -testPlatform EditMode -testResults TestResults/EditMode.xml -logFile TestResults/EditMode.log
if ($LASTEXITCODE -ne 0) { throw "EditMode tests failed: $LASTEXITCODE" }

& $unity -batchmode -nographics -quit -projectPath (Get-Location) -runTests -testPlatform PlayMode -testResults TestResults/PlayMode.xml -logFile TestResults/PlayMode.log
if ($LASTEXITCODE -ne 0) { throw "PlayMode tests failed: $LASTEXITCODE" }
```

可読性・コントラスト・状態表現の監査と基準は [`ACCESSIBILITY_AUDIT.md`](ACCESSIBILITY_AUDIT.md) を参照してください。設定画面の「高コントラスト」は地図だけでなく、全文字、ボタン、罫線、ゲージを含む画面全体へ適用されます。対応する支援技術では、画面・モーダル・戦闘の状態、操作名、ヒントを読み上げられます。

## Windows ビルド

Unity メニューの **Arcane Depths > Build Windows x64**、または次のスクリプトを使います。

```powershell
.\scripts\build-windows.ps1 -UnityPath '<Unity.exeへの絶対パス>'
```

ローカルスクリプトの出力は `Builds/Windows/ArcaneDepths.exe` です。

GitHub ActionsはEditMode、PlayMode、Windows x64ビルドの順に実行します。利用前にリポジトリの `UNITY_LICENSE` Secretを設定してください。CIビルドは `build/StandaloneWindows64/` に作られ、`ArcaneDepths-Windows-x64` というartifact名でアップロードされます。ローカルスクリプトとCIでは出力パスが異なる点に注意してください。

## データとライセンス

ゲームコードは保存データを外部送信しません。詳しくは [`PRIVACY.md`](PRIVACY.md)、コードと第三者コンポーネントの扱いは [`LICENSE`](LICENSE) と [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md) を参照してください。
