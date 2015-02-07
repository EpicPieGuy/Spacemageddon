#region Using Statements
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.GamerServices;
using Entities;
using LibGDX_Port;
#endregion

namespace Spacemageddon
{
    public class Main : Game
    {
        #region Game data
        private static Main instance;
        GraphicsDeviceManager graphics;
        private ShapeRenderer shapeRenderer;
        private SpriteBatch batch;
	    private Player player;
	    private List<Bullet> bullets, enemyBullets;
	    private List<Enemy> enemies;
	    private List<Shield> shields;
	    private List<int> framerate;
	    private List<Powerup> powerups;
        private List<Checkpoint> checkpoints;
	    private int[][] walls, spikes;
        private int score, lavaFrame;
	    private Rectangle camera;
	    private String current;
	    private Boss boss;
	    private List<String> levels = new List<String>();
        public static int WIDTH = 640, HEIGHT = 480, TILE = 32, S_WIDTH = 640, S_HEIGHT = 480;
        public static Vector2 scale;
        private static Content content;
        private KeyboardState previous;
        private Texture2D background, doorTex;
        private Door door;
        private Tileset tileset;
        private TextureRegion[][] lava;
        #endregion
        private static int delay = 0;
        public Main() : base()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            instance = this;
            content = new Content();
            this.loadOptions("options.xml");
        }

        protected override void Initialize()
        {
            base.Initialize();
            shapeRenderer = content.Add<ShapeRenderer>(new ShapeRenderer(this.GraphicsDevice));
            this.player = new Player(TILE, TILE, null);
		    this.player.Health = 5;
            levels.Add("pestilenceStage");
            levels.Add("pestilenceFight");
            levels.Add("warStage");
            levels.Add("warFight");
            levels.Add("famineStage");
            levels.Add("famineFight");
		    levels.Add("deathStage");
		    levels.Add("deathFight");

            this.walls = new int[WIDTH / TILE][];
            for(int i = 0; i < walls.Length; i++)
                walls[i] = new int[HEIGHT / TILE];
		
		    this.bullets = new List<Bullet>();
            this.enemyBullets = new List<Bullet>();
            this.shields = new List<Shield>();
            this.enemies = new List<Enemy>();
            this.framerate = new List<int>();
            this.powerups = new List<Powerup>();
            this.checkpoints = new List<Checkpoint>();
		    this.score = 0;
		
		    camera = new Rectangle();
		    camera.X = (int)player.X - S_WIDTH / 2;
		    camera.Y = (int)player.Y - S_HEIGHT / 2;
		    camera.Width = S_WIDTH;
		    camera.Height = S_HEIGHT;
            current = "pestilenceStage";		
		    load(current);        
        }

        protected override void LoadContent()
        {
            batch = new SpriteBatch(GraphicsDevice);
            content.Add(batch);
            background = loadTexture("humanShipBkg.png");
            doorTex = loadTexture("door.png");
            TextureRegion lavaSheet = new TextureRegion(loadTexture("lava.png"));
            lava = lavaSheet.Split(32, 32);
        }

