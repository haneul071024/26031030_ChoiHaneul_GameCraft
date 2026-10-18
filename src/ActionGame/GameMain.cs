using System.Numerics;
using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;

namespace ActionGame
{
    // G2AppBase는 교수님 glc2d의 기본 게임 클래스이다.
    // 상속받은 뒤 Initialize, Update, Render를 채우면 게임을 실행할 수 있다.
    internal class GameMain : G2AppBase
    {
        // 플레이어 한 개와 적 목록을 이 클래스에서 관리한다.
        private Player player = new Player();
        private List<Enemy> enemies = new List<Enemy>();
        public override System.Drawing.Size ScreenSize => GameGlobal.ScreenSize;
        public override string GameName => GameGlobal.GameName;

        // Direct2D 붓과 glc2d 글꼴은 초기화할 때 한 번 만들고 계속 재사용한다.
        // ?는 아직 생성되지 않은 상태(null)도 가능하다는 뜻이다.
        private ID2D1SolidColorBrush? brush;
        private G2Font? uiFont;
        private G2Font? titleFont;
        private RectangleF arena = new RectangleF(30, 90, 840, 480);
        private int score = 0;
        private bool gameEnded = false;
        private float kickEffectTime = 0;
        private float kickX;
        private float kickY;
        private const float KickRadius = 38;

        protected override void Initialize()
        {
            // Run이 시작될 때 한 번 호출된다. 이때는 그래픽 장치가 준비되어 있다.
            ClearColor = Rgb(24, 28, 36);
            brush = RenderTarget.CreateSolidColorBrush(Rgb(255, 255, 255));
            uiFont = new G2Font("맑은 고딕", 17, FontWeight.Normal);
            titleFont = new G2Font("맑은 고딕", 32, FontWeight.Bold);
            StartGame();
        }

        private void StartGame()
        {
            player = new Player();
            enemies.Clear();
            score = 0;
            gameEnded = false;
            kickEffectTime = 0;

            // 일단 적 6마리를 고정 위치에 배치한다. 생성자를 통해 좌표를 전달한다.
            enemies.Add(new Enemy(150, 180));
            enemies.Add(new Enemy(230, 180));
            enemies.Add(new Enemy(700, 180));
            enemies.Add(new Enemy(150, 470));
            enemies.Add(new Enemy(700, 470));
            enemies.Add(new Enemy(780, 470));
        }

        protected override void Update()
        {
            // glc2d가 매 프레임 입력을 먼저 읽고 이 함수를 호출한다.
            // 창이 비활성 상태라면 게임을 잠시 멈추고 눌림 기록을 지운다.
            if (Form.ActiveForm == null)
            {
                Input.Reset();
                return;
            }
            if (gameEnded)
            {
                // Alt+Enter는 glc2d의 전체 화면 키이므로 재시작과 구분한다.
                if (Input.IsKeyDown(Keys.Enter) && !IsHeld(Keys.Menu)) StartGame();
                return;
            }
            // 실제로 지난 시간을 이용하면 컴퓨터 속도에 따른 이동 차이가 줄어든다.
            // 창을 오래 멈췄다가 돌아와도 갑자기 멀리 이동하지 않도록 최대값을 둔다.
            float dt = Math.Min((float)DeltaTime, 0.05f);
            if (!gameEnded)
            {
                player.KickCooldown = Math.Max(0, player.KickCooldown - dt);
                player.InvincibleTime = Math.Max(0, player.InvincibleTime - dt);
                kickEffectTime = Math.Max(0, kickEffectTime - dt);
                MovePlayer(dt);
                // IsKeyDown은 처음 누른 프레임만 true라서 꾹 눌러도 연타되지 않는다.
                if (Input.IsKeyDown(Keys.J)) Kick();
                MoveEnemies(dt);
                CheckEnemyCollisions();

                // 반복문 안에서 적을 지우면 목록 순서가 바뀌므로 나중에 모아서 지운다.
                for (int i = enemies.Count - 1; i >= 0; i--)
                {
                    if (enemies[i].Hp <= 0)
                    {
                        enemies.RemoveAt(i);
                        score += 100;
                    }
                }
                gameEnded = player.Hp <= 0 || enemies.Count == 0;
            }
            // Invalidate나 Timer는 필요 없다. glc2d가 이어서 Render를 호출한다.
        }

        private bool IsHeld(Keys key)
        {
            // Down은 누른 순간, Press는 계속 누르고 있는 상태이다.
            // 이동은 두 상태를 모두 확인해야 첫 프레임부터 부드럽게 움직인다.
            return Input.IsKeyDown(key) || Input.IsKeyPress(key);
        }

