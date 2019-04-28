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
namespace SanaraV2.Games
{
    public struct Config
    {
        public Config(int refTime, Difficulty difficulty, string gameName, bool isFull)
        {
            this.refTime = refTime;
            this.difficulty = difficulty;
            this.gameName = gameName;
            this.isFull = isFull;
        }

        public int refTime; // Time before the counter end and the player loose
        public Difficulty difficulty;
        public string gameName; // Used to store the score in the db
        public bool isFull;
    }

    public enum Difficulty // Easy mode give twice more time
    {
        Normal = 1,
        Easy
    }
}
