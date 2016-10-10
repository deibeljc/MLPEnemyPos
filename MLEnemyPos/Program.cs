using LeagueSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using LeagueSharp.Common;
using SharpDX;

namespace MLPEnemyPos {
    class Program {

        private static Dictionary<String, HeroInfo> prevPos = new Dictionary<String, HeroInfo>();

        struct HeroInfo {
            public Vector3 Position;
            public float HealthPercent;
            public Vector3 Direction;
            public float Distance;
            public int CountAlliesInRange;
            public int CountEnemiesInRange;
            public int Level;
            public float Experience;
            public int CanMove;
            public int CanAttack;
            public int UnderAllyTurret;
            public float ManaPercent;
            public float MoveSpeed;
            public List<Vector3> allHeroesPos;
        }

        static void Main(string[] args) {
            Game.OnUpdate += OnUpdate;
            // Run this code every 100 milliseconds... hopefully it works :D
            var timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(UpdateEnemyPos);
            timer.Interval = 500;
            timer.Enabled = true;
            
        }

        private static void WriteToFile(String path, String text) {
            if (File.Exists(path)) {
                TextWriter tw = new StreamWriter(path, true);
                tw.WriteLine(text);
                tw.Close();
            }
            else {
                File.Create(path);
                TextWriter tw = new StreamWriter(path, true);
                tw.WriteLine(text);
                tw.Close();
            }
        }

        /**
         * TODO: Things to add:
         *  - Ensure that each player's position remains in order!!!
         *  - Exclude positon when players are backing
         */

        private static HeroInfo CopyHero(Obj_AI_Hero hero) {
            HeroInfo retHero = new HeroInfo();
            retHero.Position = hero.ServerPosition;
            retHero.HealthPercent = hero.HealthPercent;
            retHero.Direction = hero.Direction;
            retHero.Distance = hero.Distance(HeroManager.Player);
            retHero.CountAlliesInRange = hero.CountAlliesInRange(2000);
            retHero.CountEnemiesInRange = hero.CountEnemiesInRange(2000);
            retHero.Level = hero.Level;
            retHero.Experience = hero.Experience;
            retHero.CanMove = Convert.ToInt32(hero.CanMove);
            retHero.CanAttack = Convert.ToInt32(hero.CanAttack);
            retHero.UnderAllyTurret = Convert.ToInt32(hero.UnderAllyTurret());
            retHero.ManaPercent = hero.ManaPercent;
            retHero.MoveSpeed = hero.MoveSpeed;
            retHero.allHeroesPos = new List<Vector3>();
            foreach (var champ in HeroManager.AllHeroes) {
                retHero.allHeroesPos.Add(champ.ServerPosition);
            }
            return retHero;
        }

        private static String AllChampionPositions(String name) {
            String retString = "";
            foreach (var champion in prevPos[name].allHeroesPos) {
                retString += champion.X + ",";
                retString += champion.Y + ",";
            }
            return retString;
        }

        private static void UpdateEnemyPos(object sender, EventArgs e) {
            foreach (var enemy in HeroManager.Enemies) {
                if (prevPos.ContainsKey(enemy.Name) && enemy.IsVisible) {
                    Console.WriteLine("In Update Enemies!");
                    var path_X = @"C:\Users\Jon\Desktop\champion_movements\champions.X.txt";
                    var path_Y = @"C:\Users\Jon\Desktop\champion_movements\champions.Y.txt";
                    var features = prevPos[enemy.Name].Position.X + "," +
                                   prevPos[enemy.Name].Position.Y + "," +
                                   prevPos[enemy.Name].HealthPercent + "," +
                                   prevPos[enemy.Name].Distance + "," +
                                   prevPos[enemy.Name].CountAlliesInRange + "," +
                                   prevPos[enemy.Name].CountEnemiesInRange + "," +
                                   prevPos[enemy.Name].Level + "," +
                                   prevPos[enemy.Name].Experience + "," +
                                   prevPos[enemy.Name].CanMove + "," +
                                   prevPos[enemy.Name].CanAttack + "," +
                                   prevPos[enemy.Name].UnderAllyTurret + "," +
                                   prevPos[enemy.Name].ManaPercent + "," +
                                   prevPos[enemy.Name].MoveSpeed + "," +
                                   AllChampionPositions(enemy.Name) +
                                   enemy.ChampionName.GetHashCode();
                    var textToWriteX = features + "," + enemy.ServerPosition.X;
                    var textToWriteY = features + "," + enemy.ServerPosition.Y;
                    prevPos[enemy.Name] = CopyHero(enemy);
                    WriteToFile(path_X, textToWriteX);
                    WriteToFile(path_Y, textToWriteY);
                }
                if (!prevPos.ContainsKey(enemy.Name)) {
                    Console.WriteLine("In Init Enemies");
                    try {
                        prevPos[enemy.Name] = CopyHero(enemy);
                    }
                    catch (Exception exception) {
                        Console.WriteLine(exception);
                        throw;
                    }
                }
            }
        }


        private static void OnUpdate(EventArgs args) {
            // Do stuff in here eventually..
        }
    }
}
