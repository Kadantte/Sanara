﻿using Discord;
using Discord.WebSocket;
using Sanara;
using Sanara.Help;
using Sanara.Module;
using System.Text.RegularExpressions;
using VndbSharp;
using VndbSharp.Models;

public class FunModule : ISubmodule
{
    public SubmoduleInfo GetInfo()
    {
        return new("Fun", "Various small entertainement commands");
    }

    public CommandInfo[] GetCommands()
    {
        return new[]
        {
                new CommandInfo(
                    slashCommand: new SlashCommandBuilder()
                    {
                        Name = "inspire",
                        Description = "Get a random \"inspirational\" quote"
                    }.Build(),
                    callback: InspireAsync,
                    precondition: Precondition.None,
                    needDefer: false
                ),
                new CommandInfo(
                    slashCommand: new SlashCommandBuilder()
                    {
                        Name = "vnquote",
                        Description = "Get a quote from a random Visual Novel"
                    }.Build(),
                    callback: VNQuoteAsync,
                    precondition: Precondition.NsfwOnly,
                    needDefer: false
                )
            };
    }

    public async Task InspireAsync(SocketSlashCommand ctx)
    {
        await ctx.RespondAsync(embed: new EmbedBuilder
        {
            Color = Color.Blue,
            ImageUrl = await StaticObjects.HttpClient.GetStringAsync("https://inspirobot.me/api?generate=true")
        }.Build());
    }

    public async Task VNQuoteAsync(SocketSlashCommand ctx)
    {
        var html = await StaticObjects.HttpClient.GetStringAsync("https://vndb.org");
        var match = Regex.Match(html, "footer\">\"<a href=\"\\/v([0-9]+)\"[^>]+>([^<]+)+<");
        var id = match.Groups[1].Value;
        var vn = (await StaticObjects.VnClient.GetVisualNovelAsync(VndbFilters.Id.Equals(uint.Parse(id)), VndbFlags.FullVisualNovel)).ToArray()[0];
        await ctx.RespondAsync(embed: new EmbedBuilder
        {
            Title = "From " + vn.Name,
            Url = "https://vndb.org/v" + id,
            Description = match.Groups[2].Value,
            Color = Color.Blue
        }.Build());
    }
}
/*

namespace SanaraV3.Help
{
    public sealed partial class HelpPreload
    {
        public void LoadFunHelp()
        {
            _submoduleHelp.Add("Fun", "Various small entertainement commands");
            _help.Add(("Entertainment", new Help("Fun", "Inspire", new Argument[0], "Get a random 'inspirational' quote using machine learning.", new string[0], Restriction.None, null)));
            _help.Add(("Entertainment", new Help("Fun", "Complete", new[] { new Argument(ArgumentType.OPTIONAL, "sentence") }, "Complete the given sentence using machine learning.", new string[0], Restriction.None, "Complete Why can't cats just")));
            _help.Add(("Entertainment", new Help("Fun", "Photo", new[] { new Argument(ArgumentType.OPTIONAL, "query") }, "Get a photo given some search terms, if none is provided, get a random one.", new string[0], Restriction.None, "Photo France")));
            _help.Add(("Entertainment", new Help("Fun", "VNQuote", new Argument[0], "Get a quote from a random Visual Novel.", new string[0], Restriction.Nsfw, null)));
        }
    }
}

namespace SanaraV3.Module.Entertainment
{
    public sealed class FunNsfwModule : ModuleBase
    {
        [Command("VNQuote"), RequireNsfw]
        public async Task VNQuote()
        {
            var html = await StaticObjects.HttpClient.GetStringAsync("https://vndb.org");
            var match = Regex.Match(html, "footer\">\"<a href=\"\\/v([0-9]+)\"[^>]+>([^<]+)+<");
            var id = match.Groups[1].Value;
            var vn = (await StaticObjects.VnClient.GetVisualNovelAsync(VndbFilters.Id.Equals(uint.Parse(id)), VndbFlags.FullVisualNovel)).ToArray()[0];
            await ReplyAsync(embed: new EmbedBuilder
            {
                Title = "From " + vn.Name,
                Url = "https://vndb.org/v" + id,
                Description = match.Groups[2].Value,
                Color = Color.Blue
            }.Build());
        }
    }

    /// <summary>
    /// All "Fun" commands that have no real purposes
    /// </summary>
    public sealed class FunModule : ModuleBase
    {
        [Command("Photo", RunMode = RunMode.Async)]
        public async Task PhotoAsync()
        {
            var json = JsonConvert.DeserializeObject<JObject>(await StaticObjects.HttpClient.GetStringAsync("https://api.unsplash.com/photos/random?client_id=" + StaticObjects.UnsplashToken));
            await PhotoAsync(json);
        }

        [Command("Photo", RunMode = RunMode.Async)]
        public async Task PhotoAsync([Remainder]string query)
        {
            var resp = await StaticObjects.HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.unsplash.com/photos/random?query=" + HttpUtility.UrlEncode(query) + "&client_id=" + StaticObjects.UnsplashToken));
            if (resp.StatusCode == HttpStatusCode.NotFound)
                throw new CommandFailed("There is no result with these search terms.");
            var json = JsonConvert.DeserializeObject<JObject>(await resp.Content.ReadAsStringAsync());
            await PhotoAsync(json);
        }

        private async Task PhotoAsync(JToken json)
        {
            await ReplyAsync(embed: new EmbedBuilder
            {
                Title = "By " + json["user"]["name"].Value<string>(),
                Url = json["links"]["html"].Value<string>(),
                ImageUrl = json["urls"]["full"].Value<string>(),
                Footer = new EmbedFooterBuilder
                {
                    Text = json["description"].Value<string>()
                },
                Color = Color.Blue
            }.Build());
        }

        [Command("Complete", RunMode = RunMode.Async)]
        public async Task CompleteAsync([Remainder]string sentence = "")
        {
            var embed = new EmbedBuilder
            {
                Description = "Please wait, this can take up to a few minutes...",
                Color = Color.Blue
            };
            var msg = await ReplyAsync(embed: embed.Build());
            var timer = DateTime.Now;
            var resp = await StaticObjects.HttpClient.PostAsync("https://api.eleuther.ai/complete", new StringContent("{\"context\":\"" + sentence.Replace("\"", "\\\"").Replace("\n", "\\n") + "\",\"top_p\":0.9,\"temp\":1}", Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            embed.Footer = new EmbedFooterBuilder
            {
                Text = $"Time elapsed: {(DateTime.Now - timer).TotalSeconds.ToString("0.00")}s"
            };
            var json = await resp.Content.ReadAsStringAsync();
            embed.Description = "**" + sentence + "**" + JsonConvert.DeserializeObject<JArray>(json)[0]["generated_text"].Value<string>();
            await msg.ModifyAsync(x => x.Embed = embed.Build());
        }
    }
}
*/