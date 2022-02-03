using Discord;
using Sanara.Game.Impl;
using Sanara.Game.Preload.Result;
using System.Collections.ObjectModel;

namespace Sanara.Game.Preload.Impl
{
    public sealed class AnimeHardPreload : IPreload
    {
        public void Init()
        { }

        public ReadOnlyCollection<IPreloadResult> Load()
            => new BooruQuizzPreloadResult[]
            {
                new BooruQuizzPreloadResult(StaticObjects.Sakugabooru, new[] { ".mp4", ".webm" }, null, new string[] { null })
            }.Cast<IPreloadResult>().ToList().AsReadOnly();

        public string Name => "Anime Quizz (Hard)";

        public AGame CreateGame(IMessageChannel chan, IUser user, GameSettings settings)
            => new QuizzBooruAnime(chan, user, this, settings);

        public string GetRules()
            => "I'll post an extract from an anime, you'll have to give its name.\nCompared to normal version, there are more way data loaded into the bot";

        public bool IsSafe()
            => true;
    }
}
