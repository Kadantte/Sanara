﻿using Discord;
using Discord.WebSocket;
using Google;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sanara.Exception;
using Sanara.Help;
using System.Text.RegularExpressions;
using System.Web;

namespace Sanara.Module.Command.Impl
{
    public class Language : ISubmodule
    {
        public SubmoduleInfo GetInfo()
        {
            return new("Language", "Get information related to others languages");
        }

        public CommandInfo[] GetCommands()
        {
            return new[]
            {
                new CommandInfo(
                    slashCommand: new SlashCommandBuilder()
                    {
                        Name = "translate",
                        Description = "Translate a sentence",
                        Options = new()
                        {
                            new SlashCommandOptionBuilder()
                            {
                                Name = "language",
                                Description = "Target language (ISO 639-1)",
                                Type = ApplicationCommandOptionType.String,
                                IsRequired = true
                            },
                            new SlashCommandOptionBuilder()
                            {
                                Name = "sentence",
                                Description = "Sentence to translate",
                                Type = ApplicationCommandOptionType.String,
                                IsRequired = true
                            }
                        }
                    }.Build(),
                    callback: TranslateAsync,
                    precondition: Precondition.None,
                    needDefer: false
                ),
                new CommandInfo(
                    slashCommand: new SlashCommandBuilder()
                    {
                        Name = "urban",
                        Description = "Get the slang definition of a word",
                        Options = new()
                        {
                            new SlashCommandOptionBuilder()
                            {
                                Name = "word",
                                Description = "Word to get the meaning of",
                                Type = ApplicationCommandOptionType.String,
                                IsRequired = true
                            }
                        }
                    }.Build(),
                    callback: UrbanAsync,
                    precondition: Precondition.NsfwOnly,
                    needDefer: false
                ),
                new CommandInfo(
                    slashCommand: new SlashCommandBuilder()
                    {
                        Name = "kanji",
                        Description = "Get information about a kanji",
                        Options = new()
                        {
                            new SlashCommandOptionBuilder()
                            {
                                Name = "kanji",
                                Description = "Kanji to get information about",
                                Type = ApplicationCommandOptionType.String,
                                IsRequired = true
                            }
                        }
                    }.Build(),
                    callback: KanjiAsync,
                    precondition: Precondition.None,
                    needDefer: false
                ),
                new CommandInfo(
                    slashCommand: new SlashCommandBuilder()
                    {
                        Name = "japanese",
                        Description = "Get the definition of a word (Japanese only)",
                        Options = new()
                        {
                            new SlashCommandOptionBuilder()
                            {
                                Name = "word",
                                Description = "Word to get the definition of",
                                Type = ApplicationCommandOptionType.String,
                                IsRequired = true
                            }
                        }
                    }.Build(),
                    callback: JapaneseAsync,
                    precondition: Precondition.None,
                    needDefer: false
                ),
            };
        }

        public async Task TranslateAsync(SocketSlashCommand ctx)
        {
            if (StaticObjects.TranslationClient == null)
            {
                throw new CommandFailed("Translation client is not available");
            }

            var language = (string)ctx.Data.Options.First(x => x.Name == "language").Value;
            var sentence = (string)ctx.Data.Options.First(x => x.Name == "sentence").Value;

            if (StaticObjects.ISO639Reverse.ContainsKey(language))
                language = StaticObjects.ISO639Reverse[language];

            if (language.Length != 2)
                throw new CommandFailed("The language given must be in format ISO 639-1.");

            try
            {
                var translation = await StaticObjects.TranslationClient.TranslateTextAsync(sentence, language);
                await ctx.RespondAsync(embed: new EmbedBuilder
                {
                    Title = "From " + (StaticObjects.ISO639.ContainsKey(translation.DetectedSourceLanguage) ? StaticObjects.ISO639[translation.DetectedSourceLanguage] : translation.DetectedSourceLanguage),
                    Description = translation.TranslatedText,
                    Color = Color.Blue
                }.Build());
            }
            catch (GoogleApiException)
            {
                throw new CommandFailed("The language you provided is invalid.");
            }
        }

