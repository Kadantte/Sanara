﻿using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SanaraV3.Exception;
using SanaraV3.Game.Preload;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace SanaraV3.Game.Impl
{
    /// <summary>
    /// Kancolle must find images in runtime
    /// </summary>
    public sealed class QuizzKancolle : Quizz
    {
        public QuizzKancolle(IMessageChannel textChan, IUser user, IPreload preload, GameSettings settings) : base(textChan, user, preload, settings)
        { }

        protected override string GetPostInternal()
        {
            base.GetPostInternal(); // Preload a character

            string name = _current.Answers[0];
            string shipUrl = "https://kancolle.fandom.com/wiki/" + name + "/Gallery";
            string html = StaticObjects.HttpClient.GetStringAsync(shipUrl).GetAwaiter().GetResult();
            return Regex.Match(html, "https:\\/\\/vignette\\.wikia\\.nocookie\\.net\\/kancolle\\/images\\/[0-9a-z]+\\/[0-9a-z]+\\/" + name + "_Full\\.png").Value;
        }

        protected override async Task CheckAnswerInternalAsync(string answer)
        {
            // We need to override this function because some KanColle ship have alternative names (like I-168 is also called Imuya)
            try
            {
                await base.CheckAnswerInternalAsync(answer);
            } catch (InvalidGameAnswer)
            {
                // If an error occured we check the wiki for alternative names
                var json = JsonConvert.DeserializeObject<JObject>(await StaticObjects.HttpClient.GetStringAsync("https://kancolle.fandom.com/api/v1/Search/List?query=" + HttpUtility.UrlEncode(answer) + "&limit=1"));
                if (json["items"].Value<JArray>().Count == 0 || json["items"][0]["title"].Value<string>() != _current.Answers[0])
                    throw;
            }
        }
    }
}