        protected override void UnloadContent()
        {
            content.Dispose();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            base.Update(gameTime);
            if (door != null && door.update(this.player))
            {
                next();
                return;
            }
            if (Main.delay > 0)
            {
                Main.delay -= 1;
                return;
            }
            var keys = Keyboard.GetState();

            //Update game state
            this.player.update(walls);
            //Die on spikes or with 0 health
            if (this.player.Health <= 0 || !Entity.free(this.player.getBounds(), spikes))
                die();
		    //Fires bullet if bullet key is pressed
		    if(keys.IsKeyDown(Keys.F) && !previous.IsKeyDown(Keys.F))
		    {
                Bullet bullet = new Bullet(player.X + 10 * player.getFacing(), player.Y + TILE / 2, 10 * player.getFacing(), 0, null);
			    this.bullets.Add(bullet);
		    }
            if (keys.IsKeyDown(Keys.D) && !previous.IsKeyDown(Keys.D))
		    {
			    Bullet bullet = new Bullet(player.X + TILE / 2, player.Y + TILE / 2, 0, 0, TILE / 2, null);
                Rectangle bounds = player.getBounds();
                bounds.Y -= TILE / 2;
			    if(!Entity.free(bounds, this.walls))
				    bullet.setVelocity(0, TILE / 3);
			    else
				    bullet.setVelocity(0, -TILE / 3);
			    this.bullets.Add(bullet);
		    }
            if (keys.IsKeyDown(Keys.S) && ! previous.IsKeyDown(Keys.S))
		    {
			    Shield shield = new Shield(this.player.X + TILE / 2, this.player.Y + TILE / 2);
			    this.shields.Add(shield);
		    }
		    //Update bullets
            for (int i = 0; i < bullets.Count; i++)
            {
                Bullet bullet = bullets[i];
                bullet.applyVelocity();
                if (!Entity.free(bullet.getPosition(), this.walls))
                    bullets.Remove(bullet);
                if (this.boss != null && this.boss.getBounds().Intersects(bullet.getBounds()))
                {
                    this.boss.damage(player.getDamage());
                    bullets.Remove(bullet);
                }
                if (((int)bullet.X / TILE) < walls.Length && ((int)bullet.Y / TILE) < walls[(int)bullet.X / TILE].Length &&
                    walls[(int)bullet.X / TILE][(int)bullet.Y / TILE] == 2)
                {
                    walls[(int)bullet.X / TILE][(int)bullet.Y / TILE] = 0;
                    bullets.Remove(bullet);
                }
            }
		    //Update enemies
            for (int i = 0; i < enemies.Count; i++ )
            {
                Enemy enemy = enemies[i];
                if (enemy.getType() == Enemy.Type.Ghost)
                {
                    if (enemy.X > player.X) enemy.translate(-1, 0);
                    if (enemy.X < player.X) enemy.translate(1, 0);
                    if (enemy.Y > player.Y) enemy.translate(0, -1);
                    if (enemy.Y < player.Y) enemy.translate(0, 1);
                }
                else
                    enemy.update(walls);
                Bullet bullet;
                if ((bullet = enemy.checkBullets(this.bullets)) != null)
                {
                    enemy.Health -= player.getDamage();
                    if (player.abilities[Player.Abilities.Staff])
                        enemy.poisoned = true;
                    if (enemy.Health <= 0)
                    {
                        enemies.Remove(enemy);
                        Random rand = new Random();
                        float chance = 0.05f;
                        if (player.abilities[Player.Abilities.Plenty])
                            chance = 0.2f;
                        if (rand.NextDouble() <= chance)
                            this.powerups.Add(new Powerup(enemy.X, enemy.Y, Powerup.Type.Health));
                        else if (rand.NextDouble() <= chance)
                            this.powerups.Add(new Powerup(enemy.X, enemy.Y, Powerup.Type.Damage));
                        this.score += 1;
                    }
                    if(!player.abilities[Player.Abilities.Scythe])
                        this.bullets.Remove(bullet);
                }
                if (enemy.getBounds().Intersects(this.player.getBounds()))
                {
                    this.player.damage();
                }
                if ((enemy.getType() == Enemy.Type.FlyFiring || enemy.getType() == Enemy.Type.PatrolFiring)
                        && enemy.getDelay() <= 0)
                {
                    float vX = enemy.Facing * 3;
                    Bullet enemyBullet = new Bullet(enemy.X + TILE / 2, enemy.Y + TILE / 2, vX, 0, null);
                    this.enemyBullets.Add(enemyBullet);
                    enemy.setDelay(120);
                }
                else if (enemy.getType() == Enemy.Type.Turret && enemy.getDelay() <= 0)
                {
                    float vX = (-Math.Abs(enemy.X - player.X)) / (enemy.X - player.X) * 3;
                    Bullet enemyBullet = new Bullet(enemy.X + TILE / 2, enemy.Y + TILE / 2,
                            vX, 0, null);
                    this.enemyBullets.Add(enemyBullet);
                    enemy.setDelay(90);
                }
                else if (enemy.getType() == Enemy.Type.Robot && enemy.getDelay() <= 0)
                {
                    Bullet enemyBullet = new Bullet(enemy.X + TILE / 2, enemy.Y + TILE / 2,
                            0, 4, null);
                    this.enemyBullets.Add(enemyBullet);
                    enemy.setDelay(120);
                }
                if (enemy.getType() == Enemy.Type.FlySlamming && Math.Abs(enemy.X- this.player.X) <= TILE)
                    enemy.setVelocity(0, -15);

            }
		    //Update enemy bullets
            for (int i = 0; i < enemyBullets.Count; i++ )
            {
                Bullet bullet = enemyBullets[i];
                bullet.applyVelocity();
                if (!Entity.free(bullet.getPosition(), this.walls))
                    enemyBullets.Remove(bullet);
                if (this.player.getBounds().Contains(bullet.getPosition()))
                {
                    this.player.damage();
                    enemyBullets.Remove(bullet);
                    continue;
                }
                for (int j = 0; j < shields.Count; j++ )
                {
                    Shield shield = shields[j];
                    if (shield.getBounds().Intersects(bullet.getBounds()))
                    {
                        enemyBullets.Remove(bullet);
                        shields.Remove(shield);
                        break;
                    }
                }
            }
            for (int i = 0; i < shields.Count; i++ )
            {
                Shield shield = shields[i];
                if (shield.decreaseLife())
                    shields.Remove(shield);
            }
            for (int i = 0; i < powerups.Count; i++ )
            {
                Powerup p = powerups[i];
                if (this.player.getBounds().Intersects(p.getBounds()))
                {
                    switch (p.getType())
                    {
                        case Powerup.Type.Damage:
                            player.giveDamageBuff();
                            break;
                        case Powerup.Type.Health:
                            if (player.Health < 5)
                                player.Health = 5;
                            break;
                        default:
                            break;
                    }
                    powerups.Remove(p);
                }
            }
            foreach (Checkpoint check in checkpoints)
            {
                check.Update();
                if (player.getBounds().Intersects(check.getBounds()))
                    check.Activate();
            }
		    if(this.boss != null)
		    {
			    switch(this.boss.getType())
			    {
				    case Boss.Type.Death:
					    this.boss.setDelay(this.boss.getDelay() - 1);
					    if(this.boss.getDelay() <= 0)
					    {
						    float offset = this.player.X;
						    offset -= this.player.getFacing() * 200;
						    if(offset < 0) offset = 0;
						    if(offset > WIDTH - TILE * 2f) offset = WIDTH - TILE * 2f;
						    this.boss.setPosition(offset, this.boss.Y);
						    float vX = (-Math.Abs(this.boss.X - this.player.X) / (this.boss.X - this.player.X)) * 3;
						    vX *= 10 / this.boss.Health;
						    Bullet enemyBullet = new Bullet(this.boss.X + TILE / 2, this.boss.Y + TILE / 2, 
								    vX, 0, null);
						    this.enemyBullets.Add(enemyBullet);
						    this.boss.setDelay(60 + (int)this.boss.Health * 6);
					    }
					    break;
                    case Boss.Type.Famine:
					    break;
                    case Boss.Type.Pestilence:
					    break;
                    case Boss.Type.War:
					    break;
				    default:
					    break;
			    }
			    if(this.boss.Health <= 0)
				    Console.WriteLine("VICTORY");
		    }
           
		    if(keys.IsKeyDown(Keys.PageUp) && !previous.IsKeyDown(Keys.PageUp))
			    this.next();
		    if(keys.IsKeyDown(Keys.F5) && !previous.IsKeyDown(Keys.F5))
			    this.load(this.current);
            previous = Keyboard.GetState();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(128, 128, 128, 255));
            this.batch.Begin();
            int offset = (int)(camera.X / 4) % background.Width;
            //Draw background
            for (int i = -offset; i < S_WIDTH * 3; i += (int)(background.Width * scale.X))
                for (int j = 0; j < S_HEIGHT  * 3; j += (int)(background.Height * scale.Y))
                    batch.Draw(background, new Rectangle((int)(i) - offset, (int)(j), 
                        (int)(scale.X * background.Width), (int)(scale.Y * background.Height)), Color.White);
            //Set up camera
            camera.X = (int)player.X - S_WIDTH / 2 - TILE * 2;
            camera.Y = HEIGHT - 480 - TILE;
            camera.Width = S_WIDTH + TILE * 4;
            camera.Height = S_HEIGHT + TILE * 4;
            if (camera.X + camera.Width > WIDTH - TILE) camera.X = WIDTH - TILE - camera.Width;
		    if(camera.X < 0) camera.X = 0;
		    //Draw blocks
            if (tileset != null)
            {
                TextureRegion[][] tiles = tileset.GetTextures();
                for (int i = 0; i < tiles.Length; i++)
                    for (int j = 0; j < tiles[i].Length; j++)
                        if (tiles[i][j] != null)
                        {
                            Vector2 position = new Vector2(i * Main.TILE - camera.X, j * Main.TILE - camera.Y);
                            position.Y = HEIGHT - TILE * 2 - position.Y;
                            tiles[i][j].Draw(batch, position);
                        }
            }
            else
            {
                this.shapeRenderer.setScale(scale);
                this.shapeRenderer.setColor(0, 0, 0, 1);
                for (int i = 0; i < this.walls.Length; i++)
                    for (int j = 0; j < this.walls[i].Length; j++)
                        if (this.walls[i][j] == 1 && camera.Intersects(new Rectangle(i * TILE - TILE, j * TILE - TILE, TILE * 2, TILE * 2)))
                            this.shapeRenderer.rect(batch, new Rectangle(i * TILE - (int)camera.X, HEIGHT - TILE * 2 - j * TILE - (int)camera.Y, TILE, TILE));
                this.shapeRenderer.setColor(0, 0, 0, 1);
            }
            //Draw Breakable Blocks
            this.shapeRenderer.setColor(0.43f, 0.156f, 0, 1);
            for (int i = 0; i < this.walls.Length; i++)
                for (int j = 0; j < this.walls[i].Length; j++)
                    if (this.walls[i][j] == 2 && camera.Intersects(new Rectangle(i * TILE, j * TILE, TILE, TILE)))
                        this.shapeRenderer.rect(batch, new Rectangle(i * TILE - (int)camera.X, HEIGHT - TILE * 2 - j * TILE - (int)camera.Y, TILE, TILE));
            //Draw door
            if (door != null && camera.Intersects(door.getBounds()))
            {
                Vector2 pos = door.getPosition() - new Vector2(camera.X, camera.Y);
                pos.Y = HEIGHT - TILE * 2 - pos.Y;
                door.getTexture().Draw(batch, pos, scale);
            }
            //Draw Spikes
            this.shapeRenderer.setColor(1, 0, 0, 1);
		    for(int i = 0; i < this.spikes.Length; i++)
			    for(int j = 0; j < this.spikes[i].Length; j++)
                    if (this.spikes[i][j] == 1 && camera.Intersects(new Rectangle(i * TILE, j * TILE, TILE, TILE)))
                    {
                        lavaFrame = (lavaFrame + 1) % 80;
                        lava[lavaFrame / 20][0].Draw(batch, new Vector2(i * TILE - (int)camera.X, HEIGHT - TILE * 2 - j * TILE - (int)camera.Y), scale);
                    }
		    //Draw bullets
		    this.shapeRenderer.setColor(0, 1, 0, 1);
		    foreach(Bullet bullet in this.bullets)
			    if(camera.Contains(bullet.getPosition()))
                    this.shapeRenderer.rect(batch, new Rectangle((int)bullet.X - 3 - camera.X, HEIGHT - TILE - (int)bullet.Y - 3 - camera.Y, 6, 6));
		    //Draw enemy bullets
		    this.shapeRenderer.setColor(1, 0, 1, 1);
		     foreach(Bullet bullet in this.enemyBullets)
                 if (camera.Contains(bullet.getPosition()))
                    this.shapeRenderer.rect(batch, new Rectangle((int)bullet.X - 3 - camera.X, HEIGHT - TILE - (int)bullet.Y - 3 - camera.Y, 6, 6));
            //Draw shields
            foreach (Shield s in this.shields)
                if (camera.Intersects(s.getBounds()))
                {
                    Rectangle destination = new Rectangle((int)s.X - camera.X, HEIGHT - TILE - (int)s.Y - camera.Y, s.Texture.Width, s.Texture.Height);
                    destination = Scale(destination, scale);
                    batch.Draw(s.Texture, destination , Color.White);
                }
		    //Draw checkpoint
            foreach (Checkpoint check in checkpoints)
                if (camera.Intersects(check.getBounds()))
                    check.GetTexture().Draw(batch, new Vector2(check.X - camera.X, HEIGHT - TILE * 2- check.Y - camera.Y), scale);
		    //Draw player
             player.getTexture().Draw(batch, new Vector2(player.X - camera.X, HEIGHT - TILE * 2 - player.Y - camera.Y), scale);
		    //Draw enemies
		    foreach(Enemy enemy in this.enemies)
			    if(camera.Intersects(enemy.getBounds()))
                    enemy.getTex().Draw(batch, new Vector2(enemy.X - camera.X, HEIGHT - TILE * 2 - enemy.Y - camera.Y), scale);
            foreach (Powerup powerup in this.powerups)
                if (camera.Intersects(powerup.getBounds()))
                {
                    Rectangle destination = new Rectangle((int)(powerup.X - camera.X), HEIGHT - TILE * 2 - (int)(powerup.Y - camera.Y), powerup.getTexture().Width, powerup.getTexture().Height);
                    destination = Scale(destination, scale);
                    batch.Draw(powerup.getTexture(), destination, Color.White);
                }
            if (this.boss != null)
                boss.getTexture().Draw(batch, new Vector2(boss.X, HEIGHT - TILE * 3 - boss.Y), scale);
		
