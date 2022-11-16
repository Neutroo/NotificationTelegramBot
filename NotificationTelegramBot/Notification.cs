using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace NotificationTelegramBot
{
    class Notification
    {
        public long ChatId { get; set; }
        public string Event { get; set; }
        public DateTime Date { get; set; }

        public async void Start()
        {

        }
    }
}