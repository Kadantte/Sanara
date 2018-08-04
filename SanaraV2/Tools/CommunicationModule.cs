﻿/// This file is part of Sanara.
///
/// Sanara is free software: you can redistribute it and/or modify
/// it under the terms of the GNU General Public License as published by
/// the Free Software Foundation, either version 3 of the License, or
/// (at your option) any later version.
///
/// Sanara is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
/// GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with Sanara.  If not, see<http://www.gnu.org/licenses/>.
using Discord;
using Discord.Commands;
using SanaraV2.Base;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SanaraV2.Tools
{
    public class CommunicationModule : ModuleBase
    {
        Program p = Program.p;

        [Command("Help"), Summary("Give the help"), Alias("Commands")]
        public async Task Help()
        {
            p.DoAction(Context.User, Context.Guild.Id, Program.Module.Communication);
            await ReplyAsync("", false, Sentences.Help(Context.Guild.Id, (Context.Channel as ITextChannel).IsNsfw));
        }

        [Command("Hi"), Summary("Answer with hi"), Alias("Hey", "Hello", "Hi!", "Hey!", "Hello!")]
        public async Task SayHi()
        {
            p.DoAction(Context.User, Context.Guild.Id, Program.Module.Communication);
            await ReplyAsync(Sentences.HiStr(Context.Guild.Id));
        }

        [Command("Who are you"), Summary("Answer with who she is"), Alias("Who are you ?", "Who are you?")]
        public async Task WhoAreYou()
        {
            p.DoAction(Context.User, Context.Guild.Id, Program.Module.Communication);
            await ReplyAsync(Sentences.WhoIAmStr(Context.Guild.Id));
        }

        [Command("Infos"), Summary("Give informations about an user"), Alias("Info")]
        public async Task Infos(params string[] command)
        {
            p.DoAction(Context.User, Context.Guild.Id, Program.Module.Communication);
            IGuildUser user;
            if (command.Length == 0)
                user = Context.User as IGuildUser;
            else
            {
                user = await Utilities.GetUser(Utilities.AddArgs(command), Context.Guild);
                if (user == null)
                {
                    await ReplyAsync(Sentences.UserNotExist(Context.Guild.Id));
                    return;
                }
            }
            await InfosUser(user);
        }

        [Command("BotInfos"), Summary("Give informations about the bot"), Alias("BotInfo", "InfosBot", "InfoBot")]
        public async Task BotInfos(params string[] command)
        {
            p.DoAction(Context.User, Context.Guild.Id, Program.Module.Communication);
            await InfosUser(await Context.Channel.GetUserAsync(Base.Sentences.myId) as IGuildUser);
        }

        [Command("GDPR"), Summary("Show infos the bot have about the user and the guild")]
        public async Task GDPR(params string[] command)
        {
            p.DoAction(Context.User, Context.Guild.Id, Program.Module.Communication);
            string[] content = File.ReadAllLines("Saves/Users/" + Context.User.Id + ".dat");
            EmbedBuilder embed = new EmbedBuilder()
            {
                Color = Color.Green,
                Title = "Datas saved about " + Context.User.Username
            };
            embed.AddField("Name", content[0], true);
            embed.AddField("ID", content[1], true);
            embed.AddField("First message sent", DateTime.ParseExact(content[2], "ddMMyyHHmmss", CultureInfo.CurrentCulture).ToString("dd/MM/yy HH:mm:ss"), true);
            embed.AddField("Nb. of message sent", content[3], true);
            if (content.Length > 4)
                embed.AddField("Deprecated values (now unused)", String.Join(Environment.NewLine, content.Skip(4)));
            await ReplyAsync("", false, embed.Build());
            if (Context.Guild.OwnerId == Context.User.Id)
            {
                EmbedBuilder embed2 = new EmbedBuilder()
                {
                    Color = Color.Green,
                    Title = "Datas saved about " + Context.Guild.Name
                };
                string[] files = Directory.GetFiles("Saves/Servers/" + Context.Guild.Id);
                foreach (string s in files)
                {
                    FileInfo fi = new FileInfo(s);
                    string[] c = File.ReadAllLines(s);
                    if (fi.Name == "serverDatas.dat")
                    {
                        embed2.AddField("Server datas",
                            "**Server first joined:** " + DateTime.ParseExact(c[0], "ddMMyyHHmmss", CultureInfo.CurrentCulture).ToString("dd/MM/yy HH:mm:ss") + Environment.NewLine +
                            "**Deprecated value (now unused):** " + c[1] + Environment.NewLine +
                            "**Server name:** " + c[2]
                            + ((c.Length > 3) ? (Environment.NewLine + "**Deprecated values (now unused):** " + String.Join(", ", c.Skip(3))) : ("")));
                    }
                    else if (fi.Name == "kancolle.dat" || fi.Name == "shiritori.dat" || fi.Name == "booru.dat")
                    {
                        string gameName;
                        if (fi.Name == "kancolle.dat") gameName = "KanColle guess";
                        else if (fi.Name == "shiritori.dat") gameName = "Shiritori";
                        else gameName = "Booru guess";
                        embed2.AddField(gameName + " game",
                            "**Game played:** " + c[0] + Environment.NewLine +
                            "**Nb. attempts:** " + c[1] + Environment.NewLine +
                            "**Nb. found:** " + c[2] + Environment.NewLine +
                            "**Current best score:** " + c[3] + Environment.NewLine +
                            "**Ids of the players who contribued for the best score:** " + c[4]);
                    }
                    else
                        embed2.AddField(fi.Name, File.ReadAllText(fi.FullName));
                }
                DirectoryInfo di = new DirectoryInfo("Saves/Servers/" + Context.Guild.Id + "/ModuleCount");
                List<FileInfo> f = GetFiles(di);
                embed2.AddField("Messages sent",
                    "**Directory size:** " + f.Select(x => x.Length).Sum());
                await ReplyAsync("", false, embed2.Build());
            }
        }

        private List<FileInfo> GetFiles(DirectoryInfo dir)
        {
            List<FileInfo> files = dir.GetFiles().ToList();
            foreach (DirectoryInfo di in dir.GetDirectories())
            {
                files.AddRange(GetFiles(di));
                files.AddRange(di.GetFiles());
            }
            return (files);
        }

        public async Task InfosUser(IGuildUser user)
        {
            string roles = "";
            foreach (ulong roleId in user.RoleIds)
            {
                IRole role = Context.Guild.GetRole(roleId);
                if (role.Name == "@everyone")
                    continue;
                roles += role.Name + ", ";
            }
            if (roles != "")
                roles = roles.Substring(0, roles.Length - 2);
            EmbedBuilder embed = new EmbedBuilder
            {
                ImageUrl = user.GetAvatarUrl(),
                Color = Color.Purple
            };
            embed.AddField(Sentences.Username(Context.Guild.Id), user.ToString(), true);
            if (user.Nickname != null)
                embed.AddField(Sentences.Nickname(Context.Guild.Id), user.Nickname, true);
            embed.AddField(Sentences.AccountCreation(Context.Guild.Id), user.CreatedAt.ToString(Base.Sentences.DateHourFormat(Context.Guild.Id)), true);
            embed.AddField(Sentences.GuildJoined(Context.Guild.Id), user.JoinedAt.Value.ToString(Base.Sentences.DateHourFormat(Context.Guild.Id)), true);
            if (user == (await Context.Channel.GetUserAsync(Base.Sentences.myId)))
            {
                embed.AddField(Sentences.Creator(Context.Guild.Id), "Zirk#0001", true);
                embed.AddField(Sentences.LatestVersion(Context.Guild.Id), new FileInfo(Assembly.GetEntryAssembly().Location).LastWriteTimeUtc.ToString(Base.Sentences.DateHourFormat(Context.Guild.Id)), true);
                embed.AddField(Sentences.NumberGuilds(Context.Guild.Id), p.client.Guilds.Count, true);
                embed.AddField(Sentences.Uptime(Context.Guild.Id), Utilities.TimeSpanToString(DateTime.Now.Subtract(p.startTime), Context.Guild.Id));
                embed.AddField("GitHub", "https://github.com/Xwilarg/Sanara");
                embed.AddField(Sentences.Website(Context.Guild.Id), "https://zirk.eu/sanara.html");
                embed.AddField(Sentences.OfficialGuild(Context.Guild.Id), "https://discordapp.com/invite/H6wMRYV");
            }
            embed.AddField(Sentences.Roles(Context.Guild.Id), ((roles == "") ? (Sentences.NoRole(Context.Guild.Id)) : (roles)));
            await ReplyAsync("", false, embed.Build());
        }
    }
}