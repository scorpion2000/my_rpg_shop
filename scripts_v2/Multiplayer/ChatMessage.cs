using System;

namespace MRS.Chat
{
    public struct ChatMessage
    {
        string text;
        PlayerInfo player;
        string messageTime;

        public ChatMessage(string _text, PlayerInfo _player)
        {
            text = _text;
            player = _player;
            messageTime = DateTime.Now.ToString("HH:mm:ss");
        }

        public string GetMessage()
        {
            return $"[{player.name}] {text}";
        }
    }
}