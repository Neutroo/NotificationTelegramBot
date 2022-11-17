using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using NotificationTelegramBot;

internal class Program
{
    static Dictionary<long, Notification> notifications = new();

    private static void Main(string[] args)
    {
        ITelegramBotClient bot = new TelegramBotClient("5656782812:AAGJiYaOBVYfdQXIuerz_cn4kWyjkOL9il8");

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
    }

    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
        if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
        {
            var message = update.Message;

            if (message?.Text != null)
            {
                if (message.Text.ToLower() == "/start")
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
                }
                else if (message.Text.ToLower() == "/create")
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id,
                        "Хорошо, давайте приступим. Как хотите назвать событие?");

                    notifications.Add(message.Chat.Id, new(message.Chat.Id)
                    {
                        Step = FillingStep.EventName
                    });
                }
                else if (notifications.ContainsKey(message.Chat.Id))
                {
                    if (notifications[message.Chat.Id].Step == FillingStep.EventName)
                    {
                        notifications[message.Chat.Id].EventName = message.Text;
                        notifications[message.Chat.Id].Step = FillingStep.Date;

                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            "В какой день и во сколько это будет?");
                    }
                    else if (notifications[message.Chat.Id].Step == FillingStep.Date)
                    {
                        if (DateTime.TryParse(message.Text, out DateTime date))
                        {
                            if (date > DateTime.Now)
                            {
                                notifications[message.Chat.Id].Date = date;

                                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                                {
                                    new[]
                                    {
                                        InlineKeyboardButton.WithCallbackData("5 минут", "5"),
                                        InlineKeyboardButton.WithCallbackData("15 минут", "15"),
                                    },
                                    new[]
                                    {
                                        InlineKeyboardButton.WithCallbackData("30 минут", "30"),
                                        InlineKeyboardButton.WithCallbackData("Час", "60"),
                                    }
                                });

                                notifications[message.Chat.Id].Step = FillingStep.TimeBefore;

                                await botClient.SendTextMessageAsync(message.Chat.Id,
                                    "Дата установлена. За сколько до события хотите получить уведомление?",
                                    replyMarkup: inlineKeyboard);
                            }
                            else
                                await botClient.SendTextMessageAsync(message.Chat.Id,
                                    "Дата события не может быть позднее текущего времени!");
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id,
                                "Неверный формат даты, попробуйте еще раз в виде: день-месяц-год часы:минуты.");
                        }
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            "Я неправильно вас понял, попробуйте сформулировать по другому.");
                    }
                }            

                return;
            }
        }
        else if (update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
        {
            var query = update.CallbackQuery;

            if (query?.Message != null)
            {
                if (query.Data == "Yes")
                {
                    await botClient.SendTextMessageAsync(query.Message.Chat.Id,
                        "Хорошо, давайте приступим. Как хотите назвать событие?");

                    notifications.Add(query.Message.Chat.Id, new(query.Message.Chat.Id)
                    {
                        Step = FillingStep.EventName
                    });
                }
                else if (notifications.ContainsKey(query.Message.Chat.Id) && notifications[query.Message.Chat.Id].Step == FillingStep.TimeBefore)
                {
                    notifications[query.Message.Chat.Id].Date = notifications[query.Message.Chat.Id].Date.Subtract(new TimeSpan(0, int.Parse(query.Data), 0));

                    await botClient.SendTextMessageAsync(query.Message.Chat.Id,
                        $"Отлично! Я напомню вам за {query.Data} минут до начала =)");
                    
                    notifications[query.Message.Chat.Id].Start(botClient);
                }

                return;
            }
        }
    }

    static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
    }
}