using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Tools
{
    public static class ManagePrefabVariant
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
                    case "create":
                        return CreatePrefabVariant(@params);
                    case "get_overrides":
                        return GetOverrides(@params);
                    case "apply_overrides":
                        return ApplyOverrides(@params);
                    case "revert_overrides":
                        return RevertOverrides(@params);
                    default:
                        return Response.Error($"Unknown action: '{action}'.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ManagePrefabVariant] Action '{action}' failed: {e}");
                return Response.Error($"Internal error: {e.Message}");
            }
        }

        private static object CreatePrefabVariant(JObject @params)
        {
            string basePrefabPath = @params["base_prefab_path"]?.ToString();
            string variantPath = @params["variant_path"]?.ToString();
            string variantName = @params["variant_name"]?.ToString();

            if (string.IsNullOrEmpty(basePrefabPath))
            {
                return Response.Error("base_prefab_path parameter is required.");
            }

            // Load the base prefab
            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
            if (basePrefab == null)
            {
                return Response.Error($"Base prefab not found at path: {basePrefabPath}");
            }

            // Ensure the base asset is a prefab
            if (!PrefabUtility.IsPartOfAnyPrefab(basePrefab))
            {
                return Response.Error("The specified asset is not a prefab.");
            }

            // Determine variant save path
            if (string.IsNullOrEmpty(variantPath))
            {
                variantPath = Path.GetDirectoryName(basePrefabPath);
            }

            if (string.IsNullOrEmpty(variantName))
            {
                variantName = basePrefab.name + "_Variant";
            }

            string fullVariantPath = Path.Combine(variantPath, variantName + ".prefab");
            fullVariantPath = fullVariantPath.Replace("\\", "/");

            // Ensure directory exists
            string directory = Path.GetDirectoryName(fullVariantPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create prefab variant
            GameObject variantInstance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            
            try
            {
                // Save as prefab variant
                GameObject variantPrefab = PrefabUtility.SaveAsPrefabAsset(variantInstance, fullVariantPath);
                
                if (variantPrefab == null)
                {
                    return Response.Error("Failed to create prefab variant.");
                }

                // Clean up the temporary instance
                UnityEngine.Object.DestroyImmediate(variantInstance);

                AssetDatabase.Refresh();

                return Response.Success("Prefab variant created successfully.", new
                {
                    variantPath = fullVariantPath,
                    basePrefabPath = basePrefabPath,
                    variantName = variantPrefab.name,
                    isVariant = PrefabUtility.IsPartOfVariantPrefab(variantPrefab)
                });
            }
            catch (Exception e)
            {
                // Clean up on error
                if (variantInstance != null)
                {
                    UnityEngine.Object.DestroyImmediate(variantInstance);
                }
                throw;
            }
        }

        private static object GetOverrides(JObject @params)
        {
            string variantPath = @params["variant_path"]?.ToString();
            
            if (string.IsNullOrEmpty(variantPath))
            {
                return Response.Error("variant_path parameter is required.");
            }

            GameObject variantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
            if (variantPrefab == null)
            {
                return Response.Error($"Prefab variant not found at path: {variantPath}");
            }

            if (!PrefabUtility.IsPartOfVariantPrefab(variantPrefab))
            {
                return Response.Error("The specified prefab is not a variant.");
            }

            // Get property modifications
            var modifications = PrefabUtility.GetPropertyModifications(variantPrefab);
            var overridesList = new JArray();

            if (modifications != null)
            {
                foreach (var mod in modifications)
                {
                    overridesList.Add(new JObject
                    {
                        ["target"] = mod.target?.name ?? "Unknown",
                        ["propertyPath"] = mod.propertyPath,
                        ["value"] = mod.value,
                        ["objectReference"] = mod.objectReference?.name
                    });
                }
            }

            // Get added components
            var addedComponents = PrefabUtility.GetAddedComponents(variantPrefab);
            var addedComponentsList = new JArray();

            foreach (var added in addedComponents)
            {
                addedComponentsList.Add(new JObject
                {
                    ["componentType"] = added.instanceComponent.GetType().Name,
                    ["gameObject"] = added.instanceComponent.gameObject.name
                });
            }

            // Get removed components
            var removedComponents = PrefabUtility.GetRemovedComponents(variantPrefab);
            var removedComponentsList = new JArray();

            foreach (var removed in removedComponents)
            {
                removedComponentsList.Add(new JObject
                {
                    ["componentType"] = removed.assetComponent.GetType().Name,
                    ["gameObject"] = removed.containingInstanceGameObject.name
                });
            }

            return Response.Success("Overrides retrieved successfully.", new
            {
                propertyModifications = overridesList,
                addedComponents = addedComponentsList,
                removedComponents = removedComponentsList,
                hasOverrides = modifications?.Length > 0 || addedComponents.Count > 0 || removedComponents.Count > 0
            });
        }

        private static object ApplyOverrides(JObject @params)
        {
            string variantPath = @params["variant_path"]?.ToString();
            bool applyAll = @params["apply_all"]?.ToObject<bool>() ?? true;
            
            if (string.IsNullOrEmpty(variantPath))
            {
                return Response.Error("variant_path parameter is required.");
            }

            GameObject variantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
            if (variantPrefab == null)
            {
                return Response.Error($"Prefab variant not found at path: {variantPath}");
            }

            if (!PrefabUtility.IsPartOfVariantPrefab(variantPrefab))
            {
                return Response.Error("The specified prefab is not a variant.");
            }

            try
            {
                // Apply overrides to base
                string basePrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(variantPrefab);
                GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);

                if (applyAll)
                {
                    PrefabUtility.ApplyPrefabInstance(variantPrefab, InteractionMode.UserAction);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return Response.Success("Overrides applied successfully.", new
                {
                    variantPath = variantPath,
                    basePrefabPath = basePrefabPath
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to apply overrides: {e.Message}");
            }
        }

        private static object RevertOverrides(JObject @params)
        {
            string variantPath = @params["variant_path"]?.ToString();
            bool revertAll = @params["revert_all"]?.ToObject<bool>() ?? true;
            
            if (string.IsNullOrEmpty(variantPath))
            {
                return Response.Error("variant_path parameter is required.");
            }

            GameObject variantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
            if (variantPrefab == null)
            {
                return Response.Error($"Prefab variant not found at path: {variantPath}");
            }

            if (!PrefabUtility.IsPartOfVariantPrefab(variantPrefab))
            {
                return Response.Error("The specified prefab is not a variant.");
            }

            try
            {
                if (revertAll)
                {
                    PrefabUtility.RevertPrefabInstance(variantPrefab, InteractionMode.UserAction);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return Response.Success("Overrides reverted successfully.", new
                {
                    variantPath = variantPath
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to revert overrides: {e.Message}");
            }
        }
    }
}