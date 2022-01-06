using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmailAPI.Data
{
    public class Mail
    {
        public string From { get; set; }

        public string To { get; set; }

        public string Body { get; set; }

        public DateTime MailDateTime { get; set; }

        public List<string> Attachements { get; set; }

        public string MsgID { get; set; }

        public string Status { get; set; }

        public string Bank { get; set; }
    }
}
