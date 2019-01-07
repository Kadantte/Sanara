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
using Discord.Net;
using SanaraV2.Modules.Base;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SanaraV2.Modules.Tools
{
    public class Communication : ModuleBase
    {
        Program p = Program.p;

        [Command("Help"), Summary("Give the help"), Alias("Commands")]
        public async Task Help()
        {
            await p.DoAction(Context.User, Context.Guild.Id, Program.Module.Communication);
            await ReplyAsync("", false, Sentences.Help(Context.Guild.Id, (Context.Channel as ITextChannel).IsNsfw, Context.User.Id == Base.Sentences.ownerId));
        }

        [Command("Infos"), Summary("Give informations about an user"), Alias("Info")]
        public async Task Infos(params string[] command)
        {
            await p.DoAction(Context.User, Context.Guild.Id, Program.Module.Communication);
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
            await p.DoAction(Context.User, Context.Guild.Id, Program.Module.Communication);
            await InfosUser(await Context.Channel.GetUserAsync(Base.Sentences.myId) as IGuildUser);
        }

        [Command("GDPR"), Summary("Show infos the bot have about the user and the guild")]
        public async Task GDPR(params string[] command)
        {
            await p.DoAction(Context.User, Context.Guild.Id, Program.Module.Communication);
            await ReplyAsync("", false, new EmbedBuilder()
            {
                Color = Color.Blue,
                Title = "Datas saved about " + Context.Guild.Name,
                Description = await Program.p.db.GetGuild(Context.Guild.Id)
            }.Build());
        }

        [Command("Status"), Summary("Display which commands aren't available because of missing files")]
        public async Task Status()
        {
            await p.DoAction(Context.User, Context.Guild.Id, Program.Module.Settings);
            int yes = 0;
            int no = 0;
            EmbedBuilder embed = new EmbedBuilder()
            {
                Title = "Services availability"
            };
            embed.AddField("Radio Module",
                "**Opus dll:** " + ((File.Exists("opus.dll") ? ("Yes") : ("No"))) + Environment.NewLine +
                "**Lib Sodium dll:** " + ((File.Exists("libsodium.dll") ? ("Yes") : ("No"))) + Environment.NewLine +
                "**Ffmpeg:** " + ((File.Exists("ffmpeg.exe") ? ("Yes") : ("No"))) + Environment.NewLine +
                "**youtube-dl:** " + ((File.Exists("youtube-dl.exe") ? ("Yes") : ("No"))) + Environment.NewLine +
                "**YouTube API key:** " + ((p.youtubeService != null ? ("Yes") : ("No"))));
            if (File.Exists("opus.dll") && File.Exists("libsodium.dll") && File.Exists("ffmpeg.exe") && p.youtubeService != null)
                yes++;
            else
                no++;
            embed.AddField("Game Module",
                "**Shiritori:** " + ((Program.p.shiritoriDict == null) ? ("Not loaded") : (Program.p.shiritoriDict.Count + " words")) + Environment.NewLine +
                "**Booru quizz:** " + ((Program.p.booruDict == null) ? ("Not loaded") : (Program.p.booruDict.Count + " tags")) + Environment.NewLine +
                "**Anime quizz:** " + ((Program.p.animeDict == null) ? ("Not loaded") : (Program.p.animeDict.Count + " anime names")) + Environment.NewLine +
                "**KanColle quizz :** " + ((Program.p.kancolleDict == null) ? ("Not loaded") : (Program.p.kancolleDict.Count + " shipgirl names")) + Environment.NewLine +
                "**Fire Emblem quizz:** " + ((Program.p.fireEmblemDict == null) ? ("Not loaded") : (Program.p.fireEmblemDict.Count + " character names")));
            if (Program.p.shiritoriDict != null)
                yes++;
            else
                no++;
            if (Program.p.booruDict != null)
                yes++;
            else
                no++;
            if (Program.p.animeDict != null)
                yes++;
            else
                no++;
            if (Program.p.kancolleDict != null)
                yes++;
            else
                no++;
            if (Program.p.fireEmblemDict != null)
                yes++;
            else
                no++;
            embed.AddField("Linguistic Module - Translations",
                "**Google Translate API key:** " + ((p.translationClient != null ? ("Yes") : ("No"))) + Environment.NewLine +
                "**Google Vision API key:** " + ((p.visionClient != null ? ("Yes") : ("No"))));
            if (p.translationClient != null)
            {
                yes++;
                if (p.visionClient != null)
                    yes++;
                else
                    no++;
            }
            else
                no += 2;
            embed.AddField("YouTube Module", "**YouTube API key:** " + ((p.youtubeService != null) ? ("Yes") : ("No")));
            if (p.youtubeService != null)
                yes++;
            else
                no++;
            int max = yes + no;
            embed.Color = new Color(no * 255 / max, yes * 255 / max, 0);
            await ReplyAsync("", false, embed.Build());
        }

        [Command("Invite", RunMode = RunMode.Async), Summary("Get invitation link")]
        public async Task Invite()
        {
            await ReplyAsync("<https://discordapp.com/oauth2/authorize?client_id=329664361016721408&permissions=3196928&scope=bot>");
        }

        [Command("Quote", RunMode = RunMode.Async), Summary("Quote a message")]
        public async Task Quote(string id = null)
        {
            await p.DoAction(Context.User, Context.Guild.Id, Program.Module.Settings);
            IUser author = (id == null) ? (null) : (await Utilities.GetUser(id, Context.Guild));
            if (id == null || author != null)
            {
                if (author == null)
                    author = Context.User;
                IMessage msg = (await Context.Channel.GetMessagesAsync().FlattenAsync()).Skip(1).ToList().Find(x => x.Author.Id == author.Id);
                if (msg == null)
                    await ReplyAsync(Sentences.QuoteNoMessage(Context.Guild.Id));
                else
                    await ReplyAsync("", false, new EmbedBuilder()
                    {
                        Description = msg.Content
                    }.WithAuthor(msg.Author.ToString(), msg.Author.GetAvatarUrl()).WithFooter("The " + msg.CreatedAt.ToString(Base.Sentences.DateHourFormat(Context.Guild.Id)) + " in " + msg.Channel.Name).Build());
            }
            else
            {
                ulong uId;
                try
                {
                    uId = Convert.ToUInt64(id);
                }
                catch (FormatException)
                {
                    await ReplyAsync(Sentences.QuoteInvalidId(Context.Guild.Id));
                    return;
                }
                catch (OverflowException)
                {
                    await ReplyAsync(Sentences.QuoteInvalidId(Context.Guild.Id));
                    return;
                }
                IMessage msg = await Context.Channel.GetMessageAsync(uId);
                if (msg == null)
                {
                    foreach (IGuildChannel chan in await Context.Guild.GetChannelsAsync())
                    {
                        try
                        {
                            ITextChannel textChan = chan as ITextChannel;
                            if (textChan == null)
                                continue;
                            msg = await textChan.GetMessageAsync(uId);
                            if (msg != null)
                                break;
                        } catch (HttpException) { }
                    }
                }
                if (msg == null)
                    await ReplyAsync(Sentences.QuoteInvalidId(Context.Guild.Id));
                else
                {
                    await ReplyAsync("", false, new EmbedBuilder()
                    {
                        Description = msg.Content
                    }.WithAuthor(msg.Author.ToString(), msg.Author.GetAvatarUrl()).WithFooter("The " + msg.CreatedAt.ToString(Base.Sentences.DateHourFormat(Context.Guild.Id)) + " in " + msg.Channel.Name).Build());
                }
            }
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
                embed.AddField("Invitation link", "https://discordapp.com/oauth2/authorize?client_id=329664361016721408&permissions=3196928&scope=bot");
                embed.AddField(Sentences.OfficialGuild(Context.Guild.Id), "https://discordapp.com/invite/H6wMRYV");
            }
            embed.AddField(Sentences.Roles(Context.Guild.Id), ((roles == "") ? (Sentences.NoRole(Context.Guild.Id)) : (roles)));
            await ReplyAsync("", false, embed.Build());
        }
    }
}