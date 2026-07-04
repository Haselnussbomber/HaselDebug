namespace HaselDebug.Utils.SqPack;

public class CompositeIndexInfo
{
    public struct CombinedIndexEntry
    {
        public uint FolderHash;
        public uint FileHash;
        public uint FullHash;
        public string? Path;
    }

    public struct BasicIndex1Entry
    {
        public uint FolderHash;
        public uint FileHash;
    }

    public struct BasicIndex2Entry
    {
        public uint FullHash;
    }

    public uint IndexId => Index1?.IndexId ?? Index2!.IndexId;
    public IndexFile? Index1 { get; set; }
    public IndexFile? Index2 { get; set; }

    public Dictionary<ulong, CombinedIndexEntry> CombinedIndexEntries { get; set; }
    public Dictionary<ulong, BasicIndex1Entry> Index1Entries { get; set; }
    public Dictionary<ulong, BasicIndex2Entry> Index2Entries { get; set; }

    public CompositeIndexInfo(IndexFile? index1, IndexFile? index2)
    {
        Index1 = index1;
        Index2 = index2;

        CombinedIndexEntries = new Dictionary<ulong, CombinedIndexEntry>();
        Index1Entries = new Dictionary<ulong, BasicIndex1Entry>();
        Index2Entries = new Dictionary<ulong, BasicIndex2Entry>();

        Process();
    }

    private void Process()
    {
        if (Index1 is { IsIndex2: true })
            throw new ArgumentException("Index1 is not an Index1 file");

        if (Index2 is { IsIndex2: false })
            throw new ArgumentException("Index2 is not an Index2 file");

        var index1OffsetDict = new Dictionary<ulong, BasicIndex1Entry>();
        var index2OffsetDict = new Dictionary<ulong, BasicIndex2Entry>();

        if (Index1 != null)
        {
            // load index1 collisions
            if (Index1.Collisions64 != null)
            {
                foreach (var collision in Index1.Collisions64)
                {
                    CombinedIndexEntries.TryAdd(collision.FileIdentifier, new CombinedIndexEntry
                    {
                        FolderHash = collision.FolderHash,
                        FileHash = collision.FileHash,
                        FullHash = Lumina.Misc.Crc32.Get(collision.Path),
                        Path = collision.Path
                    });
                }
            }

            // load index1 hash entries
            if (Index1.Hashes64 != null)
            {
                foreach (var entry in Index1.Hashes64.Values)
                {
                    if (entry.IsSynonym) continue;
                    if (CombinedIndexEntries.ContainsKey(entry.FileIdentifier)) continue;

                    index1OffsetDict.TryAdd(entry.FileIdentifier, new BasicIndex1Entry
                    {
                        FolderHash = entry.FolderHash,
                        FileHash = entry.FileHash,
                    });
                }
            }
        }

        if (Index2 != null)
        {
            // load index2 collisions
            if (Index2.Collisions32 != null)
            {
                foreach (var collision in Index2.Collisions32)
                {
                    var (folderHash, fileHash) = HashUtils.GetHash(collision.Path);
                    CombinedIndexEntries.TryAdd(collision.FileIdentifier, new CombinedIndexEntry
                    {
                        FolderHash = folderHash,
                        FileHash = fileHash,
                        FullHash = collision.Hash,
                        Path = collision.Path,
                    });
                }
            }

            // load index2 hash entries
            if (Index2.Hashes32 != null)
            {
                foreach (var entry in Index2.Hashes32.Values)
                {
                    if (entry.IsSynonym) continue;
                    if (CombinedIndexEntries.Any(x => x.Value.FullHash == entry.Hash)) continue;

                    index2OffsetDict.TryAdd(entry.FileIdentifier, new BasicIndex2Entry
                    {
                        FullHash = entry.Hash,
                    });
                }
            }
        }

        // only an index1
        if (Index1 != null && Index2 == null)
        {
            if (Index1.Hashes64 != null)
            {
                foreach (var hashes in Index1.Hashes64.Values)
                {
                    var entry = new BasicIndex1Entry
                    {
                        FolderHash = hashes.FolderHash,
                        FileHash = hashes.FileHash,
                    };
                    Index1Entries.TryAdd(hashes.FileIdentifier, entry);
                }
            }
        } // only an index2
        else if (Index1 == null && Index2 != null)
        {
            if (Index2.Hashes32 != null)
            {
                foreach (var hashes in Index2.Hashes32.Values)
                {
                    var entry = new BasicIndex2Entry
                    {
                        FullHash = hashes.Hash,
                    };
                    Index2Entries.TryAdd(hashes.FileIdentifier, entry);
                }
            }
        }
        else // both index1 and 2
        {
            var index1Tmp = index1OffsetDict.ToArray();

            // combine index1 and index2, removing handled entries
            foreach (var element in index1Tmp)
            {
                if (!index2OffsetDict.TryGetValue(element.Key, out var index2Entries)) continue;

                index1OffsetDict.Remove(element.Key);
                index2OffsetDict.Remove(element.Key);

                CombinedIndexEntries.TryAdd(element.Key, new CombinedIndexEntry
                {
                    FolderHash = element.Value.FolderHash,
                    FileHash = element.Value.FileHash,
                    FullHash = index2Entries.FullHash,
                    Path = null,
                });
            }

            // add uncombined entries to their respective dictionaries
            foreach (var basicIndex1Entry in index1OffsetDict)
            {
                Index1Entries.TryAdd(basicIndex1Entry.Key, basicIndex1Entry.Value);
            }

            foreach (var basicIndex2Entry in index2OffsetDict)
            {
                Index2Entries.TryAdd(basicIndex2Entry.Key, basicIndex2Entry.Value);
            }
        }
    }
}
