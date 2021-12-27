﻿using BooruSharp.Search;
using BooruSharp.Search.Post;
using Discord;
using Discord.WebSocket;
using Sanara.Exception;
using Sanara.Help;

namespace Sanara.Module.Nsfw
{
    public class BooruModule : ISubmodule
    {
        public SubmoduleInfo GetInfo()
        {
            return new("Booru", "Get anime images given some tags");
        }

        public CommandInfo[] GetCommands()
        {
            return new[]
            {
                new CommandInfo(
                    slashCommand: new SlashCommandBuilder()
                    {
                        Name = "booru",
                        Description = "Get an anime image",
                        Options = new()
                        {
                            new SlashCommandOptionBuilder()
                            {
                                Name = "tags",
                                Description = "Tags of the search, separated by an empty space",
                                Type = ApplicationCommandOptionType.String,
                                IsRequired = false
                            },
                            new SlashCommandOptionBuilder()
                            {
                                Name = "source",
                                Description = "Where the image is coming from",
                                Type = ApplicationCommandOptionType.Integer,
                                IsRequired = true,
                                Choices = new()
                                {
                                    new ApplicationCommandOptionChoiceProperties()
                                    {
                                        Name = "Safebooru (SFW images only)",
                                        Value = (int)BooruType.Safebooru
                                    }
                                }
                            }
                        }
                    }.Build(),
                    callback: BooruAsync,
                    precondition: Precondition.None
                )
            };
        }

        public async Task BooruAsync(SocketSlashCommand ctx)
        {
            var tags = ((string)ctx.Data.Options.FirstOrDefault(x => x.Name == "tags")?.Value ?? "").Split(' ');
            var type = (BooruType)(long)ctx.Data.Options.First(x => x.Name == "source").Value;

            var booru = type switch
            {
                BooruType.Safebooru => StaticObjects.Safebooru,
                _ => throw new NotImplementedException($"Invalid booru type {type}")
            };

            BooruSharp.Search.Post.SearchResult post;
            List<string> newTags = new();
            try
            {
                post = await booru.GetRandomPostAsync(tags);
            }
            catch (InvalidTags)
            {
                // On invalid tags we try to get guess which one the user wanted to use
                newTags = new List<string>();
                foreach (string s in tags)
                {
                    var related = await StaticObjects.Konachan.GetTagsAsync(s); // Konachan have a feature where it can "autocomplete" a tag so we use it to guess what the user meant
                    if (related.Length == 0)
                        throw new CommandFailed("There is no image with those tags.");
                    newTags.Add(related.OrderBy(x => Utils.GetStringDistance(x.Name, s)).First().Name);
                }
                try
                {
                    // Once we got our new tags, we try doing a new search with them
                    post = await booru.GetRandomPostAsync(newTags.ToArray());
                }
                catch (InvalidTags)
                {
                    // Might happens if the Konachan tags don't exist in the current booru
                    throw new CommandFailed("There is no image with those tags");
                }
            }

            int id = int.Parse("" + (int)type + post.ID);
            StaticObjects.Tags.AddTag(id, booru, post);

            if (post.FileUrl == null)
                throw new CommandFailed("A post was found but no image was available.");

            await ctx.RespondAsync(embed: new EmbedBuilder
            {
                Color = post.Rating switch
                {
                    Rating.Safe => Color.Green,
                    Rating.Questionable => new Color(255, 255, 0), // Yellow
                    Rating.Explicit => Color.Red,
                    _ => throw new NotImplementedException($"Invalid rating {post.Rating}")
                },
                ImageUrl = post.FileUrl.AbsoluteUri,
                Url = post.PostUrl.AbsoluteUri,
                Title = "From " + Utils.ToWordCase(booru.ToString().Split('.').Last()),
                Footer = new EmbedFooterBuilder
                {
                    Text = (newTags.Any() ? $"Some of your tags were invalid, the current search was done with: {string.Join(", ", newTags)}\n" : "") +
                        $"Do the 'Tags' command with then id '{id}' to have more information about this image."
                }
            }.Build());
        }
    }
}

