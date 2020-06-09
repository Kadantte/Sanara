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
using SanaraV2.Modules.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanaraV2.Games
{
    public class GameModule : ModuleBase
    {
        public static string DisplayHelp(IGuild guild, bool isChanNsfw)
        {
            if (guild != null && !Program.p.db.IsAvailable(guild.Id, Program.Module.Game))
                return Modules.Base.Sentences.NotAvailable(guild);
            List<Tuple<APreload, string>> soloOnly = new List<Tuple<APreload, string>>();
            List<Tuple<APreload, string>> multiOnly = new List<Tuple<APreload, string>>();
            List<Tuple<APreload, string>> both = new List<Tuple<APreload, string>>();
            // Put all games on lists to know if they are multiplayer or not
            foreach (var game in Constants.allGames)
            {
                APreload preload = (APreload)Activator.CreateInstance(game.Item1);
                switch (preload.DoesAllowMultiplayer())
                {
                    case APreload.Multiplayer.SoloOnly:
                        soloOnly.Add(new Tuple<APreload, string>(preload, game.Item3));
                        break;

                    case APreload.Multiplayer.MultiOnly:
                        multiOnly.Add(new Tuple<APreload, string>(preload, game.Item3));
                        break;

                    case APreload.Multiplayer.Both:
                        both.Add(new Tuple<APreload, string>(preload, game.Item3));
                        break;
                }
            }
            // Display help
            StringBuilder str = new StringBuilder();
            if (soloOnly.Count > 0)
            {
                str.AppendLine("**" + Translation.GetTranslation(guild, "gameModuleSoloOnly") + "**");
                AppendHelp(soloOnly, str, isChanNsfw, guild);
            }
            if (multiOnly.Count > 0)
            {
                str.AppendLine("**" + Translation.GetTranslation(guild, "gameModuleMultiOnly") + "**");
                AppendHelp(multiOnly, str, isChanNsfw, guild);
            }
            if (both.Count > 0)
            {
                str.AppendLine("**" + Translation.GetTranslation(guild, "gameModuleBoth") + "**");
                AppendHelp(both, str, isChanNsfw, guild);
            }
            str.AppendLine(Translation.GetTranslation(guild, "gameModuleReset"));
            str.AppendLine(Translation.GetTranslation(guild, "gameModuleScore"));
            str.AppendLine(Translation.GetTranslation(guild, "gameModuleJoin"));
            str.AppendLine(Translation.GetTranslation(guild, "gameModuleLeave"));
            str.AppendLine(Translation.GetTranslation(guild, "gameModuleStart"));
            str.AppendLine(Environment.NewLine);
            str.AppendLine(Translation.GetTranslation(guild, "gameModuleNote"));
            str.AppendLine(Environment.NewLine);
            str.AppendLine(Translation.GetTranslation(guild, "gameModuleDifficulties"));
            str.AppendLine(Translation.GetTranslation(guild, "gameModuleDifficulties2"));
            str.AppendLine(Translation.GetTranslation(guild, "gameModuleDifficulties3"));
            str.AppendLine(Translation.GetTranslation(guild, "gameModuleDifficulties4"));
            str.AppendLine(Environment.NewLine);
            str.AppendLine(Translation.GetTranslation(guild, "gameModuleMultiHelp"));
            str.AppendLine(Translation.GetTranslation(guild, "gameModuleSoloHelp"));
            if (!isChanNsfw)
                str.AppendLine(Environment.NewLine + Translation.GetTranslation(guild, "nsfwForFull"));
            return str.ToString();
        }

        private static void AppendHelp(List<Tuple<APreload, string>> dict, StringBuilder str, bool isChanNsfw, IGuild guild)
        {
            foreach (var game in dict)
                if (isChanNsfw || (!game.Item1.IsNsfw() && !isChanNsfw))
                    str.AppendLine(Translation.GetTranslation(guild, game.Item2));
            str.AppendLine(Environment.NewLine);
        }

        public static async Task Anonymize(ulong guildId, bool value)
        {
            await Program.p.db.SetAnonymize(guildId, value);
        }

        [Command("Join")]
        public async Task Join(params string[] _)
        {
            if (Context.Guild == null)
            {
                await ReplyAsync(Modules.Base.Sentences.CommandDontPm(Context.Guild));
                return;
            }
            Utilities.CheckAvailability(Context.Guild, Program.Module.Game);
            await Program.p.DoAction(Context.User, Program.Module.Game);
            await ReplyAsync(Program.p.gm.JoinGame(Context.Guild, Context.Channel.Id, Context.User.Id));
        }

        [Command("Leave")]
        public async Task Leave(params string[] _)
        {
            if (Context.Guild == null)
            {
                await ReplyAsync(Modules.Base.Sentences.CommandDontPm(Context.Guild));
                return;
            }
            Utilities.CheckAvailability(Context.Guild, Program.Module.Game);
            await Program.p.DoAction(Context.User, Program.Module.Game);
            await ReplyAsync(Program.p.gm.LeaveGame(Context.Guild, Context.Channel.Id, Context.User.Id));
        }

        [Command("Start")]
        public async Task Start(params string[] _)
        {
            if (Context.Guild == null)
            {
                await ReplyAsync(Modules.Base.Sentences.CommandDontPm(Context.Guild));
                return;
            }
            Utilities.CheckAvailability(Context.Guild, Program.Module.Game);
            await Program.p.DoAction(Context.User, Program.Module.Game);
            string error = await Program.p.gm.StartGame(Context.Guild, Context.Channel.Id, Context.User.Id);
            if (error != null)
                await ReplyAsync(error);
        }

        [Command("Play", RunMode = RunMode.Async)]
        public async Task Play(params string[] args)
        {
            Utilities.CheckAvailability(Context.Guild, Program.Module.Game);
            await Program.p.DoAction(Context.User, Program.Module.Game);
            ITextChannel chan = (ITextChannel)Context.Channel;
            var error = await Program.p.gm.Play(args, chan, Context.User.Id);
            if (error != null)
                await ReplyAsync(error(Context.Guild));
        }

        [Command("Cancel")]
        public async Task Cancel(params string[] _)
        {
            Utilities.CheckAvailability(Context.Guild, Program.Module.Game);
            await Program.p.DoAction(Context.User, Program.Module.Game);
            if (Program.p.gm.Cancel(Context.Channel.Id))
                await ReplyAsync(Sentences.ResetDone(Context.Guild));
            else
                await ReplyAsync(Sentences.ResetNone(Context.Guild));
        }

        [Command("Score")]
        public async Task Score(params string[] _)
        {
            if (Context.Guild == null)
            {
                await ReplyAsync(Modules.Base.Sentences.CommandDontPm(Context.Guild));
                return;
            }
            Utilities.CheckAvailability(Context.Guild, Program.Module.Game);
            await Program.p.DoAction(Context.User, Program.Module.Game);
            var scores = await Program.p.db.GetAllScores();
            if (!scores.Any(x => x.Key == Context.Guild.Id.ToString()))
            {
                await ReplyAsync(Sentences.NoScore(Context.Guild));
                return;
            }
            var me = scores[Context.Guild.Id.ToString()];
            StringBuilder finalStr = new StringBuilder();
            float finalScore = 0;
            bool ranked = false;
            int nbGuilds = scores.Count(x => x.Value.Count > 0);
            foreach (var game in Constants.allRankedGames)
            {
                APreload preload = (APreload)Activator.CreateInstance(game.Item1);
                string gameName = preload.GetGameName();
                if (!me.ContainsKey(preload.GetGameName()))
                {
                    finalStr.Append("**" + preload.GetGameSentence(Context.Guild) + "**:" + Environment.NewLine +
                       Sentences.NotRanked(Context.Guild) + Environment.NewLine + Environment.NewLine);
                    continue;
                }
                ranked = true;
                string[] myElems = me[gameName].Split('|');
                var users = await Context.Guild.GetUsersAsync();
                int myScore = int.Parse(myElems[0]);
                string[] contributors = myElems.Skip(1).Select(x => users.Where(y => y.Id.ToString() == x).FirstOrDefault()?.ToString() ?? "(Unknown)").ToArray();
                int rankedNumber = scores.Where(x => Program.p.client.GetGuild(ulong.Parse(x.Key)) != null && x.Value.ContainsKey(gameName)).Count();
                int myRanking = scores.Where(x => Program.p.client.GetGuild(ulong.Parse(x.Key)) != null && x.Value.ContainsKey(gameName) && int.Parse(x.Value[gameName].Split('|')[0]) > myScore).Count() + 1;
                int bestScore = scores.Where(x => x.Value.ContainsKey(gameName)).Max(x => int.Parse(x.Value[gameName].Split('|')[0]));
                finalStr.Append("**" + preload.GetGameSentence(Context.Guild) + "**:" + Environment.NewLine +
                    Sentences.ScoreText(Context.Guild, myRanking, rankedNumber, myScore, bestScore) + Environment.NewLine +
                    Sentences.ScoreContributors(Context.Guild) + " " + string.Join(", ", contributors) + Environment.NewLine + Environment.NewLine);
                finalScore += myScore * 100f / bestScore;
            }
            int myGlobalRanking = 1;
            if (ranked)
            {
                foreach (var s in scores)
                {
                    int sScore = 0;
                    foreach (var elem in s.Value)
                    {
                        int best = scores.Where(x => x.Value.ContainsKey(elem.Key)).Max(x => int.Parse(x.Value[elem.Key].Split('|')[0]));
                        sScore += int.Parse(elem.Value.Split('|')[0]) * 100 / best;
                    }
                    if (sScore > finalScore)
                        myGlobalRanking++;
                }
            }
            await ReplyAsync((ranked ? Sentences.GlobalRanking(Context.Guild, myGlobalRanking, nbGuilds, finalScore / Constants.allRankedGames.Length)
                : Sentences.NoGlobalRanking(Context.Guild)) + Environment.NewLine + Environment.NewLine +
                finalStr.ToString());
        }
    }
}
