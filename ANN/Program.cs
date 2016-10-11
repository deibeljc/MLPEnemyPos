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
            public List<Vector3> allHeroesPos;
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
            menu.AddToMainMenu();
        }

        private static async Task WriteToDB(Obj_AI_Hero enemy) {
            try {
                var httpWebRequest =
                    (HttpWebRequest) WebRequest.Create("https://mlpdb-f6531.firebaseio.com/mlpdata.json");
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
                                   AllChampionPositions(enemy.Name) +
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
            retHero.allHeroesPos = new List<Vector3>();
            foreach (var champ in HeroManager.AllHeroes) {
                retHero.allHeroesPos.Add(champ.ServerPosition);
            }
            return retHero;
        }

        private static String AllChampionPositions(String name) {
            String retString = "";
            var iter = 0;
            foreach (var champion in prevPos[name].allHeroesPos) {
                retString += "\"champ" + iter + "PosX\":\"" + champion.X + "\",";
                retString += "\"champ" + iter + "PosY\":\"" + champion.Y + "\",";
                iter++;
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
