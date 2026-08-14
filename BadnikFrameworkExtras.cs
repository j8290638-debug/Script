#if TOOLS
using Godot;
using System;

[Tool]
public partial class BadnikFrameworkExtras : EditorPlugin
{
    private MenuButton menu;

    public const string SETTING_GENERATE_RAIL_MESH = "badnik_framework_extras/generate_rail_mesh/enabled";
    public const string SETTING_GENERATE_RAIL_MESH_COLOR = "badnik_framework_extras/generate_rail_mesh/color";
    public const string SETTING_GENERATE_SPRING_CHAIN_SPRING_SCALE = "badnik_framework_extras/generate_spring_chain/spring_scale";

    public override void _EnterTree()
    {
        menu = new Path3DMenu();
        AddControlToContainer(CustomControlContainer.SpatialEditorMenu, menu);

        RegisterSettings();
    }

    public override void _ExitTree()
    {
        RemoveControlFromContainer(CustomControlContainer.SpatialEditorMenu, menu);
    }

    void RegisterSettings()
    {
        var editorSettings = EditorInterface.Singleton.GetEditorSettings();

        // Gemerate rail mesh
        if (!editorSettings.HasSetting(SETTING_GENERATE_RAIL_MESH))
        {
            editorSettings.SetSetting(SETTING_GENERATE_RAIL_MESH, true);
            editorSettings.AddPropertyInfo(new Godot.Collections.Dictionary
            {
                { "name", SETTING_GENERATE_RAIL_MESH },
                { "type", (int)Variant.Type.Bool },
                { "hint", (int)PropertyHint.None },
                { "hint_string", "Generate rail mesh" },
                { "usage", (int)PropertyUsageFlags.Default }
            });
        }

        // Generate rail mesh color
        if (!editorSettings.HasSetting(SETTING_GENERATE_RAIL_MESH_COLOR))
        {
            editorSettings.SetSetting(SETTING_GENERATE_RAIL_MESH_COLOR, new Color(1,0,0,1));
            editorSettings.AddPropertyInfo(new Godot.Collections.Dictionary
            {
                { "name", SETTING_GENERATE_RAIL_MESH_COLOR },
                { "type", (int)Variant.Type.Color },
                { "hint", (int)PropertyHint.None },
                { "hint_string", "Generate rail mesh color" },
                { "usage", (int)PropertyUsageFlags.Default }
            });
        }

        // Generate spring chain scale
        if (!editorSettings.HasSetting(SETTING_GENERATE_SPRING_CHAIN_SPRING_SCALE))
        {
            editorSettings.SetSetting(SETTING_GENERATE_SPRING_CHAIN_SPRING_SCALE, Vector3.One);
            editorSettings.AddPropertyInfo(new Godot.Collections.Dictionary
            {
                { "name", SETTING_GENERATE_SPRING_CHAIN_SPRING_SCALE },
                { "type", (int)Variant.Type.Vector3 },
                { "hint", (int)PropertyHint.None },
                { "hint_string", "Spring scale" },
                { "usage", (int)PropertyUsageFlags.Default }
            });
        }
    }


}
#endif
