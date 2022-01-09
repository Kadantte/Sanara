﻿using Discord;
using Sanara.Game.Impl;
using Sanara.Game.Preload.Impl.Static;
using Sanara.Game.Preload.Result;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace Sanara.Game.Preload.Impl
{
    public sealed class GirlsFrontlinePreload : IPreload
    {
        public void Init()
        {
            var cache = StaticObjects.Db.GetCacheAsync(Name).GetAwaiter().GetResult().ToList();
            // Item1 is name to be used in URL
            // Item2 is answer name
            foreach (var tDoll in GirlsFrontline.GetTDolls())
            {
                if (!cache.Any(x => x.id == tDoll.Item2))
                {
                    try
                    {
                        // Get URL
                        string shipUrl = "http://iopwiki.com/wiki/File:" + tDoll.Item1 + ".png";
                        string html = StaticObjects.HttpClient.GetStringAsync(shipUrl).GetAwaiter().GetResult();
                        Match m = Regex.Match(html, "src=\"(\\/images\\/thumb\\/[^\"]+)\"");

                        var result = new QuizzPreloadResult("http://iopwiki.com" + m.Groups[1].Value, new[] { tDoll.Item2 }); // Not sure if the Replace is necessary but it was here in the V2
                        StaticObjects.Db.SetCacheAsync(Name, result).GetAwaiter().GetResult();
                        cache.Add(result);
                    }
                    catch (System.Exception e)
                    {
                        _ = Log.LogErrorAsync(new System.Exception($"Error while preloading {tDoll.Item1}:\n" + e.Message, e), null);
                    }
                    Thread.Sleep(250); // We wait a bit to not spam the HTTP requests
                }
            }
            _preload = cache.ToArray();
        }

        public ReadOnlyCollection<IPreloadResult> Load()
            => _preload.Cast<IPreloadResult>().ToList().AsReadOnly();

        public string Name => "Girls Frontline";
        public string Description => "Find the name of an Girls Frontline character from an image";

        public AGame CreateGame(IMessageChannel chan, IUser user, GameSettings settings)
            => new Quizz(chan, user, this, settings);

        public string GetRules()
            => "I'll post an image of a t-doll, you'll have to give her name.";

        public bool IsSafe()
            => true;

        private QuizzPreloadResult[] _preload;
    }
}
