using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using ZooTycoonManager.Components;
using ZooTycoonManager.Enums;

namespace ZooTycoonManager
{
    public class GameObject
    {
        private List<Component> _components = new List<Component>();

        public TransformComponent Transform { get; set; }

        public RenderLayer Layer { get; set; } = RenderLayer.World;

        public bool IsActive { get; set; } = true;

        public GameObject(Vector2 startingPosition)
        {
            Transform = new TransformComponent(startingPosition);
            AddComponent(Transform);
        }

        public void AddComponent(Component component)
        {
            _components.Add(component);
            component.Owner = this;
            component.Initialize();
        }

        public T GetComponent<T>() where T : Component
        {
            return _components.OfType<T>().FirstOrDefault();
        }

        public bool TryGetComponent<T>(out T component) where T : Component
        {
            component = GetComponent<T>();
            return component != null;
        }

        public void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            foreach (var component in _components)
            {
                component.Update(gameTime);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsActive) return;

            foreach (var component in _components)
            {
                component.Draw(spriteBatch);
            }
        }

        public virtual void LoadContent(ContentManager contentManager)
        {
            foreach (var component in _components)
            {
                component.LoadContent(contentManager);
            }
        }
    }
}

