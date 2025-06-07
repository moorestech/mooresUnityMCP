from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any
from unity_connection import get_unity_connection

def register_take_screenshot_tools(mcp: FastMCP):
    """Register screenshot capture tools with the MCP server."""

    @mcp.tool()
    def take_screenshot(
        ctx: Context,
        file_name: str = None,
        super_size: int = 1,
        capture_game_view: bool = True,
    ) -> Dict[str, Any]:
        """Captures a screenshot in Unity and returns the file path.

        Args:
            file_name: Optional filename for the screenshot (defaults to timestamp).
            super_size: Resolution multiplier (1-4, default 1).
            capture_game_view: True for Game View, False for Scene View.

        Returns:
            Dictionary with screenshot path and metadata.
        """
        try:
            params = {
                "action": "capture",
                "fileName": file_name,
                "superSize": super_size,
                "captureGameView": capture_game_view,
            }
            params = {k: v for k, v in params.items() if v is not None}
            
            response = get_unity_connection().send_command("take_screenshot", params)
            
            if response.get("success"):
                return {
                    "success": True,
                    "message": response.get("message"),
                    "path": response.get("data", {}).get("path"),
                    "absolutePath": response.get("data", {}).get("absolutePath"),
                    "fileName": response.get("data", {}).get("fileName"),
                    "captureType": response.get("data", {}).get("captureType"),
                    "superSize": response.get("data", {}).get("superSize"),
                }
            else:
                return {"success": False, "message": response.get("error")}
                
        except Exception as e:
            return {"success": False, "message": f"Python error: {str(e)}"}