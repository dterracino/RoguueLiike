﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace RogueLike
{
    public class MapTile
    {
        public Texture2D Texture { get; set; }
        public Texture2D EntityTexture { get; set; }
        public string EntityName { get; set; }
        public bool IsUnseen { get; set; } = true;
    }
}
