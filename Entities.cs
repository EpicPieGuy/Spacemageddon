#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.GamerServices;
using Spacemageddon;
using LibGDX_Port;
#endregion

namespace Entities
{
    abstract class Entity
    {
        private Vector2 position, velocity, dimensions;
        private float health;
        public Texture2D texture;

        public Entity(float x, float y, float vX, float vY, Texture2D t)
        {
            this.position = new Vector2(x, y);
            this.velocity = new Vector2(vX, vY);
            this.texture = t;
            if (this.texture != null)
                dimensions = new Vector2(texture.Width, texture.Height);
            else
                dimensions = new Vector2(Main.TILE, Main.TILE);
        }

        public Entity(float x, float y, Texture2D t) : this(x, y, 0, 0, t) { }

        public Entity(Texture2D t) : this(0, 0, t) { }

        public void applyVelocity()
        {
            this.position += this.velocity;
        }
        public float X { get  { return this.position.X;  } set  {  this.position.X = value; } }
        public float Y   { get  { return this.position.Y; }  set { this.position.Y = value; } }
        public float VelocityX { get { return this.velocity.X; } set { this.velocity.X = value; } }
        public float VelocityY { get { return this.velocity.Y; } set { this.velocity.Y = value; } }
        public float Health { get { return this.health; } set { this.health = value; } }
        public Texture2D Texture { get { return this.texture; } }
        public void setPosition(float x, float y)
        {
            this.position.X = x;
            this.position.Y = y;
        }

        public Vector2 getPosition()
        {
            return new Vector2(this.position.X, this.position.Y);
        }

        public Vector2 getVelocity()
        {
            return new Vector2(this.velocity.X, this.velocity.Y);
        }


        public void translate(float x, float y)
        {
            this.translate(new Vector2(x, y));
        }

        public void translate(Vector2 amt)
        {
            this.position += amt;
        }

        public void setVelocity(float x, float y)
        {
            this.velocity = new Vector2(x, y);
        }

        public void addVelocity(float x, float y)
        {
            this.velocity.X += x;
            this.velocity.Y += y;
        }

        public void draw(SpriteBatch batch)
        {
            batch.Draw(this.texture, this.getBounds(), Color.White);
        }

        public virtual Rectangle getBounds()
        {
            return new Rectangle((int)this.position.X, (int)this.position.Y,
                    (int)this.dimensions.X, (int)this.dimensions.Y);
        }

        public void move(int[][] walls)
        {
            int x, y;
            int vX, vY;
            x = (int)this.position.X;
            y = (int)this.position.Y;
            vX = (int)Math.Round(this.velocity.X);
            vY = (int)Math.Round(this.velocity.Y);
            Rectangle bounds = this.getBounds();
            bounds.X += (int)vX;
            while (!free(bounds, walls) && vX != 0)
            {
                if (Math.Abs(vX) < 1) vX = 0;
                if (vX > 0)
                    vX--;
                else if (vX < 0)
                    vX++;
                bounds = this.getBounds();
                bounds.X += vX;
            }
            bounds = this.getBounds();
            bounds.Y += (int)vY;
            while (!free(bounds, walls) && vY != 0)
            {
                if (Math.Abs(vY) < 1) vY = 0;
                if (vY > 0)
                    vY--;
                else if (vY < 0)
                    vY++;
                bounds = this.getBounds();
                bounds.Y += vY;
            }
            this.velocity.X = vX;
            this.velocity.Y = vY;
            this.applyVelocity();
        }

        public void damage()
        {
            this.damage(1);
        }

        public virtual void damage(int amt)
        {
            for (int i = 0; i < amt; i++)
                this.health -= 1;
        }

        protected void walk(int amt, int[][] walls)
        {
            Rectangle newBounds = this.getBounds();
            newBounds.X += amt;
            newBounds.Y += 2;
            newBounds.Height -= 4;
            if (free(newBounds, walls))
            {
                this.translate(amt, 0);
            }
            else
            {
                amt -= Math.Abs(amt) / amt;
                if (amt != 0)
                    walk(amt, walls);
            }
        }

