from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any
from unity_connection import get_unity_connection

def register_compile_and_reload_tools(mcp: FastMCP):
    """Register compile and reload tools with the MCP server."""

    @mcp.tool()
    def compile_and_reload(
        ctx: Context,
        action: str = "compile_and_reload",
    ) -> Dict[str, Any]:
        """Manually trigger Unity script compilation and domain reload.

        Args:
            action: Operation to perform:
                - 'compile': Only compile scripts
                - 'reload': Only reload domain
                - 'refresh': Only refresh assets
                - 'compile_and_reload': Full compile and reload (default)

        Returns:
            Dictionary with operation results.
        """
        try:
            params = {
                "action": action,
            }
            
            response = get_unity_connection().send_command("compile_and_reload", params)
            
            if response.get("success"):
                return {
                    "success": True,
                    "message": response.get("message"),
                    "data": response.get("data")
                }
            else:
                return {
                    "success": False,
                    "message": response.get("error", "Unknown error occurred")
                }
                
        except Exception as e:
            return {
                "success": False,
                "message": f"Python error: {str(e)}"
            }