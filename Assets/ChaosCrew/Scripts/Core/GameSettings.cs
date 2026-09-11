using UnityEngine;

namespace ChaosCrew
{
    public enum RolePreference
    {
        Random,
        AlwaysCrew,
        AlwaysSaboteur
    }

    /// <summary>Player-facing options, persisted with PlayerPrefs.</summary>
    public sealed class GameSettings
    {
        private const string KeyMuted = "cc_muted";
        private const string KeyVolume = "cc_volume";
        private const string KeyRole = "cc_role_pref";
        private const string KeyName = "cc_name";

        public bool Muted;
        public float Volume = 0.7f;
        /// <summary>Test convenience: forces the local role so both sides can be checked quickly.</summary>
        public RolePreference RolePreference = RolePreference.Random;
        public string PlayerName = "Du";

        public void Load()
        {
            Muted = PlayerPrefs.GetInt(KeyMuted, 0) == 1;
            Volume = PlayerPrefs.GetFloat(KeyVolume, 0.7f);
            RolePreference = (RolePreference)PlayerPrefs.GetInt(KeyRole, 0);
            PlayerName = PlayerPrefs.GetString(KeyName, "Du");
        }

        public void Save()
        {
            PlayerPrefs.SetInt(KeyMuted, Muted ? 1 : 0);
            PlayerPrefs.SetFloat(KeyVolume, Volume);
            PlayerPrefs.SetInt(KeyRole, (int)RolePreference);
            PlayerPrefs.SetString(KeyName, PlayerName);
            PlayerPrefs.Save();
        }
    }
}