		    //Draw GUI
		    shapeRenderer.setColor(0, 1, 0, 0.5f);
		    Vector2 health = new Vector2(TILE * 2, HEIGHT - TILE * 1.8f);
		    Vector2 filledBar = new Vector2(player.Health * TILE, TILE * 0.5f);
            this.shapeRenderer.rect(batch, new Rectangle((int)health.X, (int)health.Y, (int)filledBar.X, (int)filledBar.Y));
		    if(this.boss != null)
		    {
			    this.shapeRenderer.setColor(1, 0, 0, 1);
			    health = new Vector2(TILE * 6, HEIGHT- TILE * 1.5f);
			    filledBar = new Vector2(boss.Health * (TILE * 2), TILE);
			    this.shapeRenderer.rect(batch, new Rectangle((int)health.X, HEIGHT - (int)health.Y - TILE / 2, (int)filledBar.X, (int)filledBar.Y));
		    }
            this.batch.End();

            base.Draw(gameTime);
        }

        public static void Wait(int frames)
        {
            Main.delay = frames;
        }

        public static Texture2D loadTexture(string name)
        {
            System.IO.Stream stream = System.IO.File.Open(name, System.IO.FileMode.Open);
            Texture2D texture = Texture2D.FromStream(Main.instance.GraphicsDevice, stream);
            stream.Close();
            content.Add<Texture2D>(texture);
            return texture;
        }

        public static Rectangle Scale(Rectangle original, Vector2 scale)
        {
            Rectangle result = new Rectangle();
            result.X = (int)(original.X * scale.X);
            result.Y = (int)(original.Y * scale.Y);
            result.Width = (int)(original.Width * scale.X);
            result.Height = (int)(original.Height * scale.Y);
            return result;
        }

        private void load(String levelname)
	    {
		    player.setSpritesheet(new TextureRegion(loadTexture("spaceCharacter.png")));
		    this.score = 0;
		    this.player = new Player(TILE, TILE, null);
		    this.player.Health = player.abilities[Player.Abilities.Life] ? 10 : 5;
		    this.enemies = new List<Enemy>();
		    this.bullets = new List<Bullet>();
		    this.enemyBullets = new List<Bullet>();
            this.checkpoints = new List<Checkpoint>();
		    using(StreamReader stream = File.OpenText(levelname))
            {
			    WIDTH = Convert.ToInt16(stream.ReadLine());
			    HEIGHT = Convert.ToInt16(stream.ReadLine()) + TILE;
			    List<String> lines = new List<String>();
			    String line;
			    while((line = stream.ReadLine()) != null)
				    lines.Add(line);
			    walls = new int[WIDTH / TILE][];
                for(int i = 0; i < walls.Length; i++)
                    walls[i] = new int[HEIGHT / TILE];
			    spikes = new int[WIDTH / TILE][];
                for(int i = 0; i < spikes.Length; i++)
                    spikes[i] = new int[HEIGHT / TILE];
                Random rand = new Random();
                int offset = 0;
                if (player.abilities[Player.Abilities.Harp])
                    offset = -1;
			    for(int i = 0; i < lines.Count; i++)
				    for(int j = 0; j < lines[i].Length; j++)
				    {
					    int x = lines[i].ToCharArray()[j] - '0';
					    if(x > 10) x += '0';
					    walls[i][j] = (x == 1) ? 1 : 0;
					    switch(x)
					    {
						    case 2:
							    enemies.Add(new Enemy(i * TILE, j * TILE, (int)(rand.Next(1) - 0.5) * 2, 0, null, Enemy.Type.Patrol, 3 + offset));
							    break;
						    case 3:
                                 enemies.Add(new Enemy(i * TILE, j * TILE, (int)(rand.Next(1) - 0.5) * 2, 0, null, Enemy.Type.PatrolFiring, 3 + offset));
							    break;
						    case 4:
                                enemies.Add(new Enemy(i * TILE, j * TILE, (int)(rand.Next(1) - 0.5) * 2, 0, null, Enemy.Type.Fly, 3 + offset));
							    break;
						    case 5:
                                enemies.Add(new Enemy(i * TILE, j * TILE, (int)(rand.Next(1) - 0.5) * 2, 0, null, Enemy.Type.FlyFiring, 3 + offset));
							    break;
						    case 6:
                                enemies.Add(new Enemy(i * TILE, j * TILE, (int)(rand.Next(1) - 0.5) * 2, 0, null, Enemy.Type.Ghost, 2 + offset));
							    break;
						    case 7:
							    spikes[i][j] = 1;
							    break;
						    case 8:
                                enemies.Add(new Enemy(i * TILE, j * TILE, (int)(rand.Next(1) - 0.5) * 2, 0, null, Enemy.Type.Turret, 4 + offset));
							    break;
                            case 9:
                                enemies.Add(new Enemy(i * TILE, j * TILE,(int)(rand.Next(1) - 0.5) * 2, 0, null, Enemy.Type.Robot, 5 + offset));
                                break;
						    case 'A':
                                walls[i][j] = 2;    
							    break;
						    case 'B':
							    boss = new Boss(i * TILE, j * TILE, null);
							    break;
                            case 'C':
                                checkpoints.Add(new Checkpoint(i * TILE, j * TILE));
                                break;
                            case 'D':
                                door = new Door(i * TILE, j * TILE);
                                break;
					    }
				    }
                switch (levelname)
                {
                    case "deathStage":
                    case "deathFight":
                        tileset = new Tileset(walls, loadTexture("deathTileset.png"));
                        background = loadTexture("deathBkg.png");
                        break;
                    case "warStage":
                    case "warFight":
                        tileset = new Tileset(walls, loadTexture("warTileset.png"));
                        background = loadTexture("warBkg.png");
                        break;
                    case "famineStage":
                    case "famineFight":
                        tileset = new Tileset(walls, loadTexture("famineTileset.png"));
                        background = loadTexture("famineBkg.png");
                        break;
                    case "pestilenceStage":
                    case "pestilenceFight":
                        tileset = new Tileset(walls, loadTexture("pestilenceTileset.png"));
                        background = loadTexture("pestilenceBkg.png");
                        break;
                }
            }
	    }

        private void next()
        {
            int currentIndex = levels.IndexOf(current);
            current = levels[currentIndex + 1];
            load(current);
        }

        private void saveState(String location)
        {
            using (BinaryWriter writer = new BinaryWriter(System.IO.File.OpenWrite(location)))
            {
                bool[] abilities = new bool[8];
                abilities[0] = player.abilities[Player.Abilities.Harp];
                abilities[1] = player.abilities[Player.Abilities.Life];
                abilities[2] = player.abilities[Player.Abilities.Light];
                abilities[3] = player.abilities[Player.Abilities.Plenty];
                abilities[4] = player.abilities[Player.Abilities.Scythe];
                abilities[5] = player.abilities[Player.Abilities.Staff];
                abilities[6] = player.abilities[Player.Abilities.Swarm];
                abilities[7] = player.abilities[Player.Abilities.Sword];
                writer.Write(bitsToByte(abilities));
                byte level;
                switch (current)
                {
                    case "deathStage":
                        level = 0;
                        break;
                    case "deathFight":
                        level = 1;
                        break;
                    case "famineStage":
                        level = 2;
                        break;
                    case "famineFight":
                        level = 3;
                        break;
                    case "warStage":
                        level = 4;
                        break;
                    case "warFight":
                        level = 5;
                        break;
                    case "pestilenceStage":
                        level = 6;
                        break;
                    case "pestilenceFight":
                        level = 7;
                        break;
                    case "alienEnding":
                        level = 8;
                        break;
                    case "humanEnding":
                        level = 9;
                        break;
                    default:
                        level = 255;
                        break;
                }
                writer.Write(level);
                writer.Write((byte)checkpoints.IndexOf(Checkpoint.ActiveCheckpoint));
            }
        }

        private void loadState(String location)
        {
            using (BinaryReader reader = new BinaryReader(System.IO.File.OpenRead(location)))
            {
                bool[] abilities = byteToBits(reader.ReadByte());
                player.abilities[Player.Abilities.Harp] = abilities[0];
                player.abilities[Player.Abilities.Life] = abilities[1];
                player.abilities[Player.Abilities.Light] = abilities[2];
                player.abilities[Player.Abilities.Plenty] = abilities[3];
                player.abilities[Player.Abilities.Scythe] = abilities[4];
                player.abilities[Player.Abilities.Staff] = abilities[5];
                player.abilities[Player.Abilities.Swarm] = abilities[6];
                player.abilities[Player.Abilities.Sword] = abilities[7];
                byte level = reader.ReadByte();
                switch (level)
                {
                    case 0:
                        load("deathStage");
                        break;
                    case 1:
                        load("deathFight");
                        break;
                    case 2:
                        load("famineStage");
                        break;
                    case 3:
                        load("famineFight");
                        break;
                    case 4:
                        load("warStage");
                        break;
                    case 5:
                        load("warFight");
                        break;
                    case 6:
                        load("pestilenceStage");
                        break;
                    case 7:
                        load("pestilenceFight");
                        break;
                    case 8:
                        load("alienEnding");
                        break;
                    case 9:
                        load("humanEnding");
                        break;
                    default:
                        load("opening");
                        break;
                }
                byte checkpoint = reader.ReadByte();
                checkpoints[checkpoint].Activate();
            }
        }

        private byte bitsToByte(bool[] bits)
        {
            if (bits.Length != 8)
                throw new System.ArgumentException();
            byte b = 0;
            for (int i = bits.Length - 1; i >= 0; i--)
            {
                b += bits[i] ? (byte)Math.Pow(2, bits.Length - 1 - i) : (byte)0;
            }
            return b;
        }

        private bool[] byteToBits(byte b)
        {
            bool[] bits = new bool[8];
            for (int i = bits.Length - 1; i >= 0; i--)
            {
                if (bits[i] = b - Math.Pow(2, i) > 0)
                    b -= (byte)Math.Pow(2, i);
            }
            return bits;
        }

        private void die()
        {
            this.player.Health = player.abilities[Player.Abilities.Life] ? 10 : 5;
            this.player.setPosition(Checkpoint.ActiveCheckpoint.X, HEIGHT - TILE);
            this.player.respawning = true;
        }

        private void loadOptions(String xmlLocation)
        {
            XmlReader xml = XmlReader.Create(xmlLocation);
            while (xml.Read())
            {
                if (xml.NodeType == XmlNodeType.Element)
                {
                    switch (xml.Name)
                    {
                        case "graphics":
                            graphics.PreferredBackBufferWidth = Convert.ToInt16(xml.GetAttribute("width"));
                            graphics.PreferredBackBufferHeight = Convert.ToInt16(xml.GetAttribute("height"));
                            graphics.SynchronizeWithVerticalRetrace = Convert.ToBoolean(xml.GetAttribute("vsync"));
                            break;
                    }
                }
            }
            xml.Close();
            scale.X = graphics.PreferredBackBufferWidth / 640f;
            scale.Y = graphics.PreferredBackBufferHeight / 480f;
            S_WIDTH = graphics.PreferredBackBufferWidth;
            S_HEIGHT = graphics.PreferredBackBufferHeight;
            graphics.ApplyChanges();
        }
    }

    public class Tileset
    {
        private TextureRegion[][] tiles;
        private Dictionary<Links, TextureRegion> textures;

        public Tileset(int[][] walls, Texture2D sheet)
        {
            TextureRegion sheetRegion = new TextureRegion(sheet);
            TextureRegion[][] tiles = sheetRegion.Split(32, 32);
            this.textures = new Dictionary<Links, TextureRegion>();
            textures.Add(new Links(false, true, false, true), tiles[0][0]);
            textures.Add(new Links(false, true, true, true), tiles[1][0]);
            textures.Add(new Links(false, true, true, false), tiles[2][0]);
            textures.Add(new Links(true, true, false, true), tiles[0][1]);
            textures.Add(new Links(true, true, true, true), tiles[1][1]);
            textures.Add(new Links(true, true, true, false), tiles[2][1]);
            textures.Add(new Links(true, false, false, true), tiles[0][2]);
            textures.Add(new Links(true, false, true, true), tiles[1][2]);
            textures.Add(new Links(true, false, true, false), tiles[2][2]);
            textures.Add(new Links(false, true, false, false), tiles[3][0]);
            textures.Add(new Links(true, false, false, false), tiles[3][1]);
            textures.Add(new Links(true, true, false, false), tiles[3][2]);
            textures.Add(new Links(false, false, false, true), tiles[0][3]);
            textures.Add(new Links(false, false, true, false), tiles[1][3]);
            textures.Add(new Links(false, false, true, true), tiles[2][3]);
            textures.Add(new Links(false, false, false, false), tiles[3][3]);
            this.tiles = new TextureRegion[walls.Length][];
            for (int i = 0; i < walls.Length; i++)
            {
                this.tiles[i] = new TextureRegion[walls[i].Length];
                for (int j = 0; j < walls[i].Length; j++)
                {
                    if (walls[i][j] == 1)
                    {
                        bool up, down, left, right;
                        up = j < walls[i].Length - 1 && walls[i][j + 1] == 1; 
                        down = j > 0 && walls[i][j - 1] == 1;
                        left = i > 0 && walls[i - 1][j] == 1;
                        right = i < walls.Length - 1 && walls[i + 1][j] == 1;
                        this.tiles[i][j] = textures[new Links(up, down, left, right)];
                    }
                }
            }
        }

        public TextureRegion[][] GetTextures()
        {
            return tiles;
        }
    }

    public class Links
    {
        public bool up, down, left, right;

        public Links(bool up, bool down, bool left, bool right)
        {
            this.up = up;
            this.left = left;
            this.right = right;
            this.down = down;
        }

        public override string ToString()
        {
            return "^" + up + "\nv" + down + "\n<" + left + "\n>" + right;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == this.GetType())
            {
                Links other = (Links)obj;
                return up == other.up && down == other.down && left == other.left && right == other.right;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (up ? 1 : 0) + (down ? 10 : 0) + (left ? 100 : 0) + (right ? 1000 : 0);
        }
    }
}
