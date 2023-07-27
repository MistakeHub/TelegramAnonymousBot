using Anonymous.Bot.Models;

namespace Anonymous.Bot
{
    internal class Program
    {
      
      
        static async Task Main(string[] args)
        {
            TelegramBotManager botManager = new TelegramBotManager("YOUR_TOKEN");
            
            while (true)
            {
                Console.WriteLine("1: Start Bot /n 2: StopBot");
                string choose=Console.ReadLine();

                switch(choose)
                {
                    case "1":
                        {
                           await botManager.Start();
                            break;
                        }
                    case "2":
                        {
                         botManager.Stop();
                            break;
                        }
                    default:
                        {
                            Console.WriteLine("UnknownCommand");
                            break;

                        }

                }


            }
        }

        
    }
}