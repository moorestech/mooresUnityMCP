from mcp.server.fastmcp import FastMCP, Context
from typing import Dict, Any, Optional
from unity_connection import get_unity_connection

def register_manage_prefab_variant_tools(mcp: FastMCP):
    """Register prefab variant management tools with the MCP server."""

    @mcp.tool()
    def manage_prefab_variant(
        ctx: Context,
        action: str,
        base_prefab_path: Optional[str] = None,
        variant_path: Optional[str] = None,
        variant_name: Optional[str] = None,
        apply_all: bool = True,
        revert_all: bool = True,
    ) -> Dict[str, Any]:
        """Manages Unity Prefab Variants (create, modify, query overrides).

        Args:
            action: Operation to perform:
                - 'create': Create a new prefab variant from a base prefab
                - 'get_overrides': Get all overrides in a variant
                - 'apply_overrides': Apply variant overrides to base prefab
                - 'revert_overrides': Revert variant overrides
            base_prefab_path: Path to the base prefab (for 'create')
            variant_path: Path to save variant or existing variant path
            variant_name: Name for the new variant (optional)
            apply_all: Apply all overrides when using 'apply_overrides'
            revert_all: Revert all overrides when using 'revert_overrides'

        Returns:
            Dictionary with operation results ('success', 'message', 'data').
        """
        try:
            params = {
                "action": action,
                "base_prefab_path": base_prefab_path,
                "variant_path": variant_path,
                "variant_name": variant_name,
                "apply_all": apply_all,
                "revert_all": revert_all,
            }
            # Remove None values
            params = {k: v for k, v in params.items() if v is not None}
            
            response = get_unity_connection().send_command("manage_prefab_variant", params)
            
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