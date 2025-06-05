using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZooTycoonManager
{
    public abstract class GameObject
    {
        public abstract void Draw(SpriteBatch spriteBatch);

        public abstract void Update();

        public abstract void LoadContent(ContentManager contentManager);

        public virtual void Reset()
        {

        }
    }
}