        public async Task UrbanAsync(SocketSlashCommand ctx)
        {
            var word = (string)ctx.Data.Options.First(x => x.Name == "word").Value;

            JObject json;
            try
            {
                json = JsonConvert.DeserializeObject<JObject>(await StaticObjects.HttpClient.GetStringAsync("http://api.urbandictionary.com/v0/define?term=" + HttpUtility.UrlEncode(word)));
            }
            catch (HttpRequestException re)
            {
                if (re.StatusCode == System.Net.HttpStatusCode.InternalServerError) // Somehow for some invalid query urbandictionary throws a 500
                {
                    throw new CommandFailed("There is no definition for this query.");
                }
                throw;
            }
            if (json["list"].Value<JArray>().Count == 0)
                throw new CommandFailed("There is no definition for this query.");

            int up = json["list"][0]["thumbs_up"].Value<int>();
            int down = json["list"][0]["thumbs_down"].Value<int>();

            if (up <= down)
            {
                throw new CommandFailed("There is no definition having a positive vote ratio for this query");
            }

            int gcd = Utils.GCD(up, down);

            string definition = json["list"][0]["definition"].Value<string>();
            if (definition.Length > 1000)
                definition = definition[..1000] + " [...]";
            string example = json["list"][0]["example"].Value<string>();
            if (example.Length > 1000)
                example = example[..1000] + " [...]";

            var outWord = json["list"][0]["word"].Value<string>();
            var embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = Utils.ToWordCase(outWord),
                Url = json["list"][0]["permalink"].Value<string>(),
                Fields = new()
                {
                    new EmbedFieldBuilder
                    {
                        Name = "Definition",
                        Value = definition
                    }
                },
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Up/Down vote ratio: {up / gcd} : {down / gcd}"
                }
            };
            if (example != "")
            {
                embed.AddField("Example", example);
            }
            await ctx.RespondAsync(embed: embed.Build());
        }


        public async Task KanjiAsync(SocketSlashCommand ctx)
        {
            var kanji = (string)ctx.Data.Options.First(x => x.Name == "kanji").Value;

            JObject json = JsonConvert.DeserializeObject<JObject>(await StaticObjects.HttpClient.GetStringAsync("https://jisho.org/api/v1/search/words?keyword=" + HttpUtility.UrlEncode(kanji)));
            if (json["data"].Value<JArray>().Count == 0 || json["data"][0]["japanese"][0]["word"] == null)
                throw new CommandFailed("I didn't find any kanji with this query.");

            char? finalKanji = null;

            // If player input is a kanji
            char playerChar = kanji[0];
            foreach (var elem in json["data"].Value<JArray>())
            {
                foreach (var elem2 in elem["japanese"].Value<JArray>())
                {
                    if (elem2["word"] != null && elem2["word"].Value<string>() == playerChar.ToString())
                    {
                        finalKanji = playerChar;
                    }
                }
            }

            // Else we take the first result we got on our Jisho search
            if (!finalKanji.HasValue)
                finalKanji = json["data"][0]["japanese"][0]["word"].Value<string>()[0];

            string url = "https://jisho.org/search/" + finalKanji + "%20%23kanji";
            string html = await StaticObjects.HttpClient.GetStringAsync(url);

            // Radical of the kanji
            var radicalMatch = Regex.Match(html, "<span class=\"radical_meaning\">([^<]+)<\\/span>([^<]+)<\\/span>");

            // Kanji meaning in english
            var meaning = Regex.Match(html, "<div class=\"kanji-details__main-meanings\">([^<]+)<\\/div>").Groups[1].Value.Trim();

            // All parts composing the kanji
            Dictionary<string, string> parts = new Dictionary<string, string>();
            foreach (var match in Regex.Matches(html, "<a href=\"(\\/\\/jisho\\.org\\/search\\/[^k]+kanji)\">([^<]+)<\\/a>").Cast<Match>())
            {
                string name = match.Groups[2].Value;
                if (name[0] == finalKanji.Value)
                    parts.Add(name, meaning);
                else
                    parts.Add(name, Regex.Match(await StaticObjects.HttpClient.GetStringAsync("https:" + match.Groups[1].Value), "<div class=\"kanji-details__main-meanings\">([^<]+)<\\/div>").Groups[1].Value.Trim());
            }

            // Onyomi and kunyomi (ways to read the kanji)
            Dictionary<string, string> onyomi = new();
            Dictionary<string, string> kunyomi = new();
            if (html.Contains("<dt>On:"))
                foreach (var match in Regex.Matches(html.Split(new[] { "<dt>On:" }, StringSplitOptions.None)[1]
                    .Split(new[] { "</dd>" }, StringSplitOptions.None)[0],
                    "<a[^>]+>([^<]+)<\\/a>").Cast<Match>())
                    onyomi.Add(match.Groups[1].Value, Utility.Language.ToRomaji(match.Groups[1].Value));
            if (html.Contains("<dt>Kun:"))
                foreach (var match in Regex.Matches(html.Split(new[] { "<dt>Kun:" }, StringSplitOptions.None)[1]
                    .Split(new string[] { "</dd>" }, StringSplitOptions.None)[0],
                "<a[^>]+>([^<]+)<\\/a>").Cast<Match>())
                    kunyomi.Add(match.Groups[1].Value, Utility.Language.ToRomaji(match.Groups[1].Value));

            await ctx.RespondAsync(embed: new EmbedBuilder
            {
                Title = finalKanji.Value.ToString(),
                // Url = url, // TODO: https://github.com/dotnet/runtime/issues/21626 , .NET said they will fix that in a next release
                Description = meaning,
                ImageUrl = "http://classic.jisho.org/static/images/stroke_diagrams/" + (int)finalKanji.Value + "_frames.png",
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder
                    {
                        Name = "Radical",
                        Value = radicalMatch.Groups[2].Value.Trim() + ": " + radicalMatch.Groups[1].Value.Trim()
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "Parts",
                        Value = string.Join("\n", parts.Select(x => x.Value == "" ? x.Key : x.Key + ": " + x.Value))
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "Onyomi",
                        Value = onyomi.Count == 0 ? "None" : string.Join("\n", onyomi.Select(x => x.Key + " (" + x.Value + ")"))
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "Kunyomi",
                        Value = kunyomi.Count == 0 ? "None" : string.Join("\n", kunyomi.Select(x => x.Key + " (" + x.Value + ")"))
                    },
                }
            }.Build());
        }

        public async Task JapaneseAsync(SocketSlashCommand ctx)
        {
            var word = (string)ctx.Data.Options.First(x => x.Name == "word").Value;

            JObject json = JsonConvert.DeserializeObject<JObject>(await StaticObjects.HttpClient.GetStringAsync("http://jisho.org/api/v1/search/words?keyword="
                + HttpUtility.UrlEncode(word)));
            var data = ((JArray)json["data"]).Select(x => x).ToArray();
            if (data.Length == 0)
                throw new CommandFailed("There is no result with this term search.");
            if (data.Length > 4)
                data = data.Take(5).ToArray();
            var embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = word,
                Url = "https://jisho.org/search/" + HttpUtility.UrlEncode(word)
            };
            foreach (var elem in data)
            {
                string title = string.Join(", ", elem["senses"][0]["english_definitions"].Value<JArray>().Select(x => x.Value<string>()));
                string content = string.Join('\n', elem["japanese"].Value<JArray>().Select(x =>
                {
                    var word = x["word"];
                    var reading = x["reading"];
                    if (word == null)
                        return reading.Value<string>() + $" ({Utility.Language.ToRomaji(reading.Value<string>())})";
                    if (reading == null)
                        return word.Value<string>();
                    return word.Value<string>() + " - " + reading.Value<string>() + $" ({Utility.Language.ToRomaji(reading.Value<string>())})";
                }));
                embed.AddField(title, content);
            }
            await ctx.RespondAsync(embed: embed.Build());
        }
    }

    //_submoduleHelp.Add("Language", "Get various information related to others languages");
    //_help.Add(("Tool", new Help("Language", "Translate", new[] { new Argument(ArgumentType.Mandatory, "language"), new Argument(ArgumentType.Mandatory, "sentence/image") }, "Translate a sentence to the given language.", Array.Empty<string>(), Restriction.None, "Translate en 空は青いです")));

    /*
        public static async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> msg, Cacheable<IMessageChannel, ulong> chan, SocketReaction react)
        {
            string emote = react.Emote.ToString();
            bool allowFlags = await chan.GetOrDownloadAsync() is ITextChannel textChan && StaticObjects.Db.GetGuild(textChan.GuildId).TranslateUsingFlags;
            // If emote is not from the bot and is an arrow emote
            if (allowFlags && react.User.IsSpecified && react.User.Value.Id != StaticObjects.ClientId && StaticObjects.Flags.ContainsKey(emote))
            {
                var gMsg = (await msg.GetOrDownloadAsync()).Content;
                if (!string.IsNullOrEmpty(gMsg))
                {
                    var translation = await StaticObjects.TranslationClient.TranslateTextAsync(gMsg, StaticObjects.Flags[emote]);
                    await (await chan.GetOrDownloadAsync()).SendMessageAsync(embed: new EmbedBuilder
                    {
                        Title = "From " + (StaticObjects.ISO639.ContainsKey(translation.DetectedSourceLanguage) ? StaticObjects.ISO639[translation.DetectedSourceLanguage] : translation.DetectedSourceLanguage),
                        Description = translation.TranslatedText,
                        Color = Color.Blue
                    }.Build());
                }
            }
        }

        //TODO
        /*
        [Command("Translate", RunMode = RunMode.Async)]
        public async Task TranslateAsync(string language)
        {
            if (StaticObjects.ISO639Reverse.ContainsKey(language))
                language = StaticObjects.ISO639Reverse[language];

            if (language.Length != 2)
                throw new CommandFailed("The language given must be in format ISO 639-1.");


            if (Context.Message.Attachments.Count == 0)
                throw new CommandFailed("No text or image was attached.");

            if (!Utils.IsImage(Path.GetExtension(Context.Message.Attachments.First().Url)))
                throw new CommandFailed("The file attached was not an image");

            try
            {
                var image = await Google.Cloud.Vision.V1.Image.FetchFromUriAsync(Context.Message.Attachments.ElementAt(0).Url);
                TextAnnotation response;
                try
                {
                    response = await StaticObjects.VisionClient.DetectDocumentTextAsync(image);
                }
                catch (AnnotateImageException)
                {
                    throw new CommandFailed("The file given isn't a valid image.");
                }
                if (response == null)
                    throw new CommandFailed("There is no text on the image.");

                var translation = await StaticObjects.TranslationClient.TranslateTextAsync(response.Text, language);
                await ReplyAsync(embed: new EmbedBuilder
                {
                    Title = "From " + (StaticObjects.ISO639.ContainsKey(translation.DetectedSourceLanguage) ? StaticObjects.ISO639[translation.DetectedSourceLanguage] : translation.DetectedSourceLanguage),
                    Description = translation.TranslatedText,
                    Color = Color.Blue
                }.Build());
            }
            catch (GoogleApiException)
            {
                throw new CommandFailed("The language you provided is invalid.");
            }
        }

    */
}