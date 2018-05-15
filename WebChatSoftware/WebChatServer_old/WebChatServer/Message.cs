using System;
using System.Collections.Generic;
using System.Text;

namespace WebChatServer
{
    class Message
    {
        private static string sender;
        private static string receiver;
        private static string message;

        public Message(string author, string recipient, string content)
        {
            sender = author;
            receiver = recipient;
            message = content;
        }

        public string GetSender { get => sender; }
        public string GetReceiver { get => receiver;}
        public string GetMessage { get => message;}
    }
}
