#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.GamerServices;
using Entities;
using Spacemageddon;
#endregion
namespace LibGDX_Port
{
    public class TextureRegion
    {
        private Texture2D source;
        private Rectangle region;
        private Vector2 stretch;
        private bool flipX, flipY;
        public TextureRegion(Rectangle region, Texture2D source)
        {
            this.source = source;
            this.region = region;
        }

        public TextureRegion(Texture2D source) : this(new Rectangle(0, 0, source.Width, source.Height), source) { }

        public int X { get { return region.X; } set { region.X = value; } }
        public int Y { get { return region.Y; } set { region.Y = value; } }
        public int Width { get { return region.Width; } set { region.Width = value; } }
        public int Height { get { return region.Height; } set { region.Height = value; } }
        public bool FlipX { get { return flipX; } set { flipX = value; } }
        public bool FlipY { get { return flipY; } set { flipY = value; } }
        public void Draw(SpriteBatch batch, Vector2 position) 
        {
            this.Draw(batch, position, new Vector2(1, 1));
        }

        public void Draw(SpriteBatch batch, Vector2 position, Vector2 scale)
        {
            Rectangle destination = new Rectangle();
            destination.X = (int)position.X;
            destination.Y = (int)position.Y;
            destination.Width = region.Width;
            destination.Height = region.Height;
            destination = Main.Scale(destination, scale);
            SpriteEffects effect = SpriteEffects.None;
            if (flipX)
                effect = SpriteEffects.FlipHorizontally;
            if (flipY)
                effect = effect | SpriteEffects.FlipVertically;
            batch.Draw(source, destination, region, Color.White, 0, new Vector2(0, 0), effect, 0);
        }

        public TextureRegion[][] Split(int width, int height)
        {
            TextureRegion[][] regions = new TextureRegion[region.Width / width][];
            for (int i = 0; i < regions.Length; i++)
            {
                regions[i] = new TextureRegion[region.Height / height];
                for (int j = 0; j < region.Height / height; j++)
                {
                    regions[i][j] = new TextureRegion(new Rectangle(i * width, j * height, width, height), source);
                }
            }
            return regions;
        }
    }

    public class Content
    {
        private LinkedList<IDisposable> content;

        public Content()
        {
            content = new LinkedList<IDisposable>();
        }

        public T Add<T>(T item) where T : IDisposable
        {
            content.AddFirst(item);
            return item;
        }

        public void Dispose()
        {
            foreach (IDisposable item in content)
                item.Dispose();
        }
    }

    public class ShapeRenderer : IDisposable
    {
        Texture2D data;
        Color color;
        Vector2 scale;

        public ShapeRenderer(GraphicsDevice device)
        {
            Texture2D rect = new Texture2D(device, 80, 30);
            Color[] data = new Color[80 * 30];
            for (int i = 0; i < data.Length; ++i) data[i] = Color.Chocolate;
                rect.SetData(data);
            this.data = rect;
            scale = new Vector2(1, 1);
        }

        public void setScale(Vector2 scale)
        {
            this.scale = scale;
        }

        public void setColor(Color c)
        {
            this.color = c;
        }

        public void setColor(float r, float g, float b, float a)
        {
            this.color = new Color((int)(r * 255), (int)(g * 255), (int)(b * 255), (int)(a * 255));
        }

        public void rect(SpriteBatch batch, Rectangle rect)
        {
            rect.X = (int)(rect.X * scale.X);
            rect.Y = (int)(rect.Y * scale.Y);
            rect.Width = (int)(rect.Width * scale.X);
            rect.Height = (int)(rect.Height * scale.Y);
            batch.Draw(data, rect, color);
        }

        public void rect(SpriteBatch batch, Rectangle rect, Color color)
        {
            rect.X = (int)(rect.X * scale.X);
            rect.Y = (int)(rect.Y * scale.Y);
            rect.Width = (int)(rect.Width * scale.X);
            rect.Height = (int)(rect.Height * scale.Y);
            batch.Draw(data, rect, color);
        }

        public void Dispose()
        {
            data.Dispose();
        }
    }
}
