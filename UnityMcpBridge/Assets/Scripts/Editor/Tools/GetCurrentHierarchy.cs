using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Tools
{
    public static class GetCurrentHierarchy
    {
        public static object HandleCommand(JObject @params)
        {
            try
            {
                // Get the active scene
                Scene activeScene = SceneManager.GetActiveScene();
                if (!activeScene.IsValid())
                {
                    return Response.Error("No active scene found.");
                }

                // Build YAML representation
                StringBuilder yaml = new StringBuilder();
                yaml.AppendLine("Scene:");
                yaml.AppendLine($"  properties: {{ name: \"{activeScene.name}\" }}");

                // Get root GameObjects
                GameObject[] rootObjects = activeScene.GetRootGameObjects();
                
                foreach (GameObject rootObj in rootObjects)
                {
                    if (rootObj != null)
                    {
                        AppendGameObjectHierarchy(yaml, rootObj, 1);
                    }
                }

                return Response.Success("Current hierarchy retrieved successfully.", new
                {
                    sceneName = activeScene.name,
                    hierarchyYaml = yaml.ToString()
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[GetCurrentHierarchy] Failed: {e}");
                return Response.Error($"Failed to get hierarchy: {e.Message}");
            }
        }

        private static void AppendGameObjectHierarchy(StringBuilder yaml, GameObject obj, int indentLevel)
        {
            string indent = new string(' ', indentLevel * 2);
            yaml.AppendLine();
            yaml.AppendLine($"{indent}{obj.name}:");
            
            // Build properties
            var properties = new List<string>();
            
            // Active state
            properties.Add($"active: {obj.activeSelf.ToString().ToLower()}");
            
            // Check if this is a prefab instance
            if (PrefabUtility.IsPartOfPrefabInstance(obj))
            {
                GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(obj);
                if (prefabSource != null)
                {
                    string prefabPath = AssetDatabase.GetAssetPath(prefabSource);
                    if (!string.IsNullOrEmpty(prefabPath))
                    {
                        properties.Add($"prefab: \"{prefabPath}\"");
                    }
                }
            }
            
            // Transform properties
            Vector3 pos = obj.transform.localPosition;
            Vector3 rot = obj.transform.localEulerAngles;
            Vector3 scale = obj.transform.localScale;
            
            properties.Add($"pos: [{FormatFloat(pos.x)},{FormatFloat(pos.y)},{FormatFloat(pos.z)}]");
            properties.Add($"rot: [{FormatFloat(rot.x)},{FormatFloat(rot.y)},{FormatFloat(rot.z)}]");
            properties.Add($"scale: [{FormatFloat(scale.x)},{FormatFloat(scale.y)},{FormatFloat(scale.z)}]");
            
            // RectTransform properties if available
            RectTransform rectTransform = obj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 anchoredPos = rectTransform.anchoredPosition;
                Vector2 size = rectTransform.sizeDelta;
                Vector2 pivot = rectTransform.pivot;
                Vector2 anchorMin = rectTransform.anchorMin;
                Vector2 anchorMax = rectTransform.anchorMax;
                
                var rectProps = new List<string>
                {
                    $"anchoredPos: [{FormatFloat(anchoredPos.x)},{FormatFloat(anchoredPos.y)}]",
                    $"size: [{FormatFloat(size.x)},{FormatFloat(size.y)}]",
                    $"pivot: [{FormatFloat(pivot.x)},{FormatFloat(pivot.y)}]",
                    $"anchorMin: [{FormatFloat(anchorMin.x)},{FormatFloat(anchorMin.y)}]",
                    $"anchorMax: [{FormatFloat(anchorMax.x)},{FormatFloat(anchorMax.y)}]"
                };
                
                properties.Add($"rect: {{ {string.Join(", ", rectProps)} }}");
            }
            
            // Format properties line
            yaml.AppendLine($"{indent}  properties: {{ {string.Join(", ", properties)} }}");
            
            // Process children
            foreach (Transform child in obj.transform)
            {
                if (child != null && child.gameObject != null)
                {
                    AppendGameObjectHierarchy(yaml, child.gameObject, indentLevel + 1);
                }
            }
        }
        
        private static string FormatFloat(float value)
        {
            // Format float to remove unnecessary decimal places
            if (Mathf.Approximately(value, Mathf.Round(value)))
            {
                return ((int)value).ToString();
            }
            return value.ToString("0.#####");
        }
    }
}