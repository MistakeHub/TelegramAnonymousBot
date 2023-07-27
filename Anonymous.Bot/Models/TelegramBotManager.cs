using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Anonymous.Bot.Models
{
    internal class TelegramBotManager
    {
        private List<Dialog> _dialogs=new List<Dialog>();
        private Queue<Participant> _query=new Queue<Participant>();
        private object obj = new object();
        public TelegramBotManager(string apitoken)
        { 
            API_TOKEN = apitoken;
            _client = new TelegramBotClient(API_TOKEN);
        }
        private readonly string API_TOKEN;
        private TelegramBotClient _client;
        ReceiverOptions receiverOptions = new()
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types except ChatMember related updates
        };
       public CancellationTokenSource cts = new ();
        public async Task Start()
        {
            _client.StartReceiving(updateHandler:HandleUpdateAsync, pollingErrorHandler: HandlePollingErrorAsync, receiverOptions: receiverOptions, cancellationToken: cts.Token);
            var user = await _client.GetMeAsync();
            
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botclient, Update update, CancellationToken token)
        {
            if (update.Message is not { } message) return;
            if (message.Text is not { } messageText) return;
            CancellationTokenSource cts = new CancellationTokenSource();
            switch (messageText)
            {
                  
                case "/start":
                    {
                        if (!_query.Any(v => v.ChatId == message.Chat.Id))
                        {
                            Thread t = new Thread(async delegate ()
                            {
                                var pariticapntdialog = GetCurrentDialog(message.Chat.Id);
                                await _client.SendTextMessageAsync(chatId: message.Chat.Id, text: "U said start", cancellationToken: this.cts.Token);
                                if (pariticapntdialog == null)
                                {
                                    var result = PushToSearch(message.Chat.Id);

                                    if (result != null)
                                    {
                                        await _client.SendTextMessageAsync(chatId: message.Chat.Id, text: "U've just added to the queue...", cancellationToken: this.cts.Token);
                                        result.Ctsender = cts;
                                        result.Notify += () => { cts.Cancel(); _query.Dequeue(); };
                                        bool searchresult = await Search(result, cts.Token);
                                        await _client.SendTextMessageAsync(chatId: message.Chat.Id, text: "We've found a participant for your conversation", cancellationToken: this.cts.Token);
                                    }


                                }

                            });
                            t.Start();
                        }
                        else await _client.SendTextMessageAsync(chatId: message.Chat.Id, text: "U've already been added to queue", cancellationToken: this.cts.Token);

                        break;

                    }
                case "/stop":
                    {
                       var pariticapntdialog = GetCurrentDialog(message.Chat.Id);
                        if (pariticapntdialog != null)
                        {
                            var dialog = _dialogs.FirstOrDefault(v => v.Participants.Any(v => v.ChatId == message.Chat.Id));
                            _dialogs.Remove(dialog);
                            foreach (var item in dialog.Participants)
                            {
                                await _client.SendTextMessageAsync(chatId: item.ChatId, text: "The dialog was stopped", cancellationToken: this.cts.Token);
                            }
                           

                        }
                        else cts.Cancel();
                        await _client.SendTextMessageAsync(chatId: message.Chat.Id, text: "U said stop", cancellationToken: this.cts.Token);
                        break;
                    }
                default:
                    {
                        long? pariticapntdialog = GetCurrentDialog(message.Chat.Id);
                        if (pariticapntdialog != null) await _client.SendTextMessageAsync(chatId: pariticapntdialog, text: messageText, cancellationToken: this.cts.Token);
                        else await _client.SendTextMessageAsync(chatId: message.Chat.Id, text: "UnkownCommand", cancellationToken: cts.Token);
                        break;
                    }
            }

        }

        Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        public void Stop()
        {
            cts.Cancel();
            

        }


        private long? GetCurrentDialog(long chatid)
        {
          long? id= _dialogs?.FirstOrDefault(v => v.Participants.Any(v=>v?.ChatId==chatid))?.Participants?.FirstOrDefault(v=>v?.ChatId!=chatid)?.ChatId;

            return id;
        }

        private async Task<bool> Search(Participant participant, CancellationToken cts)
        {
            while (true)
            {
                lock (obj)
                {
                    if (cts.IsCancellationRequested) return false; 
                  

                    bool IsAbleToDequeue = _query.TryPeek(out Participant result);
                    if (IsAbleToDequeue && result != participant)
                    {
                        _dialogs.Add(new Dialog() { Participants = new List<Participant> { participant, result }, StartDate = DateTime.UtcNow });
                        _query.Dequeue();
                        result.Ctsender.Cancel();
                        participant.Notify?.Invoke();
                        return true;
                    }

                  
                }
                await Task.Delay(2500);

            }

        }

        private Participant PushToSearch(long chatid)
        {
            var partic = new Participant() { ChatId = chatid };
            _query.Enqueue(partic);
           
            return partic;
        }

    }
}
