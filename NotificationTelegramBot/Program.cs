using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using NotificationTelegramBot;

ITelegramBotClient bot = new TelegramBotClient("5656782812:AAGJiYaOBVYfdQXIuerz_cn4kWyjkOL9il8");
List<Notification> notifications= new List<Notification>();

Console.WriteLine("Бот запущен " + bot.GetMeAsync().Result.FirstName);

var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;
var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = { }
};
bot.StartReceiving(
    HandleUpdateAsync,
    HandleErrorAsync,
    receiverOptions,
    cancellationToken
);
Console.ReadLine();

static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
    if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
    {
        var message = update.Message;

        if (message?.Text?.ToLower() == "/start")
        {
            await botClient.SendTextMessageAsync(message.Chat, 
                "Привет! Я буду оповещать вашу команду о важных событиях =)");

            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Да", "Yes"),
                    InlineKeyboardButton.WithCallbackData("Нет", "No"),
                }
            });
            await botClient.SendTextMessageAsync(message.Chat.Id, 
                "Хотите создать напоминание прямо сейчас?",
                replyMarkup: inlineKeyboard);

            return;
        }
        else if (message.Text.ToLower() == "/create")
        {
            await botClient.SendTextMessageAsync(message.Chat,
                "Хорошо, давайте приступим.\n" +
                "Напиши название и дату события в формате:\n" +
                "!newevent\n" +
                "Название\n" +
                "Дата");

            return;
        }
        else if (message.Text.ToLower().Contains("!newevent"))
        {
            string[] data = message.Text.Split('\n');

            DateTime date;
            DateTime.TryParse(data[2], out date);

            Notification notification = new()
            {
                ChatId = message.Chat.Id,
                Event = data[1],
                Date = date
            };

            notification.Start();

            await botClient.SendTextMessageAsync(message.Chat, "Напоминание создано!");
        }    
        else
            await botClient.SendTextMessageAsync(message.Chat, 
                "Я неправильно вас понял, попробуйте сформулировать по другому.");
    }
    else if(update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
    {
        var message = update.Message;
        var query = update.CallbackQuery;
        if (query?.Data == "Yes")
        {
            await botClient.SendTextMessageAsync(query.Message.Chat,
                "Хорошо, давайте приступим.\n" +
                "Напиши название и дату события в формате:\n" +
                "!newevent\n" +
                "Название\n" +
                "Дата");
            return;
        }
    }
}

static async Task CreateNotificationAsync(ITelegramBotClient botClient, Update update)
{
    var inlineKeyboard = new InlineKeyboardMarkup(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Да", "Yes"),
            InlineKeyboardButton.WithCallbackData("Нет", "No"),
        }
    });
    await botClient.SendTextMessageAsync(update.Message.Chat.Id, 
        "Хотите создать напоминание прямо сейчас?", 
        replyMarkup: inlineKeyboard);

}

static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{   
    Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
}

static void SetNot(string eventName, Notification notification)
{
    notification.Event = eventName;
}

static void SetDate(DateTime dateTime, Notification notification)
{
    notification.Date = dateTime;
}