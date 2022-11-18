using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace NotificationTelegramBot
{
    public class NotificationPool : IEnumerable<Notification>
    {
        public Notification currentNotification { get; set; }
        private Dictionary<string, Notification> notifications = new();
        private readonly object sourceLock = new();

        public async void Start(ITelegramBotClient botClient)
        {
            notifications.Add(currentNotification.EventName, currentNotification);
            await Task.Run(() =>
            {
                var timeToAlarm = currentNotification.Date;
                var interval = timeToAlarm - DateTime.Now;

                currentNotification.Timer = new()
                {
                    Interval = (double)interval.TotalMilliseconds,
                    AutoReset = false
                };

                currentNotification.Timer.Elapsed += async (object source, System.Timers.ElapsedEventArgs e) =>
                {
                    await botClient.SendTextMessageAsync(currentNotification.ChatId, 
                        $"Событие {currentNotification.EventName} начнется через {interval.TotalMinutes} минут!");

                    lock (sourceLock)
                    {
                        notifications.Remove(currentNotification.EventName);
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
