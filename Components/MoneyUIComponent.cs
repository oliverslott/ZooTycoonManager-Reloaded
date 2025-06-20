using Microsoft.Xna.Framework;
using System.Globalization;

namespace ZooTycoonManager.Components
{
    public class MoneyUIComponent : Component
    {
        private TextRenderComponent _textRenderComponent;
        public override void Initialize()
        {
            _textRenderComponent = Owner.GetComponent<TextRenderComponent>();
        }

        public override void Update(GameTime gameTime)
        {
            _textRenderComponent.Text = string.Format(CultureInfo.CurrentCulture, "{0:N0} $", MoneyManager.Instance.CurrentMoney);
        }
    }
}
