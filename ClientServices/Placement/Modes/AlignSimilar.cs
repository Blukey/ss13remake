﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using CGO;
using ClientInterfaces.GOC;
using ClientWindow;
using GorgonLibrary;
using ClientInterfaces.Map;
using GorgonLibrary.Graphics;
using SS13_Shared;
using SS13_Shared.GO;

namespace ClientServices.Placement.Modes
{
    public class AlignSimilar : PlacementMode
    {
        private const uint snapToRange = 50;

        public AlignSimilar(PlacementManager pMan)
            : base(pMan)
        {
        }

        public override bool Update(Vector2D mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            spriteToDraw = GetDirectionalSprite(pManager.CurrentBaseSprite);

            mouseScreen = mouseS;
            mouseWorld = new Vector2D(mouseScreen.X + ClientWindowData.Singleton.ScreenOrigin.X, mouseScreen.Y + ClientWindowData.Singleton.ScreenOrigin.Y);

            var spriteRectWorld = new RectangleF(mouseWorld.X - (spriteToDraw.Width / 2f), mouseWorld.Y - (spriteToDraw.Height / 2f), spriteToDraw.Width, spriteToDraw.Height);

            if (pManager.CurrentPermission.IsTile)
                return false;

            //Align to similar if nearby found else free
            if (currentMap.IsSolidTile(mouseScreen)) 
                return false; //HANDLE CURSOR OUTSIDE MAP

            currentTile = currentMap.GetTileAt(mouseWorld);

            if (pManager.CurrentPermission.Range > 0)
                if ((pManager.PlayerManager.ControlledEntity.Position - mouseWorld).Length > pManager.CurrentPermission.Range) return false;

            var nearbyEntities = EntityManager.Singleton.GetEntitiesInRange(mouseWorld, snapToRange);

            var snapToEntities = from IEntity entity in nearbyEntities
                                 where entity.Template == pManager.CurrentTemplate
                                 orderby (entity.Position - mouseWorld).Length ascending
                                 select entity;

            if (snapToEntities.Any())
            {
                var closestEntity = snapToEntities.First();
                var reply = closestEntity.SendMessage(this, ComponentFamily.Renderable, ComponentMessageType.GetSprite);

                //if(replies.Any(x => x.messageType == SS13_Shared.GO.ComponentMessageType.CurrentSprite))
                //{
                //    Sprite closestSprite = (Sprite)replies.Find(x => x.messageType == SS13_Shared.GO.ComponentMessageType.CurrentSprite).paramsList[0]; //This is safer but slower.

                if (reply.MessageType == ComponentMessageType.CurrentSprite)
                {
                    var closestSprite = (Sprite)reply.ParamsList[0]; //This is faster but kinda unsafe.

                    var closestRect = new RectangleF(closestEntity.Position.X - closestSprite.Width / 2f, closestEntity.Position.Y - closestSprite.Height / 2f, closestSprite.Width, closestSprite.Height);

                    var sides = new List<Vector2D>
                                            {
                                                new Vector2D(closestRect.X + (closestRect.Width/2f), closestRect.Top - spriteToDraw.Height/2f),
                                                new Vector2D(closestRect.X + (closestRect.Width/2f), closestRect.Bottom + spriteToDraw.Height/2f),
                                                new Vector2D(closestRect.Left - spriteToDraw.Width/2f, closestRect.Y + (closestRect.Height/2f)),
                                                new Vector2D(closestRect.Right + spriteToDraw.Width/2f, closestRect.Y + (closestRect.Height/2f))
                                            };

                    var closestSide = (from Vector2D side in sides orderby (side - mouseWorld).Length ascending select side).First();

                    mouseWorld = closestSide;
                    mouseScreen = new Vector2D(closestSide.X - ClientWindowData.Singleton.ScreenOrigin.X, closestSide.Y - ClientWindowData.Singleton.ScreenOrigin.Y);
                }
            }

            spriteRectWorld = new RectangleF(mouseWorld.X - (spriteToDraw.Width / 2f), mouseWorld.Y - (spriteToDraw.Height / 2f), spriteToDraw.Width, spriteToDraw.Height);
            if (pManager.CollisionManager.IsColliding(spriteRectWorld)) return false;
            return true; 
        }

        public override void Render()
        {
            if (spriteToDraw != null)
            {
                spriteToDraw.Color = pManager.ValidPosition ? Color.ForestGreen : Color.IndianRed;
                spriteToDraw.Position = new Vector2D(mouseScreen.X - (spriteToDraw.Width / 2f), mouseScreen.Y - (spriteToDraw.Height / 2f)); //Centering the sprite on the cursor.
                spriteToDraw.Draw();
                spriteToDraw.Color = Color.White;
            }
        }
    }
}