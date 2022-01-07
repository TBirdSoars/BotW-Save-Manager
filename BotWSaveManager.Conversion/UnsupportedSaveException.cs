using System;

namespace BotWSaveManager.Conversion
{
    public class UnsupportedSaveException : Exception
    {
        public bool IsSwitch { get; set; }

        public UnsupportedSaveException(string message) : base(message)
        {
        }
    }
}
