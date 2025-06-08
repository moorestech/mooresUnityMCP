from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any
from unity_connection import get_unity_connection

def register_take_screenshot_tools(mcp: FastMCP):
    """Register screenshot capture tools with the MCP server."""

    @mcp.tool()
    def take_screenshot(
        ctx: Context,
        file_name: str = None,
        resolution: float = 1.0,
        capture_game_view: bool = True,
    ) -> Dict[str, Any]:
        """Captures a screenshot in Unity and returns the file path.

        Args:
            file_name: Optional filename for the screenshot (defaults to timestamp).
            resolution: Resolution multiplier (0.1-1.0, default 1.0).
            capture_game_view: True for Game View, False for Scene View.

        Returns:
            Dictionary with screenshot path and metadata.
        """
        try:
            # Validate resolution parameter
            if resolution < 0.1 or resolution > 1.0:
                return {"success": False, "message": "Resolution must be between 0.1 and 1.0"}
            
            params = {
                "action": "capture",
                "resolution": resolution,  # Always include resolution
                "captureGameView": capture_game_view,  # Always include this
            }
            
            # Only include fileName if provided
            if file_name is not None:
                params["fileName"] = file_name
            
            print(f"[DEBUG] Sending take_screenshot params: {params}")
            response = get_unity_connection().send_command("take_screenshot", params)
            
            if response.get("success"):
                return {
                    "success": True,
                    "message": response.get("message"),
                    "path": response.get("data", {}).get("path"),
                    "absolutePath": response.get("data", {}).get("absolutePath"),
                    "fileName": response.get("data", {}).get("fileName"),
                    "captureType": response.get("data", {}).get("captureType"),
                    "resolution": response.get("data", {}).get("resolution"),
                }
            else:
                return {"success": False, "message": response.get("error")}
                
        except Exception as e:
            return {"success": False, "message": f"Python error: {str(e)}"}