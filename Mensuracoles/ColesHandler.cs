using Mensuracoles.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
            List<string> botCommandsAdd = new List<string>()
            {
                "@mensuracolesbot",
                "счетобот",
                "countbot",
                "count",
                "add"
            };
            List<string> botCommandsShow = new List<string>()
            {
                "@mensuracolesbot show",
                "счетобот статы",
                "счетобот покажи",
                "countbot show",
                "count show",
                "show"
            };
            List<string> botCommandsRemove = new List<string>()
            {
                "@mensuracolesbot delete",
                "счетобот удали",
                "countbot delete",
                "count delete",
                "delete"
            };
            query = query.ToLowerInvariant();

            if (botCommandsAdd.Any(x => query.StartsWith(x)))
            {
                return BotCommand.Add;
            }
            else if (botCommandsShow.Any(x => query.StartsWith(x)))
            {
                return BotCommand.Show;
            }
            else if (botCommandsRemove.Any(x => query.StartsWith(x)))
            {
                return BotCommand.Delete;
            }
            else
            {
                return BotCommand.None;

            }
        }
        private string SanitizeQueryFromBotTrigger(string rawQuery)
        {

            List<string> botCommandsAdd = new List<string>()
            {
                "@mensuracolesbot",
                "счетобот",
                "countbot",
                "count",
                "add",
                "show"
            };


            string query = rawQuery;
            foreach (var command in botCommandsAdd)
            {
                query = query.Replace(command, "");
            }

            return query;
        }

        private ParsedQuery ParseQuery(string query)
        {
            var ret = new ParsedQuery();

            try
            {
                query = query.ToLowerInvariant();

                if (ShouldReactToCommand(query) == BotCommand.Add)
                {
                    query = SanitizeQueryFromBotTrigger(query);

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
                        ret.Bin = "";
                    }

                    if (splittedSanitized.Count() >= 2)
                    {
                        ret.Bin = String.Join(" ", splittedSanitized.Take(splittedSanitized.Count() - 1));
                    }

                    var lastValue = splittedSanitized.LastOrDefault();
                    decimal value = 0;

                    if (decimal.TryParse(lastValue, out value))
                    {
                        ret.Value = value;
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

            if (query != null)
            {
                if (ShouldReactToCommand(query) == BotCommand.Add)
                {
                    var measures = _repository.GetMessages();
                    measures.Add(userMeasurement);
                    _repository.SaveMessagesToFile(measures);

                    DisplayResultsAsync(e.Message.Chat, parsedQuery.Bin).GetAwaiter().GetResult();
                }
                else if (ShouldReactToCommand(query) == BotCommand.Show)
                {
                    DisplayAllResultsAsync(e.Message.Chat).GetAwaiter().GetResult();

                }
                else if (ShouldReactToCommand(query) == BotCommand.Delete)
                {
                    DisplayAllResultsAsync(e.Message.Chat).GetAwaiter().GetResult();

                }
            }

        }
        private void BotClient_OnCallbackQueryAsync(object sender, CallbackQueryEventArgs e)
        {
            // throw new NotImplementedException();
        }
    }
}
