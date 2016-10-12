// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SnapshotEntry.cs" company="ANN">
//     Copyright (c) ANN. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace MLPEnemyPos
{
    using System.Security;

    using LeagueSharp;
    using LeagueSharp.Common;

    using Newtonsoft.Json;

    [SecuritySafeCritical]
    public class SnapshotEntry
    {
        public SnapshotEntry()
        {
            this.Time = Game.ClockTime;
            this.Player = new HeroInfo(ObjectManager.Player);
            this.Enemies = new HeroInfo[HeroManager.Enemies.Count];

            // fill enemies snapshots
            for (var index = 0; index < HeroManager.Enemies.Count; index++)
            {
                var hero = HeroManager.Enemies[index];

                // don't collect hero data on hidden units
                if (!hero.IsVisible)
                {
                    continue;
                }

                this.Enemies[index] = new HeroInfo(hero);
            }
        }

        [JsonProperty(PropertyName = "enemies")]
        public HeroInfo[] Enemies { get; }

        [JsonProperty(PropertyName = "player")]
        public HeroInfo Player { get; }

        [JsonProperty(PropertyName = "time")]
        public float Time { get; }
    }
}