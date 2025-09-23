using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using HaselCommon.Graphics;
using HaselCommon.Services;
using HaselDebug.Abstracts;
using HaselDebug.Interfaces;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace HaselDebug.Tabs;

[RegisterSingleton<IDebugTab>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class CompletionTab : DebugTab
{
    private readonly TextService _textService;
    private readonly ExcelService _excelService;
    private readonly ISeStringEvaluator _evaluator;
    private readonly List<CompletionCategoryEntry> _categories = [];
    private bool _initialized;
    private string _textSearchTerm;

    private void Initialize()
    {
        var cols = new List<int>();
        var idRanges = new List<uint>();

        foreach (var row in _excelService.GetSheet<Completion>())
        {
            var lookupTable = _evaluator.Evaluate(row.LookupTable).ToString();

            if (string.IsNullOrEmpty(lookupTable))
                continue;

            cols.Clear();
            idRanges.Clear();

            if (lookupTable.Equals("@"))
            {
                _categories.Add(new CompletionCategoryEntry(_excelService, _evaluator)
                {
                    Row = row,
                    SheetName = "Completion",
                    IsNoun = false,
                    IdRanges = [.. _excelService.FindRows<Completion>(r => r.Group == row.Group).Select(r => r.RowId).Where(r => r != row.RowId)],
                    Columns = [3],
                });
                continue;
            }

            // CategoryDataCache
            if (lookupTable.Equals("#"))
            {
                // couldn't find any, so we don't handle them :p
                continue;
            }

            // All other sheets
            var rangesStart = lookupTable.IndexOf('[');
            // Sheet without ranges
            if (rangesStart == -1)
            {
                _categories.Add(new CompletionCategoryEntry(_excelService, _evaluator)
                {
                    Row = row,
                    SheetName = lookupTable,
                    IsNoun = false,
                    IdRanges = null,
                    Columns = [0],
                });
                continue;
            }

            var sheetName = lookupTable[..rangesStart];
            var ranges = lookupTable[(rangesStart + 1)..^1];
            if (ranges.Length == 0)
                continue;

            var hasRanges = false;
            var isNoun = false;

            while (!string.IsNullOrWhiteSpace(ranges))
            {
                // find the end of the current entry
                var entryEnd = ranges.IndexOf(',');
                if (entryEnd == -1)
                    entryEnd = ranges.Length;

                var entry = ranges.AsSpan(0, entryEnd);

                if (ranges.StartsWith("noun", StringComparison.Ordinal))
                {
                    isNoun = true;
                }
                else if (ranges.StartsWith("col", StringComparison.Ordinal))
                {
                    cols.Add(int.Parse(entry[4..]));
                }
                else if (ranges.StartsWith("tail", StringComparison.Ordinal))
                {
                    // currently not supported, since there are no known uses
                    // TODO: continue outer loop
                    goto nextRow;
                }
                else
                {
                    var dash = entry.IndexOf('-');

                    hasRanges |= true;

                    if (dash == -1)
                    {
                        idRanges.Add(uint.Parse(entry));
                    }
                    else
                    {
                        var start = int.Parse(entry[..dash]);
                        var end = int.Parse(entry[(dash + 1)..]);
                        idRanges.AddRange(Enumerable.Range(start, end - start).Select(i => (uint)i));
                    }
                }

                // if it's the end of the string, we're done
                if (entryEnd == ranges.Length)
                    break;

                // else, move to the next entry
                ranges = ranges[(entryEnd + 1)..].TrimStart();
            }

            _categories.Add(new CompletionCategoryEntry(_excelService, _evaluator)
            {
                Row = row,
                SheetName = sheetName,
                IsNoun = isNoun,
                IdRanges = hasRanges ? idRanges.ToList() : null,
                Columns = cols.ToList(),
            });
nextRow:;
        }
    }

    public override void Draw()
    {
        if (!_initialized)
        {
            Initialize();
            _initialized = true;
        }

        ImGui.SetNextItemWidth(-1);
        var hasSearchTermChanged = ImGui.InputTextWithHint("##TextSearch", _textService.Translate("SearchBar.Hint"), ref _textSearchTerm, 256, ImGuiInputTextFlags.AutoSelectAll);
        var hasSearchTerm = !string.IsNullOrWhiteSpace(_textSearchTerm);

        using var hostchild = ImRaii.Child("CompletionTabChild", new Vector2(-1), false, ImGuiWindowFlags.NoSavedSettings);
        if (!hostchild) return;

        foreach (var category in _categories)
        {
            var numEntries = category.Entries.Count;

            if (hasSearchTerm)
            {
                numEntries = category.Entries.Count(e => e.Texts.Values.Any(t => t.Contains(_textSearchTerm, StringComparison.OrdinalIgnoreCase)));
                if (numEntries == 0)
                    continue;
            }

            if (hasSearchTermChanged && hasSearchTerm)
                ImGui.SetNextItemOpen(true);
            using var node = ImRaii.TreeNode($"{category.Row.Text} ({numEntries})###Completion{category.Row.RowId}_{category.SheetName}", ImGuiTreeNodeFlags.SpanAvailWidth);
            if (!node)
                continue;

            ImGui.Text($"Sheet: {category.SheetName}");

            using var table = ImRaii.Table($"##{category.SheetName}CompletionTable", 1 + Enum.GetValues<ClientLanguage>().Length, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.NoSavedSettings);
            if (!table)
                continue;

            ImGui.TableSetupColumn("RowId", ImGuiTableColumnFlags.WidthFixed, 60);
            foreach (var language in Enum.GetValues<ClientLanguage>())
                ImGui.TableSetupColumn($"{language}", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var entries in category.Entries)
            {
                if (hasSearchTerm && !entries.Texts.Values.Any(t => t.Contains(_textSearchTerm, StringComparison.OrdinalIgnoreCase)))
                    continue;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(entries.RowId.ToString());

                foreach (var language in Enum.GetValues<ClientLanguage>())
                {
                    ImGui.TableNextColumn();

                    if (entries.Texts.TryGetValue(language, out var text))
                    {
                        if (hasSearchTerm && text.Contains(_textSearchTerm, StringComparison.OrdinalIgnoreCase))
                        {
                            var matchIndex = text.IndexOf(_textSearchTerm, StringComparison.OrdinalIgnoreCase);
                            var before = text[..matchIndex];
                            var match = text.Substring(matchIndex, _textSearchTerm.Length);
                            var after = text[(matchIndex + _textSearchTerm.Length)..];

                            ImGui.BeginGroup();
                            ImGui.Text(before);

                            ImGui.SameLine(0, 0);
                            using (Color.Green.Push(ImGuiCol.Text))
                                ImGui.TextUnformatted(match);

                            ImGui.SameLine(0, 0);
                            ImGui.TextUnformatted(after);
                            ImGui.EndGroup();
                            if (ImGui.IsItemHovered())
                                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                            if (ImGui.IsItemClicked())
                                ImGui.SetClipboardText(text);
                            continue;
                        }

                        ImGui.Text(text.ToString());
                        if (ImGui.IsItemHovered())
                            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        if (ImGui.IsItemClicked())
                            ImGui.SetClipboardText(text);
                    }
                }
            }
        }
    }

    public class CompletionCategoryEntry(ExcelService excelService, ISeStringEvaluator evaluator)
    {
#pragma warning disable IDE0032 // Use auto property
        private List<CompletionEntry>? _entries;
#pragma warning restore IDE0032 // Use auto property

        public required Completion Row { get; set; }
        public required string SheetName { get; set; }
        public required bool IsNoun { get; set; }
        public required List<uint>? IdRanges { get; set; }
        public required List<int> Columns { get; set; }
        public List<CompletionEntry> Entries => _entries ??= LoadEntries();

        private List<CompletionEntry> LoadEntries()
        {
            var list = new List<CompletionEntry>();

            foreach (var row in excelService.GetSheet<RawRow>(SheetName))
            {
                if (IdRanges != null && !IdRanges.Contains(row.RowId))
                    continue;

                var entry = new CompletionEntry() { RowId = row.RowId };

                foreach (var language in Enum.GetValues<ClientLanguage>())
                {
                    if (IsNoun && language == ClientLanguage.German && SheetName == "Companion")
                    {
                        entry.Texts[language] = evaluator.EvaluateObjStr(ObjectKind.Companion, row.RowId, ClientLanguage.German).ToString();
                        continue;
                    }

                    if (!excelService.TryGetRawRow(SheetName, row.RowId, language, out var entryRow))
                        continue;

                    if (Columns.Count == 0)
                    {
                        entry.Texts[language] = entryRow.ReadStringColumn(0).ToString();
                        continue;
                    }

                    foreach (var columnIndex in Columns)
                    {
                        var text = entryRow.ReadStringColumn(columnIndex);
                        if (text.IsEmpty)
                            continue;

                        entry.Texts[language] = text.ToString();
                        goto nextRow;
                    }
nextRow:;
                }

                if (entry.Texts.All(t => string.IsNullOrWhiteSpace(t.Value)))
                    continue;

                list.Add(entry);
            }

            return list;
        }
    }

    public class CompletionEntry
    {
        public uint RowId { get; set; }
        public Dictionary<ClientLanguage, string> Texts { get; } = [];
    }
}
