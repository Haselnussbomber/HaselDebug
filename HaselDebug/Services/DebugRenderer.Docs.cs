using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;

namespace HaselDebug.Services;

public partial class DebugRenderer
{
    private Dictionary<string, MemberDocumentation> _memberDocs = [];

    public bool HasDocumentation(string? name)
        => !string.IsNullOrEmpty(name) && _memberDocs.ContainsKey(name);

    public MemberDocumentation? GetDocumentation(string? name)
        => !string.IsNullOrEmpty(name) && _memberDocs.TryGetValue(name, out var doc) ? doc : null;

    public record MemberDocumentation(string Name, string Sumamry, string Remarks, KeyValuePair<string, string>[] Parameters, string Returns);

    internal void ParseCSDocs()
    {
        if (_memberDocs.Count != 0)
            return;

        var csXmlPath = Path.Join(_pluginInterface.AssemblyLocation.Directory!.FullName, "FFXIVClientStructs.xml");
        if (!File.Exists(csXmlPath))
            return;

        try
        {
            var serializer = new XmlSerializer(typeof(XmlDoc));
            using var reader = new FileStream(csXmlPath, FileMode.Open);
            var doc = (XmlDoc?)serializer.Deserialize(reader);
            if (doc == null || doc.Members == null)
                return;

            var tempDict = doc.Members.ToDictionary(member => member.Name);

            var summaries = new List<string>();
            var remarks = new List<string>();
            var parameters = new Dictionary<string, string>();
            var returns = new List<string>();
            var processedMembers = new List<string>();

            void ProcessMemberText(XmlDocMember member)
            {
                if (member.Summary != null && !string.IsNullOrEmpty(member.Summary.Value) && !member.Summary.Value.StartsWith("Inherited parent class"))
                    summaries.AddRange(member.Summary.Value.Split('\n'));

                if (member.Remarks != null && !string.IsNullOrEmpty(member.Remarks.Value) && !member.Remarks.Value.StartsWith("Field inherited"))
                    remarks.AddRange(member.Remarks.Value.Split('\n'));

                if (member.Returns != null && !string.IsNullOrEmpty(member.Returns.Value))
                    returns.AddRange(member.Returns.Value.Split('\n'));

                if (member.Parameters != null)
                {
                    foreach (var parameter in member.Parameters)
                    {
                        if (!string.IsNullOrEmpty(parameter.Text))
                            parameters.Add(parameter.Name, parameter.Text.Trim());
                    }
                }
            }

            void ProcessMember(XmlDocMember member)
            {
                if (member.Inheritdoc != null &&
                    !string.IsNullOrEmpty(member.Inheritdoc.CRef) &&
                    !processedMembers.Contains(member.Inheritdoc.CRef) &&
                    tempDict.TryGetValue(member.Inheritdoc.CRef, out var inheritedMember))
                {
                    processedMembers.Add(member.Inheritdoc.CRef);
                    ProcessMember(inheritedMember);
                }

                ProcessMemberText(member);
            }

            foreach (var member in doc.Members)
            {
                if (member.Name[0] != 'F') // fields are enough
                    continue;

                summaries.Clear();
                remarks.Clear();
                parameters.Clear();
                returns.Clear();
                processedMembers.Clear();

                ProcessMember(member);

                var name = member.Name[2..];
                var lastDotIndex = name.LastIndexOf('.');
                var parentName = lastDotIndex > -1 ? name.Substring(0, lastDotIndex) : name;

                var summariesText = ListToText(parentName, summaries);
                var remarksText = ListToText(parentName, remarks);
                var parametersArray = parameters.ToArray();
                var returnsText = ListToText(parentName, returns);

                if (string.IsNullOrEmpty(summariesText) && string.IsNullOrEmpty(remarksText) && parametersArray.Length == 0 && string.IsNullOrEmpty(returnsText))
                    continue;

                _memberDocs.Add(name, new MemberDocumentation(
                    name,
                    ListToText(parentName, summaries),
                    ListToText(parentName, remarks),
                    [.. parameters],
                    ListToText(parentName, returns)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not parse FFXIVClientStructs.xml");
        }
    }

    private static string GetLongestCommonPrefix(string ns1, string ns2)
    {
        // Split both namespaces into parts by '.'
        var parts1 = ns1.Split('.');
        var parts2 = ns2.Split('.');

        // Compare parts to find the common prefix
        var commonParts = parts1.Zip(parts2, (part1, part2) => part1 == part2 ? part1 : null)
                                .TakeWhile(part => part != null);

        // Join the common parts back into a namespace string
        return string.Join(".", commonParts);
    }

    private static string RemovePrefix(string value, string parentName)
    {
        return value.Replace(GetLongestCommonPrefix(parentName, value) + ".", string.Empty);
    }

    private static string ListToText(string parentName, List<string> list)
    {
        var sb = new StringBuilder();
        var firstLineIndentation = -1;

        foreach (var line in list)
        {
            if (string.IsNullOrEmpty(line))
                continue;

            var cutLine = line;

            var indentation = line.TakeWhile(char.IsWhiteSpace).Count();

            if (firstLineIndentation == -1)
                firstLineIndentation = indentation;

            if (firstLineIndentation <= indentation)
                cutLine = cutLine[firstLineIndentation..];

            cutLine = RegexXmlNewLine().Replace(cutLine, "\n");
            cutLine = RegexXmlSeeAlsoWithText().Replace(cutLine, match => RemovePrefix(match.Groups[1].Value, parentName));
            cutLine = RegexXmlSeeAlso().Replace(cutLine, match => match.Groups[1].Value.Replace(GetLongestCommonPrefix(parentName, match.Groups[1].Value) + ".", string.Empty));
            cutLine = RegexXmlCode().Replace(cutLine, "$1");
            cutLine = RegexXmlList().Replace(cutLine, match =>
            {
                if (match.Groups[1].Value == "table")
                    return match.Value; // not supported

                var index = 1;
                return RegexXmlItem().Replace(match.Groups[2].Value, lineMatch =>
                {
                    var prefix = match.Groups[1].Value switch
                    {
                        "bullet" => "\u2022 ",
                        "number" => $"{index++}) ", // idk actually
                        _ => "- "
                    };
                    return prefix + lineMatch.Groups[1].Value + "\n";
                });
            });

            sb.AppendLine(cutLine.TrimEnd());
        }

        return sb.ToString().TrimEnd();
    }

    [GeneratedRegex("<br\\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex RegexXmlNewLine();

    [GeneratedRegex("<see\\s*cref=\"\\w:([^\"]*)\"\\s*/>", RegexOptions.IgnoreCase)]
    private static partial Regex RegexXmlSeeAlso();

    [GeneratedRegex("<see\\s*cref=\"\\w:[^\"]*\"\\s*>(.*)</see>", RegexOptions.IgnoreCase)]
    private static partial Regex RegexXmlSeeAlsoWithText();

    [GeneratedRegex("<c>(.*?)</c>", RegexOptions.IgnoreCase)]
    private static partial Regex RegexXmlCode();

    [GeneratedRegex("<list(?: type=\"(bullet|number)\")?>(.*?)</list>", RegexOptions.IgnoreCase)]
    private static partial Regex RegexXmlList();

    [GeneratedRegex("<item>(.*?)</item>", RegexOptions.IgnoreCase)]
    private static partial Regex RegexXmlItem();

    [XmlRoot("doc")]
    public record XmlDoc
    {
        [XmlArray("members")]
        public XmlDocMember[]? Members;
    }

    [XmlType("member")]
    public record XmlDocMember
    {
        [XmlAttribute("name")]
        public required string Name;

        [XmlElement("inheritdoc", IsNullable = true)]
        public XmlDocInheritdoc? Inheritdoc;

        [XmlElement("summary", IsNullable = true)]
        public RawString? Summary;

        [XmlElement("remarks", IsNullable = true)]
        public RawString? Remarks;

        [XmlArray("param", IsNullable = true)]
        public XmlDocParam[]? Parameters;

        [XmlElement("returns", IsNullable = true)]
        public RawString? Returns;
    }

    public record XmlDocInheritdoc
    {
        [XmlAttribute("cref")]
        public required string CRef;
    }

    public record XmlDocParam
    {
        [XmlAttribute("name")]
        public required string Name;

        [XmlText]
        public string? Text;
    }

    // https://github.com/dotnet/runtime/issues/102737#issuecomment-2134464479
    public record RawString : IXmlSerializable
    {
        public required string Value { get; set; }
        public XmlSchema? GetSchema() => null;
        public void ReadXml(XmlReader reader) => Value = reader.ReadInnerXml();
        public void WriteXml(XmlWriter writer) => writer.WriteRaw(Value);
        public override string ToString() => Value;
    }
}