/*

namespace SanaraV3.Help
{
    public sealed partial class HelpPreload
    {
        public void LoadBooruHelp()
        {
            _submoduleHelp.Add("Booru", "Get anime images given some tags");

#if NSFW_BUILD
            var categoryName = "Nsfw";
#else
            var categoryName = "Images";
#endif

            _help.Add((categoryName, new Help("Booru", "Safebooru", new[] { new Argument(ArgumentType.OPTIONAL, "tags") }, "Get a random image from Safebooru (only SFW images).", new string[0], Restriction.None, "Safebooru kantai_collection")));
            _help.Add((categoryName, new Help("Booru", "E926", new[] { new Argument(ArgumentType.OPTIONAL, "tags") }, "Get a random image from E926 (only SFW furry images).", new string[0], Restriction.None, "E926 short_hair")));
            _help.Add((categoryName, new Help("Booru", "Gelbooru", new[] { new Argument(ArgumentType.OPTIONAL, "tags") }, "Get a random image from Gelbooru (all kinds of anime images).", new string[0], Restriction.Nsfw, "Gelbooru cirno")));
            _help.Add((categoryName, new Help("Booru", "E621", new[] { new Argument(ArgumentType.OPTIONAL, "tags") }, "Get a random image from E621 (only furry images).", new string[0], Restriction.Nsfw, "E621 swimsuit")));
            _help.Add((categoryName, new Help("Booru", "Rule34", new[] { new Argument(ArgumentType.OPTIONAL, "tags") }, "Get a random image from Rule34 (mostly weird images).", new string[0], Restriction.Nsfw, "Rule34 cat_ears")));
            _help.Add((categoryName, new Help("Booru", "Konachan", new[] { new Argument(ArgumentType.OPTIONAL, "tags") }, "Get a random image from Konachan (only images that can be used as wallpaper).", new string[0], Restriction.Nsfw, "Konachan landscape")));
#if NSFW_BUILD
            _help.Add((categoryName, new Help("Booru", "Booru", new[] { new Argument(ArgumentType.OPTIONAL, "tags") }, "Get a random image from Safebooru or Gelbooru depending if you're on a NSFW or not.", new [] { "Image" }, Restriction.None, "Booru arknights")));
#endif
            _help.Add((categoryName, new Help("Booru", "Tags", new[] { new Argument(ArgumentType.MANDATORY, "id") }, "Get information about the tags of an image that was sent.", new string[0], Restriction.None, null)));
        }
    }
}

namespace SanaraV3.Module.Nsfw
{
    public sealed class BooruSfwModule : ModuleBase
    {
        [Command("Safebooru", RunMode = RunMode.Async)]
        public async Task SafebooruAsync(params string[] tags)
            => await BooruModule.SearchBooruAsync(StaticObjects.Safebooru, tags, BooruType.SAFEBOORU, Context.Channel);

        [Command("E926", RunMode = RunMode.Async)]
        public async Task E926Async(params string[] tags)
            => await BooruModule.SearchBooruAsync(StaticObjects.E926, tags, BooruType.E926, Context.Channel);

        [Command("Tags", RunMode = RunMode.Async)]
        public async Task TagsAsync(int id)
        {
            var info = await StaticObjects.Tags.GetTagAsync(id);
            if (!info.HasValue)
                throw new CommandFailed("There is no post with this id.");

            var res = info.Value;

            // If the post is SFW or if we are in a NSFW channel
            bool isPostAcceptable = res.Booru.IsSafe || (!res.Booru.IsSafe && Utils.CanSendNsfw(Context.Channel));
            int gcd = Utils.GCD(res.Post.Width, res.Post.Height);

            var embed = new EmbedBuilder
            {
                Color = BooruModule.RatingToColor(res.Post.Rating),
                Title = "From " + Utils.ToWordCase(res.Booru.ToString().Split('.').Last()),
                Description = res.Post.Width + " x " + res.Post.Height + "(" + (res.Post.Width / gcd) + ":" + (res.Post.Height / gcd) + ")",
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder
                    {
                        Name = "Artists",
                        Value = res.Artists.Count == 0 ? "Unkown" : "`" + string.Join(", ", res.Artists) + "`"
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "Characters",
                        Value = res.Characters.Count == 0 ? "Unkown" : "`" + string.Join(", ", res.Characters) + "`"
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "Sources",
                        Value = res.Sources.Count == 0 ? "Unkown" : "`" + string.Join(", ", res.Sources) + "`"
                    }
                }
            };

            if (isPostAcceptable)
                embed.Url = res.Post.PostUrl.AbsoluteUri;
            if (isPostAcceptable || res.Post.Rating == Rating.Safe)
                embed.ImageUrl = res.Post.PreviewUrl.AbsoluteUri;

            await ReplyAsync(embed: embed.Build());
        }
    }

    /// <summary>
    /// Contains all Booru related commands (booru are websites such as Gelbooru and Rule34)
    /// </summary>
    public sealed class BooruModule : ModuleBase
    {
        [Command("Gelbooru", RunMode = RunMode.Async), RequireNsfw]
        public async Task GelbooruAsync(params string[] tags)
            => await SearchBooruAsync(StaticObjects.Gelbooru, tags, BooruType.GELBOORU, Context.Channel);

        [Command("E621", RunMode = RunMode.Async), RequireNsfw]
        public async Task E621Async(params string[] tags)
            => await SearchBooruAsync(StaticObjects.E621, tags, BooruType.E621, Context.Channel);

        [Command("Rule34", RunMode = RunMode.Async), RequireNsfw]
        public async Task Rule34Async(params string[] tags)
            => await SearchBooruAsync(StaticObjects.Rule34, tags, BooruType.RULE34, Context.Channel);

        [Command("Konachan", RunMode = RunMode.Async), RequireNsfw]
        public async Task KonachanAsync(params string[] tags)
            => await SearchBooruAsync(StaticObjects.Konachan, tags, BooruType.KONACHAN, Context.Channel);

        [Command("Booru", RunMode = RunMode.Async), Alias("Image")]
        public async Task ImageAsync(params string[] tags)
        {
            if (Utils.CanSendNsfw(Context.Channel)) await SearchBooruAsync(StaticObjects.Gelbooru, tags, BooruType.GELBOORU, Context.Channel);
            else await SearchBooruAsync(StaticObjects.Safebooru, tags, BooruType.SAFEBOORU, Context.Channel);
        }

        public static async Task SearchBooruAsync(ABooru booru, string[] tags, BooruType booruId, IMessageChannel chan)
        {
            
        }
    }
}
*/