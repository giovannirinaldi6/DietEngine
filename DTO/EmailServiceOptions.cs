using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DietWorker.DTO
{
    public class EmailServiceOptions
    {
        public ImapOptions Imap { get; set; } = new();
        public SmtpOptions Smtp { get; set; } = new();

        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class ImapOptions
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public bool UseSsl { get; set; }
    }

    public class SmtpOptions
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public bool UseSsl { get; set; }
    }

    public class PersoneOptions
    {
        public string MenuFrom { get; set; } = "";
        public string MenuTo { get; set; } = "";
    }
}
