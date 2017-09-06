﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Input;
using MouseState = osu.Framework.Input.MouseState;
using NUnit.Framework;

namespace osu.Framework.Desktop.Tests.Visual
{
    internal class TestCaseNestedMenus : TestCase
    {
        private const int max_depth = 5;
        private const int max_count = 5;

        private Random rng;

        private ManualInputManager inputManager;
        private MenuStructure menus;

        public TestCaseNestedMenus()
        {
            rng = new Random(1337);

            Add(createMenu());
        }

        [SetUp]
        public void SetUp()
        {
            Clear();

            rng = new Random(1337);

            Menu menu;
            Add(inputManager = new ManualInputManager
            {
                UseParentState = false,
                Children = new Drawable[]
                {
                    new CursorContainer(),
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = menu = createMenu()
                    }
                }
            });

            menus = new MenuStructure(menu);
            inputManager.MoveMouseTo(Vector2.Zero);
        }

        private Menu createMenu() => new ClickOpenMenu(TimePerAction)
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            AlwaysOpen = true,
            Items = new[]
            {
                generateRandomMenuItem("First"),
                generateRandomMenuItem("Second"),
                generateRandomMenuItem("Third"),
            }
        };

        private class ClickOpenMenu : Menu
        {
            protected override Menu CreateSubMenu() => new ClickOpenMenu(HoverOpenDelay) { RequireClickToOpen = false };

            public ClickOpenMenu(double timePerAction) : base(Direction.Vertical)
            {
                RequireClickToOpen = true;
                HoverOpenDelay = timePerAction;
            }
        }

        #region Test Cases

        /// <summary>
        /// Tests if the <see cref="Menu"/> respects <see cref="Menu.AlwaysOpen"/> = true, by not alowing it to be closed
        /// when a click happens outside the <see cref="Menu"/>.
        /// </summary>
        [Test]
        public void TestAlwaysOpen()
        {
            AddStep("Click outside", () => inputManager.Click(MouseButton.Left));
            AddAssert("Check AlwaysOpen = true", () => menus.GetSubMenu(0).State == MenuState.Open);
        }

        /// <summary>
        /// Tests if the hover state on <see cref="Menu.DrawableMenuItem"/>s is valid.
        /// </summary>
        [Test]
        public void TestHoverState()
        {
            AddAssert("Check submenu closed", () => menus.GetSubMenu(1)?.State != MenuState.Open);
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetMenuItem(0)));
            AddAssert("Check item hovered", () => menus.GetMenuItem(0).IsHovered);
        }

        /// <summary>
        /// Tests if the <see cref="Menu"/> respects <see cref="Menu.RequireClickToOpen"/> = true.
        /// </summary>
        [Test]
        public void TestRequireClickToOpen()
        {
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(0).GetMenuItem(0)));
            AddAssert("Check closed", () => menus.GetSubMenu(1)?.State != MenuState.Open);
            AddAssert("Check closed", () => menus.GetSubMenu(1)?.State != MenuState.Open);
            AddStep("Click item", () => inputManager.Click(MouseButton.Left));
            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Open);
        }

        /// <summary>
        /// Tests if clicking once on a menu that has <see cref="Menu.RequireClickToOpen"/> opens it, and clicking a second time
        /// closes it.
        /// </summary>
        [Test]
        public void TestDoubleClick()
        {
            AddStep("Click item", () => clickItem(0, 0));
            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Open);
            AddStep("Click item", () => clickItem(0, 0));
            AddAssert("Check closed", () => menus.GetSubMenu(1)?.State != MenuState.Open);
        }

        /// <summary>
        /// Tests whether click on <see cref="Menu.DrawableMenuItem"/>s causes sub-menus to instantly appear.
        /// </summary>
        [Test]
        public void TestInstantOpen()
        {
            AddStep("Click item", () => clickItem(0, 1));
            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Open);
            AddStep("Click item", () => clickItem(1, 0));
            AddAssert("Check open", () => menus.GetSubMenu(2).State == MenuState.Open);
        }

        /// <summary>
        /// Tests if clicking on an item that has no sub-menu causes the menu to close.
        /// </summary>
        [Test]
        public void TestActionClick()
        {
            AddStep("Click item", () => clickItem(0, 0));
            AddStep("Click item", () => clickItem(1, 0));
            AddAssert("Check closed", () => menus.GetSubMenu(1)?.State != MenuState.Open);
        }

        /// <summary>
        /// Tests if hovering over menu items respects the <see cref="Menu.HoverOpenDelay"/>.
        /// </summary>
        [Test]
        public void TestHoverOpen()
        {
            AddStep("Click item", () => clickItem(0, 1));
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(1).GetMenuItem(0)));
            AddAssert("Check closed", () => menus.GetSubMenu(2)?.State != MenuState.Open);
            AddAssert("Check open", () => menus.GetSubMenu(2).State == MenuState.Open);
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(2).GetMenuItem(0)));
            AddAssert("Check closed", () => menus.GetSubMenu(3)?.State != MenuState.Open);
            AddAssert("Check open", () => menus.GetSubMenu(3).State == MenuState.Open);
        }

        /// <summary>
        /// Tests if hovering over a different item on the main <see cref="Menu"/> will instantly open another menu
        /// and correctly changes the sub-menu items to the new items from the hovered item.
        /// </summary>
        [Test]
        public void TestHoverChange()
        {
            IReadOnlyList<MenuItem> currentItems = null;
            AddStep("Click item", () =>
            {
                clickItem(0, 0);
            });

            AddStep("Get items", () =>
            {
                currentItems = menus.GetSubMenu(1).Items;
            });

            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Open);
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(0).GetMenuItem(1)));
            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Open);

            AddAssert("Check new items", () => !menus.GetSubMenu(1).Items.SequenceEqual(currentItems));
            AddAssert("Check closed", () =>
            {
                int currentSubMenu = 3;
                while (true)
                {
                    var subMenu = menus.GetSubMenu(currentSubMenu);
                    if (subMenu == null)
                        break;

                    if (subMenu.State == MenuState.Open)
                        return false;
                    currentSubMenu++;
                }

                return true;
            });
        }

        /// <summary>
        /// Tests whether hovering over a different item on a sub-menu opens a new sub-menu in a delayed fashion
        /// and correctly changes the sub-menu items to the new items from the hovered item.
        /// </summary>
        [Test]
        public void TestDelayedHoverChange()
        {
            AddStep("Click item", () => clickItem(0, 2));
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(1).GetMenuItem(0)));
            AddAssert("Check closed", () => menus.GetSubMenu(2)?.State != MenuState.Open);
            AddAssert("Check open", () => menus.GetSubMenu(2).State == MenuState.Open);

            IReadOnlyList<MenuItem> currentItems = null;
            AddStep("Hover item", () =>
            {
                currentItems = menus.GetSubMenu(2).Items;
                inputManager.MoveMouseTo(menus.GetSubStructure(1).GetMenuItem(1));
            });

            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Open);
            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Open);

            AddAssert("Check new items", () => !menus.GetSubMenu(2).Items.SequenceEqual(currentItems));
            AddAssert("Check closed", () =>
            {
                int currentSubMenu = 3;
                while (true)
                {
                    var subMenu = menus.GetSubMenu(currentSubMenu);
                    if (subMenu == null)
                        break;

                    if (subMenu.State == MenuState.Open)
                        return false;
                    currentSubMenu++;
                }

                return true;
            });
        }

        /// <summary>
        /// Tests whether clicking on <see cref="Menu"/>s that have opened sub-menus don't close the sub-menus.
        /// </summary>
        [Test]
        public void TestMenuClicksDontClose()
        {
            AddStep("Click item", () => clickItem(0, 1));
            AddStep("Click item", () => clickItem(1, 0));
            AddStep("Click item", () => clickItem(2, 0));
            AddStep("Click item", () => clickItem(3, 0));

            for (int i = 3; i >= 1; i--)
            {
                int menuIndex = i;
                AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(menuIndex).GetMenuItem(0)));
                AddAssert("Check submenu open", () => menus.GetSubMenu(menuIndex + 1).State == MenuState.Open);
                AddStep("Click item", () => inputManager.Click(MouseButton.Left));
                AddAssert("Check all open", () =>
                {
                    for (int j = 0; j <= menuIndex; j++)
                    {
                        int menuIndex2 = j;
                        if (menus.GetSubMenu(menuIndex2)?.State != MenuState.Open)
                            return false;
                    }

                    return true;
                });
            }
        }

        /// <summary>
        /// Tests whether clicking on the <see cref="Menu"/> that has <see cref="Menu.RequireClickToOpen"/> closes all sub menus.
        /// </summary>
        [Test]
        public void TestMenuClickClosesSubMenus()
        {
            AddStep("Click item", () => clickItem(0, 1));
            AddStep("Click item", () => clickItem(1, 0));
            AddStep("Click item", () => clickItem(2, 0));
            AddStep("Click item", () => clickItem(3, 0));
            AddStep("Click item", () => clickItem(0, 1));

            AddAssert("Check submenus closed", () =>
            {
                for (int j = 1; j <= 3; j++)
                {
                    int menuIndex2 = j;
                    if (menus.GetSubMenu(menuIndex2).State == MenuState.Open)
                        return false;
                }

                return true;
            });
        }

        /// <summary>
        /// Tests whether clicking on an action in a sub-menu closes all <see cref="Menu"/>s.
        /// </summary>
        [Test]
        public void TestActionClickClosesMenus()
        {
            AddStep("Click item", () => clickItem(0, 1));
            AddStep("Click item", () => clickItem(1, 0));
            AddStep("Click item", () => clickItem(2, 0));
            AddStep("Click item", () => clickItem(3, 0));
            AddStep("Click item", () => clickItem(4, 0));

            AddAssert("Check submenus closed", () =>
            {
                for (int j = 1; j <= 3; j++)
                {
                    int menuIndex2 = j;
                    if (menus.GetSubMenu(menuIndex2).State == MenuState.Open)
                        return false;
                }

                return true;
            });
        }

        /// <summary>
        /// Tests whether clicking outside the <see cref="Menu"/> structure closes all sub-menus.
        /// </summary>
        /// <param name="hoverPrevious">Whether the previous menu should first be hovered before clicking outside.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void TestClickingOutsideClosesMenus(bool hoverPrevious)
        {
            for (int i = 0; i <= 3; i++)
            {
                int i2 = i;

                for (int j = 0; j <= i; j++)
                {
                    int menuToOpen = j;
                    int itemToOpen = menuToOpen == 0 ? 1 : 0;
                    AddStep("Click item", () => clickItem(menuToOpen, itemToOpen));
                }

                if (hoverPrevious && i > 0)
                    AddStep("Hover previous", () => inputManager.MoveMouseTo(menus.GetSubStructure(i2 - 1).GetMenuItem(i2 > 1 ? 0 : 1)));

                AddStep("Remove hover", () => inputManager.MoveMouseTo(Vector2.Zero));
                AddStep("Click outside", () => inputManager.Click(MouseButton.Left));
                AddAssert("Check submenus closed", () =>
                {
                    for (int j = 1; j <= i2 + 1; j++)
                    {
                        int menuIndex2 = j;
                        if (menus.GetSubMenu(menuIndex2).State == MenuState.Open)
                            return false;
                    }

                    return true;
                });
            }
        }
        #endregion

        private void clickItem(int menuIndex, int itemIndex)
        {
            inputManager.MoveMouseTo(menus.GetSubStructure(menuIndex).GetMenuItem(itemIndex));
            inputManager.Click(MouseButton.Left);
        }

        private MenuItem generateRandomMenuItem(string name = "Menu Item", int currDepth = 1)
        {
            var item = new MenuItem(name);

            if (currDepth == max_depth)
                return item;

            int subCount = rng.Next(0, max_count);
            var subItems = new List<MenuItem>();
            for (int i = 0; i < subCount; i++)
                subItems.Add(generateRandomMenuItem(item.Text + $" #{i + 1}", currDepth + 1));

            item.Items = subItems;
            return item;
        }

        private class ManualInputManager : PassThroughInputManager
        {
            private readonly ManualInputHandler handler;

            public ManualInputManager()
            {
                AddHandler(handler = new ManualInputHandler());
            }

            public void MoveMouseTo(Drawable drawable) => MoveMouseTo(drawable.ToScreenSpace(drawable.LayoutRectangle.Centre));
            public void MoveMouseTo(Vector2 position) => handler.MoveMouseTo(position);
            public void Click(MouseButton button) => handler.Click(button);
        }

        private class ManualInputHandler : InputHandler
        {
            private Vector2 lastMousePosition;

            public void MoveMouseTo(Vector2 position)
            {
                PendingStates.Enqueue(new InputState { Mouse = new MouseState { Position = position } });
                lastMousePosition = position;
            }

            public void Click(MouseButton button)
            {
                var mouseState = new MouseState { Position = lastMousePosition };
                mouseState.SetPressed(button, true);

                PendingStates.Enqueue(new InputState { Mouse = mouseState });

                mouseState = (MouseState)mouseState.Clone();
                mouseState.SetPressed(button, false);

                PendingStates.Enqueue(new InputState { Mouse = mouseState });
            }

            public override bool Initialize(GameHost host) => true;
            public override bool IsActive => true;
            public override int Priority => 0;
        }

        private class MenuStructure
        {
            private readonly Menu menu;

            public MenuStructure(Menu menu)
            {
                this.menu = menu;
            }

            public Drawable GetMenuItem(int index)
            {
                var contents = (CompositeDrawable)menu.InternalChildren[0];
                var contentContainer = (CompositeDrawable)contents.InternalChildren[1];
                var itemsContainer = (CompositeDrawable)((CompositeDrawable)contentContainer.InternalChildren[0]).InternalChildren[0];

                return itemsContainer.InternalChildren[index];
            }

            public MenuStructure GetSubStructure(int index) => new MenuStructure(GetSubMenu(index));

            public Menu GetSubMenu(int index)
            {
                var currentMenu = menu;
                for (int i = 0; i < index; i++)
                {
                    if (currentMenu == null)
                        break;

                    var container = (CompositeDrawable)currentMenu.InternalChildren[1];
                    currentMenu = (container.InternalChildren.Count > 0 ? container.InternalChildren[0] : null) as Menu;
                }

                return currentMenu;
            }
        }
    }
}