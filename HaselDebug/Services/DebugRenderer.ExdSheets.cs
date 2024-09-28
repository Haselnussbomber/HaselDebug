using System.Linq;
using System.Reflection;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ExdSheets;
using HaselCommon.Extensions;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public void DrawExdSheet(ExdSheets.Module module, Type rowType, uint rowId, uint depth, NodeOptions nodeOptions)
    {
        if (depth > 10)
        {
            ImGui.TextUnformatted("max depth reached");
            return;
        }

        nodeOptions = nodeOptions.WithAddress((rowType.Name.GetHashCode(), (nint)rowId).GetHashCode());

        using var titleColor = ImRaii.PushColor(ImGuiCol.Text, (uint)ColorTreeNode);
        using var node = ImRaii.TreeNode($"{rowType.Name}#{rowId}###{nodeOptions.AddressPath}", nodeOptions.GetTreeNodeFlags());
        if (!node) return;
        titleColor.Dispose();

        GetSheetGeneric ??= module.GetType().GetMethod("GetSheetGeneric", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var sheet = GetSheetGeneric.Invoke(module, [rowType, nodeOptions.Language.ToLumina()]);
        if (sheet == null)
        {
            ImGui.TextUnformatted("sheet is null");
            return;
        }

        var getRow = sheet.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(info => info.Name == "TryGetRow" && info.GetParameters().Length == 1);
        if (getRow == null)
        {
            ImGui.TextUnformatted("Could not find TryGetRow");
            return;
        }

        var row = getRow?.Invoke(sheet, [rowId]);
        if (row == null)
        {
            ImGui.TextUnformatted($"Row {rowId} is null");
            return;
        }

        foreach (var propInfo in rowType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (propInfo.Name == "RowId") continue;

            DrawCopyableText(propInfo.PropertyType.ReadableTypeName(), propInfo.PropertyType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)), textColor: ColorType);
            ImGui.SameLine();
            ImGui.TextColored(ColorFieldName, propInfo.Name);
            ImGui.SameLine();
            DrawExdSheetColumnValue(module, rowType, rowId, propInfo.Name, depth, nodeOptions);
        }
    }

    public void DrawExdSheetColumnValue(ExdSheets.Module module, Type rowType, uint rowId, string propName, uint depth, NodeOptions nodeOptions)
    {
        GetSheetGeneric ??= module.GetType().GetMethod("GetSheetGeneric", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var sheet = GetSheetGeneric.Invoke(module, [rowType, nodeOptions.Language.ToLumina()]);
        if (sheet == null)
        {
            ImGui.TextUnformatted("sheet is null");
            return;
        }

        var getRow = sheet.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(info => info.Name == "TryGetRow" && info.GetParameters().Length == 1);
        if (getRow == null)
        {
            ImGui.TextUnformatted("Could not find TryGetRow");
            return;
        }

        var row = getRow?.Invoke(sheet, [rowId]);
        if (row == null)
        {
            ImGui.TextUnformatted($"Row {rowId} is null");
            return;
        }

        var propInfo = rowType.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
        if (propInfo == null)
            return;

        var value = propInfo.GetValue(row);
        if (value == null)
        {
            ImGui.TextUnformatted("null");
            return;
        }

        if (propInfo.PropertyType == typeof(ReadOnlySeString))
        {
            DrawSeString(((ReadOnlySeString)value).AsSpan(), true, new NodeOptions()
            {
                RenderSeString = nodeOptions.RenderSeString,
                AddressPath = nodeOptions.AddressPath.With(propInfo.Name.GetHashCode())
            });
            return;
        }

        if (propInfo.PropertyType == typeof(LazyRow))
        {
            var columnRowId = (uint)propInfo.PropertyType.GetProperty("RowId")?.GetValue(value)!;
            ImGui.TextUnformatted(columnRowId.ToString());
            return;
        }

        if (propInfo.PropertyType.IsGenericType && propInfo.PropertyType.GetGenericTypeDefinition() == typeof(LazyRow<>))
        {
            var isValid = (bool)propInfo.PropertyType.GetProperty("IsValid")?.GetValue(value)!;
            if (!isValid)
            {
                ImGui.TextUnformatted("null");
                return;
            }

            var columnRowType = propInfo.PropertyType.GenericTypeArguments[0];
            var columnRowId = (uint)propInfo.PropertyType.GetProperty("RowId")?.GetValue(value)!;
            DrawExdSheet(module, columnRowType, columnRowId, depth + 1, new NodeOptions()
            {
                RenderSeString = nodeOptions.RenderSeString,
                Language = nodeOptions.Language,
                AddressPath = nodeOptions.AddressPath.With((columnRowType.Name.GetHashCode(), (nint)columnRowId).GetHashCode())
            });
            return;
        }

        if (propInfo.PropertyType.IsGenericType && propInfo.PropertyType.GetGenericTypeDefinition() == typeof(LazyCollection<>))
        {
            var count = (int)propInfo.PropertyType.GetProperty("Count")?.GetValue(value)!;
            if (count == 0)
            {
                ImGui.TextUnformatted("No values");
                return;
            }

            var collectionType = propInfo.PropertyType.GenericTypeArguments[0];
            var propNodeOptions = nodeOptions.WithAddress(collectionType.Name.GetHashCode());

            using var colTitleColor = ImRaii.PushColor(ImGuiCol.Text, (uint)ColorTreeNode);
            using var colNode = ImRaii.TreeNode($"{count} Value{(count != 1 ? "s" : "")}{propNodeOptions.GetKey("LazyCollectionNode")}", nodeOptions.GetTreeNodeFlags());
            if (!colNode) return;
            colTitleColor?.Dispose();

            using var table = ImRaii.Table(propNodeOptions.GetKey("LazyCollectionTable"), 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
            if (!table) return;

            ImGui.TableSetupColumn("Index", ImGuiTableColumnFlags.WidthFixed, 40);
            ImGui.TableSetupColumn("Value");
            ImGui.TableSetupScrollFreeze(2, 1);
            ImGui.TableHeadersRow();

            using var indent = ImRaii.PushIndent(1, nodeOptions.Indent);
            for (var i = 0; i < count; i++)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn(); // Index
                ImGui.TextUnformatted(i.ToString());

                ImGui.TableNextColumn(); // Value

                var colValue = propInfo.PropertyType.GetMethod("get_Item")?.Invoke(value, [i]);
                if (colValue == null)
                {
                    ImGui.TextUnformatted("null");
                    continue;
                }

                if (collectionType == typeof(ReadOnlySeString))
                {
                    DrawSeString(((ReadOnlySeString)colValue).AsSpan(), true, new NodeOptions()
                    {
                        RenderSeString = nodeOptions.RenderSeString,
                        AddressPath = nodeOptions.AddressPath.With(collectionType.Name.GetHashCode())
                    });
                    continue;
                }

                if (collectionType == typeof(LazyRow))
                {
                    var columnRowId = (uint)collectionType.GetProperty("RowId")?.GetValue(colValue)!;
                    ImGui.TextUnformatted(columnRowId.ToString());
                    continue;
                }

                if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(LazyRow<>))
                {
                    var isValid = (bool)collectionType.GetProperty("IsValid")?.GetValue(colValue)!;
                    if (!isValid)
                    {
                        ImGui.TextUnformatted("null");
                        continue;
                    }

                    var columnRowType = collectionType.GenericTypeArguments[0];
                    var columnRowId = (uint)collectionType.GetProperty("RowId")?.GetValue(colValue)!;

                    DrawExdSheet(module, columnRowType, columnRowId, depth + 1, new NodeOptions()
                    {
                        RenderSeString = nodeOptions.RenderSeString,
                        Language = nodeOptions.Language,
                        AddressPath = nodeOptions.AddressPath.With((i, columnRowType.Name.GetHashCode(), (nint)columnRowId).GetHashCode())
                    });
                }
                else
                {
                    ImGui.TextUnformatted("Unsupported type");
                }
            }

            return;
        }

        ImGui.TextUnformatted(value.ToString());
    }
}
