using Microsoft.Xna.Framework;

namespace ZooTycoonManager.Components
{
    public class VisitorCountUIComponent : Component
    {
        private TextRenderComponent _textRenderComponent;
        public override void Initialize()
        {
            _textRenderComponent = Owner.GetComponent<TextRenderComponent>();
        }

        public override void Update(GameTime gameTime)
        {
            _textRenderComponent.Text = $"Visitors: {GameWorld.Instance.GetVisitors().Count}";
        }
    }
}
