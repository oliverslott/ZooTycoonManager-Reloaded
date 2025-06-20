using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using ZooTycoonManager.Components;
using ZooTycoonManager.Enums;

namespace ZooTycoonManager
{
    public static class EntityFactory
    {
        public static GameObject CreateAnimal(Vector2 spawnPosition, AnimalTypes animalType, GameObject habitat)
        {
            var animal = new GameObject(spawnPosition);
            animal.AddComponent(new AnimalComponent(habitat));
            animal.AddComponent(new MovableComponent());
            switch (animalType)
            {
                case AnimalTypes.Buffalo:
                    animal.AddComponent(new RenderComponent("EnragedBuffalo"));
                    break;
                case AnimalTypes.Orangutan:
                    animal.AddComponent(new RenderComponent("AgitatedOrangutan"));
                    break;
                case AnimalTypes.Kangaroo:
                    animal.AddComponent(new RenderComponent("HoppingKangaroo"));
                    break;
                case AnimalTypes.Elephant:
                    animal.AddComponent(new RenderComponent("StompingElephant"));
                    break;
                case AnimalTypes.Polarbear:
                    animal.AddComponent(new RenderComponent("PolarBear"));
                    break;
                case AnimalTypes.Turtle:
                    animal.AddComponent(new RenderComponent("SlowTurtle"));
                    break;
                case AnimalTypes.Camel:
                    animal.AddComponent(new RenderComponent("ThirstyCamel"));
                    break;
                case AnimalTypes.Bear:
                    animal.AddComponent(new RenderComponent("KodiakBear"));
                    break;
                case AnimalTypes.Wolf:
                    animal.AddComponent(new RenderComponent("ArcticWolf"));
                    break;
                case AnimalTypes.Chimpanze:
                    animal.AddComponent(new RenderComponent("MindfulChimpanze"));
                    break;
            }
            animal.GetComponent<RenderComponent>().SourceRectangle = new Rectangle(0, 0, 16, 16);
            animal.GetComponent<RenderComponent>().CenterOrigin = true;

            animal.GetComponent<RenderComponent>().SetSize(new Vector2(32, 32));
            return animal;
        }

        public static GameObject CreateHabitat(Vector2 spawnPosition, HabitatSizeType size)
        {
            var habitat = new GameObject(spawnPosition);
            habitat.AddComponent(new HabitatComponent(size));
            return habitat;
        }

        public static GameObject CreateVisitor(Vector2 spawnPosition)
        {
            var visitor = new GameObject(spawnPosition);
            var renderComponent = new RenderComponent("294f5329-d985-4d20-86d5-98e9dfb256fc");
            renderComponent.CenterOrigin = true;
            visitor.AddComponent(renderComponent);
            visitor.AddComponent(new MovableComponent());
            visitor.AddComponent(new VisitorComponent());
            visitor.AddComponent(new HungerComponent());
            visitor.AddComponent(new MoodComponent());
            visitor.AddComponent(new ClickableComponent());
            return visitor;
        }

        public static GameObject CreateShop(Vector2 spawnPosition)
        {
            var shop = new GameObject(spawnPosition);
            shop.AddComponent(new RenderComponent("foodshopsprite_cut"));
            shop.AddComponent(new ShopComponent());
            shop.GetComponent<RenderComponent>().SetSize(new Vector2(3 * GameWorld.TILE_SIZE, 3 * GameWorld.TILE_SIZE));

            return shop;
        }

        public static GameObject CreateZookeeper(Vector2 spawnPosition)
        {
            var zookeeper = new GameObject(spawnPosition);
            zookeeper.AddComponent(new ZookeeperComponent());
            return zookeeper;
        }

        public static GameObject CreateFence(Vector2 spawnPosition)
        {
            var fence = new GameObject(spawnPosition);
            var renderComponent = new RenderComponent("fence");
            renderComponent.CenterOrigin = true;
            fence.AddComponent(renderComponent);
            return fence;
        }

        public static GameObject CreateText(Vector2 position, string text)
        {
            var newText = new GameObject(position);
            newText.Layer = RenderLayer.Screen;
            newText.AddComponent(new TextRenderComponent(text, "font"));
            return newText;
        }

        public static GameObject CreateButton(Vector2 position, string text, Action onClickAction, ButtonSize buttonSize = ButtonSize.Big)
        {
            var buttonObj = new GameObject(position);
            buttonObj.Layer = RenderLayer.Screen;
            RenderComponent renderComponent;
            switch (buttonSize)
            {
                case ButtonSize.Small:
                    renderComponent = new RenderComponent("Button_Blue");
                    break;
                case ButtonSize.Big:
                    renderComponent = new RenderComponent("ButtonTexture");
                    break;
                default:
                    renderComponent = new RenderComponent("ButtonTexture");
                    break;
            }
            buttonObj.AddComponent(renderComponent);

            var textRenderComponent = new TextRenderComponent(text, "font");
            textRenderComponent.Offset = new Vector2(renderComponent.Width / 2, renderComponent.Height / 2);
            buttonObj.AddComponent(textRenderComponent);

            var clickable = new ClickableComponent();
            clickable.OnClick += onClickAction;
            buttonObj.AddComponent(clickable);
            return buttonObj;
        }

