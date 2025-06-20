using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZooTycoonManager.Components
{
    public class AnimalCountUIComponent : Component
    {
        private TextRenderComponent _textRenderComponent;
        public override void Initialize()
        {
            _textRenderComponent = Owner.GetComponent<TextRenderComponent>();
        }

        public override void Update(GameTime gameTime)
        {
            _textRenderComponent.Text = $"Animals: {GameWorld.Instance.GetAnimals().Count}";
        }
    }
}
