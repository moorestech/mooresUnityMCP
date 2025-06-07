# CLAUDE.md

このファイルは、このリポジトリでコードを扱う際のClaude Code (claude.ai/code)へのガイダンスを提供します。

## ビルドとテストコマンド

### Pythonサーバー
```bash
# サーバーを実行（uvが依存関係を自動インストール）
cd UnityMcpServer/src
uv run server.py

# デバッグモードで実行する場合は、config.pyでlog_levelを"DEBUG"に変更
```

### Unityパッケージ
- UnityパッケージはUnity Package Managerを通じて管理されます
- 明示的なビルドスクリプトはありません - Unityの標準パッケージシステムに依存
- テストスクリプトは現在含まれていません

## アーキテクチャ概要

Unity MCPは、LLM（Claude、Cursorなど）がUnity Editorと直接対話できるようにするブリッジシステムです。

### コンポーネント構成

1. **Unity MCP Bridge（C#）** - UnityEditor内で実行
   - `UnityMcpBridge.cs`: メインTCPサーバー（ポート6400）、コマンドキュー管理
   - `CommandRegistry.cs`: コマンド名をハンドラーメソッドにマッピング
   - `Tools/`フォルダ: 各Unity操作の実装

2. **Unity MCP Server（Python）** - ローカルで実行
   - `server.py`: FastMCPベースのサーバー、stdio経由でMCPクライアントと通信
   - `unity_connection.py`: UnityブリッジへのTCP接続管理
   - `tools/`フォルダ: Pythonツール定義

3. **通信フロー**
   ```
   MCPクライアント <-[stdio]-> Pythonサーバー <-[TCP:6400]-> Unityブリッジ
   ```

### 主要機能

**利用可能なツール:**
- `manage_gameobject`: GameObject作成、変更、削除、コンポーネント操作
- `manage_asset`: アセットのインポート/エクスポート、プレハブ管理
- `manage_scene`: シーンの読み込み、保存、階層管理
- `manage_script`: C#スクリプトファイルの作成と編集
- `manage_editor`: エディターの状態制御（再生/一時停止、設定）
- `execute_menu_item`: Unityメニュー項目をパスで実行
- `read_console`: Unityコンソールログの読み取り

### 実装上の重要点

1. **スレッドセーフティ**: すべてのUnity操作は`EditorApplication.update`中にメインスレッドで実行されます
2. **エラーハンドリング**: 包括的なエラー処理と詳細なエラーメッセージ
3. **接続管理**: ping/pongによる接続検証と自動再接続
4. **Undo対応**: ほとんどの操作がUnityのUndoシステムをサポート
5. **JSON通信**: すべてのコマンドとレスポンスはJSON形式

### 開発時の注意

- Pythonサーバーは`uv`パッケージマネージャーを使用（`pip install uv`が必要）
- デバッグ時は`config.py`の`log_level`を"DEBUG"に設定
- UnityブリッジはPythonサーバーを自動的にインストール/更新（`ServerInstaller.cs`）
- 新しいツールを追加する場合は、Unity側とPython側の両方に実装が必要