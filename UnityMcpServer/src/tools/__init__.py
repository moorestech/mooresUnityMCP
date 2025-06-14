from .manage_script import register_manage_script_tools
from .manage_scene import register_manage_scene_tools
from .manage_editor import register_manage_editor_tools
from .manage_gameobject import register_manage_gameobject_tools
from .manage_asset import register_manage_asset_tools
from .read_console import register_read_console_tools
from .execute_menu_item import register_execute_menu_item_tools
from .take_screenshot import register_take_screenshot_tools
from .get_current_hierarchy import register_get_current_hierarchy_tools
from .compile_and_reload import register_compile_and_reload_tools
from .manage_prefab_variant import register_manage_prefab_variant_tools

def register_all_tools(mcp):
    """Register all refactored tools with the MCP server."""
    print("Registering Unity MCP Server refactored tools...")
    register_manage_script_tools(mcp)
    register_manage_scene_tools(mcp)
    register_manage_editor_tools(mcp)
    register_manage_gameobject_tools(mcp)
    register_manage_asset_tools(mcp)
    register_read_console_tools(mcp)
    register_execute_menu_item_tools(mcp)
    register_take_screenshot_tools(mcp)
    register_get_current_hierarchy_tools(mcp)
    register_compile_and_reload_tools(mcp)
    register_manage_prefab_variant_tools(mcp)
    print("Unity MCP Server tool registration complete.")
