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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SanaraV2.Features
{
    public static class Utilities
    {
        public static string RemoveHTML(string html)
        {
            html = Regex.Replace(html, "<br ?\\/>", "\n");
            html = Regex.Replace(html, "<strong>([^<]+)<\\/strong>", "**$1**");
            html = Regex.Replace(html, "<[^>]+>([^<]+)<\\/[^>]+>", "$1");
            html = Regex.Replace(html, "<[^<]+>", "");
            return html;
        }

        /// <summary>
        /// Discord messages must be less than 2048 characters
        /// This function allow to "properly cut" them to remove the excess of characters
        /// (Properly mean that the text is cut by new lines)
        /// </summary>
        /// <param name="text">The text to clean</param>
        public static string RemoveExcess(string text)
        {
            if (text == null)
                return null;
            while (text.Length > 2048)
            {
                string[] tmp = text.Split('\n');
                text = string.Join(", ", tmp.Take(tmp.Length - 1));
            }
            return text;
        }

        /// <summary>
        /// Every commands take a string[] in parameter so they can be called with any number of arguments.
        /// This function transform it to a string adding spaces between each elements of the array
        /// </summary>
        /// <param name="args">The string[] to deal with</param>
        public static string AddArgs(string[] args)
        {
            if (args == null || args.Length == 0)
                return "";
            return string.Join(" ", args);
        }

        /// <summary>
        /// Generate a random code containing numbers
        /// </summary>
        /// <param name="nbDigits">The number of digits in the code</param>
        public static string GenerateRandomCode(int nbDigits, Random r)
        {
            StringBuilder code = new StringBuilder();
            for (int i = 0; i < nbDigits; i++)
                code.Append(r.Next(10));
            return code.ToString();
        }
        
        /// <summary>
        /// For comparaisons between 2 string it's sometimes useful that you remove everything except number and letters
        /// </summary>
        /// <param name="word">The string to deal with</param>
        public static string CleanWord(string word)
        {
            if (word == null)
                return null;
            string finalStr = "";
            foreach (char c in word)
            {
                if (char.IsLetterOrDigit(c))
                    finalStr += char.ToUpper(c);
            }
            return finalStr;
        }
        
        /// <summary>
        /// Get a language in 2 letters (ex: fr for french)
        /// </summary>
        /// <param name="languageName">Language string</param>
        public static string GetLanguage(string languageName, Dictionary<string, List<string>> allLanguages)
        {
            string lang = null;
            languageName = languageName.ToLower();
            if (allLanguages.ContainsKey(languageName))
                lang = languageName;
            foreach (var key in allLanguages)
            {
                if (key.Value.Contains(languageName))
                {
                    lang = key.Key;
                    break;
                }
            }
            return lang;
        }
        
        /// <summary>
        /// Get a language name from 2 letters (ex: french for fr)
        /// </summary>
        /// <param name="languageName">Language string</param>
        public static string GetFullLanguage(string languageName, Dictionary<string, List<string>> allLanguages)
        {
            languageName = languageName.ToLower();
            if (allLanguages.ContainsKey(languageName))
                return allLanguages[languageName][0];
            return languageName;
        }

        /// <summary>
        /// Check if an URL is valid
        /// </summary>
        /// <param name="url">The URL to check</param>
        public static bool IsLinkValid(string url)
        {
            if (url.StartsWith("http://") || url.StartsWith("https://"))
            {
                try
                {
                    WebRequest request = WebRequest.Create(url);
                    request.Method = "HEAD";
                    request.GetResponse();
                    return true;
                }
                catch (WebException)
                {
                    return false;
                }
            }
            return false;
        }

        public static async Task<ITextChannel> GetTextChannelAsync(string name, IGuild guild)
        {
            Match match = Regex.Match(name, "<#([0-9]{18})>");
            if (match.Success)
            {
                ITextChannel chan = await guild.GetTextChannelAsync(ulong.Parse(match.Groups[1].Value));
                if (chan != null)
                    return (chan);
            }
            if (ulong.TryParse(name, out ulong id2))
            {
                ITextChannel chan = await guild.GetTextChannelAsync(id2);
                if (chan != null)
                    return (chan);
            }
            foreach (ITextChannel chan in await guild.GetTextChannelsAsync())
            {
                if (chan.Name == name)
                    return (chan);
            }
            return (null);
        }
    }
}
