using Mensuracoles.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Mensuracoles
{
    public enum BotCommand
    {
        None,
        Show,
        Add,
        Delete
    }
    public class ColesHandler
    {
        private ITelegramBotClient _botClient;
        private FileRepository _repository;
        private List<string> _botCommandsAdd = new List<string>()
            {
                "@mensuracolesbot",
                "счетобот",
                "countbot",
                "count",
                "add"
            };
        private List<string> _botCommandsShow = new List<string>()
            {
                "@mensuracolesbot show",
                "счетобот статы",
                "счетобот покажи",
                "countbot show",
                "count show",
                "show"
            };
        private List<string> _botCommandsDelete = new List<string>()
            {
                "@mensuracolesbot delete",
                "счетобот удали",
                "countbot delete",
                "count delete",
                "delete"
            };
        public ColesHandler(string token, FileRepository repository)
        {
            _repository = repository;
            _botClient = new TelegramBotClient(token);
            Console.WriteLine($"bot will run until {TimeSpan.MaxValue.TotalDays.ToString()}");

            _botClient.OnMessage += Bot_OnMessage;
            _botClient.OnCallbackQuery += BotClient_OnCallbackQueryAsync;
            _botClient.StartReceiving();

            while (true)
            {
                Console.WriteLine($"bot working");

                Thread.Sleep(100000);
            }
        }

        private BotCommand ShouldReactToCommand(string query)
        {
            query = query.ToLowerInvariant();

            if (_botCommandsShow.Any(x => query.StartsWith(x)))
            {
                return BotCommand.Show;
            }
            else if (_botCommandsDelete.Any(x => query.StartsWith(x)))
            {
                return BotCommand.Delete;
            }
            else if (_botCommandsAdd.Any(x => query.StartsWith(x)))
            {
                return BotCommand.Add;
            }
            else
            {
                return BotCommand.None;

            }
        }
        private string SanitizeQueryFromBotTrigger(string rawQuery, List<string> queryPrefix)
        {
            string query = rawQuery.ToLowerInvariant();
            query = query.Trim();

            foreach (var command in queryPrefix)
            {
                if (query.StartsWith(command))
                {
                    query = query.Substring(command.Length);
                }
            }

            return query.Trim();
        }

        private ParsedQuery ParseQuery(string query)
        {
            var ret = new ParsedQuery();

            try
            {
                if (!String.IsNullOrWhiteSpace(query))
                {

                    var splittedQuery = query.Split(" ");
                    if (!splittedQuery.Any())
                    {
                        return ret;
                    }
                    var splittedSanitized = splittedQuery.Where(x => !String.IsNullOrEmpty(x));
                    if (!splittedSanitized.Any())
                    {
                        return ret;
                    }
                    if (splittedSanitized.Count() == 1)
                    {
                        ret.Bin = splittedSanitized.FirstOrDefault();
                    }

                    if (splittedSanitized.Count() >= 2)
                    {
                        ret.Bin = String.Join(" ", splittedSanitized.Take(splittedSanitized.Count() - 1));
                    }

                    var lastValue = splittedSanitized.LastOrDefault();
                    lastValue = lastValue.Replace(",", ".");
                    decimal value = 0;

                    if (decimal.TryParse(lastValue, out value))
                    {
                        ret.Value = value;
                    }
                    else
                    {
                        ret.Bin = query;
                        ret.Value = 0;
                    }
                }

            }
            catch (Exception e)
            {
                //todo: logging
                Console.WriteLine(e);
            }
            return ret;
        }


        private async System.Threading.Tasks.Task DisplayResultsAsync(Chat chatId, string binName)
        {

            var messages = _repository.GetMessages();

            var groupedMessages = messages
                .Where(x => x.ChatId == chatId.Id && x.BinName == binName)
                .GroupBy(x => new { x.BinName, x.UserId, x.UserName });
            var listedSum = groupedMessages.Select(x => new { x.Key.UserName, x.Key.BinName, SumDistance = x.Sum(k => k.Data) }).ToList();
            var text = " ";
            foreach (var item in listedSum)
            {
                text += item.BinName + ": <b>" + item.UserName + "</b>: " + item.SumDistance + Environment.NewLine;
            }
            text += "";
            if (!String.IsNullOrWhiteSpace(text))
            {
                await _botClient.SendTextMessageAsync(
                      chatId: chatId,
                      text: text,
                      parseMode: ParseMode.Html,
                      disableWebPagePreview: false
                      );
            }

        }

        private async System.Threading.Tasks.Task DisplayAllResultsAsync(Chat chatId)
        {

            var messages = _repository.GetMessages();

            var groupedMessages = messages
                .Where(x => x.ChatId == chatId.Id)
                .GroupBy(x => new { x.BinName, x.UserId });
            var listedSum = groupedMessages.Select(x => new { x.OrderByDescending(k => k.DataPointTimestamp).LastOrDefault()?.UserName, x.Key.BinName, SumDistance = x.Sum(k => k.Data) })
                .OrderBy(x => x.BinName)
                .ThenBy(x => x.SumDistance)
                .ToList();
            var text = " ";
            var groupedListedSum = listedSum.GroupBy(x => x.BinName);
            foreach (var bin in groupedListedSum)
            {
                text += "__<code>" + bin.Key + "</code>__" + Environment.NewLine;
                foreach (var item in bin)
                {
                    text += "<b>" + item.UserName + "</b>: \t\t" + item.SumDistance + Environment.NewLine;
                }
                text += Environment.NewLine;
                text += Environment.NewLine;

            }

            text += "";
            if (!String.IsNullOrWhiteSpace(text))
            {
                await _botClient.SendTextMessageAsync(
                      chatId: chatId,
                      text: text,
                      parseMode: ParseMode.Html,
                      disableWebPagePreview: false
                      );
            }

        }


        private void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            var userFrom = e.Message.From;
            var usersent = $"{userFrom.FirstName} {userFrom.LastName}";
            if (!String.IsNullOrWhiteSpace(userFrom.Username))
            {
                usersent = $"{userFrom.Username}";
            }

            var userIdsent = userFrom.Id;

            var query = e.Message.Text;
            var messageId = e.Message.MessageId;
            var chatId = e.Message.Chat.Id;


            if (query != null)
            {


                if (ShouldReactToCommand(query) == BotCommand.Show)
                {
                    query = SanitizeQueryFromBotTrigger(query, _botCommandsShow);
                    var parsedQuery = ParseQuery(query);
                    if (String.IsNullOrWhiteSpace(parsedQuery.Bin))
                    {
                        DisplayAllResultsAsync(e.Message.Chat).GetAwaiter().GetResult();
                    }
                    else
                    {
                        DisplayResultsAsync(e.Message.Chat, parsedQuery.Bin).GetAwaiter().GetResult();
                    }

                }
                else if (ShouldReactToCommand(query) == BotCommand.Delete)
                {
                    query = SanitizeQueryFromBotTrigger(query, _botCommandsDelete);
                    var parsedQuery = ParseQuery(query);


                    AskButtonWithCallBack(
                        e.Message.Chat.Id,
                        parsedQuery.Bin,
                        $"Are you sure you want to delete the counter named '{parsedQuery.Bin}' and all of the saved scores?",
                        new List<string>() { "YES", "nope" }
                        );


                }
                else if (ShouldReactToCommand(query) == BotCommand.Add)
                {
                    query = SanitizeQueryFromBotTrigger(query, _botCommandsAdd);
                    var parsedQuery = ParseQuery(query);
                    var userMeasurement = new UserMeasurement()
                    {
                        BinName = parsedQuery.Bin,
                        DataPointTimestamp = DateTime.Now,
                        MessageId = messageId,
                        Data = parsedQuery.Value,
                        UserName = usersent,
                        UserId = userIdsent,
                        ChatId = chatId
                    };

                    var measures = _repository.GetMessages();
                    measures.Add(userMeasurement);
                    _repository.SaveMessagesToFile(measures);

                    DisplayResultsAsync(e.Message.Chat, parsedQuery.Bin).GetAwaiter().GetResult();
                }
            }

        }

        private void RemoveCounter(long chatId, string binName)
        {
            var messages = _repository.GetMessages();

            var cleansedMessages = messages
                .Where(x => x.ChatId != chatId || x.BinName != binName).ToList();

            if (String.IsNullOrWhiteSpace(binName))
            {
                cleansedMessages = messages.Except(messages.Where(x => x.ChatId == chatId && (x.BinName == null || x.BinName == binName)).ToList()).ToList();
            }

            _repository.SaveMessagesToFile(cleansedMessages, true);
        }
        private async void AskButtonWithCallBack(long chatId, string binName, string qustionText, List<string> options)
        {
            var keyboard = new InlineKeyboardMarkup(options.Select(x => new[] { new InlineKeyboardButton() { Text = x, CallbackData = $"{x}:{binName}" } }).ToArray());

            await _botClient.SendTextMessageAsync(chatId, qustionText,
                replyMarkup: keyboard);
        }

        private async void BotClient_OnCallbackQueryAsync(object sender, CallbackQueryEventArgs e)
        {
            var messageText = e.CallbackQuery.Message.Text;
            var replyData = e.CallbackQuery.Data;
            var chat = e.CallbackQuery.Message.Chat;
            if (messageText != null && replyData != null && replyData.Contains(":"))
            {
                //replyData is in the format chatId:binName
                string buttonName = replyData.Split(":").FirstOrDefault();

                if (buttonName != null && buttonName == "YES")
                {
                    string binName = replyData.Substring(replyData.IndexOf(":")).TrimStart(':');
                    var origMessage = e.CallbackQuery.InlineMessageId;

                    RemoveCounter(chat.Id, binName);

                    await _botClient.DeleteMessageAsync(chat, e.CallbackQuery.Message.MessageId);
                    DisplayAllResultsAsync(e.CallbackQuery.Message.Chat).GetAwaiter().GetResult();

                }

            }
        }

    }
}
