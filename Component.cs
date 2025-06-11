using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ZooTycoonManager
{
    public abstract class Component
    {
        public GameObject Owner { get; internal set; }
        public virtual void Initialize() { }

        public virtual void Update(GameTime gameTime) { }

        public virtual void LoadContent(ContentManager contentManager) { }

        public virtual void Draw(SpriteBatch spriteBatch) { }
    }
}
