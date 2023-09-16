using ServiceStack;
using TypeChatExamples.ServiceModel;

namespace TypeChatExamples.ServiceInterface;

public class CalendarServices : Service
{
    public async Task<object> Post(CreateCalendarChat request)
    {
        var chat = await Gateway.SendAsync(new CreateChat
        {
            Feature = Tags.Calendar, 
            UserMessage = request.UserMessage, 
        });
        var response = chat.ChatResponse.FromJson<CreateCalendarChatResponse>();
        return response;
    }
}