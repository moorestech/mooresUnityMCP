from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any
from unity_connection import get_unity_connection

def register_get_current_hierarchy_tools(mcp: FastMCP):
    """Register hierarchy visualization tools with the MCP server."""

    @mcp.tool()
    def get_current_hierarchy(
        ctx: Context,
    ) -> Dict[str, Any]:
        """Gets the current Unity scene hierarchy in YAML format.
        
        Returns a YAML-formatted representation of the current scene hierarchy,
        including GameObject names, positions, rotations, scales, and UI rect properties.

        Returns:
            Dictionary with:
            - success: bool indicating if operation succeeded
            - sceneName: Name of the current scene
            - hierarchyYaml: YAML-formatted hierarchy string
        """
        try:
            # Send command with empty params (no parameters needed)
            response = get_unity_connection().send_command("get_current_hierarchy", {})
            
            if response.get("success"):
                data = response.get("data", {})
                return {
                    "success": True,
                    "sceneName": data.get("sceneName", "Unknown"),
                    "hierarchyYaml": data.get("hierarchyYaml", "")
                }
            else:
                return {
                    "success": False,
                    "message": response.get("error", "Failed to get hierarchy")
                }
                
        except Exception as e:
            return {
                "success": False,
                "message": f"Python error: {str(e)}"
            }