        public static bool valid(Vector2 position)
        {
            return position.X >= 0 &&
                    position.X < Main.WIDTH &&
                    position.Y >= 0 &&
                    position.Y < Main.HEIGHT;
        }

        public static bool valid(float x, float y)
        {
            return valid(new Vector2(x, y));
        }

        public static bool valid(Rectangle region)
        {
            return valid(region.X, region.Y) &&
                    valid(region.X + region.Width, region.Y + region.Height);
        }

        public static bool free(Vector2 position, int[][] walls)
        {
            int x, y;
            x = (int)position.X / Main.TILE;
            y = (int)position.Y / Main.TILE;
            if (x < 0 || y < 0 || x >= walls.Length || y >= walls[x].Length)
                return false;
            return valid(position) && walls[x][y] == 0;
        }

        public static bool free(Rectangle region, int[][] walls)
        {
            return free(new Vector2(region.X, region.Y), walls) &&
                    free(new Vector2(region.X + region.Width, region.Y), walls) &&
                    free(new Vector2(region.X, region.Y + region.Height), walls) &&
                    free(new Vector2(region.X + region.Width, region.Y + region.Height), walls);
        }
    }

    class Player : Entity
    {
        private int facing, invincibility, state, walkState, walkFrame, amtDamage, damageTimer, fallDelay;
	    private TextureRegion[] textures;
        private TextureRegion respawnTex;
        private bool jumping = false;
        public bool respawning = false;
        private KeyboardState previous;
        public Dictionary<Player.Abilities, bool> abilities;

	    public Player(int x, int y, Texture2D t) : base(x, y, t)
	    {
		    this.invincibility = 0;
		    this.amtDamage = 1;
		    walkState = 0;
			this.setSpritesheet(new TextureRegion(Main.loadTexture("spaceCharacter.png")));
            respawnTex = new TextureRegion(Main.loadTexture("respawnPod.png"));
            abilities = new Dictionary<Abilities, bool>();
            abilities[Abilities.Harp] = false;
            abilities[Abilities.Light] = false;
            abilities[Abilities.Plenty] = false;
            abilities[Abilities.Life] = false;
            abilities[Abilities.Scythe] = false;
            abilities[Abilities.Sword] = false;
            abilities[Abilities.Swarm] = false;
            abilities[Abilities.Staff] = false;
	    }
	
	    public void update(int[][] walls)
	    {
            if (abilities[Abilities.Light] && this.Health < (this.abilities[Player.Abilities.Life] ? 10 : 5))
                this.Health += 0.01f;
            KeyboardState keyboard = Keyboard.GetState();
            int walk = (keyboard.IsKeyDown(Keys.LeftShift)) ? 6 : 3;
		    state = 0;
		    jumping = false;
		    //Update game state
		    if(keyboard.IsKeyDown(Keys.Left) && !respawning)
		    {
			    base.walk(-walk, walls);
			    this.facing = -1;
			    walkFrame = (walkFrame + 1) % 40;
			    walkState = walkFrame / 10;
		    }
            else if (keyboard.IsKeyDown(Keys.Right) && !respawning)
		    {
			    base.walk(walk, walls);
			    this.facing = 1;
			    walkFrame = (walkFrame + 1) % 40;
			    walkState = walkFrame / 10;
		    }
		    else
		    {
			    walkState = 1;
		    }
            Rectangle bounds = base.getBounds();
            bounds.Y -= 1;
            if (keyboard.IsKeyDown(Keys.Space) && !previous.IsKeyDown(Keys.Space) && !free(bounds, walls))
		    {
			    this.addVelocity(0, 8);
		    }
		    if(free(bounds, walls))
			    jumping = true;
            if (keyboard.IsKeyDown(Keys.F) && !respawning)
			    state = 4;
            if (fallDelay == 0)
            {
                base.addVelocity(0, -1f);
                if (keyboard.IsKeyDown(Keys.Space))
                    fallDelay = 4;
                else
                    fallDelay = 1;
            }
            else
                fallDelay -= 1;
		    base.move(walls);
		    if(this.invincibility > 0)
			    this.invincibility -= 1;
		    if(this.damageTimer >= 0) 
			    this.damageTimer--;
		    if(this.damageTimer == 0)
			    this.amtDamage /= 2;
            bounds = base.getBounds();
            bounds.Y -= 1;
            if (respawning && !free(bounds, walls))
                respawning = false;
            previous = Keyboard.GetState();
	    }
	
