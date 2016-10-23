using LeagueSharp;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
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

        private static Dictionary<String, List<HeroInfo>> prevPos = new Dictionary<String, List<HeroInfo>>();
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
            public int IsRecalling; 
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
            timer.Interval = 500;
            timer.Enabled = true;
            menu = new Menu("ANN", "ann", true);
            menu.AddItem(new MenuItem("sendData", "Send Data").SetValue(true));
            menu.AddItem(new MenuItem("debug", "Draw Debug").SetValue(false));
            menu.AddToMainMenu();
            Drawing.OnDraw += OnDraw;
        }

        private static void OnDraw(EventArgs args) {
            if (menu.Item("debug").IsActive()) {
                foreach (var enemy in HeroManager.AllHeroes) {
                    Render.Circle.DrawCircle(enemy.ServerPosition, 10f, System.Drawing.Color.Blue, 10);
                    if (prevPos.ContainsKey(enemy.Name)) {
                        Render.Circle.DrawCircle(prevPos[enemy.Name][prevPos[enemy.Name].Count - 1].Position, 10f,
                            System.Drawing.Color.Red, 10);
                        Console.WriteLine("Drawing " + enemy.ChampionName);
                    }
                }
            }
        }

        private static async Task WriteToDB(Obj_AI_Hero enemy) {
            try {
                var httpWebRequest =
                    (HttpWebRequest) WebRequest.Create("https://mlpdb-f6531.firebaseio.com/mlpdata-feature-eng-5.json");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                using (var streamWriterX = new StreamWriter(httpWebRequest.GetRequestStream())) {
                    var prevInfo = prevPos[enemy.Name][prevPos[enemy.Name].Count - 1];
                    var prevPrevInfo = prevPos[enemy.Name][prevPos[enemy.Name].Count - 2];
                    var features = "{\"posX\":\"" + prevInfo.Position.X + "\"," +
                                   "\"posY\":\"" + prevInfo.Position.Y + "\"," +
                                   "\"health\":\"" + prevInfo.HealthPercent + "\"," +
                                   "\"healthDelta\":\"" + (prevPrevInfo.HealthPercent - prevInfo.HealthPercent) + "\"," +
                                   "\"alliesInRange\":\"" + prevInfo.CountAlliesInRange + "\"," +
                                   "\"enemiesInRange\":\"" + prevInfo.CountEnemiesInRange + "\"," +
                                   "\"level\":\"" + prevInfo.Level + "\"," +
                                   "\"exp\":\"" + prevInfo.Experience + "\"," +
                                   "\"canMove\":\"" + prevInfo.CanMove + "\"," +
                                   "\"canAttack\":\"" + prevInfo.CanAttack + "\"," +
                                   "\"underAllyTurret\":\"" + prevInfo.UnderAllyTurret + "\"," +
                                   "\"manaPercent\":\"" + prevInfo.ManaPercent + "\"," +
                                   "\"moveSpeed\":\"" + prevInfo.MoveSpeed + "\"," +
                                   AllChampionPositions(enemy) +
                                   ChampionPath(enemy) +
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
            foreach (var champion in prevPos[enemy.Name][prevPos[enemy.Name].Count - 1].allHeroesInfo) {
                retString += "\"champ" + iter + "PosX\":\"" + champion.ServerPosition.X + "\",";
                retString += "\"champ" + iter + "PosY\":\"" + champion.ServerPosition.Y + "\",";
                retString += "\"champ" + iter + "Health\":\"" + champion.HealthPercent + "\",";
                retString += "\"champ" + iter + "AttackDamage\":\"" + champion.TotalAttackDamage + "\",";
                retString += "\"champ" + iter + "APDamage\":\"" + champion.TotalMagicalDamage + "\",";
                retString += "\"champ" + iter + "Level\":\"" + champion.Level + "\",";
                retString += "\"champ" + iter + "GoldTotal\":\"" + champion.GoldTotal + "\",";
                retString += "\"champ" + iter + "Gold\":\"" + champion.Gold + "\",";
                // Get the previous recorded position.
                var pos = prevPos[enemy.Name][prevPos[enemy.Name].Count - 2].Position;
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
            if (prevPos[enemy.Name].Count >= 5) {
                for (int i = prevPos[enemy.Name].Count - 1; i < prevPos[enemy.Name].Count - 6; i--) {
                    retString += "\"champPathX" + iter + "\":\"" + prevPos[enemy.Name][i].Position.X + "\",";
                    retString += "\"champPathY" + iter + "\":\"" + prevPos[enemy.Name][i].Position.Y + "\",";
                    iter++;
                }
            }
            return retString;
        }

        private static void UpdateEnemyPos(object sender, EventArgs e) {
            if (menu.Item("sendData").IsActive() && HeroManager.AllHeroes.Count == 10) {
                foreach (var enemy in HeroManager.AllHeroes) {
                    if (prevPos.ContainsKey(enemy.Name) && enemy.IsVisible) {
                        try {
                            if (prevPos[enemy.Name].Count >= 5) {
                                WriteToDB(enemy);
                                prevPos[enemy.Name].Add(CopyHero(enemy));
                            }
                            else {
                                prevPos[enemy.Name].Add(CopyHero(enemy));
                            }
                        }
                        catch (Exception ex) {
                            Console.WriteLine(ex);
                            throw;
                        }
                    }

                    if (!prevPos.ContainsKey(enemy.Name)) {
                        Console.WriteLine("In Init Enemies");
                        try {
                            prevPos[enemy.Name] = new List<HeroInfo>();
                            prevPos[enemy.Name].Add(CopyHero(enemy));
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
