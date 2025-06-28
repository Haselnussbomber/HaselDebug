using System.Linq;
using System.Reflection;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using HaselDebug.Utils;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Text.ReadOnly;

namespace HaselDebug.Services;

public unsafe partial class DebugRenderer
{
    public void DrawExdRow(Type sheetType, uint rowId, uint depth, NodeOptions nodeOptions)
    {
        if (depth > 10)
        {
            ImGui.TextUnformatted("max depth reached");
            return;
        }

        nodeOptions = nodeOptions.WithAddress((sheetType.Name.GetHashCode(), (nint)rowId).GetHashCode());

        var title = $"{sheetType.Name}#{rowId}";
        if (!string.IsNullOrEmpty(nodeOptions.Title))
        {
            title = nodeOptions.Title;
            nodeOptions = nodeOptions with { Title = null };
        }

        using var titleColor = ImRaii.PushColor(ImGuiCol.Text, nodeOptions.TitleColor ?? ColorTreeNode.ToVector());
        using var node = ImRaii.TreeNode($"{title}###{nodeOptions.AddressPath}", nodeOptions.GetTreeNodeFlags());
        nodeOptions = nodeOptions.ConsumeTreeNodeOptions();
        if (!node) return;
        titleColor.Dispose();

        foreach (var propInfo in sheetType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (propInfo.Name == "RowId")
                continue;

            ImGuiUtilsEx.DrawCopyableText(propInfo.PropertyType.ReadableTypeName(), propInfo.PropertyType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)), textColor: ColorType);
            ImGui.SameLine();
            ImGui.TextColored(ColorFieldName, propInfo.Name);
            ImGui.SameLine();
            DrawExdSheetColumnValue(sheetType, rowId, propInfo.Name, depth, nodeOptions.WithAddress(propInfo.Name.GetHashCode()));
        }
    }

    public void DrawExdSheetColumnValue(Type sheetType, uint rowId, string propName, uint depth, NodeOptions nodeOptions)
    {
        var getSheet = _dataManager.Excel.GetType().GetMethod("GetSheet", BindingFlags.Instance | BindingFlags.Public)!;
        var genericGetSheet = getSheet.MakeGenericMethod(sheetType);
        var language = nodeOptions.Language ?? _languageProvider.ClientLanguage;
        var sheet = genericGetSheet.Invoke(_dataManager.Excel, [language.ToLumina(), sheetType.GetCustomAttribute<SheetAttribute>()?.Name ?? sheetType.Name]);
        if (sheet == null)
        {
            ImGui.TextUnformatted("sheet is null");
            return;
        }

        var getRow = sheet.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(info => info.Name == "GetRowOrDefault" && info.GetParameters().Length == 1);
        if (getRow == null)
        {
            ImGui.TextUnformatted("Could not find GetRowOrDefault");
            return;
        }

        var row = getRow?.Invoke(sheet, [rowId]);
        if (row == null)
        {
            ImGui.TextUnformatted($"Row {rowId} is null");
            return;
        }

        var propInfo = sheetType.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
        if (propInfo == null)
            return;

        DrawExcelProp(propInfo, row, rowId, depth, nodeOptions);
    }

    private void DrawExcelProp(PropertyInfo propInfo, object? row, uint rowId, uint depth, NodeOptions nodeOptions)
    {
        var propName = propInfo.Name;
        var propType = propInfo.PropertyType;
        var value = propInfo.GetValue(row);

        if (value == null)
        {
            ImGui.TextUnformatted("null");
            return;
        }

        if (propType == typeof(ReadOnlySeString))
        {
            var language = nodeOptions.Language ?? _languageProvider.ClientLanguage;
            DrawSeString(((ReadOnlySeString)value).AsSpan(), new NodeOptions()
            {
                AddressPath = nodeOptions.AddressPath.With(propName.GetHashCode()),
                RenderSeString = false,
                Title = $"{row!.GetType().Name}#{rowId} ({language})",
                Language = language
            });
            return;
        }

        if (propType == typeof(RowRef))
        {
            var columnRowId = (uint)propType.GetProperty("RowId")?.GetValue(value)!;
            ImGui.TextUnformatted(columnRowId.ToString());
            return;
        }

        if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(RowRef<>))
        {
            var isValid = (bool)propType.GetProperty("IsValid")?.GetValue(value)!;
            if (!isValid)
            {
                ImGui.TextUnformatted("null");
                return;
            }

            var columnRowType = propType.GenericTypeArguments[0];
            var columnRowId = (uint)propType.GetProperty("RowId")?.GetValue(value)!;
            DrawExdRow(columnRowType, columnRowId, depth + 1, new NodeOptions()
            {
                RenderSeString = nodeOptions.RenderSeString,
                Language = nodeOptions.Language,
                AddressPath = nodeOptions.AddressPath.With((columnRowType.Name.GetHashCode(), (nint)columnRowId).GetHashCode())
            });
            return;
        }

        if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Collection<>))
        {
            var count = (int)propType.GetProperty("Count")?.GetValue(value)!;
            if (count == 0)
            {
                ImGui.TextUnformatted("No values");
                return;
            }

            var collectionType = propType.GenericTypeArguments[0];
            var propNodeOptions = nodeOptions.WithAddress(collectionType.Name.GetHashCode());

            using var colTitleColor = ImRaii.PushColor(ImGuiCol.Text, ColorTreeNode.ToVector());
            using var colNode = ImRaii.TreeNode($"{count} Value{(count != 1 ? "s" : "")}{propNodeOptions.GetKey("CollectionNode")}", nodeOptions.GetTreeNodeFlags());
            if (!colNode) return;
            colTitleColor?.Dispose();

            using var table = ImRaii.Table(propNodeOptions.GetKey("CollectionTable"), 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
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

                var colValue = propType.GetMethod("get_Item")?.Invoke(value, [i]);
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

                if (collectionType == typeof(RowRef))
                {
                    var columnRowId = (uint)collectionType.GetProperty("RowId")?.GetValue(colValue)!;
                    ImGui.TextUnformatted(columnRowId.ToString());
                    continue;
                }

                if (collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(RowRef<>))
                {
                    var isValid = (bool)collectionType.GetProperty("IsValid")?.GetValue(colValue)!;
                    if (!isValid)
                    {
                        ImGui.TextUnformatted("null");
                        continue;
                    }

                    var columnRowType = collectionType.GenericTypeArguments[0];
                    var columnRowId = (uint)collectionType.GetProperty("RowId")?.GetValue(colValue)!;

                    DrawExdRow(columnRowType, columnRowId, depth + 1, new NodeOptions()
                    {
                        RenderSeString = nodeOptions.RenderSeString,
                        Language = nodeOptions.Language,
                        AddressPath = nodeOptions.AddressPath.With((i, columnRowType.Name.GetHashCode(), (nint)columnRowId).GetHashCode())
                    });
                    continue;
                }

                if (collectionType.IsStruct())
                {
                    using var structTitleColor = ImRaii.PushColor(ImGuiCol.Text, ColorTreeNode.ToVector());
                    using var structNode = ImRaii.TreeNode($"{collectionType.Name}{propNodeOptions.GetKey($"{collectionType.Name}_{i}")}", nodeOptions.GetTreeNodeFlags());
                    if (!structNode) continue;
                    structTitleColor?.Dispose();

                    foreach (var pi in collectionType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    {
                        if (pi.Name == "RowId")
                            continue;

                        ImGuiUtilsEx.DrawCopyableText(pi.PropertyType.ReadableTypeName(), pi.PropertyType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)), textColor: ColorType);
                        ImGui.SameLine();
                        ImGui.TextColored(ColorFieldName, pi.Name);
                        ImGui.SameLine();
                        DrawExcelProp(pi, colValue, rowId, depth, nodeOptions);
                    }

                    continue;
                }

                if (collectionType.IsPrimitive)
                {
                    ImGui.TextUnformatted(colValue.ToString());
                    continue;
                }

                ImGui.TextUnformatted($"Unsupported type: {collectionType.Name}");
            }

            return;
        }

        if (nodeOptions.IsIconIdField && propType.IsNumericType())
        {
            DrawIcon(value, propType);
        }

        ImGui.TextUnformatted(value.ToString());
    }
}
