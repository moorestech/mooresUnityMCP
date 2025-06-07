using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Tools
{
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
                int superSize = @params["superSize"]?.ToObject<int>() ?? 1;
                bool captureGameView = @params["captureGameView"]?.ToObject<bool>() ?? true;

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

                // Use Screenshots directory in project root
                string projectPath = Path.GetDirectoryName(Application.dataPath);
                string screenshotsDir = Path.Combine(projectPath, "Screenshots");
                if (!Directory.Exists(screenshotsDir))
                {
                    Directory.CreateDirectory(screenshotsDir);
                    Debug.Log($"Created Screenshots directory at: {screenshotsDir}");
                }

                string fullPath = Path.Combine(screenshotsDir, fileName);

                if (captureGameView)
                {
                    // Capture Game View
                    ScreenCapture.CaptureScreenshot(fullPath, superSize);
                    
                    // Wait for the file to be written
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        int maxWaitTime = 5000; // 5 seconds max wait
                        int waitedTime = 0;
                        int checkInterval = 100; // Check every 100ms
                        
                        while (waitedTime < maxWaitTime)
                        {
                            if (File.Exists(fullPath))
                            {
                                // File exists, but might still be writing
                                await System.Threading.Tasks.Task.Delay(500); // Extra delay to ensure write is complete
                                break;
                            }
                            
                            await System.Threading.Tasks.Task.Delay(checkInterval);
                            waitedTime += checkInterval;
                        }
                        
                        // Log on main thread
                        EditorApplication.delayCall += () =>
                        {
                            if (File.Exists(fullPath))
                            {
                                Debug.Log($"Screenshot saved to: {fullPath}");
                            }
                            else
                            {
                                Debug.LogError($"Screenshot file was not created at: {fullPath}");
                            }
                        };
                    });
                    
                    // Add a synchronous delay to give the screenshot time to start saving
                    System.Threading.Thread.Sleep(1000);
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

                    RenderTexture rt = new RenderTexture(width * superSize, height * superSize, 24);
                    sceneView.camera.targetTexture = rt;
                    sceneView.camera.Render();

                    RenderTexture.active = rt;
                    Texture2D screenshot = new Texture2D(width * superSize, height * superSize, TextureFormat.RGB24, false);
                    screenshot.ReadPixels(new Rect(0, 0, width * superSize, height * superSize), 0, 0);
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
                    superSize = superSize
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to capture screenshot: {e.Message}");
            }
        }
    }
}