	    public int getFacing()
	    {
		    return this.facing;
	    }
	
	    public int getInvulnFrames()
	    {
		    return this.invincibility;
	    }
	
	    public int getDamage()
	    {
		    return this.amtDamage * (this.abilities[Abilities.Sword] ? 2 : 1);
	    }
	
	    public void giveDamageBuff()
	    {
		    if(damageTimer == -1)
		    {
			    this.amtDamage *= 2;
			    this.damageTimer = 60 * 4;
		    }
	    }

	    public override void damage(int amt)
	    {
		    if(this.invincibility <= 0)
		    {
                this.Health -= amt;
			    this.invincibility = 90;
		    }
	    }

        public override Rectangle getBounds()
        {
            Rectangle rect = base.getBounds();
            rect.X += Main.TILE / 4;
            rect.Width -= Main.TILE / 2;
            rect.Y += 1;
            rect.Height -= 2;
            return rect;
        }
	
	    public TextureRegion getTexture()
	    {
            if (respawning)
                return respawnTex;
		    TextureRegion tex;
		    int index = state;
		    if(!jumping)
			    if(walkState == 3)
				    index += 1;
			    else
				    index += walkState;
		    else
			    index += 3;
		    tex = textures[index];
            tex.FlipX = !(facing == 1);
		    return tex;
	    }
	
	    public void setSpritesheet(TextureRegion tr)
	    {
		    textures = new TextureRegion[8];
		    TextureRegion[][] sprites = tr.Split(32, 32);
		    for(int i = 0; i < textures.Length; i++)
		    {
			    int x, y;
			    x = i % 4;
			    y = i / 4;
			    textures[i] = sprites[x][y];
		    }
	    }

        public enum Abilities
        {
            Scythe, Sword, Staff, Swarm, Plenty, Life, Harp, Light
        }
    }

    class Enemy : Entity
    {
	    private Type type;
        private int delay, facing, yHeight, frame;
        private static Dictionary<Type, TextureRegion> textures;
        private static Dictionary<Type, int> frames;
        public bool poisoned;
	    static Enemy()
	    {
			textures = new Dictionary<Type, TextureRegion>();
			textures.Add(Type.Ghost, new TextureRegion(Main.loadTexture("ghost.png")));
			TextureRegion patrol = new TextureRegion(Main.loadTexture("enemyPatrol.png"));
            TextureRegion flying = new TextureRegion(Main.loadTexture("flyingEnemy.png"));
            TextureRegion flyShooting = new TextureRegion(Main.loadTexture("flyEnemyShooting.png"));
			textures.Add(Type.Patrol, patrol.Split(32, 32)[0][0]);
			textures.Add(Type.PatrolFiring, patrol.Split(32, 32)[0][1]);
			textures.Add(Type.Fly, flying);
			textures.Add(Type.FlyFiring, flyShooting);
			textures.Add(Type.FlySlamming, patrol.Split(32, 32)[0][0]);
			textures.Add(Type.Turret, new TextureRegion(Main.loadTexture("turret.png")));
            textures.Add(Type.Robot, new TextureRegion(Main.loadTexture("robot.png")));
            frames = new Dictionary<Type, int>();
            frames.Add(Type.Fly, 4);
            frames.Add(Type.FlyFiring, 4);
	    }
	
	    public Enemy(float x, float y, Texture2D t, int health) : this(x, y, 0, 0, t, Type.Patrol, health) { }
	
	    public Enemy(float x, float y, Texture2D t, Type type, int health) : this(x, y, 0, 0, t, type, health) {  }
	
