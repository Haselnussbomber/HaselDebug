using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Lua;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;

namespace HaselDebug.Tabs;

// Thanks to Pohky

public readonly record struct LuaGlobal(string Key, string? Value, LuaType Type);

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append)]
public unsafe class LuaDebugTab : DebugTab
{
    public static lua_State* L => Framework.Instance()->LuaState.State;
    public string InspectorFilter = string.Empty;
    public string GlobalsFilter = string.Empty;
    public readonly string[] TypeFilterStrings = Enum.GetNames<LuaType>();
    public int TypeFilter = (int)LuaType.UserData;

    public override void Draw()
    {
        if (ImGui.BeginTabBar("##luaTabs"))
        {
            if (ImGui.BeginTabItem("Inspector##LuaInspectorTab"))
            {
                DrawInspectorTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Globals##LuaGlobalsTab"))
            {
                DrawGlobalsTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawInspectorTab()
    {
        ImGui.SetNextItemWidth(120);
        ImGui.Combo("##inspectorTypeFilter", ref TypeFilter, TypeFilterStrings, TypeFilterStrings.Length);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##inspectorFilter", ref InspectorFilter, 512);
        ImGui.Separator();
        if (ImGui.BeginChild("##inspectorChild"))
        {
            var filterType = Enum.Parse<LuaType>(TypeFilterStrings[TypeFilter]);
            if (ImGui.TreeNodeEx("_G", ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.DefaultOpen))
            {
                var top = L->lua_gettop();
                L->lua_getglobal("_G");
                L->lua_pushnil();
                while (L->lua_next(-2) != 0)
                {
                    var strKey = L->lua_tostring(-2).ToString();
                    var typeValue = L->lua_type(-1);

                    var filtered = false;
                    if (!string.IsNullOrWhiteSpace(InspectorFilter))
                    {
                        if (!strKey.Contains(InspectorFilter, StringComparison.OrdinalIgnoreCase))
                            filtered = true;
                    }
                    else
                    {
                        if (filterType != LuaType.None && filterType != LuaType.Nil)
                        {
                            if (typeValue != filterType)
                                filtered = true;
                        }
                    }

                    if (typeValue != LuaType.Function && !filtered)
                        DrawNodeByType(typeValue, strKey, "_G", L->lua_gettop());

                    L->lua_pop(1);
                }

                L->lua_settop(top);

                ImGui.TreePop();
            }
        }
        ImGui.EndChild();
    }

    private void DrawNodeByType(LuaType type, string key, string id, int idx)
    {
        var drawTop = L->lua_gettop();

        switch (type)
        {
            case LuaType.Table:
                if (ImGui.TreeNodeEx($"[{type}] {key}##{id}", ImGuiTreeNodeFlags.SpanAvailWidth))
                {
                    var top = L->lua_gettop();
                    L->lua_pushvalue(idx);
                    L->lua_pushnil();
                    while (L->lua_next(-2) != 0)
                    {
                        var typeValue = L->lua_type(-1);

                        string strKey;
                        var typeKey = L->lua_type(-2);
                        if (typeKey == LuaType.Number)
                        {
                            strKey = $"{L->lua_tonumber(-2)}";
                            DrawNodeByType(typeValue, strKey, key, L->lua_gettop());
                        }
                        else if (typeKey == LuaType.String)
                        {
                            strKey = $"{L->lua_tostring(-2)}";
                            DrawNodeByType(typeValue, strKey, key, L->lua_gettop());
                        }
                        else
                        {
                            ImGui.TreeNodeEx($"[{typeValue}] {key}.??##{id}", ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.NoTreePushOnOpen);
                        }

                        L->lua_pop(1);
                    }

                    L->lua_settop(top);
                    ImGui.TreePop();
                }

                break;
            case LuaType.UserData:
                if (ImGui.TreeNodeEx($"[{type}] {key}##{id}", ImGuiTreeNodeFlags.SpanAvailWidth))
                {
                    var top = L->lua_gettop();

                    if (L->lua_getmetatable(idx) == 1)
                    {
                        var className = "Class";
                        L->lua_getfield(idx, "className");
                        if (L->lua_type(-1) == LuaType.String)
                            className = $"{L->lua_tostring(-1)}";
                        L->lua_pop(1);
                        DrawNodeByType(L->lua_type(-1), className, id, L->lua_gettop());
                        L->lua_pop(1);
                    }
                    else
                    {
                        L->lua_getfield(idx, "className");
                        if (L->lua_type(-1) == LuaType.String)
                            ImGui.TreeNodeEx($"className = {L->lua_tostring(-1)}##{id}", ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.NoTreePushOnOpen);
                        L->lua_pop(1);
                    }

                    L->lua_settop(top);
                    ImGui.TreePop();
                }

                break;
            case LuaType.Boolean:
            case LuaType.Number:
            case LuaType.String:
                ImGui.TreeNodeEx($"[{type}] {key} = \"{L->lua_tostring(idx)}\"##{id}", ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.NoTreePushOnOpen);
                break;
            case LuaType.LightUserData:
            case LuaType.Function:
            case LuaType.Thread:
            case LuaType.Proto:
            case LuaType.Upval:
            case LuaType.None:
            case LuaType.Nil:
            default:
                ImGui.TreeNodeEx($"[{type}] {key}##{id}", ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.NoTreePushOnOpen);
                break;
        }

        L->lua_settop(drawTop);
    }

    private void DrawGlobalsTab()
    {
        ImGui.SetNextItemWidth(120);
        ImGui.Combo("##globalsTypeFilter", ref TypeFilter, TypeFilterStrings, TypeFilterStrings.Length);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##globalsFilter", ref GlobalsFilter, 512);
        ImGui.Separator();
        if (!ImGui.BeginTable("##luaGlobalEnvTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
            return;
        ImGui.TableSetupScrollFreeze(0, 1);

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        var filterType = Enum.Parse<LuaType>(TypeFilterStrings[TypeFilter]);

        foreach (var g in EnumGlobals().OrderBy(v => v.Type))
        {
            if (!string.IsNullOrWhiteSpace(GlobalsFilter))
            {
                if (!g.Key.Contains(GlobalsFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
            }
            else
            {
                if (filterType != LuaType.None && filterType != LuaType.Nil)
                {
                    if (filterType != g.Type)
                        continue;
                }
            }

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{g.Key}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{g.Type}");

            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{g.Value}");
        }

        ImGui.EndTable();
    }

    private static IEnumerable<LuaGlobal> EnumGlobals()
    {
        var list = new List<LuaGlobal>();

        var top = L->lua_gettop();
        L->lua_getglobal("_G");
        L->lua_pushnil();
        while (L->lua_next(-2) != 0)
        {
            var strKey = L->lua_tostring(-2).ToString();
            if (string.IsNullOrEmpty(strKey))
                strKey = "INVALID_KEY";
            var typeValue = L->lua_type(-1);
            var strValue = L->lua_tostring(-1);

            list.Add(new LuaGlobal(strKey, strValue, typeValue));

            L->lua_pop(1);
        }

        L->lua_settop(top);

        return list;
    }
}
