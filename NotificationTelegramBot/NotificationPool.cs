using System.Collections;
using Telegram.Bot;

namespace NotificationTelegramBot
{
    public class NotificationPool : IEnumerable<Notification>
    {
        public Notification currentNotification { get; set; }
        private Dictionary<string, Notification> notifications = new();
        private readonly object sourceLock = new();

        public async void Start(ITelegramBotClient botClient, int timeBefore)
        {
            notifications.Add(currentNotification.EventName, currentNotification);
            await Task.Run(() =>
            {
                var timeToAlarm = currentNotification.Date.Subtract(new TimeSpan(0, timeBefore, 0));
                var interval = timeToAlarm - DateTime.Now;
                var eventName = currentNotification.EventName;

                currentNotification.Timer = new()
                {
                    Interval = (double)interval.TotalMilliseconds,
                    AutoReset = false
                };

                currentNotification.Timer.Elapsed += async (object source, System.Timers.ElapsedEventArgs e) =>
                {
                    await botClient.SendTextMessageAsync(currentNotification.ChatId, 
                        $"Событие {eventName} начнется через {timeBefore} минут!");

                    lock (sourceLock)
                    {
                        notifications.Remove(eventName);
                    }
                };

                currentNotification.Timer.Enabled = true;
            });
        }

        public IEnumerator<Notification> GetEnumerator()
        {
            return notifications.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return notifications.GetEnumerator();
        }
    }
}
