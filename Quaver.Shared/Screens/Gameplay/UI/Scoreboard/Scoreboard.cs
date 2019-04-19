/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 * Copyright (c) Swan & The Quaver Team <support@quavergame.com>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Xna.Framework;
using Quaver.API.Enums;
using Quaver.API.Helpers;
using Quaver.API.Maps.Processors.Rating;
using Quaver.Server.Client.Handlers;
using Quaver.Shared.Config;
using Quaver.Shared.Database.Maps;
using Quaver.Shared.Modifiers;
using Quaver.Shared.Online;
using Wobble.Graphics;
using Wobble.Logging;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace Quaver.Shared.Screens.Gameplay.UI.Scoreboard
{
    public class Scoreboard : Container
    {
        /// <summary>
        ///     The list of users on the scoreboard.
        /// </summary>
        public List<ScoreboardUser> Users { get; }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        internal Scoreboard(IEnumerable<ScoreboardUser> users)
        {
            Users = users.OrderBy(x => x.Processor.Health <= 0).ThenByDescending(x => x.RatingProcessor.CalculateRating(x.Processor.Accuracy)).ToList();
            SetTargetYPositions();

            Users.ForEach(x =>
            {
                x.Scoreboard = this;
                x.Y = x.TargetYPosition;
            });

            if (OnlineManager.CurrentGame != null)
                OnlineManager.Client.OnGameJudgements += OnGameJudgements;
        }

        /// <summary>
        /// </summary>
        public override void Destroy()
        {
            if (OnlineManager.CurrentGame != null)
                OnlineManager.Client.OnGameJudgements -= OnGameJudgements;

            base.Destroy();
        }

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnGameJudgements(object sender, GameJudgementsEventArgs e)
        {
            ScoreboardUser user = null;

            foreach (var u in Users)
            {
                if (u.LocalScore == null)
                    continue;

                if (u.LocalScore.PlayerId == e.UserId)
                {
                    user = u;
                    break;
                }
            }

            if (user == null)
                return;

            lock (user.Judgements)
            lock (user.Processor.CurrentJudgements)
            {
                foreach (var t in e.Judgements)
                {
                    user.Judgements.Add(t);
                    user.CalculateScoreForNextObject();
                }
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            var dt = gameTime.ElapsedGameTime.TotalMilliseconds;

            // Tween to target Y positions
            Users.ForEach(user =>
            {
                user.Y = MathHelper.Lerp(user.Y, user.TargetYPosition, (float) Math.Min(dt / 120, 1));

                // Tween X Position based on if the scoreboard is hidden
                if (ConfigManager.ScoreboardVisible.Value)
                    user.X = MathHelper.Lerp(user.X, 0, (float) Math.Min(dt / 120, 1));
                else
                    user.X = MathHelper.Lerp(user.X, -user.Width - 10, (float) Math.Min(dt / 120, 1));
            });

            base.Update(gameTime);
        }

        /// <summary>
        ///     Calculates scores for each user.
        /// </summary>
        internal void CalculateScores()
        {
            Users.ForEach(x =>
            {
                x.CalculateScoreForNextObject();
            });

            // Set each user's target position
            // Set Y positions.
            SetTargetYPositions();
        }

        /// <summary>
        ///     Sets the target y positions (where the scoreboard should move to)
        ///     Based on their rank.
        /// </summary>
        public void SetTargetYPositions()
        {
            List<ScoreboardUser> users;

            if (Users.First().Processor.MultiplayerProcessor != null)
            {
               users = Users
                    .OrderBy(x => x.HasQuit)
                    .ThenBy(x => x.Processor.MultiplayerProcessor.IsEliminated)
                    .ThenBy(x => x.Processor.MultiplayerProcessor.IsRegeneratingHealth)
                    .ThenByDescending(x => x.RatingProcessor.CalculateRating(x.Processor.Accuracy))
                    .ThenByDescending(x => x.Processor.Accuracy)
                    .ToList();
            }
            else
            {
                users = Users
                    .OrderBy(x => x.Processor.Health <= 0)
                    .ThenByDescending(x => x.RatingProcessor.CalculateRating(x.Processor.Accuracy))
                    .ThenByDescending(x => x.Processor.Accuracy)
                    .ToList();
            }

            for (var i = 0; i < users.Count; i++)
            {
                // Set new username and rank.
                users[i].Rank = i + 1;

                // Normalize the position of the first one so that all the rest will be completely in the middle.
                if (i == 0)
                {
                    users[i].TargetYPosition = users.Count * -users[i].Height / 2f;
                    continue;
                }

                users[i].TargetYPosition = users[i - 1].TargetYPosition + users[i].Height + 8;
            }
        }
    }
}
