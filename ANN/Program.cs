using LeagueSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Timers;
using LeagueSharp.Common;
using SharpDX;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MLPEnemyPos {
    class Program {

        private static Dictionary<String, HeroInfo> prevPos = new Dictionary<String, HeroInfo>();
        private static Menu menu;

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
            public List<Obj_AI_Hero> allHeroesInfo;
        }

        static void Main(string[] args) {
            Game.OnUpdate += OnUpdate;
            // Run this code every 100 milliseconds... hopefully it works :D
            var timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(UpdateEnemyPos);
            timer.Interval = 1000;
            timer.Enabled = true;
            menu = new Menu("ANN", "ann", true);
            menu.AddItem(new MenuItem("sendData", "Send Data").SetValue(true));
            menu.AddItem(new MenuItem("debug", "Draw Debug").SetValue(false));
            menu.AddToMainMenu();
            Drawing.OnDraw += OnDraw;
        }

        private static void OnDraw(EventArgs args) {
            if (menu.Item("debug").IsActive()) {
                foreach (var enemy in HeroManager.Enemies) {
                    Render.Circle.DrawCircle(enemy.ServerPosition, 10f, System.Drawing.Color.Blue, 10);
                    Render.Circle.DrawCircle(prevPos[enemy.Name].Position, 10f, System.Drawing.Color.Red, 10);
                }
            }
        }

        private static async Task WriteToDB(Obj_AI_Hero enemy) {
            try {
                var httpWebRequest =
                    (HttpWebRequest) WebRequest.Create("https://mlpdb-f6531.firebaseio.com/mlpdata-feature-eng-2.json");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                using (var streamWriterX = new StreamWriter(httpWebRequest.GetRequestStream())) {
                    var features = "{\"posX\":\"" + prevPos[enemy.Name].Position.X + "\"," +
                                   "\"posY\":\"" + prevPos[enemy.Name].Position.Y + "\"," +
                                   "\"health\":\"" + prevPos[enemy.Name].HealthPercent + "\"," +
                                   "\"alliesInRange\":\"" + prevPos[enemy.Name].CountAlliesInRange + "\"," +
                                   "\"enemiesInRange\":\"" + prevPos[enemy.Name].CountEnemiesInRange + "\"," +
                                   "\"level\":\"" + prevPos[enemy.Name].Level + "\"," +
                                   "\"exp\":\"" + prevPos[enemy.Name].Experience + "\"," +
                                   "\"canMove\":\"" + prevPos[enemy.Name].CanMove + "\"," +
                                   "\"canAttack\":\"" + prevPos[enemy.Name].CanAttack + "\"," +
                                   "\"underAllyTurret\":\"" + prevPos[enemy.Name].UnderAllyTurret + "\"," +
                                   "\"manaPercent\":\"" + prevPos[enemy.Name].ManaPercent + "\"," +
                                   "\"moveSpeed\":\"" + prevPos[enemy.Name].MoveSpeed + "\"," +
                                   AllChampionPositions(enemy) +
                                   "\"champHash\":\"" + enemy.ChampionName.GetHashCode() + "\"";
                    var textToWriteX = features + ",\"enemyPredX\":\"" + enemy.ServerPosition.X + "\",\"enemyPredY\":\"" + enemy.ServerPosition.Y + "\"}";

                    streamWriterX.Write(textToWriteX);
                    streamWriterX.Flush();
                    streamWriterX.Close();
                }

                var httpResponse = (HttpWebResponse) httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                    var result = streamReader.ReadToEnd();
                    Console.WriteLine(result);
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
                throw;
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
            retHero.allHeroesInfo = new List<Obj_AI_Hero>();
            foreach (var champ in HeroManager.AllHeroes) {
                retHero.allHeroesInfo.Add(champ);
            }
            return retHero;
        }

        private static String AllChampionPositions(Obj_AI_Hero enemy) {
            String retString = "";
            var iter = 0;
            foreach (var champion in prevPos[enemy.Name].allHeroesInfo) {
                retString += "\"champ" + iter + "PosX\":\"" + champion.ServerPosition.X + "\",";
                retString += "\"champ" + iter + "PosY\":\"" + champion.ServerPosition.Y + "\",";
                retString += "\"champ" + iter + "Health\":\"" + champion.HealthPercent + "\",";
                retString += "\"champ" + iter + "AttackDamage\":\"" + champion.TotalAttackDamage + "\",";
                retString += "\"champ" + iter + "APDamage\":\"" + champion.TotalMagicalDamage + "\",";
                retString += "\"champ" + iter + "Level\":\"" + champion.Level + "\",";
                retString += "\"champ" + iter + "GoldTotal\":\"" + champion.GoldTotal + "\",";
                retString += "\"champ" + iter + "Gold\":\"" + champion.Gold + "\",";
                retString += "\"champ" + iter + "TotalScore\":\"" + champion.TotalPlayerScore + "\",";
                retString += "\"champ" + iter + "Score\":\"" + champion.Score + "\",";
                Vector3 pos = champion.ServerPosition;
                foreach (var hero in HeroManager.AllHeroes) {
                    if (hero.Name == champion.Name) {
                        pos = hero.ServerPosition;
                    }
                }
                retString += "\"champ" + iter + "Velocity\":\"" + Math.Sqrt(Math.Pow(champion.ServerPosition.X - pos.X, 2) + Math.Pow(champion.ServerPosition.Y - pos.Y, 2)) + "\",";
                retString += "\"champ" + iter + "DistanceDeltaX\":\"" + (champion.ServerPosition.X - pos.X) + "\",";
                retString += "\"champ" + iter + "DistanceDeltaY\":\"" + (champion.ServerPosition.Y - pos.Y) + "\",";
                iter++;
            }
            return retString;
        }

        private static String ChampionPath(Obj_AI_Hero enemy) {
            String retString = "";
            var iter = 0;
            if (enemy.Path.Length >= 5) {
                for (int i = enemy.Path.Length - 6; i < 5; i++) {
                    retString += "\"champPathX" + iter + "\":\"" + enemy.Path[i].X + "\",";
                    retString += "\"champPathY" + iter + "\":\"" + enemy.Path[i].Y + "\",";
                    iter++;
                }
            }
            else {
                for (int i = 0; i < 5; i++) {
                    retString += "\"champPathX" + iter + "\":\"" + (enemy.Path.Length - 1 <= i ? enemy.Path[i].X : -1) + "\",";
                    retString += "\"champPathY" + iter + "\":\"" + (enemy.Path.Length - 1 <= i ? enemy.Path[i].Y : -1) + "\",";
                    iter++;
                }
            }
            return retString;
        }

        private static void UpdateEnemyPos(object sender, EventArgs e) {
            if (menu.Item("sendData").IsActive() && HeroManager.AllHeroes.Count == 10) {
                foreach (var enemy in HeroManager.Enemies) {
                    if (prevPos.ContainsKey(enemy.Name) && enemy.IsVisible) {
                        prevPos[enemy.Name] = CopyHero(enemy);
                        try {
                            WriteToDB(enemy);
                        }
                        catch (Exception ex) {
                            Console.WriteLine(ex);
                            throw;
                        }
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
        }


        private static void OnUpdate(EventArgs args) {
            // Do stuff in here eventually..
        }
    }
}