	    public Enemy(float x, float y, float vX, float vY, Texture2D t, Type type, int health) : base(x, y, vX, vY, t)
	    {
		    this.type = type;
		    delay = 0;
		    yHeight = (int)y;
		    switch(type)
		    {
			    case Type.Patrol:
			    case Type.PatrolFiring:
			    case Type.Fly:
			    case Type.FlyFiring:
				    this.setVelocity(2, 0);
				    break;
			    default:
				    break;
		    }
            frame = 0;
            switch (type)
            {
                case Type.Patrol:
                    this.Health = 3;
                    break;
                case Type.PatrolFiring:
                    this.Health = 4;
                    break;
                case Type.Fly:
                    this.Health = 3;
                    break;
                case Type.FlyFiring:
                    Health = 4;
                    break;
                case Type.Ghost:
                    Health = 2;
                    break;
                case Type.Turret:
                    Health = 2;
                    break;
                case Type.Robot:
                    Health = 5;
                    break;
            }
            poisoned = false;
	    }

        public int Facing { get { return facing; } }
	
	    public void update(int[][] walls)
	    {
            Random rand = new Random();
            if (poisoned && rand.NextDouble() <= 0.1)
            {
                Health--;
                if (rand.NextDouble() <= 0.2)
                    poisoned = false;
            }
		    if(this.type == Type.Ghost)
			    return;
		    Rectangle bounds = this.getBounds();
            bounds.X = (int)(this.X + this.VelocityX);
		    if(!free(bounds, walls))
			    this.setVelocity(-this.VelocityX, this.VelocityY);
		    else
		    {
			    bounds.Y -= 1;
			    if(free(bounds, walls) && this.type != Type.FlySlamming 
					    && this.type != Type.Fly && this.type != Type.FlyFiring)
				    this.setVelocity(-this.VelocityX, this.VelocityY);
			    else
				    this.applyVelocity();
		    }
		    if(this.delay > 0)
			    this.delay--;
		    if(this.type == Type.FlySlamming)
		    {
			    bounds = this.getBounds();
                bounds.X += 5;
			    if(!free(bounds, walls) || this.Y < yHeight)
				    this.translate(0, 1);
		    }
		    facing = (int) (Math.Abs(this.VelocityX) / this.VelocityX);
            if (frames.ContainsKey(this.type))
                frame = (frame + 1) % (frames[this.type] * 10);
	    }
	
	    public Bullet checkBullets(List<Bullet> bullets)
	    {
		    Bullet bullet = null;
		    List<Bullet>.Enumerator iterator = bullets.GetEnumerator();
		    while(iterator.MoveNext() && bullet == null)
		    {
			    bullet = iterator.Current;
			    bullet = (bullet.getBounds().Intersects(this.getBounds())) ? bullet : null;
		    }
		    return bullet;
	    }
	
	    public int getDelay()
	    {
		    return this.delay;
	    }
	
	    public void setDelay(int delay)
	    {
		    this.delay = delay;
	    }
	
	    public Type getType()
	    {
		    return this.type;
	    }
	
	    public TextureRegion getTex()
	    {
            TextureRegion tex;
            if(!frames.ContainsKey(this.type))
            {
		        tex = Enemy.textures[this.type];
            }
            else
            {
                TextureRegion sheet = Enemy.textures[this.type];
                TextureRegion[][] textures = sheet.Split(Main.TILE, Main.TILE);
                tex = textures[frame / 10][0];
            }
            tex.FlipX = !(facing == 1);
            return tex;
	    }

	    public enum Type
	    {
		    Patrol, PatrolFiring, Fly, FlyFiring, FlySlamming, Ghost, Turret, Robot
	    }
    }

    class Bullet : Entity
    {
	    private float radius;
	    public Bullet(float x, float y, float vX, float vY, Texture2D t) : this(x, y, vX, vY, 2, t)
	    {
		    
	    }
	
	    public Bullet(float x, float y, float vX, float vY, float radius, Texture2D t) : base(x, y, vX, vY, t)
	    {
		    this.radius = radius;
	    }
	
