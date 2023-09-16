using ServiceStack;

namespace TypeChatExamples.ServiceModel;

using System.Collections.Generic;

public class CreateCalendarChat : IReturn<CreateCalendarChatResponse>
{
    public string UserMessage { get; set; }
}

public class CreateCalendarChatResponse
{
    public List<CalendarAction> Actions { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

public class CalendarAction
{
    public string ActionType { get; set; }
    public CalendarEvent Event { get; set; }
    public EventReference EventReference { get; set; }
    public List<string> Participants { get; set; }
    public EventTimeRange TimeRange { get; set; }
    public string Description { get; set; }
    public string Text { get; set; }
}

public class EventTimeRange
{
    public string StartTime { get; set; }
    public string EndTime { get; set; }
    public string Duration { get; set; }
}

public class CalendarEvent
{
    public string Day { get; set; }
    public EventTimeRange TimeRange { get; set; }
    public string Description { get; set; }
    public string Location { get; set; }
    public List<string> Participants { get; set; }
}

public class EventReference
{
    public string Day { get; set; }
    public string DayRange { get; set; }
    public EventTimeRange TimeRange { get; set; }
    public string Description { get; set; }
    public string Location { get; set; }
    public List<string> Participants { get; set; }
}
