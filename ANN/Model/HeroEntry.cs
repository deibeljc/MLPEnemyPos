// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HeroEntry.cs" company="ANN">
//     Copyright (c) ANN. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace ANN.Model
{
    using System.Security;

    using LeagueSharp;
    using LeagueSharp.Common;

    using Newtonsoft.Json;

    using SharpDX;

    [SecuritySafeCritical]
    public class HeroInfo
    {
        public HeroInfo(Obj_AI_Hero hero)
        {
            this.ChampionName = hero.ChampionName;
            this.Position = hero.ServerPosition;
            this.Direction = hero.Direction;
            this.Path = hero.Path;
            this.HealthPercent = hero.HealthPercent;
            this.ManaPercent = hero.ManaPercent;
            this.Experience = hero.Experience;
            this.MoveSpeed = hero.MoveSpeed;
            this.Level = hero.Level;
            this.IsDead = hero.IsDead;
            this.CanMove = hero.CanMove;
            this.CanAttack = hero.CanAttack;
            this.CountAlliesInRange = hero.CountAlliesInRange(2000);
            this.CountEnemiesInRange = hero.CountEnemiesInRange(2000);
            this.UnderAllyTurret = hero.UnderAllyTurret();

            if (hero.IsMe)
            {
                this.Distance = hero.Distance(ObjectManager.Player);
            }
        }

        [JsonProperty(PropertyName = "canAttack")]
        public bool CanAttack { get; }

        [JsonProperty(PropertyName = "canMove")]
        public bool CanMove { get; }

        [JsonProperty(PropertyName = "championName")]
        public string ChampionName { get; }

        [JsonProperty(PropertyName = "alliesInRange")]
        public int CountAlliesInRange { get; }

        [JsonProperty(PropertyName = "enemiesInRange")]
        public int CountEnemiesInRange { get; }

        [JsonProperty(PropertyName = "direction")]
        public Vector3 Direction { get; }

        [JsonProperty(PropertyName = "distance")]
        public float Distance { get; }

        [JsonProperty(PropertyName = "exp")]
        public float Experience { get; }

        [JsonProperty(PropertyName = "health")]
        public float HealthPercent { get; }

        [JsonProperty(PropertyName = "dead")]
        public bool IsDead { get; }

        [JsonProperty(PropertyName = "level")]
        public int Level { get; }

        [JsonProperty(PropertyName = "mana")]
        public float ManaPercent { get; }

        [JsonProperty(PropertyName = "moveSpeed")]
        public float MoveSpeed { get; }

        [JsonProperty(PropertyName = "path")]
        public Vector3[] Path { get; }

        [JsonProperty(PropertyName = "position")]
        public Vector3 Position { get; }

        [JsonProperty(PropertyName = "underAllyTurret")]
        public bool UnderAllyTurret { get; }
    }
}