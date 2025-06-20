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
        private List<GameObject> _children = new List<GameObject>();

        public GameObject Parent { get; set; }

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

        public void AddChild(GameObject child)
        {
            if(child.Parent != null)
            {
                child.Parent._children.Remove(child);
            }
            child.Parent = this;
            _children.Add(child);
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

            foreach(var child in _children)
            {
                child.Update(gameTime);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsActive) return;

            foreach (var component in _components)
            {
                component.Draw(spriteBatch);
            }

            foreach(var child in _children)
            {
                child.Draw(spriteBatch);
            }
        }

        public virtual void LoadContent(ContentManager contentManager)
        {
            foreach (var component in _components)
            {
                component.LoadContent(contentManager);
            }

            foreach(var child in _children)
            {
                child.LoadContent(contentManager);
            }
        }
    }
}

