using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
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
            animal.GetComponent<RenderComponent>().Width = 32;
            animal.GetComponent<RenderComponent>().Height = 32;
            //animal.GetComponent<RenderComponent>().sca
            animal.LoadContent(GameWorld.Instance.Content);
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
            visitor.AddComponent(new RenderComponent("294f5329-d985-4d20-86d5-98e9dfb256fc"));
            visitor.AddComponent(new MovableComponent());
            visitor.AddComponent(new VisitorComponent());
            visitor.LoadContent(GameWorld.Instance.Content);
            return visitor;
        }

        public static GameObject CreateShop(Vector2 spawnPosition)
        {
            var shop = new GameObject(spawnPosition);
            shop.AddComponent(new RenderComponent("foodshopsprite_cut"));
            shop.AddComponent(new ShopComponent());
            shop.LoadContent(GameWorld.Instance.Content);

            shop.GetComponent<RenderComponent>().Width = 3 * GameWorld.TILE_SIZE;
            shop.GetComponent<RenderComponent>().Height = 3 * GameWorld.TILE_SIZE;

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
            fence.AddComponent(new RenderComponent("fence"));
            fence.LoadContent(GameWorld.Instance.Content);
            return fence;
        }

        public static GameObject CreateText(Vector2 position, string text)
        {
            var newText = new GameObject(position);
            newText.Layer = RenderLayer.Screen;
            newText.AddComponent(new TextRenderComponent(text, "font"));
            newText.LoadContent(GameWorld.Instance.Content);
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
            buttonObj.LoadContent(GameWorld.Instance.Content);

            var textRenderComponent = new TextRenderComponent(text, "font");
            buttonObj.AddComponent(textRenderComponent);

            var clickable = new ClickableComponent();
            clickable.OnClick += onClickAction;
            buttonObj.AddComponent(clickable);
            buttonObj.LoadContent(GameWorld.Instance.Content);
            return buttonObj;
        }

        public static GameObject CreateButtonWithIcon(Vector2 position, string text, Action onClickAction, ButtonSize buttonSize = ButtonSize.Big)
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
            buttonObj.LoadContent(GameWorld.Instance.Content);

            var textRenderComponent = new TextRenderComponent(text, "font");
            buttonObj.AddComponent(textRenderComponent);

            var clickable = new ClickableComponent();
            clickable.OnClick += onClickAction;
            buttonObj.AddComponent(clickable);
            buttonObj.LoadContent(GameWorld.Instance.Content);
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
                menuComponent.AddMenuItem(button);
            }

            //GameWorld.Instance.Instantiate(newBtn);

            menuObject.LoadContent(GameWorld.Instance.Content);
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

            fpsCounterObj.LoadContent(GameWorld.Instance.Content);
            return fpsCounterObj;
        }
    }
}
