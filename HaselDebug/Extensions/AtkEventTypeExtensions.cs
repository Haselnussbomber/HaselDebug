using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselDebug.Extensions;

public static class AtkEventTypeExtensions
{
    extension(AtkEventType eventType)
    {
        public Type GetAtkEventDataType()
        {
            var type = typeof(AtkEventData);

            if ((int)eventType is >= (int)AtkEventType.MouseDown and <= (int)AtkEventType.MouseDoubleClick)
            {
                type = typeof(AtkEventData.AtkMouseData);
            }
            else if ((int)eventType is >= (int)AtkEventType.InputReceived and <= (int)AtkEventType.InputNavigation)
            {
                type = typeof(AtkEventData.AtkInputData);
            }
            else if ((int)eventType is >= (int)AtkEventType.ListItemRollOver and <= (int)AtkEventType.ListItemSelect)
            {
                type = typeof(AtkEventData.AtkListItemData);
            }
            else if ((int)eventType is >= (int)AtkEventType.DragDropBegin and <= (int)AtkEventType.DragDropClick)
            {
                type = typeof(AtkEventData.AtkDragDropData);
            }
            else if (eventType == AtkEventType.ChildAddonAttached)
            {
                type = typeof(AtkEventData.AtkAddonControlData);
            }
            else if (eventType == AtkEventType.ValueUpdate)
            {
                type = typeof(AtkEventData.AtkValueData);
            }
            else if (eventType == AtkEventType.TimelineActiveLabelChanged)
            {
                type = typeof(AtkEventData.AtkTimelineData);
            }
            else if ((int)eventType is >= (int)AtkEventType.LinkMouseClick and <= (int)AtkEventType.LinkMouseOut)
            {
                type = typeof(LinkData);
            }

            return type;
        }
    }
}
