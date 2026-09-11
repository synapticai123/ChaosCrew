using UnityEngine;

namespace ChaosCrew
{
    /// <summary>Colour script for the 3D office. Bright, saturated, daylight-cartoon.</summary>
    public static class Pal3D
    {
        public static Color Hex(string hex) => Palette.Hex(hex);

        // Shell
        public static readonly Color FloorTileA   = Hex("B7C7D8");
        public static readonly Color FloorTileB   = Hex("AABCCF");
        public static readonly Color FloorGrout   = Hex("A6B6C6");
        public static readonly Color CarpetBlue   = Hex("63769B");
        public static readonly Color FloorLift    = Hex("A3B4C8");
        public static readonly Color FloorWC      = Hex("9CBECD");
        public static readonly Color FloorKitchen = Hex("AFC6CE");
        public static readonly Color FloorStore   = Hex("C0B49E");

        // Walls
        public static readonly Color WallTeal     = Hex("5EC5D2");
        public static readonly Color WallDeep     = Hex("3E8FA5");
        public static readonly Color WallBlue     = Hex("8CA6C6");
        public static readonly Color WallWarm     = Hex("D9E2EA");
        public static readonly Color WallCap      = Hex("3E5C78");
        public static readonly Color WallCapLight = Hex("2F7C90");
        public static readonly Color CarpetDeep   = Hex("70819F");
        public static readonly Color WallTrim     = Hex("F2F6F9");

        // Materials
        public static readonly Color WoodLight    = Hex("C9A176");
        public static readonly Color WoodDark     = Hex("9A7248");
        public static readonly Color CabinetBlue  = Hex("3E6BA8");
        public static readonly Color CabinetDeep  = Hex("2E5183");
        public static readonly Color CounterWhite = Hex("EFF3F6");
        public static readonly Color MetalLight   = Hex("C3CAD2");
        public static readonly Color MetalDark    = Hex("7C868F");
        public static readonly Color PlasticDark  = Hex("2A3140");
        public static readonly Color PaperWhite   = Hex("F7F8FA");

        // Accents
        public static readonly Color Yellow       = Hex("F5C518");
        public static readonly Color YellowDeep   = Hex("D9A410");
        public static readonly Color Green        = Hex("3E9E58");
        public static readonly Color GreenDeep    = Hex("2E7A42");
        public static readonly Color Red          = Hex("E0483C");
        public static readonly Color Cyan         = Hex("3FD2F0");
        public static readonly Color WarmLight    = Hex("FFD9A0");
        public static readonly Color ScreenBlue   = Hex("6FC3E8");
        public static readonly Color Coffee       = Hex("5A3A22");
        public static readonly Color WaterBlue    = Hex("5FC8E8");
    }
}
