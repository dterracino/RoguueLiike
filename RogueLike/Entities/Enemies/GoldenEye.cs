﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using RogueLike.Levels;

namespace RogueLike.Enemies
{
    class GoldenEye : Enemy
    {
        public override string Name { get; set; } = "Golden_Eye";
        public override EnemyType EnemyType { get; } = EnemyType.Special;
        public override Texture2D Texture { get; set; }
        public override string TextureName { get; set; } = "enemy/golden_eye";
        public override Position Position { get; set; }
        public override int Health { get; set; } = 5;
        public override int MaxHealth { get; set; } = 5;
        public override int Attack { get; set; } = 10;
        public override int Shield { get; set; } = 0;
        public override Inventory.Inventory Inventory { get; set; } = new Inventory.Inventory();
        public override Equipment Equipment { get; set; } = new Equipment();
        public override int XPReward { get; set; } = 25;
        public override EnemyKI EnemyKi { get; } = new EnemyKI();
        public override LevelInfo Level { get; set; } = new Level0();
    }
}
