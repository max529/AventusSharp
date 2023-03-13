using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace AventusSharp.Log
{
    /// <summary>
    /// Predefined color for colorized prefix 
    /// </summary>
    public enum Color
    {
#pragma warning disable CS1591 // Commentaire XML manquant pour le type ou le membre visible publiquement
        None = -1,
        Red = 0xff3636,
        Yellow = 0xf9f1a5,
        Cyan = 0x3a96dd,
        Green = 0x1ed75f,

        BLACK = 0x000000,
        NAVY = 0x000080,
        DARKBLUE = 0x00008B,
        MEDIUMBLUE = 0x0000CD,
        BLUE = 0x0000FF,
        DARKGREEN = 0x006400,
        GREEN = 0x008000,
        TEAL = 0x008080,
        DARKCYAN = 0x008B8B,
        DEEPSKYBLUE = 0x00BFFF,
        DARKTURQUOISE = 0x00CED1,
        MEDIUMSPRINGGREEN = 0x00FA9A,
        LIME = 0x00FF00,
        SPRINGGREEN = 0x00FF7F,
        AQUA = 0x00FFFF,
        CYAN = 0x00FFFF,
        MIDNIGHTBLUE = 0x191970,
        DODGERBLUE = 0x1E90FF,
        LIGHTSEAGREEN = 0x20B2AA,
        FORESTGREEN = 0x228B22,
        SEAGREEN = 0x2E8B57,
        DARKSLATEGRAY = 0x2F4F4F,
        LIMEGREEN = 0x32CD32,
        MEDIUMSEAGREEN = 0x3CB371,
        TURQUOISE = 0x40E0D0,
        ROYALBLUE = 0x4169E1,
        STEELBLUE = 0x4682B4,
        DARKSLATEBLUE = 0x483D8B,
        MEDIUMTURQUOISE = 0x48D1CC,
        INDIGO = 0x4B0082,
        DARKOLIVEGREEN = 0x556B2F,
        CADETBLUE = 0x5F9EA0,
        CORNFLOWERBLUE = 0x6495ED,
        MEDIUMAQUAMARINE = 0x66CDAA,
        DIMGRAY = 0x696969,
        SLATEBLUE = 0x6A5ACD,
        OLIVEDRAB = 0x6B8E23,
        SLATEGRAY = 0x708090,
        LIGHTSLATEGRAY = 0x778899,
        MEDIUMSLATEBLUE = 0x7B68EE,
        LAWNGREEN = 0x7CFC00,
        CHARTREUSE = 0x7FFF00,
        AQUAMARINE = 0x7FFFD4,
        MAROON = 0x800000,
        PURPLE = 0x800080,
        OLIVE = 0x808000,
        GRAY = 0x808080,
        SKYBLUE = 0x87CEEB,
        LIGHTSKYBLUE = 0x87CEFA,
        BLUEVIOLET = 0x8A2BE2,
        DARKRED = 0x8B0000,
        DARKMAGENTA = 0x8B008B,
        SADDLEBROWN = 0x8B4513,
        DARKSEAGREEN = 0x8FBC8F,
        LIGHTGREEN = 0x90EE90,
        MEDIUMPURPLE = 0x9370DB,
        DARKVIOLET = 0x9400D3,
        PALEGREEN = 0x98FB98,
        DARKORCHID = 0x9932CC,
        YELLOWGREEN = 0x9ACD32,
        SIENNA = 0xA0522D,
        BROWN = 0xA52A2A,
        DARKGRAY = 0xA9A9A9,
        LIGHTBLUE = 0xADD8E6,
        GREENYELLOW = 0xADFF2F,
        PALETURQUOISE = 0xAFEEEE,
        LIGHTSTEELBLUE = 0xB0C4DE,
        POWDERBLUE = 0xB0E0E6,
        FIREBRICK = 0xB22222,
        DARKGOLDENROD = 0xB8860B,
        MEDIUMORCHID = 0xBA55D3,
        ROSYBROWN = 0xBC8F8F,
        DARKKHAKI = 0xBDB76B,
        SILVER = 0xC0C0C0,
        MEDIUMVIOLETRED = 0xC71585,
        INDIANRED = 0xCD5C5C,
        PERU = 0xCD853F,
        CHOCOLATE = 0xD2691E,
        TAN = 0xD2B48C,
        LIGHTGRAY = 0xD3D3D3,
        THISTLE = 0xD8BFD8,
        ORCHID = 0xDA70D6,
        GOLDENROD = 0xDAA520,
        PALEVIOLETRED = 0xDB7093,
        CRIMSON = 0xDC143C,
        GAINSBORO = 0xDCDCDC,
        PLUM = 0xDDA0DD,
        BURLYWOOD = 0xDEB887,
        LIGHTCYAN = 0xE0FFFF,
        LAVENDER = 0xE6E6FA,
        DARKSALMON = 0xE9967A,
        VIOLET = 0xEE82EE,
        PALEGOLDENROD = 0xEEE8AA,
        LIGHTCORAL = 0xF08080,
        KHAKI = 0xF0E68C,
        ALICEBLUE = 0xF0F8FF,
        HONEYDEW = 0xF0FFF0,
        AZURE = 0xF0FFFF,
        SANDYBROWN = 0xF4A460,
        WHEAT = 0xF5DEB3,
        BEIGE = 0xF5F5DC,
        WHITESMOKE = 0xF5F5F5,
        MINTCREAM = 0xF5FFFA,
        GHOSTWHITE = 0xF8F8FF,
        SALMON = 0xFA8072,
        ANTIQUEWHITE = 0xFAEBD7,
        LINEN = 0xFAF0E6,
        LIGHTGOLDENRODYELLOW = 0xFAFAD2,
        OLDLACE = 0xFDF5E6,
        RED = 0xFF0000,
        FUCHSIA = 0xFF00FF,
        MAGENTA = 0xFF00FF,
        DEEPPINK = 0xFF1493,
        ORANGERED = 0xFF4500,
        TOMATO = 0xFF6347,
        HOTPINK = 0xFF69B4,
        CORAL = 0xFF7F50,
        DARKORANGE = 0xFF8C00,
        LIGHTSALMON = 0xFFA07A,
        ORANGE = 0xFFA500,
        LIGHTPINK = 0xFFB6C1,
        PINK = 0xFFC0CB,
        GOLD = 0xFFD700,
        PEACHPUFF = 0xFFDAB9,
        NAVAJOWHITE = 0xFFDEAD,
        MOCCASIN = 0xFFE4B5,
        BISQUE = 0xFFE4C4,
        MISTYROSE = 0xFFE4E1,
        BLANCHEDALMOND = 0xFFEBCD,
        PAPAYAWHIP = 0xFFEFD5,
        LAVENDERBLUSH = 0xFFF0F5,
        SEASHELL = 0xFFF5EE,
        CORNSILK = 0xFFF8DC,
        LEMONCHIFFON = 0xFFFACD,
        FLORALWHITE = 0xFFFAF0,
        SNOW = 0xFFFAFA,
        YELLOW = 0xFFFF00,
        LIGHTYELLOW = 0xFFFFE0,
        IVORY = 0xFFFFF0,
        WHITE = 0xFFFFFF
#pragma warning restore CS1591 // Commentaire XML manquant pour le type ou le membre visible publiquement

    }

    /// <summary>
    /// 
    /// </summary>
    public static class ColorExtension
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        //private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        /// <summary>
        /// Get last Console Error
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        //private static bool _enabled;

        static ColorExtension()
        {
            //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            //{
            //    IntPtr iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);

            //    bool enable = GetConsoleMode(iStdOut, out uint outConsoleMode)
            //                 && SetConsoleMode(iStdOut, outConsoleMode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
            //}

            //if (Environment.GetEnvironmentVariable("NO_COLOR") == null)
            //{
            //    Enable();
            //}
            //else
            //{
            //    Disable();
            //}
        }

        ///// <summary>
        ///// Enables any future console color output produced by Pastel.
        ///// </summary>
        //public static void Enable()
        //{
        //    _enabled = true;
        //}

        ///// <summary>
        ///// Disables any future console color output produced by Pastel.
        ///// </summary>
        //public static void Disable()
        //{
        //    _enabled = false;
        //}

        /// <summary>
        /// Enable color capability for Windows Console
        /// </summary>
        public static void enableColor()
        {
            IntPtr iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            if (!GetConsoleMode(iStdOut, out uint outConsoleMode))
            {
                Console.WriteLine("failed to get output console mode");
                //Console.ReadKey();
                return;
            }

            outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;// | DISABLE_NEWLINE_AUTO_RETURN;
            if (!SetConsoleMode(iStdOut, outConsoleMode))
            {
                Console.WriteLine($"failed to set output console mode, error code: {GetLastError()}");
                //Console.ReadKey();
                return;
            }
        }

        /// <summary>
        /// Get the value of the RGB for <paramref name="color"/>
        /// </summary>
        /// <param name="color"></param>
        /// <param name="R"></param>
        /// <param name="G"></param>
        /// <param name="B"></param>
        public static void getRGB(Color color, out int R, out int G, out int B)
        {
            R = ((int)color & 0xff0000) >> 16;
            G = ((int)color & 0xff00) >> 8;
            B = (int)color & 0xff;
        }

        /// <summary>
        /// Get the Hue Saturation Value for <paramref name="color"/>
        /// </summary>
        /// <param name="color"></param>
        /// <param name="hue"></param>
        /// <param name="saturation"></param>
        /// <param name="value"></param>
        /// <param name="luminance"></param>
        // https://www.alanzucconi.com/2015/09/30/colour-sorting/
        // https://www.codeproject.com/Articles/9202/Creating-a-color-selection-palette-control
        public static void ColorToHSV(Color color, out double hue, out double saturation, out double value, out double luminance)
        {
            getRGB(color, out int Rx, out int Gx, out int Bx);

            double max = Math.Max(Rx, Math.Max(Gx, Bx));
            double min = Math.Min(Rx, Math.Min(Gx, Bx));

            double R = (double)Rx / 255;
            double G = (double)Gx / 255;
            double B = (double)Bx / 255;

            double Max = Math.Max(R, Math.Max(G, B));
            double Min = Math.Min(R, Math.Min(G, B));

            if (Max == R)
            {
                hue = (G - B) / (Max - Min);
            }
            else if (Max == G)
            {
                hue = 2.0 + ((B - R) / (Max - Min));
            }
            else
            {
                hue = 4.0 + ((R - G) / (Max - Min));
            }

            hue *= 60;
            if (hue < 0)
            {
                hue += 360;
            }

            saturation = (max == 0) ? 0 : 1d - (1d * min / max);
            value = max / 255d;
            luminance = Math.Sqrt((0.241 * Rx) + (0.691 * Gx) + (0.068 * Bx));
        }

        /// <summary>
        /// Function that sort the given colors
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int SortColors(Color x, Color y)
        {
            ColorToHSV(x, out double huex, out double saturationx, out double valuex, out double luminancex);
            ColorToHSV(y, out double huey, out double saturationy, out double valuey, out double luminancey);

            // 1 : hue
            if (huex < huey)
            {
                return -1;
            }
            else if (huex > huey)
            {
                return 1;
            }
            else
            {
                // 2 : saturation
                if (saturationx < saturationy)
                {
                    return -1;
                }
                else if (saturationx > saturationy)
                {
                    return 1;
                }
                else
                {
                    // 3 : brightness
                    if (valuex < valuey)
                    {
                        return -1;
                    }
                    else if (valuex > valuey)
                    {
                        return 1;
                    }
                    else
                    {
                        // 4 : Luminance
                        if (luminancex < luminancey)
                        {
                            return -1;
                        }
                        else if (luminancex > luminancey)
                        {
                            return 1;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                }
            }
        }

        private static void PrintColor(Color color)
        {
            string colorStr = "";
            if ((int)color >= 0)
            {
                int R = ((int)color & 0xff0000) >> 16;
                int G = ((int)color & 0xff00) >> 8;
                int B = (int)color & 0xff;
                colorStr = "\u001B[38;2;" + R + ";" + G + ";" + B + "m";
            }
            Console.Write(colorStr + color.ToString() + "  \x1b[0m");
        }

        /// <summary>
        /// Print all colors sorted by HSV values
        /// </summary>
        public static void printAllColors()
        {
            Array values = Enum.GetValues(typeof(Color));
            List<Color> colors = values.Cast<Color>().ToList();
            colors.Sort((x, y) => SortColors(x, y));

            foreach (Color color in colors)
            {
                PrintColor(color);
            }
        }
    }
}
