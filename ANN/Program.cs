// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="ANN">
//     Copyright (c) ANN. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace ANN
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Reflection;
    using System.Security;
    using System.Security.Permissions;
    using System.Text;
    using System.Threading.Tasks;

    using ANN.Helpers;
    using ANN.Model;

    using LeagueSharp;
    using LeagueSharp.Common;

    using log4net;

    using Newtonsoft.Json;

    using PlaySharp.Toolkit.Logging;

    internal class Program
    {
        private static readonly ILog Log = AssemblyLogs.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [SecuritySafeCritical]
        public Program()
        {
            // httpclient
            this.Client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            // menu
            this.Menu = new Menu("ANN", "ann", true);
            this.SendMenu = this.Menu.AddItem(new MenuItem("send", "Send Data").SetValue(true));
            this.Menu.AddToMainMenu();

            // activation
            this.SendMenu.ValueChanged += this.SendOnValueChanged;
            if (this.SendMenu.GetValue<bool>())
            {
                this.Activate();
            }
        }

        public static Program Instance { get; private set; }

        public Menu Menu { [SecuritySafeCritical] get; }

        public MenuItem SendMenu { [SecuritySafeCritical] get; }

        private HttpClient Client { get; }

        private string Endpoint { get; } = "https://mlpdb-f6531.firebaseio.com/mlpdata.json";

        private bool IsActive { get; set; }

        private TimeSpan LastSnapshot { get; set; }

        private TimeSpan SnapshotRate { get; } = TimeSpan.FromMilliseconds(500);

        private List<SnapshotEntry> Snapshots { get; } = new List<SnapshotEntry>();

        private int SnapshotsSendLimit { get; } = 10;

        [SecuritySafeCritical]
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        [SecuritySafeCritical]
        private static void OnGameLoad(EventArgs args)
        {
            if (HeroManager.AllHeroes.Count != 10)
            {
                Log.Warn($"Exiting -- expected total champion count to be 10.");
                return;
            }

            Instance = new Program();
        }

        [SecuritySafeCritical]
        private void Activate()
        {
            if (this.IsActive)
            {
                return;
            }

            this.IsActive = true;
            Game.OnUpdate += this.OnUpdate;
            Log.Debug($"[ANN] Activated");
        }

        [SecuritySafeCritical]
        private void Deactivate()
        {
            if (!this.IsActive)
            {
                return;
            }

            this.IsActive = false;
            Game.OnUpdate -= this.OnUpdate;
            Log.Debug($"[ANN] Deactivated");
        }

        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        private HttpContent GetSnapshotContent(IEnumerable<SnapshotEntry> snapshots)
        {
            // convert to json
            var json = JsonConvert.SerializeObject(snapshots, Formatting.Indented, new BooleanIntConverter());
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            return stringContent;

            // TODO: server retusn 401 - gzip not supported?
            // compress with gzip
            return new CompressedContent(stringContent, CompressionMethods.GZip);
        }

        [SecuritySafeCritical]
        private void OnUpdate(EventArgs args)
        {
            try
            {
                var delta = Time.Current - this.LastSnapshot;
                if (delta < this.SnapshotRate)
                {
                    return;
                }

                // take new snapshot
                this.LastSnapshot = Time.Current;
                this.Snapshots.Add(new SnapshotEntry());

                // send 
                if (this.Snapshots.Count > this.SnapshotsSendLimit)
                {
                    var dataToSend = this.Snapshots.ToArray();

                    Task.Factory.StartNew(
                            () =>
                            {
                                try
                                {
                                    this.SendSnapshots(dataToSend);
                                }
                                catch (Exception e)
                                {
                                    Log.Error(e);
                                }
                            });

                    this.Snapshots.Clear();
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        [SecuritySafeCritical]
        private void SendOnValueChanged(object sender, OnValueChangeEventArgs args)
        {
            if (args.GetNewValue<bool>())
            {
                this.Activate();
            }
            else
            {
                this.Deactivate();
            }
        }

        [SecuritySafeCritical]
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        private void SendSnapshots(IEnumerable<SnapshotEntry> snapshots)
        {
            try
            {
                var content = this.GetSnapshotContent(snapshots);

                // send and verify
                var result = this.Client.PostAsync(this.Endpoint, content).Result;
                result.EnsureSuccessStatusCode();

                // print result
                var resultContent = result.Content.ReadAsStringAsync().Result;
                Log.Debug($"Result: {resultContent}");
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}