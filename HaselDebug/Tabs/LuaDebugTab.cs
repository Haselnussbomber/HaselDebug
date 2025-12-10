using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Lua;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using HaselDebug.Services;
using HaselDebug.Utils;

namespace HaselDebug.Tabs;

// Thanks to Pohky

public readonly record struct LuaGlobal(string Key, string? Value, LuaType Type);

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class LuaDebugTab : DebugTab
{
    public static lua_State* L => Framework.Instance()->LuaState.State;

    private readonly DebugRenderer _debugRenderer;

    private string _inspectorFilter = string.Empty;
    private string _globalsFilter = string.Empty;
    private readonly string[] _typeFilterStrings = Enum.GetNames<LuaType>();
    private int _typeFilter = (int)LuaType.UserData;

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
        ImGui.Combo("##inspectorTypeFilter", ref _typeFilter, _typeFilterStrings, _typeFilterStrings.Length);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##inspectorFilter", ref _inspectorFilter, 512);
        ImGui.Separator();
        if (ImGui.BeginChild("##inspectorChild"))
        {
            var filterType = Enum.Parse<LuaType>(_typeFilterStrings[_typeFilter]);
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
                    if (!string.IsNullOrWhiteSpace(_inspectorFilter))
                    {
                        if (!strKey.Contains(_inspectorFilter, StringComparison.OrdinalIgnoreCase))
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
                        DrawNodeByType(typeValue, strKey, "_G", L->lua_gettop(), new NodeOptions());

                    L->lua_pop(1);
                }

                L->lua_settop(top);

                ImGui.TreePop();
            }
        }
        ImGui.EndChild();
    }

    private void DrawNodeByType(LuaType type, string key, string id, int idx, NodeOptions nodeOptions)
    {
        var drawTop = L->lua_gettop();

        nodeOptions = nodeOptions.WithAddress(key.GetHashCode());

        ImGui.TextColored(DebugRenderer.ColorType, type.ToString());
        ImGui.SameLine();

        switch (type)
        {
            case LuaType.Table:
                {
                    var className = "";

                    if (key == "__index")
                    {
                        var indextop = L->lua_gettop();
                        L->lua_pushvalue(idx);
                        L->lua_pushnil();
                        while (L->lua_next(-2) != 0)
                        {
                            var typeKey = L->lua_type(-2);
                            if (typeKey == LuaType.String && L->lua_tostring(-2) == "className")
                            {
                                className = L->lua_tostring(-1).ToString();
                                L->lua_pop(1);
                                break;
                            }
                            L->lua_pop(1);
                        }
                        L->lua_settop(indextop);
                    }

                    using var rssb = new RentedSeStringBuilder();
                    var title = rssb.Builder
                        .PushColorBgra(DebugRenderer.ColorFieldName)
                        .Append(key);

                    if (!string.IsNullOrEmpty(className))
                    {
                        rssb.Builder.Append($" ({className})");
                    }

                    rssb.Builder.PopColor();

                    using var treeNode = _debugRenderer.DrawTreeNode(nodeOptions with
                    {
                        TitleColor = DebugRenderer.ColorTreeNode,
                        SeStringTitle = title.ToReadOnlySeString()
                    });

                    if (!treeNode)
                        break;

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
                            DrawNodeByType(typeValue, strKey, key, L->lua_gettop(), nodeOptions);
                        }
                        else if (typeKey == LuaType.String)
                        {
                            strKey = $"{L->lua_tostring(-2)}";
                            DrawNodeByType(typeValue, strKey, key, L->lua_gettop(), nodeOptions);
                        }
                        else
                        {
                            ImGui.TreeNodeEx($"[{typeValue}] {key}.??##{id}", ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.NoTreePushOnOpen);
                        }

                        L->lua_pop(1);
                    }

                    L->lua_settop(top);
                }
                break;
            case LuaType.UserData:
                {
                    using var rssb = new RentedSeStringBuilder();
                    using var treeNode = _debugRenderer.DrawTreeNode(nodeOptions with
                    {
                        TitleColor = DebugRenderer.ColorTreeNode,
                        SeStringTitle = rssb.Builder
                        .PushColorBgra(DebugRenderer.ColorFieldName)
                        .Append(key)
                        .PopColor()
                        .ToReadOnlySeString()
                    });

                    if (!treeNode)
                        break;

                    var top = L->lua_gettop();

                    if (L->lua_getmetatable(idx) == 1)
                    {
                        var className = "Class";
                        L->lua_getfield(idx, "className");
                        if (L->lua_type(-1) == LuaType.String)
                            className = $"{L->lua_tostring(-1)}";
                        L->lua_pop(1);
                        DrawNodeByType(L->lua_type(-1), className, id, L->lua_gettop(), nodeOptions);
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
                }
                break;
            case LuaType.Boolean:
            case LuaType.Number:
            case LuaType.String:
                ImGuiUtils.DrawCopyableText(key, new() { TextColor = DebugRenderer.ColorFieldName });
                ImGui.SameLine();
                ImGuiUtils.DrawCopyableText(L->lua_tostring(idx));
                break;
            case LuaType.Function:
                ImGuiUtils.DrawCopyableText(key, new() { TextColor = DebugRenderer.ColorFieldName });

                var cFunctionPtr = L->lua_tocfunction(-1);
                if (cFunctionPtr != null)
                {
                    ImGui.SameLine();
                    _debugRenderer.DrawAddress(cFunctionPtr);
                }
                break;
            case LuaType.LightUserData:
            case LuaType.Thread:
            case LuaType.Proto:
            case LuaType.Upval:
            case LuaType.None:
            case LuaType.Nil:
            default:
                ImGuiUtils.DrawCopyableText(key, new() { TextColor = DebugRenderer.ColorFieldName });
                break;
        }

        L->lua_settop(drawTop);
    }

    private void DrawGlobalsTab()
    {
        ImGui.SetNextItemWidth(120);
        ImGui.Combo("##globalsTypeFilter", ref _typeFilter, _typeFilterStrings, _typeFilterStrings.Length);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText("##globalsFilter", ref _globalsFilter, 512);
        ImGui.Separator();
        if (!ImGui.BeginTable("##luaGlobalEnvTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
            return;
        ImGui.TableSetupScrollFreeze(0, 1);

        ImGui.TableSetupColumn("Name"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Type"u8, ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("Value"u8, ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        var filterType = Enum.Parse<LuaType>(_typeFilterStrings[_typeFilter]);

        foreach (var g in EnumGlobals().OrderBy(v => v.Type))
        {
            if (!string.IsNullOrWhiteSpace(_globalsFilter))
            {
                if (!g.Key.Contains(_globalsFilter, StringComparison.OrdinalIgnoreCase))
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
            ImGui.Text($"{g.Key}");

            ImGui.TableNextColumn();
            ImGui.Text($"{g.Type}");

            ImGui.TableNextColumn();
            ImGui.Text($"{g.Value}");
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
