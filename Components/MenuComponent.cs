using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace ZooTycoonManager.Components
{
    public class MenuComponent : Component
    {
        private List<GameObject> _menuItems = new List<GameObject>();
        private bool _wasActive = false;

        public override void Initialize()
        {
            _wasActive = Owner.IsActive;
        }

        public void AddMenuItem(GameObject item)
        {
            _menuItems.Add(item);
            item.IsActive = Owner.IsActive;
        }

        public override void Update(GameTime gameTime)
        {
            if (Owner.IsActive != _wasActive)
            {
                foreach (var item in _menuItems)
                {
                    item.IsActive = Owner.IsActive;
                }
                _wasActive = Owner.IsActive;
            }
            if (Owner.IsActive)
            {
                foreach (var item in _menuItems)
                {
                    item.Update(gameTime);
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Owner.IsActive)
            {
                foreach (var item in _menuItems)
                {
                    item.Draw(spriteBatch);
                }
            }
        }
    }
}
