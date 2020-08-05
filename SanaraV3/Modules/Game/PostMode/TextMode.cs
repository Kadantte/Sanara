﻿using Discord;
using System.Threading.Tasks;

namespace SanaraV3.Modules.Game.PostMode
{
    public class TextMode : IPostMode
    {
        public async Task PostAsync(IMessageChannel chan, string text)
        {
            await chan.SendMessageAsync(text);
        }
    }
}