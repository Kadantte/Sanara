﻿using BooruSharp.Booru;
using Discord;
using Discord.WebSocket;
using Sanara.Exception;
using Sanara.Game.Preload;
using Sanara.Game.Preload.Result;

namespace Sanara.Game.Impl
{
    public abstract class QuizzBooru : Quizz
    {
        public QuizzBooru(IMessageChannel textChan, IUser user, IPreload preload, GameSettings settings) : base(textChan, user, preload, settings)
        {
            var info = new List<BooruQuizzPreloadResult>(preload.Load().Cast<BooruQuizzPreloadResult>())[0];
            _booru = info.Booru;
            _allowedFormats = info.AllowedFormats;
        }

        protected override Task CheckAnswerInternalAsync(SocketSlashCommand answer)
        {
            string userAnswer = Utils.CleanWord((string)answer.Data.Options.First(x => x.Name == "answer").Value);
            if (!_current.Answers.Any(x => Utils.CleanWord(x) == userAnswer))
                throw new InvalidGameAnswer("");
            return Task.CompletedTask;
        }

        protected override int GetGameTime()
            => 30;

        protected override string GetHelp()
        {
            var answer = _current.Answers[0].Replace('_', ' ');
            string answerHelp = char.ToUpper(answer[0]).ToString();
            foreach (var c in answer.Skip(1))
            {
                if (c == ' ')
                    answerHelp += c;
                else
                    answerHelp += "\\*";
            }
            return answerHelp;
        }

        protected ABooru _booru;
        protected string[] _allowedFormats;
    }
}
