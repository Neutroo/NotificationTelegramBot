using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types.ReplyMarkups;
using NotificationTelegramBot;
using Quartz.Xml;
using Quartz;

internal class Program
{
    static Dictionary<long, NotificationPool> notifications = new();

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
                else if (message.Text.ToLower() == "/new_event")
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id,
                        "Хорошо, давайте приступим. Как хотите назвать событие?");

                    if (notifications.ContainsKey(message.Chat.Id)) 
                    {
                        notifications[message.Chat.Id].currentNotification = new(message.Chat.Id)
                        {
                            Step = FillingStep.EventName
                        };
                    }
                    else
                    {
                        notifications.Add(message.Chat.Id, new() 
                        { 
                            currentNotification= new(message.Chat.Id) 
                            { 
                                Step = FillingStep.EventName 
                            } 
                        });
                    }
                }
                else if (message.Text.ToLower() == "/events")
                {
                    string events = string.Empty;
                    foreach (var notification in notifications[message.Chat.Id])
                    {
                        events += $"{notification.EventName} - {notification.Date}\n";
                    }

                    await botClient.SendTextMessageAsync(message.Chat.Id,
                        events);
                }
                else if (notifications.ContainsKey(message.Chat.Id))
                {
                    switch (notifications[message.Chat.Id].currentNotification.Step)
                    {
                        case FillingStep.EventName:
                            notifications[message.Chat.Id].currentNotification.EventName = message.Text;
                            notifications[message.Chat.Id].currentNotification.Step = FillingStep.Date;

                            await botClient.SendTextMessageAsync(message.Chat.Id,
                                "В какой день и во сколько это будет?");
                            break;

                        case FillingStep.Date:
                            if (DateTime.TryParse(message.Text, out DateTime date))
                            {
                                if (date > DateTime.Now)
                                {
                                    notifications[message.Chat.Id].currentNotification.Date = date;

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

                                    notifications[message.Chat.Id].currentNotification.Step = FillingStep.TimeBefore;

                                    await botClient.SendTextMessageAsync(message.Chat.Id,
                                        "Дата установлена. За сколько до события хотите получить уведомление?",
                                        replyMarkup: inlineKeyboard);
                                }
                                else
                                    await botClient.SendTextMessageAsync(message.Chat.Id,
                                        "Дата события не может быть позднее текущего времени!");
                            }
                            break;

                        default:
                            await botClient.SendTextMessageAsync(message.Chat.Id,
                                "Неверный формат даты, попробуйте еще раз в виде: день-месяц-год часы:минуты.");
                            break;
                    }  
                }
                else
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id,
                        "Я неправильно вас понял, попробуйте сформулировать по другому.");
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

                    if (notifications.ContainsKey(query.Message.Chat.Id))
                    {
                        notifications[query.Message.Chat.Id].currentNotification = new(query.Message.Chat.Id)
                        {
                            Step = FillingStep.EventName
                        };
                    }
                    else
                    {
                        notifications.Add(query.Message.Chat.Id, new()
                        {
                            currentNotification = new(query.Message.Chat.Id)
                            {
                                Step = FillingStep.EventName
                            }
                        });
                    }
                }
                else if (query.Data == "No")
                {
                    await botClient.SendTextMessageAsync(query.Message.Chat.Id,
                        "Хорошо, когда нужно будет создать уведомление, напишите /new_event.");
                }
                else if (notifications.ContainsKey(query.Message.Chat.Id) && 
                    notifications[query.Message.Chat.Id].currentNotification.Step == FillingStep.TimeBefore)
                {
                    var timeBefore = notifications[query.Message.Chat.Id].currentNotification.Date.Subtract(new TimeSpan(0, int.Parse(query.Data), 0));

                    if (timeBefore > DateTime.Now)
                    {
                        notifications[query.Message.Chat.Id].currentNotification.Date = timeBefore;

                        await botClient.SendTextMessageAsync(query.Message.Chat.Id, 
                            $"Отлично! Я напомню вам за {query.Data} минут до начала =)");

                        notifications[query.Message.Chat.Id].Start(botClient);
                    }
                    else
                    {
                        notifications[query.Message.Chat.Id].currentNotification.Step = FillingStep.Date;

                        await botClient.SendTextMessageAsync(query.Message.Chat.Id,
                                        "Время напоминания о событии не может быть меньше текущего времени! " +
                                        "В какой день и во сколько хотите получить уведомление?");              
                    }           
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