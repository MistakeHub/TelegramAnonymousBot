using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Anonymous.Bot.Models
{
    internal class Participant
    {

        public delegate void AccountHandler();
        public AccountHandler? Notify;
        public CancellationTokenSource Ctsender;
        public long ChatId { get; set; }

      
        
    }
}
