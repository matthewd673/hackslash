using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections;

namespace Hack_and_Slash
{
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        //textures and sprites
        Texture2D player;
        Texture2D enemy;
        Texture2D sword;
        Texture2D shield;

        Texture2D cursor;
        Texture2D heart;
        Texture2D boundingbox;
        Texture2D trail;

        //keep track of everything
        int playerX = 0;
        int playerY = 0;
        int playerHealth = 10;

        int playerAttackRate = 10;
        int playerAttackTimer = 0;

        int oldSwordX = 0;
        int oldSwordY = 0;
        int currentSwordX = 0;
        int currentSwordY = 0;

        //sword stats
        int swordOffsetX = 0;
        int swordOffsetY = 0;

        int enemySpawnRate = 500;
        int enemySpawnTimer = 0;
        int enemyAttackRate = 20;
        ArrayList enemyList = new ArrayList();

        bool shouldReturnEnemy = true;

        Random rng;

        //visual stuff
        ArrayList trailFragmentList = new ArrayList();

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);

            graphics.PreferredBackBufferWidth = 600;
            graphics.PreferredBackBufferHeight = 600;

            Content.RootDirectory = "Content";

            rng = new Random();
        }

        protected override void Initialize()
        {
            base.Initialize();
        }
        
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            //load in textures
            player = Content.Load<Texture2D>("player");
            enemy = Content.Load<Texture2D>("enemy");
            sword = Content.Load<Texture2D>("sword");
            shield = Content.Load<Texture2D>("shield");

            cursor = Content.Load<Texture2D>("cursor");
            heart = Content.Load<Texture2D>("heart");
            boundingbox = Content.Load<Texture2D>("boundingbox");
            trail = Content.Load<Texture2D>("trail");
        }
        
        protected override void UnloadContent()
        {
            
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (Keyboard.GetState().IsKeyDown(Keys.W))
                //move forward
                playerY -= 2;

            if (Keyboard.GetState().IsKeyDown(Keys.A))
                //move left
                playerX -= 2;

            if (Keyboard.GetState().IsKeyDown(Keys.S))
                //move down
                playerY += 2;

            if (Keyboard.GetState().IsKeyDown(Keys.D))
                //move right
                playerX += 2;

            //sword movement
            swordOffsetX = Mouse.GetState().X - playerX;
            swordOffsetY = Mouse.GetState().Y - playerY;

            int maxReach = 48;

            //keep the sword in check
            if (swordOffsetX > maxReach)
                swordOffsetX = maxReach;
            if (swordOffsetX < -maxReach)
                swordOffsetX = -maxReach;
            if (swordOffsetY > maxReach)
                swordOffsetY = maxReach;
            if (swordOffsetY < -maxReach)
                swordOffsetY = -maxReach;

            //for trails and stuff
            oldSwordX = currentSwordX;
            oldSwordY = currentSwordY;
            currentSwordX = swordOffsetX;
            currentSwordY = swordOffsetY;

            if(currentSwordX != oldSwordX || currentSwordY != oldSwordY)
            {
                //create trail
                //this is purely visual for now
                //this will later determine attack damage and speed
                trailFragmentList.Add(new trailFragment(oldSwordX + playerX, oldSwordY + playerY));
            }

            enemySpawnTimer++;
            playerAttackTimer++;

            if(playerAttackTimer == playerAttackRate)
            {
                playerAttack();
                playerAttackTimer = 0;
            }

            if (enemySpawnTimer == enemySpawnRate)
            {
                spawnEnemy();
                enemySpawnTimer = 0;
            }

            for (int i = 0; i < enemyList.Count; i++)
            {
                if (enemyList[i].GetType() == typeof(enemyStruct))
                {
                    enemyStruct tempEnemy = (enemyStruct)enemyList[i];
                    //something weird is happening here
                    if (i < enemyList.Count)
                        enemyList[i] = enemyLogic(tempEnemy);
                    else
                        enemyList[enemyList.Count] = enemyLogic(tempEnemy);
                }
            }

            updateTrailFragments();

            //update stuff
            base.Update(gameTime);
        }
        
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            //start drawing

            for(int i = 0; i < enemyList.Count; i++)
            {
                if(enemyList[i].GetType() == typeof(enemyStruct))
                {
                    enemyStruct tempEnemy = (enemyStruct)enemyList[i];
                    //render enemy;
                    spriteBatch.Draw(enemy, new Vector2(tempEnemy.x, tempEnemy.y), Color.White);
                    spriteBatch.Draw(boundingbox, new Vector2(tempEnemy.x, tempEnemy.y), Color.White);
                }
            }
            
            spriteBatch.Draw(player, new Vector2(playerX, playerY), Color.White);
            spriteBatch.Draw(boundingbox, new Vector2(playerX, playerY), Color.White);

            spriteBatch.Draw(sword, new Vector2(swordOffsetX + playerX, swordOffsetY + playerY), Color.White);

            for (int i = 0; i < trailFragmentList.Count; i++)
            {
                if (trailFragmentList[i].GetType() == typeof(trailFragment))
                {
                    trailFragment tempTrail = (trailFragment)trailFragmentList[i];
                    spriteBatch.Draw(trail, new Rectangle(tempTrail.x, tempTrail.y, tempTrail.width, tempTrail.height), Color.White);
                }
            }

            //ui stuff
            for (int i = 0; i < playerHealth; i++)
            {
                spriteBatch.Draw(heart, new Vector2(i * 32, 0), Color.White);
            }

            spriteBatch.Draw(cursor, new Vector2(Mouse.GetState().X, Mouse.GetState().Y), Color.White);
            
            //end drawing
            spriteBatch.End();

            base.Draw(gameTime);
        }

        public struct enemyStruct
        {
            public int x;
            public int y;
            public int health;
            public int attackTimer;

            public enemyStruct(int x, int y, int health)
            {
                this.x = x;
                this.y = y;
                this.health = health;
                this.attackTimer = 0;
            }
        }

        public void spawnEnemy()
        {
            int enemyX = rng.Next(600);
            int enemyY = rng.Next(600);
            int lockToSide = rng.Next(3);

            //ensure that enemys spawn on the sides
            if (lockToSide == 0)
                enemyX = 0;
            if (lockToSide == 1)
                enemyX = 600;
            if (lockToSide == 2)
                enemyY = 0;
            if (lockToSide == 3)
                enemyY = 600;

            enemyStruct newEnemy = new enemyStruct(enemyX, enemyY, 3);
            enemyList.Add(newEnemy);
            enemySpawnRate -= 10;
        }

        public enemyStruct enemyLogic(enemyStruct enemy)
        {

            if (enemy.health <= 0)
            {
                enemyList.Remove(enemy);
                shouldReturnEnemy = false;
            }

            if (enemy.x < playerX)
                enemy.x++;
            if (enemy.x > playerX)
                enemy.x--;
            if (enemy.y < playerY)
                enemy.y++;
            if (enemy.y > playerY)
                enemy.y--;

            enemy.attackTimer++;

            if (enemy.attackTimer == enemyAttackRate)
            {
                //if enemy in range, deal damage
                bool shouldAttack = false;

                if (detectCollision(new Rectangle(playerX, playerY, 32, 32), new Rectangle(enemy.x, enemy.y, 32, 32)))
                {
                    shouldAttack = true;
                }

                if (shouldAttack)
                {
                    playerHealth -= 1;
                    
                }

                enemy.attackTimer = 0;

            }

            return enemy;
        }

        public bool detectCollision(Rectangle rect1, Rectangle rect2)
        {
            if (rect1.X < (rect2.X + rect2.Width) &&
                (rect1.X + rect1.Width) > rect2.X &&
                rect1.Y < (rect2.Y + rect2.Height) &&
                (rect1.Height + rect1.Y) > rect2.Y)
                return true;
            else
                return false;
        }

        public struct trailFragment
        {

            public int x;
            public int y;
            public int width;
            public int height;

            public trailFragment(int x, int y)
            {
                this.x = x;
                this.y = y;
                width = 8;
                height = 8;
            }
        }

        public void updateTrailFragments()
        {
            for (int i = 0; i < trailFragmentList.Count; i++)
            {
                if (trailFragmentList[i].GetType() == typeof(trailFragment))
                {
                    trailFragment tempTrail = (trailFragment)trailFragmentList[i];
                    //make it smaller
                    tempTrail.width--;
                    tempTrail.height--;

                    trailFragmentList[i] = tempTrail;

                    //remove if too old
                    if (tempTrail.width <= 0 && tempTrail.height <= 0)
                        trailFragmentList.Remove(trailFragmentList[i]);
                }
            }
        }

        public void playerAttack()
        {
            bool shouldAttack = false;

            if(oldSwordX != currentSwordX || oldSwordY != currentSwordY)
            {
                shouldAttack = true;
            }

            if (shouldAttack)
            {
                for (int i = 0; i < enemyList.Count; i++)
                {
                    if (enemyList[i].GetType() == typeof(enemyStruct))
                    {
                        enemyStruct tempEnemy = (enemyStruct)enemyList[i];
                        if (detectCollision(new Rectangle(swordOffsetX + playerX, swordOffsetY + playerY, 32, 32), new Rectangle(tempEnemy.x, tempEnemy.y, 32, 32)))
                        {
                            tempEnemy.health -= 1;
                            enemyList[i] = tempEnemy;
                        }
                    }

                }
            }
            
        }
    }
}
