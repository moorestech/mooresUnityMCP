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
   - `Assets/Scripts/Editor/Tools/`フォルダ: 各Unity操作の実装

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

## 新しいMCPツールの追加方法

### 1. Unity側（C#）の実装

#### 1.1 新しいツールクラスを作成
`UnityMcpBridge/Assets/Scripts/Editor/Tools/`フォルダに新しいC#ファイルを作成：

```csharp
using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Tools
{
    public static class ManageAnimation  // 例：アニメーション管理ツール
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString().ToLower();
            if (string.IsNullOrEmpty(action))
            {
                return Response.Error("Action parameter is required.");
            }

            try
            {
                switch (action)
                {
                    case "play":
                        return PlayAnimation(@params);
                    case "stop":
                        return StopAnimation(@params);
                    default:
                        return Response.Error($"Unknown action: '{action}'.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ManageAnimation] Action '{action}' failed: {e}");
                return Response.Error($"Internal error: {e.Message}");
            }
        }

        private static object PlayAnimation(JObject @params)
        {
            // 実装
            return Response.Success("Animation played successfully.");
        }
    }
}
```

#### 1.2 UnityMcpBridge.csにツールを登録
`ExecuteCommand`メソッドのswitchステートメントに追加：

```csharp
object result = command.type switch
{
    // 既存のツール...
    "manage_animation" => ManageAnimation.HandleCommand(paramsObject), // 新規追加
    _ => throw new ArgumentException($"Unknown command type: {command.type}"),
};
```

### 2. Python側の実装

#### 2.1 新しいツールファイルを作成
`UnityMcpServer/src/tools/`フォルダに新しいPythonファイルを作成：

```python
from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any
from unity_connection import get_unity_connection

def register_manage_animation_tools(mcp: FastMCP):
    """Register animation management tools with the MCP server."""

    @mcp.tool()
    def manage_animation(
        ctx: Context,
        action: str,
        target: str = None,
        animation_name: str = None,
        speed: float = 1.0,
    ) -> Dict[str, Any]:
        """Manages animations in Unity.

        Args:
            action: Operation ('play', 'stop', 'pause', etc.)
            target: GameObject identifier
            animation_name: Name of the animation clip
            speed: Playback speed

        Returns:
            Dictionary with operation results.
        """
        try:
            params = {
                "action": action,
                "target": target,
                "animationName": animation_name,
                "speed": speed,
            }
            params = {k: v for k, v in params.items() if v is not None}
            
            response = get_unity_connection().send_command("manage_animation", params)
            
            if response.get("success"):
                return {"success": True, "message": response.get("message"), "data": response.get("data")}
            else:
                return {"success": False, "message": response.get("error")}
                
        except Exception as e:
            return {"success": False, "message": f"Python error: {str(e)}"}
```

#### 2.2 tools/__init__.pyでツールを登録
`UnityMcpServer/src/tools/__init__.py`ファイルを編集：

```python
# インポートセクションに追加
from .manage_animation import register_manage_animation_tools

# register_all_tools関数内に追加
def register_all_tools(mcp):
    """Register all refactored tools with the MCP server."""
    print("Registering Unity MCP Server refactored tools...")
    register_manage_script_tools(mcp)
    register_manage_scene_tools(mcp)
    register_manage_editor_tools(mcp)
    register_manage_gameobject_tools(mcp)
    register_manage_asset_tools(mcp)
    register_read_console_tools(mcp)
    register_execute_menu_item_tools(mcp)
    register_manage_animation_tools(mcp)  # 新規追加
    print("Unity MCP Server tool registration complete.")
```

### 3. ツール実装のベストプラクティス

1. **エラーハンドリング**: 包括的なtry-catchでエラーを捕捉
2. **Undo対応**: Unity側で`Undo.RecordObject`を使用
3. **パラメータ検証**: 必須パラメータの確認とデフォルト値設定
4. **レスポンス形式**: `Response.Success`/`Response.Error`で一貫性維持
5. **ログ出力**: `Debug.Log`/`Debug.LogError`で適切なデバッグ情報

### 4. テスト手順

1. Unityエディタを再起動（C#コードのリロード）
2. MCPクライアント（Claude、Cursor）を再起動
3. 新しいツールを呼び出してテスト

# ファイルの作成について
*.metaファイルは作成しないでください。この作成によってGUIDが被り、正しくコンパイルできない状況が発生します。