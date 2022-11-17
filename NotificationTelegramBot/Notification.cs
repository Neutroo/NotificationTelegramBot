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

    class Notification
    {
        public long ChatId { get; private set; }
        public string EventName { get; set; }
        public DateTime Date { get; set; }
        public FillingStep Step{ get; set; }
        public System.Timers.Timer Timer { get; set; } = new();

        public Notification(long chatId)
        {           
            ChatId = chatId;
            Step = FillingStep.EventName;
        }

        public async void Start(ITelegramBotClient botClient)
        {
            await Task.Run(() =>
            {
                var timeToAlarm = Date;
                var interval = timeToAlarm - DateTime.Now;
                Timer.Interval = (double)interval.TotalMilliseconds;
                Timer.AutoReset = false;

                Timer.Elapsed += async (object source, System.Timers.ElapsedEventArgs e) =>
                {
                    await botClient.SendTextMessageAsync(ChatId, $"Событие {EventName} начнется через {interval.Minutes} минут!");
                };

                Timer.Enabled = true;
            });          
        }
    }
}