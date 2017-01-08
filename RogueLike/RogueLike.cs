﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RogueLike.Enemies;
using RogueLike.Levels;
using RogueLikeMapBuilder;

namespace RogueLike
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class RogueLike : Game
    {
        readonly GraphicsDeviceManager _graphics;
        SpriteBatch _spriteBatch;

        private const int TILE_WIDTH = 32;
        private const int TILE_HEIGHT = 32;
        private const int WIDTH = 150;
        private const int HEIGHT = 150;
        private const int FOV = 20;
        private const int PLAYER_FOV = 1; // 1 or 2 is best

        private const int ENEMY_AMOUNT = 150; // around 150 is a good number
        private const int ENEMY_RADIUS = 3;
        private int enemyCounter = 0;
        Random r = new Random();

        //Texture2D[,] _map = new Texture2D[WIDTH,HEIGHT];
        MapTile[,] _map = new MapTile[WIDTH,HEIGHT];

        private Player _player = new Player();
        readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        readonly Dictionary<string, Enemy> _enemies = new Dictionary<string, Enemy>();
        List<Enemy> _enemyTypes = new List<Enemy>();

        readonly InternalSettings _internalSettings = new InternalSettings();

        int _framesPassed = 0;

        public RogueLike()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = FOV*TILE_WIDTH;
            _graphics.PreferredBackBufferHeight = FOV*TILE_HEIGHT;
            _graphics.ApplyChanges();
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            _enemyTypes = GetEnemies(); // Load the Enemy classes

            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            
            /* Load Map textures */
            _textures.Add("floor", Content.Load<Texture2D>("floor/vines0"));
            _textures.Add("wall", Content.Load<Texture2D>("wall/vines0"));
            _textures.Add("unseen", Content.Load<Texture2D>("floor/unseen"));
            _textures.Add("blood_red", Content.Load<Texture2D>("enemy/additions/blood_red"));
            _textures.Add("blood_green", Content.Load<Texture2D>("enemy/additions/blood_green"));

            /* Load internal settings */
            _internalSettings.Cursor.Texture = Content.Load<Texture2D>("user/cursor");

            _player.Texture = Content.Load<Texture2D>("player/base/human_m");

            csMapbuilder mpbuild = new csMapbuilder(WIDTH, HEIGHT)
            {
                MaxRooms = 20
            };
            int[,] randomMap = new int[WIDTH,HEIGHT];
            if (mpbuild.Build_ConnectedStartRooms() == true)
            {
                randomMap = mpbuild.map;
            }

            bool playerPosSet = false;

            for (int i = 0; i < WIDTH; i++)
            {
                for (int j = 0; j < HEIGHT; j++)
                {
                    if (randomMap[i, j] == 0)
                    {
                        _map[i, j] = new MapTile()
                        {
                            Texture = _textures.FirstOrDefault(t => t.Key == "floor").Value
                        };
                        if (!playerPosSet && i > 50 && j > 50)
                        {
                            _player.Position = new Position(i*TILE_WIDTH, j*TILE_HEIGHT);
                            playerPosSet = true;
                            continue;
                        }
                        if (r.Next(0,8) == 1 && _map[i,j].EntityTexture == null && enemyCounter < ENEMY_AMOUNT && !IsEnemyNearby(i,j)) // around 10 is a good number
                        {
                            int enemyRandSelector = r.Next(0, 10);
                            var kvp = new KeyValuePair<string, Enemy>();
                            if (enemyRandSelector >= 0 && enemyRandSelector < 7)
                            {
                                //elf
                                kvp = new KeyValuePair<string, Enemy>(
                                "elf" + Guid.NewGuid().ToString().Substring(0, 10),
                                new Dwarf()
                                {
                                    Texture = Content.Load<Texture2D>("enemy/elf"),
                                    Position = new Position(i * TILE_WIDTH, j * TILE_HEIGHT)
                                });
                            }
                            else if(enemyRandSelector >= 7)
                            {
                                //dwarf
                                kvp = new KeyValuePair<string, Enemy>(
                                "dwarf" + Guid.NewGuid().ToString().Substring(0, 10),
                                new Dwarf()
                                {
                                    Texture = Content.Load<Texture2D>("enemy/dwarf"),
                                    Position = new Position(i * TILE_WIDTH, j * TILE_HEIGHT)
                                });
                            }
                            _map[i, j].EntityTexture = kvp.Value.Texture;
                            _map[i, j].EntityName = kvp.Key;
                            _enemies.Add(kvp.Key, kvp.Value);
                            enemyCounter++;
                        }
                    }
                    else
                    {
                        _map[i, j] = new MapTile()
                        {
                            Texture = _textures.FirstOrDefault(t => t.Key == "wall").Value
                        };
                    }
                }
            }

            _map[_player.Position.X/TILE_WIDTH, _player.Position.Y/TILE_HEIGHT].EntityTexture = _player.Texture;
            CalculateUnseen();

            // Load Healthbars
            DamageDescriber.AlmostDead = Content.Load<Texture2D>("health/almost");
            DamageDescriber.SeverelyDamaged = Content.Load<Texture2D>("health/severely");
            DamageDescriber.HeavilyDamaged = Content.Load<Texture2D>("health/heavily");
            DamageDescriber.ModeratelyDamaged = Content.Load<Texture2D>("health/moderately");
            DamageDescriber.LightlyDamaged = Content.Load<Texture2D>("health/lightly");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            _textures.Clear();
            _enemies.Clear();
            _map = new MapTile[WIDTH,HEIGHT];
            _player = new Player();
            enemyCounter = 0;
            Content.Unload();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            MouseState mouseState = Mouse.GetState();
            _internalSettings.Cursor.Position = new Vector2(mouseState.X, mouseState.Y);

            if (_framesPassed > 5)
            {
                _framesPassed = 0;
            }
            _framesPassed++;

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            if (_framesPassed > 5 && Keyboard.GetState().IsKeyDown(Keys.Left))
            {
                if (VerifyMovement(-1, 0))
                {
                    _player.Position.X -= TILE_WIDTH;
                    _map[_player.Position.X / TILE_WIDTH, _player.Position.Y / TILE_HEIGHT].EntityTexture = _player.Texture;
                    _map[(_player.Position.X / TILE_WIDTH)+1, _player.Position.Y / TILE_HEIGHT].EntityTexture = null;
                }
                else if (IsEnemyNext(-1, 0))
                {
                    if (Attack(-1, 0))
                    {
                        _player.Position.X -= TILE_WIDTH;
                        _map[_player.Position.X / TILE_WIDTH, _player.Position.Y / TILE_HEIGHT].EntityTexture = _player.Texture;
                        _map[(_player.Position.X / TILE_WIDTH) + 1, _player.Position.Y / TILE_HEIGHT].EntityTexture = null;
                    }
                }
                MoveEnemies();
                CalculateUnseen();
            }
            if (_framesPassed > 5 && Keyboard.GetState().IsKeyDown(Keys.Right))
            {
                if (VerifyMovement(1, 0))
                {
                    _player.Position.X += TILE_WIDTH;
                    _map[_player.Position.X / TILE_WIDTH, _player.Position.Y / TILE_HEIGHT].EntityTexture = _player.Texture;
                    _map[(_player.Position.X / TILE_WIDTH)-1, _player.Position.Y / TILE_HEIGHT].EntityTexture = null;
                }
                else if (IsEnemyNext(1, 0))
                {
                    if (Attack(1, 0))
                    {
                        _player.Position.X += TILE_WIDTH;
                        _map[_player.Position.X / TILE_WIDTH, _player.Position.Y / TILE_HEIGHT].EntityTexture = _player.Texture;
                        _map[(_player.Position.X / TILE_WIDTH) - 1, _player.Position.Y / TILE_HEIGHT].EntityTexture = null;
                    }
                }
                MoveEnemies();
                CalculateUnseen();
            }
            if (_framesPassed > 5 && Keyboard.GetState().IsKeyDown(Keys.Down))
            {
                if (VerifyMovement(0, 1))
                {
                    _player.Position.Y += TILE_HEIGHT;
                    _map[_player.Position.X / TILE_WIDTH, _player.Position.Y / TILE_HEIGHT].EntityTexture = _player.Texture;
                    _map[(_player.Position.X / TILE_WIDTH), (_player.Position.Y / TILE_HEIGHT) - 1].EntityTexture = null;
                }
                else if (IsEnemyNext(0, 1))
                {
                    if (Attack(0, 1))
                    {
                        _player.Position.Y += TILE_HEIGHT;
                        _map[_player.Position.X / TILE_WIDTH, _player.Position.Y / TILE_HEIGHT].EntityTexture = _player.Texture;
                        _map[(_player.Position.X / TILE_WIDTH), (_player.Position.Y / TILE_HEIGHT) - 1].EntityTexture = null;
                    }
                }
                MoveEnemies();
                CalculateUnseen();
            }
            if (_framesPassed > 5 && Keyboard.GetState().IsKeyDown(Keys.Up))
            {
                if (VerifyMovement(0, -1))
                {
                    _player.Position.Y -= TILE_HEIGHT;
                    _map[_player.Position.X / TILE_WIDTH, _player.Position.Y / TILE_HEIGHT].EntityTexture = _player.Texture;
                    _map[(_player.Position.X/TILE_WIDTH), (_player.Position.Y/TILE_HEIGHT) + 1].EntityTexture = null;
                }
                else if (IsEnemyNext(0, -1))
                {
                    if (Attack(0, -1))
                    {
                        _player.Position.Y -= TILE_HEIGHT;
                        _map[_player.Position.X / TILE_WIDTH, _player.Position.Y / TILE_HEIGHT].EntityTexture = _player.Texture;
                        _map[(_player.Position.X / TILE_WIDTH), (_player.Position.Y / TILE_HEIGHT) + 1].EntityTexture = null;
                    }
                }
                MoveEnemies();
                CalculateUnseen();
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            _spriteBatch.Begin();

            for (int i = -10; i < FOV/2; i++)
            {
                var x = (_player.Position.X/TILE_WIDTH) + i;
                if (x >= WIDTH)
                    continue;
                if(x < 0)
                    continue;
                for (int j = -10; j < FOV/2; j++)
                {
                    var y = (_player.Position.Y/TILE_HEIGHT) + j;
                    if(y < 0)
                        continue;
                    if (y >= HEIGHT)
                        continue;
                    if (!_map[x, y].IsUnseen)
                    {
                        _spriteBatch.Draw(_map[x, y].Texture, new Vector2((i + 10)*TILE_WIDTH, (j + 10)*TILE_HEIGHT));
                        var pos = new Vector2((i + 10)*TILE_WIDTH, (j + 10)*TILE_HEIGHT);

                        if (_map[x, y].AdditionalTextures.Count > 0)
                        {
                            // blood, etc...
                            foreach (var texture in _map[x, y].AdditionalTextures)
                            {
                                _spriteBatch.Draw(texture, pos);
                            }
                        }

                        if (_map[x, y].EntityTexture != null)
                        {
                            _spriteBatch.Draw(_map[x, y].EntityTexture, pos);
                            if (_map[x, y].EntityTexture == _player.Texture)
                            {
                                DrawHealthBar(_player.Health, _player.MaxHealth, pos);
                            }
                            else
                            {
                                Enemy enemy = GetEnemy(x, y);
                                DrawHealthBar(enemy.Health, enemy.MaxHealth, pos);
                            }
                        }
                    }
                    else
                    {
                        _spriteBatch.Draw(_textures.FirstOrDefault(t => t.Key == "unseen").Value, new Vector2((i+10) * TILE_WIDTH, (j+10) * TILE_HEIGHT));
                    }
                }
            }

            _spriteBatch.Draw(_internalSettings.Cursor.Texture, _internalSettings.Cursor.Position, Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        void MoveEnemies()
        {
            foreach (var enemy in _enemies)
            {
                int[] mov = enemy.Value.EnemeKi.Decide(_map, enemy.Value, enemy.Key, _player);
                _map[enemy.Value.Position.X/TILE_WIDTH, enemy.Value.Position.Y/TILE_HEIGHT].EntityTexture = null;
                enemy.Value.Position.X += mov[0];
                enemy.Value.Position.Y += mov[1];
                _map[enemy.Value.Position.X / TILE_WIDTH, enemy.Value.Position.Y / TILE_HEIGHT].EntityTexture = enemy.Value.Texture;
                _map[enemy.Value.Position.X / TILE_WIDTH, enemy.Value.Position.Y / TILE_HEIGHT].EntityName =
                    enemy.Key;
            }
        }

        bool VerifyMovement(int x, int y)
        {
            int[] virtPos = GetVirtualPostition(x, y);

            bool isValid = _map[virtPos[0], virtPos[1]].Texture.Name != "wall/vines0";
            return isValid && _enemies.All(enemy => !IsCovering(enemy.Value.Position, new Position(virtPos[0]*TILE_WIDTH, virtPos[1]*TILE_HEIGHT)));
        }

        bool Attack(int x, int y)
        {
            int[] virtPos = GetVirtualPostition(x, y);
            if (
                !_enemies.Any(
                    enemy =>
                            IsCovering(enemy.Value.Position, new Position(virtPos[0]*TILE_WIDTH, virtPos[1]*TILE_HEIGHT))))
                return false;
            Console.WriteLine("Attack");

            var target = GetEnemy(virtPos);

            _player.Health -= target.Value.Attack;
            target.Value.Health -= _player.Attack;

            if (IsPlayerDead())
            {
                Die();
                return false;
            }
            if (target.Value.Health > 0) return false;
            KillEnemey(target.Key);
            return true;
        }

        bool IsCovering(Position p1, Position p2)
        {
            if (p1.X == p2.X && p1.Y == p2.Y)
                return true;
            return false;
        }

        bool IsEnemyNext(int x, int y)
        {
            int[] virtPos = GetVirtualPostition(x, y);
            return _map[virtPos[0], virtPos[1]].EntityTexture != null && _map[virtPos[0], virtPos[1]].EntityTexture.Name.StartsWith("enemy/");
        }

        int[] GetVirtualPostition(int x, int y)
        {
            int virtPosX = 0;
            int virtPosY = 0;
            if (x != 0)
            {
                virtPosX = (_player.Position.X + (x * TILE_WIDTH)) / TILE_WIDTH;
                virtPosY = _player.Position.Y / TILE_HEIGHT;
            }
            else if (y != 0)
            {
                virtPosX = _player.Position.X / TILE_WIDTH;
                virtPosY = (_player.Position.Y + (y * TILE_HEIGHT)) / TILE_HEIGHT;
            }

            return new[] {virtPosX, virtPosY};
        }

        Enemy GetEnemy(int x, int y)
        {
            x *= TILE_WIDTH;
            y *= TILE_HEIGHT;

            return (from enemy in _enemies where IsCovering(enemy.Value.Position, new Position(x, y)) select enemy.Value).FirstOrDefault();
        }

        KeyValuePair<string, Enemy> GetEnemy(int[] virtPos)
        {
            virtPos[0] *= TILE_WIDTH;
            virtPos[1] *= TILE_HEIGHT;
            return (from enemy in _enemies where IsCovering(enemy.Value.Position, new Position(virtPos[0], virtPos[1])) select enemy).FirstOrDefault();
        }

        bool IsPlayerDead()
        {
            return _player.Health <= 0;
        }

        void Die()
        {
            UnloadContent();
            LoadContent();
        }

        void KillEnemey(string name)
        {
            Drop(_enemies[name].XPReward); // enemy list is fast empty
            _map[_enemies[name].Position.X/TILE_WIDTH, _enemies[name].Position.Y / TILE_HEIGHT].AdditionalTextures.Add(_textures["blood_red"]);
            _enemies.Remove(name);
        }

        void Drop(int xp)
        {
            // give xp
            _player.XP += xp;
            // drop item
            _player.Inventory.Items.Add(new LargeSword());
            UpdatePlayerStats(new LargeSword());
        }

        void UpdatePlayerStats(InventoryItem ii = null)
        {
            if (ii != null)
            {
                // Update Player Stats with Item Stats
                switch (ii.ItemType)
                {
                    case ItemType.Weapon:
                        _player.Attack += ii.Damage;
                        break;
                    case ItemType.HealthSlot:
                        _player.MaxHealth += ii.Health;
                        break;
                    case ItemType.Armor:
                        _player.Shield += ii.Shield;
                        break;
                }
            }
            _player.Inventory.Amount = _player.Inventory.Items.Count;
            if (_player.XP >= _player.Level.XP)
            {
                int xp = _player.Level.XP - _player.XP;
                LevelUp();
                _player.XP += xp;
            }
        }

        void LevelUp()
        {
            if(_player.Level.Level == 1) return; // max level here
            int lvl = _player.Level.Level;
            int origDamage = _player.Level.Damage;
            int origShield = _player.Level.Shield;
            _player.Level = (LevelInfo)Activator.CreateInstance(Type.GetType("RogueLike.Levels.Level" + (lvl + 1)));
            _player.Attack -= origDamage;
            _player.Attack += _player.Level.Damage;
            _player.MaxHealth += _player.Level.Health;
            _player.Health = _player.MaxHealth;
            _player.Shield -= origShield;
            _player.Shield += _player.Level.Shield;
        }

        void DrawHealthBar(int health, int maxHealth, Vector2 pos)
        {
            if (health <= maxHealth / 5)
                _spriteBatch.Draw(DamageDescriber.AlmostDead, pos);
            else if (health <= maxHealth / 4)
                _spriteBatch.Draw(DamageDescriber.SeverelyDamaged, pos);
            else if (health <= maxHealth / 3)
                _spriteBatch.Draw(DamageDescriber.HeavilyDamaged, pos);
            else if (health <= maxHealth / 2)
                _spriteBatch.Draw(DamageDescriber.ModeratelyDamaged, pos);
            else if (health < maxHealth)
                _spriteBatch.Draw(DamageDescriber.LightlyDamaged, pos);
        }

        bool IsEnemyNearby(int x, int y)
        {
            // x
            if (x + 1 < WIDTH && _map[(x + 1), y] != null && _map[(x + 1), y].EntityTexture != null && _map[(x + 1), y].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (x + 2 < WIDTH && _map[(x + 2), y] != null && _map[(x + 2), y].EntityTexture != null && _map[(x + 2), y].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (x + 3 < WIDTH && _map[(x + 3), y] != null && _map[(x + 3), y].EntityTexture != null && _map[(x + 3), y].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (x - 1 >= 0 && _map[x - 1, y] != null && _map[x - 1, y].EntityTexture != null && _map[x - 1, y].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (x - 2 >= 0 && _map[x - 2, y] != null && _map[x - 2, y].EntityTexture != null && _map[x - 2, y].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (x - 3 >= 0 && _map[x - 3, y] != null && _map[x - 3, y].EntityTexture != null && _map[x - 3, y].EntityTexture.Name.Contains("enemy/"))
                return true;

            // y
            if (y + 1 < HEIGHT && _map[x, y + 1] != null && _map[x, y + 1].EntityTexture != null && _map[x, y + 1].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (y + 2 < HEIGHT && _map[x, y + 2] != null && _map[x, y + 2].EntityTexture != null && _map[x, y + 2].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (y + 3 < HEIGHT && _map[x, y + 3] != null && _map[x, y + 3].EntityTexture != null && _map[x, y + 3].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (y - 1 >= 0 && _map[x, y - 1] != null && _map[x, y - 1].EntityTexture != null && _map[x, y - 1].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (y - 2 >= 0 && _map[x, y - 2] != null && _map[x, y - 2].EntityTexture != null && _map[x, y - 2].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (y - 3 >= 0 && _map[x, y - 3] != null && _map[x, y - 3].EntityTexture != null && _map[x, y - 3].EntityTexture.Name.Contains("enemy/"))
                return true;

            // diag
            if (x + 1 < WIDTH && y + 1 < HEIGHT && _map[x + 1, y + 1] != null && _map[x + 1, y + 1].EntityTexture != null && _map[x + 1, y + 1].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (x + 2 < WIDTH && y + 2 < HEIGHT && _map[x + 2, y + 2] != null && _map[x + 2, y + 2].EntityTexture != null && _map[x + 2, y + 2].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (x + 3 < WIDTH && y + 3 < HEIGHT && _map[x + 3, y + 3] != null && _map[x + 3, y + 3].EntityTexture != null && _map[x + 3, y + 3].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (x - 1 >= 0 && y - 1 >= 0 && _map[x - 1, y - 1] != null && _map[x - 1, y - 1].EntityTexture != null && _map[x - 1, y - 1].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (x - 2 >= 0 && y - 2 >= 0 && _map[x - 2, y - 2] != null && _map[x - 2, y - 2].EntityTexture != null && _map[x - 2, y - 2].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (x - 3 >= 0 && y - 3 >= 0 && _map[x - 3, y - 3] != null && _map[x - 3, y - 3].EntityTexture != null && _map[x - 3, y - 3].EntityTexture.Name.Contains("enemy/"))
                return true;

            // diag
            if (x - 1 >= 0 && y + 1 < HEIGHT && _map[x - 1, y + 1] != null && _map[x - 1, y + 1].EntityTexture != null && _map[x - 1, y + 1].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (x - 2 >= 0 && y + 2 < HEIGHT && _map[x - 2, y + 2] != null && _map[x - 2, y + 2].EntityTexture != null && _map[x - 2, y + 2].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (x - 3 >= 0 && y + 3 < HEIGHT && _map[x - 3, y + 3] != null && _map[x - 3, y + 3].EntityTexture != null && _map[x - 3, y + 3].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (x + 1 < WIDTH && y - 1 >= 0 && _map[x + 1, y - 1] != null && _map[x + 1, y - 1].EntityTexture != null && _map[x + 1, y - 1].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (x + 2 < WIDTH && y - 2 >= 0 && _map[x + 2, y - 2] != null && _map[x + 2, y - 2].EntityTexture != null && _map[x + 2, y - 2].EntityTexture.Name.Contains("enemy/"))
                return true;
            if (x + 3 < WIDTH && y - 3 >= 0 && _map[x + 3, y - 3] != null && _map[x + 3, y - 3].EntityTexture != null && _map[x + 3, y - 3].EntityTexture.Name.Contains("enemy/"))
                return true;

            return false;
        }

        List<Enemy> GetEnemies()
        {
            List<Type> types = Assembly.GetExecutingAssembly().GetTypes().Where(t => string.Equals(t.Namespace, "RogueLike.Enemies", StringComparison.Ordinal)).ToArray().ToList();
            return (from type in types where type.Name != "Enemy" select (Enemy) Activator.CreateInstance(type)).ToList();
        }

        void CalculateUnseen()
        {
            for (int i = PLAYER_FOV * -1; i <= PLAYER_FOV; i++)
            {
                for (int j = PLAYER_FOV * -1; j <= PLAYER_FOV; j++)
                {
                    _map[(_player.Position.X / TILE_WIDTH) + i, (_player.Position.Y / TILE_HEIGHT) + j].IsUnseen = false;
                }
            }
        }
    }
}
