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

        private ParsedQuery ParseQuery(string query)
        {
            var ret = new ParsedQuery();

            try
            {
                query = query.ToLowerInvariant();

                if (query.Contains("@mensuracolesbot") || query.Contains("счетобот") || query.Contains("countbot") || query.Contains("count") || query.Contains("add"))
                {
                    query = query.Replace("@mensuracolesbot", "");
                    query = query.Replace("счетобот", "");
                    query = query.Replace("countbot", "");
                    query = query.Replace("count", "");
                    query = query.Replace("add", "");



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


            if (parsedQuery.Value != 0)
            {
                var measures = _repository.GetMessages();
                measures.Add(userMeasurement);
                _repository.SaveMessagesToFile(measures);

                DisplayResultsAsync(e.Message.Chat, parsedQuery.Bin).GetAwaiter().GetResult();
            }
        }
        private void BotClient_OnCallbackQueryAsync(object sender, CallbackQueryEventArgs e)
        {
            // throw new NotImplementedException();
        }
    }
}
