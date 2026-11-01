using System.Numerics;
using Vortice.Direct2D1;
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

        // Direct2D 붓은 초기화할 때 한 번 만들고 계속 재사용한다.
        // ?는 아직 생성되지 않은 상태(null)도 가능하다는 뜻이다.
        private ID2D1SolidColorBrush? brush;
        private RectangleF arena = new RectangleF(30, 90, 840, 480);
        private int score = 0;
        // 숫자 대신 이름으로 상태를 구분하면 if 조건의 뜻을 바로 알 수 있다.
        private enum GameState { Lobby, Playing, RoundClear, Result, Paused }
        private GameState state = GameState.Lobby;
        // 전투 중인지 여부로 걷기 애니메이션을 멈춘다.
        private bool gameEnded => state != GameState.Playing;
        private int round;
        private const int RoundCount = GameRules.RoundCount;
        private float roundTime;
        private int roundBonus;
        private int bestScore;
        private bool musicEnabled = true;
        private readonly HashSet<Enemy> shockedEnemies = new();
        private float HazardPhase => roundTime % GameRules.HazardCycle; // 나머지: 0~6초 반복
        private bool HazardActive => round >= 2 && HazardPhase >= GameRules.HazardStart;
        private bool InHazard(float x, float y, float radius) =>
            Math.Abs(x - 450) < 44 + radius || (round == 3 && Math.Abs(y - 330) < 34 + radius);
        private float kickEffectTime = 0;
        private float kickX;
        private float kickY;
        private const float KickRadius = 38;
        private GameAssets? assets;
        private GameHud? hud;
        private float kickAngle;
        private readonly List<GameEffect> effects = new();
        private float playerWalkTime;
        private float enemyWalkTime;
        private bool playerMoving;
        private float visualTime;
        private float clearTime;
        private bool wasHazardActive; // 이전 프레임에도 전기가 켜져 있었는지 기억한다.
        private float readyTime;       // 라운드 시작 전 준비 시간
        private int recoveredHp;       // 실제로 회복한 양 (체력이 가득 차 있으면 0)
        private int healthBonus;
        private readonly List<ScorePopup> scorePopups = new List<ScorePopup>();

        private void AddEffect(string sheet, float x, float y, float size, float duration, float angle = 0)
        {
            if (effects.Count >= 64) effects.RemoveAt(0);
            effects.Add(new GameEffect(sheet, x, y, size, duration, angle));
        }

        private void UpdateEffects(float dt)
        {
            // 뒤에서부터 검사하면 RemoveAt 뒤에도 다음 검사 대상이 밀리지 않는다.
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                effects[i].Update(dt);
                if (effects[i].Finished) effects.RemoveAt(i);
            }
            for (int i = scorePopups.Count - 1; i >= 0; i--)
            {
                scorePopups[i].Update(dt);
                if (scorePopups[i].Life <= 0) scorePopups.RemoveAt(i);
            }
        }

        private void RemoveDefeatedEnemies()
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i].Hp > 0) continue;
                AddEffect("enemy_break_sheet", enemies[i].X, enemies[i].Y, 88, 0.4f);
                assets?.QueueSound("enemy_break");
                AddScore(GameRules.DefeatScore, enemies[i].X, enemies[i].Y, "처치");
                enemies.RemoveAt(i);
            }
        }

        protected override void Initialize()
        {
            // Run이 시작될 때 한 번 호출된다. 이때는 그래픽 장치가 준비되어 있다.
            ClearColor = Rgb(17, 22, 18);
            brush = RenderTarget.CreateSolidColorBrush(Rgb(255, 255, 255));
            assets = new GameAssets();
            hud = new GameHud();
            state = GameState.Lobby;
        }

        // 새 도전에서는 전부 초기화한다. bestScore는 이번 실행 기록이므로 유지한다.
        private void StartGame()
        {
            assets?.StopSounds();
            player = new Player();
            score = 0;
            round = 0;
            StartRound();
        }

        // 다음 라운드에서는 체력과 누적 점수를 유지하고 전투 대상만 다시 만든다.
        private void StartRound()
        {
            round++;
            state = GameState.Playing;
            roundTime = 0;
            readyTime = GameRules.ReadyDuration;
            recoveredHp = 0;
            healthBonus = 0;
            scorePopups.Clear();
            clearTime = 0;
            wasHazardActive = false;
            roundBonus = 0;
            player.X = 310;
            player.Y = 320;
            player.KickCooldown = player.InvincibleTime = 0;
            player.DirectionX = 1;
            player.DirectionY = 0;
            enemies.Clear();
            effects.Clear();
            shockedEnemies.Clear();
            kickEffectTime = playerWalkTime = enemyWalkTime = 0;
            playerMoving = false;
            int count = GameRules.EnemyCount(round);
            for (int i = 0; i < count; i++)
            {
                // 원 한 바퀴(2π)를 적 수만큼 나눈다. X/Y 반지름이 달라서 타원이 된다.
                float angle = MathF.Tau * i / count;
                enemies.Add(new Enemy(450 + MathF.Cos(angle) * 330, 330 + MathF.Sin(angle) * 190));
            }
        }

        protected override void Update()
        {
            if (Form.ActiveForm == null)
            {
                assets?.StopSounds();
                assets?.UpdateMusic(false);
                Input.Reset();
                return;
            }
            // 지연 후 큰 이동이 발생하지 않도록 한 번에 반영할 시간을 제한한다.
            float dt = Math.Min((float)DeltaTime, 0.05f);
            if (Input.IsKeyDown(Keys.M)) musicEnabled = !musicEnabled;
            if (Input.IsKeyDown(Keys.Escape))
            {
                if (state == GameState.Playing) state = GameState.Paused;
                else if (state == GameState.Paused) state = GameState.Playing;
                else state = GameState.Lobby;
            }
            // 일시정지 메뉴에서만 L로 로비로 간다. 전투 중 실수로 진행을 버리지 않게 한다.
            if (state == GameState.Paused && Input.IsKeyDown(Keys.L)) state = GameState.Lobby;
            if (state != GameState.Paused)
            {
                visualTime += dt;
                UpdateEffects(dt);
            }
            if (state == GameState.RoundClear || (state == GameState.Result && player.Hp > 0)) clearTime += dt;
            assets?.UpdateMusic(musicEnabled && state != GameState.Paused);
            bool enter = Input.IsKeyDown(Keys.Enter) && !IsHeld(Keys.Menu);
            if (state == GameState.Lobby || state == GameState.Result)
            {
                if (enter) { StartGame(); assets?.QueueSound("restart"); }
                assets?.FlushSounds(TotalTime);
                return;
            }
            if (state == GameState.Paused) return;
            if (state == GameState.RoundClear)
            {
                if (enter) StartRound();
                return;
            }
            // 준비 중에는 입력 안내만 보여 주고 적, 플레이어, 점수용 시간은 멈춘다.
            if (readyTime > 0)
            {
                readyTime = Math.Max(0, readyTime - dt);
                return;
            }
            roundTime += dt;
            player.KickCooldown = Math.Max(0, player.KickCooldown - dt);
            player.InvincibleTime = Math.Max(0, player.InvincibleTime - dt);
            kickEffectTime = Math.Max(0, kickEffectTime - dt);
            enemyWalkTime += dt;
            MovePlayer(dt);
            if (Input.IsKeyDown(Keys.J)) Kick();
            MoveEnemies(dt);
            CheckEnemyCollisions();
            UpdateHazard();
            RemoveDefeatedEnemies();
            CheckRoundEnd();
            assets?.FlushSounds(TotalTime);
        }

        // 플레이 중에만 판정한다. 완료 상태로 바뀌면 다음 프레임에 보상을 다시 주지 않는다.
        private void CheckRoundEnd()
        {
            if (state != GameState.Playing) return;
            // 같은 프레임에 마지막 적과 플레이어가 모두 쓰러지면 패배를 우선한다.
            if (player.Hp <= 0)
            {
                state = GameState.Result;
                assets?.QueueSound("game_over");
            }
            else if (enemies.Count == 0)
            {
                // 회복하기 전 체력으로 보너스를 계산해야 보상이 부풀려지지 않는다.
                healthBonus = player.Hp * 50;
                roundBonus = 300 + GameRules.TimeBonus(roundTime) + healthBonus;
                score += roundBonus;
                if (round == RoundCount)
                {
                    state = GameState.Result;
                }
                else
                {
                    state = GameState.RoundClear;
                    int previousHp = player.Hp;
                    player.Hp = Math.Min(GameRules.MaxHp, player.Hp + 1);
                    recoveredHp = player.Hp - previousHp;
                }
                assets?.QueueSound("clear");
            }
            bestScore = Math.Max(bestScore, score);
        }

        // 전기 그림은 GameAssets, 실제 피해 계산은 여기서 담당한다.
        private void UpdateHazard()
        {
            if (!HazardActive)
            {
                // 새 작동 주기에 다시 피해를 줄 수 있도록 명단을 비운다.
                shockedEnemies.Clear();
                wasHazardActive = false;
                return;
            }
            if (!wasHazardActive)
            {
                wasHazardActive = true;
                assets?.QueueSound("wall_hit");
                for (int i = 0; i < 6; i++) AddEffect("hit_spark_sheet", 450, 130 + i * 80, 100, 0.35f);
                if (round == 3)
                    for (int i = 0; i < 10; i++) AddEffect("hit_spark_sheet", 72 + i * 84, 330, 80, 0.35f);
            }
            if (InHazard(player.X, player.Y, player.Radius) && player.InvincibleTime <= 0 && player.Hp > 0)
            {
                player.Hp--;
                player.InvincibleTime = 1;
                assets?.QueueSound("player_hurt");
                AddEffect("hit_spark_sheet", player.X, player.Y, 60, 0.2f);
            }
            foreach (Enemy enemy in enemies)
            {
                if (enemy.Hp <= 0) continue;
                if (!InHazard(enemy.X, enemy.Y, enemy.Radius)) continue;
                if (shockedEnemies.Contains(enemy)) continue;
                shockedEnemies.Add(enemy); // 이번 작동에서 이미 피해를 받은 적을 기록한다.
                enemy.Hp--;
                AddEffect("hit_spark_sheet", enemy.X, enemy.Y, 60, 0.2f);
                assets?.QueueSound("metal_hit");
            }
        }
        private bool IsHeld(Keys key)
        {
            // Down은 누른 순간, Press는 계속 누르고 있는 상태이다.
            // 이동은 두 상태를 모두 확인해야 첫 프레임부터 부드럽게 움직인다.
            return Input.IsKeyDown(key) || Input.IsKeyPress(key);
        }

        private void MovePlayer(float dt)
        {
            float previousX = player.X;
            float previousY = player.Y;
            float dx = 0;
            float dy = 0;
            if (IsHeld(Keys.W) || IsHeld(Keys.Up)) dy -= 1;
            if (IsHeld(Keys.S) || IsHeld(Keys.Down)) dy += 1;
            if (IsHeld(Keys.A) || IsHeld(Keys.Left)) dx -= 1;
            if (IsHeld(Keys.D) || IsHeld(Keys.Right)) dx += 1;

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
            playerMoving = Distance(previousX, previousY, player.X, player.Y) > 0.001f;
            playerWalkTime = playerMoving ? playerWalkTime + dt : 0;
        }

        private void Kick()
        {
            if (player.KickCooldown > 0) return;
            player.KickCooldown = GameRules.KickCooldown;
            kickEffectTime = 0.12f;
            // 방향 벡터 (dx, dy)를 이미지 회전에 필요한 각도(라디안)로 바꾼다.
            kickAngle = MathF.Atan2(player.DirectionY, player.DirectionX);
            assets?.QueueSound("kick_swing");
            kickX = player.X + player.DirectionX * 42;
            kickY = player.Y + player.DirectionY * 42;

            foreach (Enemy enemy in enemies)
            {
                if (enemy.Hp <= 0) continue;
                // 플레이어 앞쪽의 원에 닿은 적에게만 킥이 맞는다.
                if (Distance(kickX, kickY, enemy.X, enemy.Y) < KickRadius + enemy.Radius)
                {
                    AddEffect("hit_spark_sheet", enemy.X, enemy.Y, 66, 0.20f);
                    assets?.QueueSound("metal_hit");
                    enemy.Hp -= 1;
                    LaunchEnemy(enemy, player.DirectionX, player.DirectionY);
                }
            }
        }

        // 좌표를 즉시 옮기지 않고 속도를 저장한다. 실제 이동은 MoveEnemies에서 한다.
        private void LaunchEnemy(Enemy enemy, float dx, float dy)
        {
            enemy.VelocityX = dx * GameRules.LaunchSpeed;
            enemy.VelocityY = dy * GameRules.LaunchSpeed;
            enemy.FlyTime = GameRules.FlyDuration;
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
                    // Exp는 지수 함수다. 0~1 사이의 감속 비율을 곱해 프레임 속도 차이를 줄인다.
                    enemy.VelocityX *= MathF.Exp(-2 * dt);
                    enemy.VelocityY *= MathF.Exp(-2 * dt);

                    if (enemy.X < arena.Left + enemy.Radius || enemy.X > arena.Right - enemy.Radius ||
                        enemy.Y < arena.Top + enemy.Radius || enemy.Y > arena.Bottom - enemy.Radius)
                    {
                        float wallX = Math.Clamp(enemy.X, arena.Left, arena.Right);
                        float wallY = Math.Clamp(enemy.Y, arena.Top, arena.Bottom);
                        float wallAngle;
                        if (enemy.X < arena.Left + enemy.Radius) { wallX = arena.Left; wallAngle = MathF.PI; }
                        else if (enemy.X > arena.Right - enemy.Radius) { wallX = arena.Right; wallAngle = 0; }
                        else if (enemy.Y < arena.Top + enemy.Radius) { wallY = arena.Top; wallAngle = -MathF.PI / 2; }
                        else { wallY = arena.Bottom; wallAngle = MathF.PI / 2; }
                        AddEffect("wall_hit_sheet", wallX, wallY, 80, 0.28f, wallAngle);
                        assets?.QueueSound("wall_hit");
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
                        enemy.X += dx / distance * (55 + round * 10) * dt;
                        enemy.Y += dy / distance * (55 + round * 10) * dt;
                    }
                    if (Distance(player.X, player.Y, enemy.X, enemy.Y) < player.Radius + enemy.Radius &&
                        player.InvincibleTime <= 0 && player.Hp > 0)
                    {
                        AddEffect("hit_spark_sheet", player.X, player.Y, 52, 0.20f);
                        assets?.QueueSound("player_hurt");
                        player.Hp -= 1;
                        player.InvincibleTime = 1; // 1초 동안 추가 피격을 막는다.
                    }
                }
                enemy.X = Math.Clamp(enemy.X, arena.Left + enemy.Radius, arena.Right - enemy.Radius);
                enemy.Y = Math.Clamp(enemy.Y, arena.Top + enemy.Radius, arena.Bottom - enemy.Radius);
            }
        }

        // 중첩 반복문: 날아가는 적 하나를 다른 모든 적과 비교한다.
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
                        AddEffect("hit_spark_sheet", target.X, target.Y, 66, 0.20f);
                        assets?.QueueSound("metal_hit");
                        target.Hp -= 1;
                        // 맞은 적도 같은 방향으로 날아가서 연쇄 충돌을 만들 수 있다.
                        float speed = MathF.Sqrt(flying.VelocityX * flying.VelocityX + flying.VelocityY * flying.VelocityY);
                        if (speed > 0)
                        {
                            LaunchEnemy(target, flying.VelocityX / speed, flying.VelocityY / speed);
                            target.HitEnemies.Add(flying); // 방금 부딪힌 적을 즉시 다시 때리지 않는다.
                        }
                        AddScore(GameRules.ChainScore, target.X, target.Y - 25, "연쇄");
                    }
                }
            }
        }

        private void AddScore(int points, float x, float y, string reason)
        {
            score += points;
            // 표시용 목록이 무한히 커지지 않도록 오래된 글자부터 지운다.
            if (scorePopups.Count >= 24) scorePopups.RemoveAt(0);
            scorePopups.Add(new ScorePopup(x, y, points, reason));
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
            if (brush == null || assets == null || hud == null) return;

            assets.DrawFloor(new Rect(arena.X, arena.Y, arena.Width, arena.Height));
            brush.Color = Rgb(112, 119, 99);
            RenderTarget.DrawRectangle(new Rect(arena.X, arena.Y, arena.Width, arena.Height), brush);
            if (state == GameState.Lobby)
            {
                hud.DrawLobby(assets, bestScore, musicEnabled, visualTime);
                return;
            }
            assets.DrawElectricFloor(round, roundTime);
            DrawElectricArcs();
            hud.DrawStatus(assets, player, score, enemies.Count);
            hud.DrawRound(round, roundTime, enemies.Count, musicEnabled);

            foreach (Enemy enemy in enemies)
            {
                bool flying = enemy.FlyTime > 0;
                float angle = flying
                    ? MathF.Atan2(enemy.VelocityY, enemy.VelocityX)
                    : MathF.Atan2(player.Y - enemy.Y, player.X - enemy.X);
                if (flying) assets.DrawSprite("enemy_flying", enemy.X, enemy.Y, 48, angle);
                else if (!gameEnded && Distance(player.X, player.Y, enemy.X, enemy.Y) > 1)
                    assets.DrawFrame("enemy_walk_sheet", (int)(enemyWalkTime * 7) % 4, enemy.X, enemy.Y, 48, angle);
                else assets.DrawSprite("enemy_basic", enemy.X, enemy.Y, 48, angle);
                hud.DrawEnemyHealth(enemy.X, enemy.Y, enemy.Hp, flying);
            }

            float playerAngle = kickEffectTime > 0 ? kickAngle : MathF.Atan2(player.DirectionY, player.DirectionX);
            float opacity = player.InvincibleTime > 0 && (int)(player.InvincibleTime * 12) % 2 == 0 ? 0.35f : 1;
            if (kickEffectTime > 0)
                assets.DrawSprite("player_kick", player.X, player.Y, 50, playerAngle, opacity);
            else if (playerMoving && !gameEnded)
                assets.DrawFrame("player_walk_sheet", (int)(playerWalkTime * 9) % 4, player.X, player.Y, 50, playerAngle, opacity);
            else assets.DrawSprite("player_idle", player.X, player.Y, 50, playerAngle, opacity);
            if (kickEffectTime > 0)
                assets.DrawSprite("kick_arc", kickX, kickY, 96, kickAngle, 0.9f);

            foreach (GameEffect effect in effects)
                assets.DrawFrame(effect.Sheet, effect.Frame, effect.X, effect.Y, effect.Size, effect.Angle);

            foreach (ScorePopup popup in scorePopups) hud.DrawScorePopup(popup);
            if (state == GameState.Playing && readyTime > 0) hud.DrawReady(round, readyTime);
            if (state == GameState.RoundClear)
                hud.DrawRoundClear(assets, round, roundBonus, score, visualTime, recoveredHp, GameRules.TimeBonus(roundTime), healthBonus);
            if (state == GameState.Paused) hud.DrawPause(assets, musicEnabled);
            if (state == GameState.Result)
                hud.DrawResult(assets, player.Hp <= 0, score, player.Hp, round, bestScore);
            if (state == GameState.RoundClear || (state == GameState.Result && player.Hp > 0)) DrawCelebration();
        }
        private void DrawElectricArcs()
        {
            if (brush == null || round < 2) return;
            if (!HazardActive)
            {
                if (HazardPhase >= 2.5f)
                {
                    brush.Color = new Color4(1, 0.7f, 0.1f, 0.45f + 0.4f * MathF.Sin(roundTime * 20));
                    RenderTarget.DrawRectangle(new Rect(406, 90, 88, 480), brush, 3);
                    if (round == 3) RenderTarget.DrawRectangle(new Rect(30, 296, 840, 68), brush, 3);
                }
                return;
            }
            // 시간을 정수로 나누어 번개 모양을 초당 20번만 바꾼다.
            int tick = (int)(roundTime * 20);
            brush.Color = new Color4(0.65f, 0.96f, 1, 0.9f);
            for (int i = 0; i < 18; i++)
            {
                float y = 100 + i * 26;
                float x = 450 + MathF.Sin(i * 8 + tick) * 28;
                float nextX = 450 + MathF.Sin((i + 1) * 8 + tick) * 28;
                RenderTarget.DrawLine(new Vector2(x, y), new Vector2(nextX, y + 26), brush, 2);
                if (round == 3)
                {
                    float hx = 40 + i * 45;
                    float hy = 330 + MathF.Sin(i * 6 + tick) * 23;
                    RenderTarget.DrawLine(new Vector2(hx, hy), new Vector2(hx + 42, 330 + MathF.Sin((i + 1) * 6 + tick) * 23), brush, 2);
                }
            }
        }

        private void DrawCelebration()
        {
            // 원 방향으로 퍼지는 점에 아래쪽 가속도를 더해 불꽃처럼 보이게 한다. 피해 판정은 없다.
            if (clearTime > 2.6f) return;
            for (int i = 0; i < 36; i++)
            {
                float t = Math.Max(0, clearTime - i % 4 * 0.1f);
                float angle = i * 2.39996f;
                float speed = 90 + i % 7 * 24;
                float x = 450 + MathF.Cos(angle) * t * speed;
                float y = 250 + MathF.Sin(angle) * t * speed + 65 * t * t;
                FillCircle(x, y, 2 + i % 3, new Color4(1, 0.7f + i % 3 * 0.1f, 0.2f, Math.Max(0, 1 - t / 2.6f)));
            }
        }
        // 아래 함수들은 자주 쓰는 그리기 코드를 짧게 묶은 것이다.
        // Rect의 인수는 왼쪽, 위쪽, 가로 길이, 세로 길이이다.
        private void FillCircle(float x, float y, float radius, Color4 color)
        {
            if (brush == null) return;
            brush.Color = color;
            // Direct2D의 Ellipse는 중심 좌표와 가로/세로 반지름을 받는다.
            RenderTarget.FillEllipse(new Ellipse(new Vector2(x, y), radius, radius), brush);
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
            hud?.Dispose();
            hud = null;
            assets?.Dispose();
            assets = null;
            brush?.Dispose();
            brush = null;
            base.Dispose();
        }
    }
}
