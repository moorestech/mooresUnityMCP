using System;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Newtonsoft.Json.Linq;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Tools
{
    public static class CompileAndReload
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
                    case "compile":
                        return CompileScripts();
                    case "reload":
                        return ReloadDomain();
                    case "refresh":
                        return RefreshAssets();
                    case "compile_and_reload":
                        return CompileAndReloadAll();
                    default:
                        return Response.Error($"Unknown action: '{action}'. Valid actions: compile, reload, refresh, compile_and_reload");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CompileAndReload] Action '{action}' failed: {e}");
                return Response.Error($"Internal error: {e.Message}");
            }
        }

        private static object CompileScripts()
        {
            try
            {
                Debug.Log("[CompileAndReload] Requesting script compilation...");
                CompilationPipeline.RequestScriptCompilation();
                return Response.Success("Script compilation requested successfully.");
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to compile scripts: {e.Message}");
            }
        }

        private static object ReloadDomain()
        {
            try
            {
                Debug.Log("[CompileAndReload] Requesting domain reload...");
                EditorUtility.RequestScriptReload();
                return Response.Success("Domain reload requested successfully.");
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to reload domain: {e.Message}");
            }
        }

        private static object RefreshAssets()
        {
            try
            {
                Debug.Log("[CompileAndReload] Refreshing assets...");
                AssetDatabase.Refresh();
                return Response.Success("Assets refreshed successfully.");
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to refresh assets: {e.Message}");
            }
        }

        private static object CompileAndReloadAll()
        {
            try
            {
                Debug.Log("[CompileAndReload] Performing full compile and reload...");
                
                // First refresh assets
                AssetDatabase.Refresh();
                
                // Then request compilation
                CompilationPipeline.RequestScriptCompilation();
                
                // Finally request domain reload
                EditorUtility.RequestScriptReload();
                
                return Response.Success("Full compile and reload requested successfully.");
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to compile and reload: {e.Message}");
            }
        }
    }
}