	    public override Rectangle getBounds()
	    {
		    return new Rectangle((int)(this.X - this.radius), (int)(this.Y - this.radius), (int)this.radius * 2, (int)this.radius * 2);
	    }
    }

    class Powerup : Entity
    {
	    private static Dictionary<Type, Texture2D> textures;
	    private Type type;
	    static Powerup()
	    {
		    textures = new Dictionary<Type, Texture2D>();
			textures.Add(Type.Health, Main.loadTexture("health.png"));
			textures.Add(Type.Damage, Main.loadTexture("damage.png"));
	    }
	
	    public Powerup(float x, float y, Type t) : base(x, y, textures[t])
	    {
		    this.type = t;
	    }
	
	    public Texture2D getTexture()
	    {
		    return textures[this.type];
	    }
	
	    public Type getType()
	    {
		    return type;
	    }

	    public enum Type
	    {
		    Health, Damage
	    }
    }

    class Shield : Entity
    {
	    private int life;
	    public const int MAX_LIFE = 30;
        private new static Texture2D texture;
        static Shield()
        {
            texture = Main.loadTexture("shield.png");
        }
        public Shield(float x, float y)  : base(x, y, texture)
	    {
		    life = MAX_LIFE;
	    }
	
	    public bool decreaseLife()
	    {
		    life--;
		    return life <= 0;
	    }

    }

    class Boss : Entity
    {
	    private static Dictionary<Type, TextureRegion> textures;
	    private int facing, delay;
	    private Type type;
	    static Boss()
	    {
		    textures = new Dictionary<Type, TextureRegion>();
			textures.Add(Type.Death, new TextureRegion(Main.loadTexture("death.png")));
	    }
	    public Boss(float x, float y, Texture2D t) : base(x, y, t)
	    {
		    this.facing = 1;
		    this.type = Type.Death;
            this.Health = 10;
	    }
	
	    public Type getType()
	    {
		    return this.type;
	    }
	
	    public int getDelay()
	    {
		    return this.delay;
	    }
	
	    public void setDelay(int delay)
	    {
		    this.delay = delay;
	    }
	
	    public TextureRegion getTexture()
        {
            TextureRegion tex = textures[this.type];
            tex.FlipX = !(facing == 1);
            return tex;
	    }
	
	    public enum Type
	    {
		    Death, War, Pestilence, Famine
	    }
    }

    class Checkpoint : Entity
    {
        private static Checkpoint selected;
        private static TextureRegion[][] textures;
        private int frame;
        static Checkpoint()
        {
            Texture2D sheet = Main.loadTexture("checkpoint.png");
            textures = new TextureRegion(sheet).Split(32, 32);
        }

        public Checkpoint(float x, float y)
            : base(x, y, null)
        {

        }

        public void Activate()
        {
            if (selected != this)
            {
                selected = this;
                frame = 1;
            }
        }

        public void Update()
        {
            if (frame > 0 && frame < (textures.Length * 10) - 1)
                frame++;
            if (selected != this)
                frame = 0;
        }
        
        public TextureRegion GetTexture()
        {
            return textures[frame / 10][0];
        }

        public static Checkpoint ActiveCheckpoint
        {
            get
            {
                return selected;
            }
        }
    }
    class Door : Entity
    {
        private int animation;
        private static Texture2D sheet;
        private static TextureRegion[][] frames;
        private bool animating;
        public bool Animating { get { return animating; } }

        static Door()
        {
            sheet = Main.loadTexture("door.png");
            frames = new TextureRegion(sheet).Split(32, 32);
        }

        public Door(float x, float y)
            : base(x, y, null)
        {
            animation = 0;
        }

        public TextureRegion getTexture()
        {
            return frames[animation / 10][0];
        }

        public bool update(Player p)
        {
            if (this.getBounds().Intersects(p.getBounds()))
            {
                animating = true;
                Main.Wait((frames.Length - 1) * 10);
            }
            if (animating)
                animation += 1;
            return animation >= 50;
        }
    }
}
