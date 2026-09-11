using UnityEngine;

namespace ChaosCrew
{
    /// <summary>Bright, flat cartoon palette. Owns the game's whole visual identity.</summary>
    public static class Palette
    {
        public static Color Hex(string hex)
        {
            hex = hex.TrimStart('#');
            byte r = System.Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = System.Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = System.Convert.ToByte(hex.Substring(4, 2), 16);
            byte a = hex.Length >= 8 ? System.Convert.ToByte(hex.Substring(6, 2), 16) : (byte)255;
            return new Color32(r, g, b, a);
        }

        // Shell / UI
        public static readonly Color Backdrop    = Hex("1A1B33");
        public static readonly Color BackdropAlt = Hex("24264A");
        public static readonly Color Panel       = Hex("2E3160");
        public static readonly Color PanelSoft   = Hex("3A3D77");
        public static readonly Color Ink         = Hex("FFFFFF");
        public static readonly Color InkMuted    = Hex("A9AEE0");

        // Accents
        public static readonly Color Accent      = Hex("FFC93C");
        public static readonly Color AccentDeep  = Hex("F79F1F");
        public static readonly Color Good        = Hex("3DDC97");
        public static readonly Color Bad         = Hex("FF5A5F");
        public static readonly Color Chaos       = Hex("F72585");
        public static readonly Color Info        = Hex("4CC9F0");

        // Map
        public static readonly Color Wall        = Hex("3D2C55");
        public static readonly Color WallTop     = Hex("57406F");
        public static readonly Color DoorFrame   = Hex("8A5A3B");

        /// <summary>Player identity colours, index = seat.</summary>
        public static readonly Color[] Crew =
        {
            Hex("FF5A5F"), // Tomate
            Hex("4CC9F0"), // Himmel
            Hex("3DDC97"), // Minze
            Hex("FFC93C"), // Sonne
        };

        public static readonly string[] CrewColorNames = { "Tomate", "Himmel", "Minze", "Sonne" };

        public static Color Darken(Color c, float t) => Color.Lerp(c, Color.black, t);
        public static Color Lighten(Color c, float t) => Color.Lerp(c, Color.white, t);

        public static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
