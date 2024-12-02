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
    private Dictionary<string, MemberDocumentation> MemberDocs = [];

    public bool HasDocumentation(string? name)
        => !string.IsNullOrEmpty(name) && MemberDocs.ContainsKey(name);

    public MemberDocumentation? GetDocumentation(string? name)
        => !string.IsNullOrEmpty(name) && MemberDocs.TryGetValue(name, out var doc) ? doc : null;

    public record MemberDocumentation(string Name, string Sumamry, string Remarks, KeyValuePair<string, string>[] Parameters, string Returns);

    internal void ParseCSDocs()
    {
        if (MemberDocs.Count != 0)
            return;

        var csXmlPath = Path.Join(PluginInterface.AssemblyLocation.Directory!.FullName, "FFXIVClientStructs.xml");
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
                    ProcessMemberText(inheritedMember);
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
                var summariesText = ListToText(summaries);
                var remarksText = ListToText(remarks);
                var parametersArray = parameters.ToArray();
                var returnsText = ListToText(returns);

                if (string.IsNullOrEmpty(summariesText) && string.IsNullOrEmpty(remarksText) && parametersArray.Length == 0 && string.IsNullOrEmpty(returnsText))
                    continue;

                MemberDocs.Add(name, new MemberDocumentation(
                    name,
                    ListToText(summaries),
                    ListToText(remarks),
                    [.. parameters],
                    ListToText(returns)));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not parse FFXIVClientStructs.xml");
        }

        static string ListToText(List<string> list)
        {
            var sb = new StringBuilder();
            var firstLineIndentation = -1;

            foreach (var line in list)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    var cutLine = line;

                    var indentation = line.TakeWhile(char.IsWhiteSpace).Count();

                    if (firstLineIndentation == -1)
                        firstLineIndentation = indentation;

                    if (firstLineIndentation <= indentation)
                        cutLine = cutLine[firstLineIndentation..];

                    cutLine = RegexXmlNewLine().Replace(cutLine, "\n");
                    cutLine = RegexXmlSeeAlsoWithText().Replace(cutLine, "$1");
                    cutLine = RegexXmlSeeAlso().Replace(cutLine, "$1");
                    cutLine = RegexXmlCode().Replace(cutLine, "$1");

                    sb.AppendLine(cutLine.TrimEnd());
                }
            }

            return sb.ToString().TrimEnd();
        }
    }

    [GeneratedRegex("<br\\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex RegexXmlNewLine();

    [GeneratedRegex("<see\\s*cref=\"\\w:([^\"]*)\"\\s*/>", RegexOptions.IgnoreCase)]
    private static partial Regex RegexXmlSeeAlso();

    [GeneratedRegex("<see\\s*cref=\"\\w:[^\"]*\"\\s*>(.*)</see>", RegexOptions.IgnoreCase)]
    private static partial Regex RegexXmlSeeAlsoWithText();

    [GeneratedRegex("<c>(.*)</c>", RegexOptions.IgnoreCase)]
    private static partial Regex RegexXmlCode();

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