        public static GameObject CreateButtonWithIcon(Vector2 position, string texturePath, Action onClickAction, ButtonSize buttonSize = ButtonSize.Big)
        {
            var buttonObj = new GameObject(position);
            buttonObj.Layer = RenderLayer.Screen;
            RenderComponent renderComponent;
            switch (buttonSize)
            {
                case ButtonSize.Small:
                    renderComponent = new RenderComponent("Button_Blue");
                    break;
                case ButtonSize.Big:
                    renderComponent = new RenderComponent("ButtonTexture");
                    break;
                default:
                    renderComponent = new RenderComponent("ButtonTexture");
                    break;
            }
            buttonObj.AddComponent(renderComponent);
            var iconRenderComponent = new RenderComponent(texturePath);
            var iconPosition = position + new Vector2(
                (renderComponent.Width - iconRenderComponent.Width) / 2,
                (renderComponent.Height - iconRenderComponent.Height) / 2
            );
            var childObj = new GameObject(iconPosition);
            childObj.AddComponent(new RenderComponent(texturePath));
            buttonObj.AddChild(childObj);

            var clickable = new ClickableComponent();
            clickable.OnClick += onClickAction;
            buttonObj.AddComponent(clickable);
            return buttonObj;
        }

        public static GameObject CreateMenu(Vector2 position, List<GameObject> buttons)
        {
            var menuObject = new GameObject(position);
            menuObject.Layer = RenderLayer.Screen;

            var background = new RenderComponent("Button_Blue_9Slides");
            menuObject.AddComponent(background);

            var menuComponent = new MenuComponent();
            menuObject.AddComponent(menuComponent);

            foreach (var button in buttons)
            {
                menuObject.AddChild(button);
            }

            return menuObject;
        }

        public static GameObject CreateFPSCounter(Vector2 position)
        {
            var fpsCounterObj = new GameObject(position);
            fpsCounterObj.Layer = RenderLayer.Screen;

            var textComponent = new TextRenderComponent("FPS: 0.0", "font");
            textComponent.Color = Color.White;
            fpsCounterObj.AddComponent(textComponent);

            var fpsCalc = new FPSCounterComponent();
            fpsCounterObj.AddComponent(fpsCalc);

            return fpsCounterObj;
        }

        public static GameObject CreateMoneyUI(Vector2 position)
        {
            var gameObject = new GameObject(position);
            gameObject.Layer = RenderLayer.Screen;

            var renderComponent = new RenderComponent("ButtonTexture");
            gameObject.AddComponent(renderComponent);

            var textRenderComponent = new TextRenderComponent("0", "font");
            textRenderComponent.Offset = new Vector2(renderComponent.Width / 2, renderComponent.Height / 2);
            gameObject.AddComponent(textRenderComponent);

            gameObject.AddComponent(new MoneyUIComponent());
            return gameObject;
        }

        public static GameObject CreateVisitorUI(Vector2 position)
        {
            var gameObject = new GameObject(position);
            gameObject.Layer = RenderLayer.Screen;

            var renderComponent = new RenderComponent("ButtonTexture");
            gameObject.AddComponent(renderComponent);

            var textRenderComponent = new TextRenderComponent("0", "font");
            textRenderComponent.Offset = new Vector2(renderComponent.Width / 2, renderComponent.Height / 2);
            gameObject.AddComponent(textRenderComponent);

            gameObject.AddComponent(new VisitorCountUIComponent());
            return gameObject;
        }

        public static GameObject CreateAnimalUI(Vector2 position)
        {
            var gameObject = new GameObject(position);
            gameObject.Layer = RenderLayer.Screen;

            var renderComponent = new RenderComponent("ButtonTexture");
            gameObject.AddComponent(renderComponent);

            var textRenderComponent = new TextRenderComponent("0", "font");
            textRenderComponent.Offset = new Vector2(renderComponent.Width / 2, renderComponent.Height / 2);
            gameObject.AddComponent(textRenderComponent);

            gameObject.AddComponent(new AnimalCountUIComponent());
            return gameObject;
        }

        public static GameObject CreateInfoPanel()
        {
            Vector2 panelSize = new Vector2(800, 300);
            Vector2 panelPosition = new Vector2(
                (GameWorld.Instance.GraphicsDevice.Viewport.Width - panelSize.X) / 2,
                (GameWorld.Instance.GraphicsDevice.Viewport.Height - panelSize.Y) / 2
            );

            var gameObject = new GameObject(panelPosition);
            gameObject.Layer = RenderLayer.Screen;
            var renderComponent = new RenderComponent("Button_Blue_9Slides");
            renderComponent.SetSize(panelSize);
            gameObject.AddComponent(renderComponent);
            string[] lines = new[]
            {
                "The big number at the top is your Zoo Score. It is influenced by the visitors' mood.",
                "So, remember to keep your visitors happy!",
                "They like to see happy animals, and have easy access to food when they're hungry.",
                "Remember to hire zookeepers to look out for your animals!",
                "",
                "Controls: ",
                "Use middle mouse or arrow keys to move camera",
                "Use mouse wheel to zoom",
                "You can undo and redo your actions by pressing Ctrl + Z and Ctrl + Y",
                "",
            };

            var textComponent = new TextRenderComponent(string.Join("\n", lines), "font");
            textComponent.Offset = new Vector2(renderComponent.Width / 2, renderComponent.Height / 2);

            gameObject.AddComponent(textComponent);
            gameObject.IsActive = false;
            return gameObject;
        }

        public static GameObject CreateEntityInfoPopup()
        {
            var gameObject = new GameObject(Vector2.Zero);
            gameObject.Layer = RenderLayer.Screen;
            gameObject.AddComponent(new EntityInfoPopupComponent());
            gameObject.IsActive = false;
            return gameObject;
        }
    }
}
