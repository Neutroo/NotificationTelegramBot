using Telegram.Bot;

namespace NotificationTelegramBot
{
    public enum FillingStep
    {
        None,
        EventName,
        Date,
        TimeBefore
    }

    public class Notification
    {
        public long ChatId { get; private set; }
        public string EventName { get; set; }
        public DateTime Date { get; set; }
        public FillingStep Step{ get; set; }
        public System.Timers.Timer Timer { get; set; }

        public Notification(long chatId)
        {           
            ChatId = chatId;
            Step = FillingStep.None;
        }    
    }
}