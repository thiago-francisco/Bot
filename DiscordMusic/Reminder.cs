using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordMusic
{
    class Reminder
    {
        public DateTime Date;

        public string Server;

        public string Channel;

        public List<string> Users;

        public Reminder()
        {
            this.Users = new List<string>();
        }
        
    }
}
