using System;
using System.Collections.Generic;
using System.Text;

namespace WebChatServer
{
    class Message
    {
        private Client associate;
        private static string sender;
        private static string receiver;
        private static string message;

        public Message(Client associate, string author, string recipient, string content)
        {
            this.associate = associate;
            sender = author;
            receiver = recipient;
            message = content;
        }

        public string GetSender { get => sender; }
        public string GetReceiver { get => receiver;}
        public string GetMessage { get => message;}
        public Client GetClient { get => associate; }
    }
}
