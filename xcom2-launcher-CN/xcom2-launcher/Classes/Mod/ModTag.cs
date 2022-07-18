﻿using System;
using System.Drawing;

namespace XCOM2Launcher.Mod
{
    public class ModTag
    {
        private Color _bgColor;

        public ModTag()
        {
        }

        public ModTag(string label, Color? color = null)
        {
            Color = color ?? RandomColor();
            Label = label;
        }

        public string Label { get; set; } = "新标签";

        public Color Color
        {
            get => _bgColor;
            set
            {
                _bgColor = value;
                double L = (0.299 * value.R + 0.587 * value.G + 0.114 * value.B) / 255;

                if (L > 0.5)
                    TextColor = Color.Black;
                else
                    TextColor = Color.White;
            }
        }

        public Color TextColor { get; private set; }

        public static Color RandomColor()
        {
            var newColor = Color.Black.GetRandom(0.75, 0.9);
            var random = new Random(newColor.B);

            return random.NextDouble() <= 0.5 ? newColor.GetPastelShade() : newColor;
        }
    }
}