using System.Reflection;
using Dalamud.Utility;
using HaselDebug.Utils;

namespace HaselDebug.Services;

public partial class DebugRenderer
{
    public void DrawExdRow(Type sheetType, uint rowId, uint depth, NodeOptions nodeOptions)
    {
        if (depth > 10)
        {
            ImGui.Text("max depth reached"u8);
            return;
        }

        nodeOptions = nodeOptions.WithAddress((StringComparer.Ordinal.GetHashCode(sheetType.Name), (nint)rowId).GetHashCode());

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
            if (propInfo.Name is "RowId" or "ExcelPage" or "RowOffset")
                continue;

            ImGuiUtils.DrawCopyableText(propInfo.PropertyType.ReadableTypeName(), new()
            {
                CopyText = propInfo.PropertyType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)),
                TextColor = ColorType
            });
            ImGui.SameLine();
            ImGuiUtils.DrawCopyableText(propInfo.Name, new CopyableTextOptions() { TextColor = ColorFieldName });
            ImGui.SameLine();
            DrawExdSheetColumnValue(sheetType, rowId, propInfo.Name, depth, nodeOptions.WithAddress(StringComparer.Ordinal.GetHashCode(propInfo.Name)));
        }
    }

    public void DrawExdSubrow(Type sheetType, uint rowId, ushort subrowId, uint depth, NodeOptions nodeOptions)
    {
        if (depth > 10)
        {
            ImGui.Text("max depth reached"u8);
            return;
        }

        nodeOptions = nodeOptions.WithAddress((StringComparer.Ordinal.GetHashCode(sheetType.Name), (nint)rowId, (nint)subrowId).GetHashCode());

        var title = $"{sheetType.Name}#{rowId}.{subrowId}";
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
            if (propInfo.Name is "RowId" or "SubrowId" or "ExcelPage" or "RowOffset")
                continue;

            ImGuiUtils.DrawCopyableText(propInfo.PropertyType.ReadableTypeName(), new()
            {
                CopyText = propInfo.PropertyType.ReadableTypeName(ImGui.IsKeyDown(ImGuiKey.LeftShift)),
                TextColor = ColorType
            });
            ImGui.SameLine();
            ImGuiUtils.DrawCopyableText(propInfo.Name, new CopyableTextOptions() { TextColor = ColorFieldName });
            ImGui.SameLine();
            DrawExdSubrowSheetColumnValue(sheetType, rowId, subrowId, propInfo.Name, depth, nodeOptions.WithAddress(StringComparer.Ordinal.GetHashCode(propInfo.Name)));
        }
    }

    private void DrawExdSheetColumnValue(Type sheetType, uint rowId, string propName, uint depth, NodeOptions nodeOptions)
    {
        var getSheet = _dataManager.Excel.GetType().GetMethod("GetSheet", BindingFlags.Instance | BindingFlags.Public)!;
        var genericGetSheet = getSheet.MakeGenericMethod(sheetType);
        var language = nodeOptions.Language ?? _languageProvider.ClientLanguage;
        var sheet = genericGetSheet.Invoke(_dataManager.Excel, [language.ToLumina(), sheetType.GetCustomAttribute<SheetAttribute>()?.Name ?? sheetType.Name]);
        if (sheet == null)
        {
            ImGui.Text("sheet is null"u8);
            return;
        }

        var getRow = sheet.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(info => info.Name == "GetRowOrDefault" && info.GetParameters().Length == 1);
        if (getRow == null)
        {
            ImGui.Text("Could not find GetRowOrDefault"u8);
            return;
        }

        var row = getRow?.Invoke(sheet, [rowId]);
        if (row == null)
        {
            ImGui.Text($"Row {rowId} is null");
            return;
        }

        var propInfo = sheetType.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
        if (propInfo == null)
            return;

        DrawExcelColumn(
            propInfo.Name,
            propInfo.PropertyType,
            propInfo.GetValue(row),
            rowId,
            depth,
            nodeOptions);
    }

    private void DrawExdSubrowSheetColumnValue(Type sheetType, uint rowId, ushort subrowId, string propName, uint depth, NodeOptions nodeOptions)
    {
        var getSheet = _dataManager.Excel.GetType().GetMethod("GetSubrowSheet", BindingFlags.Instance | BindingFlags.Public)!;
        var genericGetSheet = getSheet.MakeGenericMethod(sheetType);
        var language = nodeOptions.Language ?? _languageProvider.ClientLanguage;
        var sheet = genericGetSheet.Invoke(_dataManager.Excel, [language.ToLumina(), sheetType.GetCustomAttribute<SheetAttribute>()?.Name ?? sheetType.Name]);
        if (sheet == null)
        {
            ImGui.Text("sheet is null"u8);
            return;
        }

        var getSubrowOrDefault = sheet.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(info => info.Name == "GetSubrowOrDefault" && info.GetParameters().Length == 2);
        if (getSubrowOrDefault == null)
        {
            ImGui.Text("Could not find GetSubrowOrDefault"u8);
            return;
        }

        var row = getSubrowOrDefault?.Invoke(sheet, [rowId, subrowId]);
        if (row == null)
        {
            ImGui.Text($"Row {rowId}.{subrowId} is null");
            return;
        }

        var propInfo = sheetType.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
        if (propInfo == null)
            return;

        DrawExcelColumn(
            propInfo.Name,
            propInfo.PropertyType,
            propInfo.GetValue(row),
            rowId,
            depth,
            nodeOptions);
    }

    private void DrawExcelColumn(string columnName, Type columnType, object? value, uint rowId, uint depth, NodeOptions nodeOptions)
    {
        if (value == null)
        {
            ImGui.Text("null"u8);
            return;
        }

        if (columnType == typeof(ReadOnlySeString))
        {
            var language = nodeOptions.Language ?? _languageProvider.ClientLanguage;
            DrawSeString(((ReadOnlySeString)value).AsSpan(), new NodeOptions()
            {
                AddressPath = nodeOptions.AddressPath.With(StringComparer.Ordinal.GetHashCode(columnName)),
                RenderSeString = false,
                Title = $"{value!.GetType().Name}#{rowId} ({language}) {columnName}",
                Language = language
            });
            return;
        }

        if (columnType == typeof(RowRef))
        {
            var columnRowId = (uint)columnType.GetProperty("RowId")?.GetValue(value)!;
            ImGuiUtils.DrawCopyableText(columnRowId.ToString());
            return;
        }

        if (columnType.IsGenericType && (columnType.GetGenericTypeDefinition() == typeof(RowRef<>) || columnType.GetGenericTypeDefinition() == typeof(SubrowRef<>)))
        {
            var isValid = (bool)columnType.GetProperty("IsValid")?.GetValue(value)!;
            var columnRowId = (uint)columnType.GetProperty("RowId")?.GetValue(value)!;

            if (!isValid)
            {
                ImGui.Text("Invalid (RowId: "u8);
                ImGui.SameLine(0, 0);
                ImGuiUtils.DrawCopyableText(columnRowId.ToString());
                ImGui.SameLine(0, 0);
                ImGui.Text(")"u8);
                return;
            }

            var columnRowType = columnType.GenericTypeArguments[0];
            DrawExdRow(columnRowType, columnRowId, depth + 1, new NodeOptions()
            {
                RenderSeString = nodeOptions.RenderSeString,
                Language = nodeOptions.Language,
                AddressPath = nodeOptions.AddressPath.With((StringComparer.Ordinal.GetHashCode(columnRowType.Name), (nint)columnRowId).GetHashCode())
            });
            return;
        }

        if (columnType.IsGenericType && columnType.GetGenericTypeDefinition() == typeof(Collection<>))
        {
            var count = (int)columnType.GetProperty("Count")?.GetValue(value)!;
            if (count == 0)
            {
                ImGui.Text("No values"u8);
                return;
            }

            var collectionType = columnType.GenericTypeArguments[0];
            var propNodeOptions = nodeOptions.WithAddress(StringComparer.Ordinal.GetHashCode(collectionType.Name));

            using var colTitleColor = ImRaii.PushColor(ImGuiCol.Text, ColorTreeNode.ToVector());
            using var colNode = ImRaii.TreeNode($"{count} Value{(count != 1 ? "s" : "")}{propNodeOptions.GetKey("CollectionNode")}", nodeOptions.GetTreeNodeFlags());
            if (!colNode) return;
            colTitleColor?.Dispose();

            using var table = ImRaii.Table(propNodeOptions.GetKey("CollectionTable"), 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.NoSavedSettings);
            if (!table) return;

            ImGui.TableSetupColumn("Index"u8, ImGuiTableColumnFlags.WidthFixed, 40);
            ImGui.TableSetupColumn("Value");
            ImGui.TableSetupScrollFreeze(2, 1);
            ImGui.TableHeadersRow();

            using var indent = ImRaii.PushIndent(1, nodeOptions.Indent);
            for (var i = 0; i < count; i++)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn(); // Index
                ImGui.Text(i.ToString());

                ImGui.TableNextColumn(); // Value

                var colValue = columnType.GetMethod("get_Item")?.Invoke(value, [i]);

                DrawExcelColumn(columnName + $"[{i}]", collectionType, colValue, rowId, depth + 1, nodeOptions);
            }

            return;
        }

        if (nodeOptions.IsIconIdField && columnType.IsNumericType())
        {
            DrawIcon(value, columnType);
        }

        ImGuiUtils.DrawCopyableText(value.ToString() ?? string.Empty);
    }
}
