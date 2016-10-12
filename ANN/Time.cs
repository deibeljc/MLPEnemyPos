// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Time.cs" company="ANN">
//     Copyright (c) ANN. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace MLPEnemyPos
{
    using System;

    using LeagueSharp;

    public static class Time
    {
        public static TimeSpan Current => TimeSpan.FromSeconds(Game.ClockTime);
    }
}