using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselDebug.Utils;

public unsafe record AtkValuesCopy : IDisposable
{
    public AtkValuesCopy(Span<AtkValue> values)
    {
        Time = DateTime.Now;
        Ptr = CopyAtkValues(values);
        ValueCount = values.Length;
    }

    public DateTime Time { get; }
    public Pointer<AtkValue> Ptr { get; private set; }
    public int ValueCount { get; }
    public Span<AtkValue> Values => Ptr.Value != null ? new(Ptr, ValueCount) : [];

    public string AdditionalText { get; set; } = string.Empty;

    public void Dispose()
    {
        FreeAtkValues(Values);
        Ptr = null;
    }

    private AtkValue* CopyAtkValues(Span<AtkValue> values)
    {
        var ptr = (AtkValue*)IMemorySpace.GetDefaultSpace()->Malloc((ulong)sizeof(AtkValue) * (ulong)values.Length, 0x8);
        var valuesCopy = new Span<AtkValue>(ptr, values.Length);
        var valueCountCopy = 0;

        for (var i = 0; i < values.Length; i++)
        {
            var value = values.GetPointer(i);
            var valueCopy = valuesCopy.GetPointer(i);

            if (value->Type == ValueType.Int && i < values.Length - 1 && values.GetPointer(i + 1)->Type == ValueType.AtkValues)
                valueCountCopy = value->Int;
            else if (value->Type != ValueType.AtkValues)
                valueCountCopy = 0;

            valueCopy->Ctor();

            if (value->Type == ValueType.String)
            {
                var str = new ReadOnlySeStringSpan(value->String.Value);
                var strPtr = (byte*)IMemorySpace.GetDefaultSpace()->Malloc((ulong)str.ByteLength + 1, 0x8);
                Marshal.Copy(str.Data.ToArray(), 0, (nint)strPtr, str.ByteLength);
                strPtr[str.ByteLength] = 0;
                valueCopy->SetString(strPtr);
            }
            else if (value->Type == ValueType.AtkValues && valueCountCopy > 0)
            {
                valueCopy->ChangeType(ValueType.AtkValues);
                valueCopy->AtkValues = CopyAtkValues(new Span<AtkValue>(value->AtkValues, valueCountCopy));
            }
            else
            {
                valueCopy->Copy(value);
            }
        }

        return ptr;
    }

    private void FreeAtkValues(Span<AtkValue> values)
    {
        if (values.Length == 0)
            return;

        var valueCountCopy = 0;

        for (var i = 0; i < values.Length; i++)
        {
            var value = values.GetPointer(i);

            if (value->Type == ValueType.Int && i < values.Length - 1 && values[i + 1].Type == ValueType.AtkValues)
                valueCountCopy = value->Int;
            else if (value->Type != ValueType.AtkValues)
                valueCountCopy = 0;

            if (value->Type == ValueType.String)
            {
                IMemorySpace.Free(value->String, (ulong)new ReadOnlySeStringSpan(value->String.Value).ByteLength + 1);
                value->ChangeType(ValueType.Undefined);
                value->String = null;
            }
            else if (value->Type == ValueType.AtkValues && valueCountCopy > 0)
            {
                FreeAtkValues(new Span<AtkValue>(value->AtkValues, valueCountCopy));
            }

            value->Dtor();
        }

        IMemorySpace.Free(values.GetPointer(0), (ulong)sizeof(AtkValue) * (ulong)values.Length);
    }
}
