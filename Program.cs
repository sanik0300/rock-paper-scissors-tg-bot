using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Resources;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Exceptions;
using System.Globalization;
using System.Reflection;
using System.ComponentModel;

namespace Rock_paper_scissors_bot
{
    class Program
    {
        static private string token;
        static TelegramBotClient Bot;      

        static Random rm = new Random();


        static ReplyKeyboardMarkup GenerateReplyMarkup(bool viewModeReal)
        {
            string[] array = viewModeReal ?  GameMechanics.RealThings : GameMechanics.Hands ;
            return new ReplyKeyboardMarkup(new KeyboardButton[][]
            {
               new[] { new KeyboardButton(array[0]), new KeyboardButton(array[2]) },
               new[] { new KeyboardButton(array[1]), new KeyboardButton(array[3]), }
            }) { ResizeKeyboard = true };
        }
        static ReplyKeyboardRemove remove = new ReplyKeyboardRemove();
               
        static void Main(string[] args)
        {
            token = System.IO.File.ReadAllText("telegram_credentials.txt");
            
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            Bot = new TelegramBotClient(token);

            Bot.StartReceiving(new DefaultUpdateHandler(HandleUpdateAsync, HandlePollingErrorAsync));
            #if DEBUG
            Console.WriteLine("я открылся");
            Console.ReadKey();
            #else
            while(true)
            {
                Thread.Sleep(1000);
            }
            #endif
        }