        private void MovePlayer(float dt)
        {
            float dx = 0;
            float dy = 0;
            if (IsHeld(Keys.W)) dy -= 1;
            if (IsHeld(Keys.S)) dy += 1;
            if (IsHeld(Keys.A)) dx -= 1;
            if (IsHeld(Keys.D)) dx += 1;

            float length = MathF.Sqrt(dx * dx + dy * dy);
            if (length > 0)
            {
                // 대각선도 같은 속도가 되도록 방향의 길이를 1로 만든다.
                dx /= length;
                dy /= length;
                player.DirectionX = dx;
                player.DirectionY = dy;
                player.X += dx * player.Speed * dt;
                player.Y += dy * player.Speed * dt;
            }
            player.X = Math.Clamp(player.X, arena.Left + player.Radius, arena.Right - player.Radius);
            player.Y = Math.Clamp(player.Y, arena.Top + player.Radius, arena.Bottom - player.Radius);
        }

        private void Kick()
        {
            if (player.KickCooldown > 0) return;
            player.KickCooldown = 0.4f;
            kickEffectTime = 0.12f;
            kickX = player.X + player.DirectionX * 42;
            kickY = player.Y + player.DirectionY * 42;

            foreach (Enemy enemy in enemies)
            {
                if (enemy.Hp <= 0) continue;
                // 플레이어 앞쪽의 원에 닿은 적에게만 킥이 맞는다.
                if (Distance(kickX, kickY, enemy.X, enemy.Y) < KickRadius + enemy.Radius)
                {
                    enemy.Hp -= 1;
                    LaunchEnemy(enemy, player.DirectionX, player.DirectionY);
                }
            }
        }

        private void LaunchEnemy(Enemy enemy, float dx, float dy)
        {
            enemy.VelocityX = dx * 600;
            enemy.VelocityY = dy * 600;
            enemy.FlyTime = 0.65f;
            enemy.HitEnemies.Clear();
        }

        private void MoveEnemies(float dt)
        {
            foreach (Enemy enemy in enemies)
            {
                if (enemy.Hp <= 0) continue;
                if (enemy.FlyTime > 0)
                {
                    enemy.FlyTime = Math.Max(0, enemy.FlyTime - dt);
                    enemy.X += enemy.VelocityX * dt;
                    enemy.Y += enemy.VelocityY * dt;
                    // 시간이 지날수록 속도가 줄어드는 간단한 마찰 표현이다.
                    enemy.VelocityX *= MathF.Exp(-2 * dt);
                    enemy.VelocityY *= MathF.Exp(-2 * dt);

                    if (enemy.X < arena.Left + enemy.Radius || enemy.X > arena.Right - enemy.Radius ||
                        enemy.Y < arena.Top + enemy.Radius || enemy.Y > arena.Bottom - enemy.Radius)
                    {
                        enemy.Hp -= 1; // 벽에 날려 맞히면 추가 피해!
                        enemy.FlyTime = 0;
                    }
                }
                else
                {
                    float dx = player.X - enemy.X;
                    float dy = player.Y - enemy.Y;
                    float distance = Distance(player.X, player.Y, enemy.X, enemy.Y);
                    if (distance > 0)
                    {
                        enemy.X += dx / distance * 65 * dt;
                        enemy.Y += dy / distance * 65 * dt;
                    }
                    if (Distance(player.X, player.Y, enemy.X, enemy.Y) < player.Radius + enemy.Radius &&
                        player.InvincibleTime <= 0 && player.Hp > 0)
                    {
                        player.Hp -= 1;
                        player.InvincibleTime = 1; // 1초 동안 추가 피격을 막는다.
                    }
                }
                enemy.X = Math.Clamp(enemy.X, arena.Left + enemy.Radius, arena.Right - enemy.Radius);
                enemy.Y = Math.Clamp(enemy.Y, arena.Top + enemy.Radius, arena.Bottom - enemy.Radius);
            }
        }

        private void CheckEnemyCollisions()
        {
            foreach (Enemy flying in enemies)
            {
                if (flying.Hp <= 0 || flying.FlyTime <= 0) continue;
                foreach (Enemy target in enemies)
                {
                    if (flying == target || target.Hp <= 0 || flying.HitEnemies.Contains(target)) continue;
                    if (Distance(flying.X, flying.Y, target.X, target.Y) < flying.Radius + target.Radius)
                    {
                        flying.HitEnemies.Add(target);
                        target.Hp -= 1;
                        // 맞은 적도 같은 방향으로 날아가서 연쇄 충돌을 만들 수 있다.
                        float speed = MathF.Sqrt(flying.VelocityX * flying.VelocityX + flying.VelocityY * flying.VelocityY);
                        if (speed > 0)
                        {
                            LaunchEnemy(target, flying.VelocityX / speed, flying.VelocityY / speed);
                            target.HitEnemies.Add(flying); // 방금 부딪힌 적을 즉시 다시 때리지 않는다.
                        }
                        score += 50;
                    }
                }
            }
        }

