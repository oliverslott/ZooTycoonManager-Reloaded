using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZooTycoonManager
{
    public class ScoreManager
    {
        private static ScoreManager _instance;
        public static ScoreManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (_instance == null)
                    {
                        _instance = new ScoreManager();
                    }
                }
                return _instance;
            }
        }
        private ScoreManager()
        {

        }

        private int _score = 60;

        public int Score
        {
            get => _score;
            set
            {
                if (value < 0 || value > 100)
                    return;
                _score = value;
            }
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont spriteFont, Vector2 position)
        {
            var scoreText = spriteFont.MeasureString(Score.ToString());
            spriteBatch.DrawString(spriteFont, Score.ToString(), position - scoreText / 2, Color.White, 0, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
        }
    }
}