        private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            #if DEBUG
            Exception e = exception;
            Console.WriteLine(e.Message);
            while (e.InnerException != null)
            {
                Console.WriteLine(e.Message);
                e = e.InnerException;
            }
            Console.WriteLine("-------------------------------");
            Environment.Exit(36060);
            #endif
            return Task.CompletedTask;
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            switch(update.Type)
            {
                case UpdateType.MyChatMember:
                    if(update.MyChatMember.NewChatMember.Status == ChatMemberStatus.Kicked)
                    {
                        DbController.DeleteUser(update.MyChatMember.Chat.Id);
                    }                    
                    break;
                case UpdateType.Message:
                    string msg_text_or_caption = update.Message.Type == MessageType.Text? update.Message.Text : update.Message.Caption;
                    bool userViewMode = DbController.GetUserViewMode(update.Message.Chat.Id);

                    
                    ResourceManager resMngr = new ResourceManager("Rock_paper_scissors_bot.StringRes.Strings", Assembly.GetExecutingAssembly());
                    CultureInfo userCulture = new CultureInfo(update.Message.From.LanguageCode);

                    switch (msg_text_or_caption)
                    {
                        case GameMechanics.StartCommand:
                            if(!DbController.UserExists(update.Message.Chat.Id)) 
                            {
                                DbController.AddOrResetUser(update.Message.Chat.Id);
                            }
                            await botClient.SendTextMessageAsync(update.Message.Chat.Id, 
                                                                string.Format(resMngr.GetString("menuText", userCulture), 
                                                                                GameMechanics.StartCommand,
                                                                                GameMechanics.ChooseCommand,
                                                                                GameMechanics.StopCommand,
                                                                                GameMechanics.ChangeEmojiCommand),
                                                                replyMarkup: GenerateReplyMarkup(userViewMode));
                            return;
                        case GameMechanics.StopCommand:
                            DbController.AddOrResetUser(update.Message.Chat.Id);
                            await botClient.SendTextMessageAsync(update.Message.Chat.Id, 
                                                                resMngr.GetString("gameStop", userCulture), 
                                                                replyMarkup: GenerateReplyMarkup(userViewMode));
                            return;
                        case GameMechanics.ChangeEmojiCommand:
                            await botClient.SendTextMessageAsync(update.Message.Chat.Id,
                                                                resMngr.GetString("emojiChange", userCulture),
                                                                replyMarkup: GenerateReplyMarkup(!userViewMode));
                            DbController.SetUserViewMode(update.Message.Chat.Id, !userViewMode);
                            return;
                        default:

                            switch(DbController.GetUserState(update.Message.Chat.Id))
                            {
                                case GameState.Neutral:
                                    if (msg_text_or_caption == GameMechanics.ChooseCommand)
                                    {
                                        await botClient.SendTextMessageAsync(update.Message.Chat.Id,
                                                                            resMngr.GetString("ifUserWins", userCulture), 
                                                                            replyMarkup: remove);
                                        DbController.SetUserState(update.Message.Chat.Id, GameState.Option1Request);
                                        return;
                                    }
                                    break;
                                case GameState.Option1Request:
                                    DbController.SetDecisionOption(update.Message.Chat.Id, update.Message.MessageId, true);
                                    await botClient.SendTextMessageAsync(update.Message.Chat.Id,
                                                                        resMngr.GetString("ifBotWins", userCulture));
                                    DbController.SetUserState(update.Message.Chat.Id, GameState.Option2Request);
                                    return;
                                case GameState.Option2Request:                                
                                    DbController.SetDecisionOption(update.Message.Chat.Id, update.Message.MessageId, false);
                                    await botClient.SendTextMessageAsync(update.Message.Chat.Id,
                                                                        resMngr.GetString("optionsOk", userCulture), 
                                                                        replyMarkup: GenerateReplyMarkup(userViewMode));
                                    DbController.SetUserState(update.Message.Chat.Id, GameState.Playing);
                                    return;                         
                            }

                            if (string.IsNullOrEmpty(msg_text_or_caption) || msg_text_or_caption.Length > 2) { return; }
                            
                            string[] array_ref;
                            sbyte index1 = (sbyte)Array.IndexOf(GameMechanics.Hands, msg_text_or_caption),
                                  index2 = (sbyte)Array.IndexOf(GameMechanics.RealThings, msg_text_or_caption);

                            if(index1 == -1 && index2 == -1) { return; }
                            array_ref = index1 >= 0 ? GameMechanics.Hands : GameMechanics.RealThings;

                            byte user_gesture = (byte)(index1 >= 0 ? index1 : index2),
                                 bot_gesture = (byte)rm.Next(0, GameMechanics.NumberOfGestures);

                            bool? humanWon = null;
                            if (bot_gesture != user_gesture)
                            {
                                humanWon = DbController.ModifyUserScore(update.Message.Chat.Id, GameMechanics.GetUserScoreDelta(user_gesture, bot_gesture));
                            }
                            await botClient.SendTextMessageAsync(update.Message.Chat.Id, array_ref[bot_gesture], replyMarkup: GenerateReplyMarkup(userViewMode));
                            
                            if(bot_gesture == user_gesture || humanWon == null) {  return; }

                            await botClient.SendTextMessageAsync(update.Message.Chat.Id,
                                                                resMngr.GetString(humanWon.Value? "userWon" : "botWon", userCulture));
                            
                            int winningId = DbController.TryGetWinningOptionId(update.Message.Chat.Id, humanWon.Value);
                            if (winningId > 0)
                            {
                                try {
                                    await botClient.ForwardMessageAsync(update.Message.Chat.Id, update.Message.Chat.Id, winningId);
                                }
                                catch (ApiRequestException)
                                {
                                    await botClient.SendTextMessageAsync(update.Message.Chat.Id, 
                                                                        $"_{resMngr.GetString("resultNotFound", userCulture)}_", 
                                                                        parseMode: ParseMode.MarkdownV2);
                                }
                            }
                            await botClient.SendTextMessageAsync(update.Message.Chat.Id, 
                                                                $"_{resMngr.GetString("playAgain", userCulture)}_",
                                                                parseMode: ParseMode.MarkdownV2, 
                                                                replyMarkup: GenerateReplyMarkup(userViewMode));
                            DbController.AddOrResetUser(update.Message.Chat.Id);
                            break;
                    }
                    break;
            }
        }
    }
}
