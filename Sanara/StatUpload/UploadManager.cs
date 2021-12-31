﻿namespace Sanara.StatUpload
{
    public class UploadManager
    {
        public UploadManager(string url, string token)
        {
            _url = url;
            _token = token;
            _isSendingStats = false;
        }

        private bool _isSendingStats;

        public void KeepSendStats()
        {
            if (_isSendingStats)
                return;
            _isSendingStats = true;
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(60000); // 1 minute

                    await UpdateElement(new Tuple<string, string>[] {
                        new Tuple<string, string>("serverCount", StaticObjects.Client.Guilds.Count.ToString())
                    });
                }
            });
        }

        /// <summary>
        /// Called when an user did a command
        /// </summary>
        /// <param name="name">Command used</param>
        public async Task AddNewCommandAsync(string name)
        {
            string botName =
#if NSFW_BUILD
                "Sanara";
#else
                "Hanaki";
#endif
            await UpdateElement(new Tuple<string, string>[] { new Tuple<string, string>("nbMsgs", botName) });
            await UpdateElement(new Tuple<string, string>[] { new Tuple<string, string>("errors", "OK") });
            await UpdateElement(new Tuple<string, string>[] { new Tuple<string, string>("commands", name) });
        }

        /// <summary>
        /// Called when an error occured
        /// </summary>
        public async Task AddErrorAsync(System.Exception e)
        {
            await UpdateElement(new Tuple<string, string>[] { new Tuple<string, string>("errors", e.GetType().ToString()) });
        }

        /// <summary>
        /// Called when a game was played
        /// </summary>
        /// <param name="name">Name of the game</param>
        /// <param name="option">"Options" of the game (for image based game, if shaded, etc...)</param>
        public async Task AddGameAsync(string name, string option)
        {
            await UpdateElement(new Tuple<string, string>[] { new Tuple<string, string>("games", name + (option == null ? "" : "-" + option)) });
        }

        /// <summary>
        /// Called when a game was played
        /// </summary>
        /// <param name="name">Name of the game</param>
        /// <param name="option">"Options" of the game (for image based game, if shaded, etc...)</param>
        /// <param name="playerCount">Number of player</param>
        public async Task AddGamePlayerAsync(string name, string option, int playerCount)
        {
            await UpdateElement(new Tuple<string, string>[] { new Tuple<string, string>("gamesPlayers", name + (option == null ? "" : "-" + option) + ";" + playerCount.ToString()) });
        }

        public async Task AddBooruAsync(string name)
        {
            await UpdateElement(new Tuple<string, string>[] { new Tuple<string, string>("booru", name) });
        }

        private async Task UpdateElement(Tuple<string, string>[] elems)
        {
            var values = new Dictionary<string, string> {
                           { "token", _token },
                           { "action", "add" },
                           { "name", "Sanara" }
                        };
            foreach (var elem in elems)
            {
                values.Add(elem.Item1, elem.Item2);
            }
            HttpRequestMessage msg = new(HttpMethod.Post, _url);
            msg.Content = new FormUrlEncodedContent(values);

            try
            {
                await StaticObjects.HttpClient.SendAsync(msg);
            }
            catch (HttpRequestException) // TODO: We should probably retry
            { }
            catch (TaskCanceledException)
            { }
        }

        private readonly string _url;
        private readonly string _token;
    }
}