        private float Distance(float x1, float y1, float x2, float y2)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            return MathF.Sqrt(dx * dx + dy * dy); // 피타고라스 정리로 두 점의 거리 계산
        }

        protected override void Render()
        {
            // glc2d가 BeginDraw, 배경 지우기, EndDraw를 처리한다.
            // 여기서는 이번 화면에 보일 도형과 글자만 그린다.
            // RenderTarget은 glc2d가 제공하는 Direct2D 그리기 대상이다.
            if (brush == null || uiFont == null || titleFont == null) return;

            FillBox(arena.X, arena.Y, arena.Width, arena.Height, Rgb(47, 79, 79));
            brush.Color = Rgb(119, 136, 153);
            RenderTarget.DrawRectangle(new Rect(arena.X, arena.Y, arena.Width, arena.Height), brush);
            DrawLabel($"고철 킥!    체력: {player.Hp} / 5    점수: {score}    남은 적: {enemies.Count}", 30, 18);
            DrawLabel("WASD: 이동  |  J: 킥  |  적을 벽이나 다른 적에게 날려 보세요!", 30, 50);

            foreach (Enemy enemy in enemies)
            {
                Color4 color = enemy.FlyTime > 0 ? Rgb(255, 165, 0) : Rgb(205, 92, 92);
                FillCircle(enemy.X, enemy.Y, enemy.Radius, color);
                DrawLabel(enemy.Hp.ToString(), enemy.X - 6, enemy.Y - 12);
            }

            Color4 playerColor = player.InvincibleTime > 0 ? Rgb(224, 255, 255) : Rgb(0, 191, 255);
            FillCircle(player.X, player.Y, player.Radius, playerColor);
            brush.Color = Rgb(255, 255, 255);
            RenderTarget.DrawLine(new Vector2(player.X, player.Y),
                new Vector2(player.X + player.DirectionX * 28, player.Y + player.DirectionY * 28), brush, 2);
            if (kickEffectTime > 0)
            {
                brush.Color = Rgb(255, 255, 0);
                RenderTarget.DrawEllipse(new Ellipse(new Vector2(kickX, kickY), KickRadius, KickRadius), brush, 2);
            }

            DrawLabel("빨강: 적 / 주황: 날아가는 적 / 흰 선: 공격 방향 / Alt+Enter: 전체 화면", 30, 585);
            if (gameEnded)
            {
                FillBox(190, 230, 520, 150, Rgb(0, 0, 0));
                string result = player.Hp <= 0 ? "게임 오버" : "클리어!";
                titleFont.DrawText(result, new Rect(340, 250, 300, 50), Rgb(255, 255, 255));
                DrawLabel($"최종 점수: {score}    Enter: 다시 시작", 260, 320);
            }
        }

        // 아래 함수들은 자주 쓰는 그리기 코드를 짧게 묶은 것이다.
        // Rect의 인수는 왼쪽, 위쪽, 가로 길이, 세로 길이이다.
        private void FillBox(float x, float y, float width, float height, Color4 color)
        {
            if (brush == null) return;
            brush.Color = color;
            RenderTarget.FillRectangle(new Rect(x, y, width, height), brush);
        }

        private void FillCircle(float x, float y, float radius, Color4 color)
        {
            if (brush == null) return;
            brush.Color = color;
            // Direct2D의 Ellipse는 중심 좌표와 가로/세로 반지름을 받는다.
            RenderTarget.FillEllipse(new Ellipse(new Vector2(x, y), radius, radius), brush);
        }

        private void DrawLabel(string text, float x, float y)
        {
            // G2Font가 DirectWrite를 이용해 한글 문자열을 그린다.
            uiFont?.DrawText(text, new Rect(x, y, 850, 30), Rgb(255, 255, 255));
        }

        private static Color4 Rgb(int red, int green, int blue)
        {
            // 일반적인 RGB 0~255를 Direct2D의 0~1 범위로 바꾼다.
            return new Color4(red / 255f, green / 255f, blue / 255f, 1);
        }

        public override void Dispose()
        {
            // 우리가 만든 자원을 먼저 정리하고, 마지막에 glc2d 장치를 정리한다.
            // 장치부터 없애면 그 장치를 쓰는 글꼴과 붓이 남을 수 있다.
            uiFont?.Dispose();
            titleFont?.Dispose();
            brush?.Dispose();
            uiFont = null;
            titleFont = null;
            brush = null;
            base.Dispose();
        }
    }
}
