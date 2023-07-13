using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Never Teamless", "Dana", "1.0.0")]
    [Description("No one goes without a team, even if it's a team of one.")]

    public class NeverTeamless : RustPlugin
    {
        #region Fields

        private Coroutine _teamFormationCoroutine;
        private static Configuration _config;
        private Timer _scanTimer;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Scan Interval Seconds")]
            public float ScanIntervalSeconds { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Detected changes in configuration! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Configuration update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                ScanIntervalSeconds = 300f
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void OnServerInitialized()
        {
            _scanTimer = timer.Every(_config.ScanIntervalSeconds, () =>
            {
                StartTeamFormationProcess();
            });
        }

        private void Unload()
        {
            _scanTimer?.Destroy();
            StopTeamFormationProcess();
            _config = null;
        }

        #endregion Oxide Hooks

        #region Functions

        private IEnumerator FormTeams()
        {
            WaitForSeconds waitDuration = ConVar.FPS.limit > 80 ? CoroutineEx.waitForSeconds(0.01f) : null;

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player.Team != null)
                    continue;

                RelationshipManager.PlayerTeam newTeam = RelationshipManager.ServerInstance.CreateTeam();
                newTeam.AddPlayer(player);
                yield return waitDuration;
            }
        }

        private void StartTeamFormationProcess()
        {
            _teamFormationCoroutine = ServerMgr.Instance.StartCoroutine(FormTeams());
        }

        private void StopTeamFormationProcess()
        {
            if (!_teamFormationCoroutine.IsUnityNull())
            {
                ServerMgr.Instance.StopCoroutine(_teamFormationCoroutine);
                _teamFormationCoroutine = null;
            }
        }

        #endregion Functions
    }
}
