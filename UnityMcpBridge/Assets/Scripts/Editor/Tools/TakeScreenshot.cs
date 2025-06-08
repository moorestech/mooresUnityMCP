using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Tools
{
    // Force Unity to recompile - version 2
    public static class TakeScreenshot
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
                    case "capture":
                        return CaptureScreenshot(@params);
                    default:
                        return Response.Error($"Unknown action: '{action}'.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TakeScreenshot] Action '{action}' failed: {e}");
                return Response.Error($"Internal error: {e.Message}");
            }
        }

        private static object CaptureScreenshot(JObject @params)
        {
            try
            {
                // Get optional parameters
                string fileName = @params["fileName"]?.ToString();
                
                // Try to get resolution in multiple ways
                float resolution = 1.0f;
                if (@params["resolution"] != null)
                {
                    var resolutionToken = @params["resolution"];
                    if (resolutionToken.Type == JTokenType.Float || resolutionToken.Type == JTokenType.Integer)
                    {
                        resolution = resolutionToken.ToObject<float>();
                    }
                    else if (resolutionToken.Type == JTokenType.String)
                    {
                        float.TryParse(resolutionToken.ToString(), out resolution);
                    }
                }
                
                bool captureGameView = @params["captureGameView"]?.ToObject<bool>() ?? true;
                
                Debug.Log($"[TakeScreenshot] Received params: fileName={fileName}, resolution={resolution}, captureGameView={captureGameView}");
                Debug.Log($"[TakeScreenshot] Raw params JSON: {@params.ToString()}");
                
                // Validate resolution parameter (0.1 to 1.0)
                if (resolution < 0.1f || resolution > 1.0f)
                {
                    return Response.Error("Resolution must be between 0.1 and 1.0");
                }

                // Generate filename if not provided
                if (string.IsNullOrEmpty(fileName))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                    fileName = $"Screenshot_{timestamp}.png";
                }

                // Ensure .png extension
                if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    fileName = Path.GetFileNameWithoutExtension(fileName) + ".png";
                }

                // Use Screenshots directory in Unity project
                string screenshotsDir = Path.Combine(Application.dataPath, "..", "Screenshots");
                if (!Directory.Exists(screenshotsDir))
                {
                    Directory.CreateDirectory(screenshotsDir);
                }

                string fullPath = Path.Combine(screenshotsDir, fileName);

                if (captureGameView)
                {
                    // Find the main camera
                    Camera camera = Camera.main;
                    if (camera == null)
                    {
                        // If no main camera, find any active camera
                        Camera[] cameras = Camera.allCameras;
                        foreach (Camera cam in cameras)
                        {
                            if (cam.enabled && cam.gameObject.activeInHierarchy)
                            {
                                camera = cam;
                                break;
                            }
                        }
                    }
                    
                    if (camera == null)
                    {
                        return Response.Error("No active camera found in the scene.");
                    }
                    
                    // Store original settings
                    RenderTexture originalTarget = camera.targetTexture;
                    
                    // Get Game View resolution using reflection
                    System.Type gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                    EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
                    
                    // Use reflection to get the actual game view size
                    PropertyInfo prop = gameViewType.GetProperty("currentGameViewSize", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop == null)
                    {
                        // Fallback: use default resolution
                        Debug.LogWarning("Could not get Game View size via reflection, using default resolution");
                    }
                    
                    // Get Game View size - try multiple methods
                    int baseWidth = 0;
                    int baseHeight = 0;
                    
                    // Method 1: Try to get from Handles
                    Vector2 gameViewSize = Handles.GetMainGameViewSize();
                    baseWidth = (int)gameViewSize.x;
                    baseHeight = (int)gameViewSize.y;
                    
                    // Method 2: If that didn't work, try reflection on GameView
                    if (baseWidth <= 0 || baseHeight <= 0)
                    {
                        var gameViewSizeProperty = gameViewType.GetProperty("currentGameViewSize", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (gameViewSizeProperty != null)
                        {
                            var sizeValue = gameViewSizeProperty.GetValue(gameView);
                            if (sizeValue != null)
                            {
                                var widthProperty = sizeValue.GetType().GetProperty("width");
                                var heightProperty = sizeValue.GetType().GetProperty("height");
                                if (widthProperty != null && heightProperty != null)
                                {
                                    baseWidth = (int)widthProperty.GetValue(sizeValue);
                                    baseHeight = (int)heightProperty.GetValue(sizeValue);
                                }
                            }
                        }
                    }
                    
                    // Method 3: If still no luck, use default HD resolution
                    if (baseWidth <= 0 || baseHeight <= 0)
                    {
                        Debug.LogWarning("Could not get Game View size, using default 1920x1080");
                        baseWidth = 1920;
                        baseHeight = 1080;
                    }
                    
                    // Calculate target dimensions
                    int targetWidth = Mathf.Max(1, Mathf.RoundToInt(baseWidth * resolution));
                    int targetHeight = Mathf.Max(1, Mathf.RoundToInt(baseHeight * resolution));
                    
                    Debug.Log($"[TakeScreenshot] Base resolution: {baseWidth}x{baseHeight}, Target resolution: {targetWidth}x{targetHeight} (factor: {resolution})");
                    
                    // Create render texture with target dimensions
                    RenderTexture rt = new RenderTexture(targetWidth, targetHeight, 24, RenderTextureFormat.ARGB32);
                    rt.antiAliasing = 1;
                    
                    // Set camera to render to our texture
                    camera.targetTexture = rt;
                    camera.Render();
                    
                    // Read pixels
                    RenderTexture.active = rt;
                    Texture2D screenshot = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
                    screenshot.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
                    screenshot.Apply();
                    
                    // Save to file
                    byte[] bytes = screenshot.EncodeToPNG();
                    File.WriteAllBytes(fullPath, bytes);
                    
                    // Cleanup
                    camera.targetTexture = originalTarget;
                    RenderTexture.active = null;
                    UnityEngine.Object.DestroyImmediate(rt);
                    UnityEngine.Object.DestroyImmediate(screenshot);
                    
                    Debug.Log($"Game View screenshot saved to: {fullPath} (Resolution factor: {resolution})");
                }
                else
                {
                    // Capture Scene View
                    SceneView sceneView = SceneView.lastActiveSceneView;
                    if (sceneView == null)
                    {
                        return Response.Error("No active Scene View found.");
                    }

                    int width = (int)sceneView.position.width;
                    int height = (int)sceneView.position.height;
                    
                    // Calculate target dimensions with resolution multiplier
                    int targetWidth = Mathf.RoundToInt(width * resolution);
                    int targetHeight = Mathf.RoundToInt(height * resolution);

                    RenderTexture rt = new RenderTexture(targetWidth, targetHeight, 24);
                    sceneView.camera.targetTexture = rt;
                    sceneView.camera.Render();

                    RenderTexture.active = rt;
                    Texture2D screenshot = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
                    screenshot.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
                    screenshot.Apply();

                    byte[] bytes = screenshot.EncodeToPNG();
                    File.WriteAllBytes(fullPath, bytes);

                    // Cleanup
                    sceneView.camera.targetTexture = null;
                    RenderTexture.active = null;
                    UnityEngine.Object.DestroyImmediate(rt);
                    UnityEngine.Object.DestroyImmediate(screenshot);

                    Debug.Log($"Scene View screenshot saved to: {fullPath}");
                }

                return Response.Success($"Screenshot saved successfully", new
                {
                    path = fullPath,
                    absolutePath = Path.GetFullPath(fullPath),
                    fileName = fileName,
                    captureType = captureGameView ? "GameView" : "SceneView",
                    resolution = resolution
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to capture screenshot: {e.Message}");
            }
        }
    }
}