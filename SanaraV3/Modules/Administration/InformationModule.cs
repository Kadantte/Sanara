﻿using Discord;
using Discord.Commands;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanaraV3.Modules.Administration
{
    public sealed partial class HelpPreload
    {
        public void LoadInformationHelp()
        {
            _help.Add(new Help("Help", new Argument[0], "Display this help.", false));
            _help.Add(new Help("Status", new Argument[0], "Display various information about the bot.", false));
        }
    }

    public class InformationModule : ModuleBase
    {
        [Command("Help")]
        public async Task Help()
        {
            StringBuilder str = new StringBuilder();
            foreach (var help in StaticObjects.Help.GetHelp())
            {
                if (!help.IsNsfw || !(Context.Channel is ITextChannel) || ((ITextChannel)Context.Channel).IsNsfw)
                    str.AppendLine($"**{help.CommandName} {string.Join(" ", help.Arguments.Select(x => x.Type == ArgumentType.MANDATORY ? $"[{x.Content}]" : $"({x.Content})"))}**: {help.Description}");
            }
            await ReplyAsync(embed: new EmbedBuilder
            {
                Color = Color.Blue,
                Title = "Help",
                Description = str.ToString()
            }.Build());
        }

        [Command("Status")]
        public async Task Status()
        {
            var embed = new EmbedBuilder
            {
                Title = "Status",
                Color = Color.Purple
            };
            embed.AddField("Server count", StaticObjects.Client.Guilds.Count, true);
            embed.AddField("Total user count (may contains duplicate)", StaticObjects.Client.Guilds.Sum(x => x.Users.Count), true);
            await ReplyAsync(embed: embed.Build());
        }
    }
}