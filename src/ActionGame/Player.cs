namespace ActionGame
{
    // 플레이어의 정보만 보관하는 클래스이다.
    // 실제 이동과 공격 처리는 GameMain에서 한다.
    public class Player
    {
        public float X = 450;
        public float Y = 320;
        public float Radius = 18;
        public float Speed = 240; // 1초에 이동하는 픽셀 수
        public int Hp = 5;

        // 마지막으로 이동한 방향을 기억해서 그 방향으로 발차기한다.
        public float DirectionX = 1;
        public float DirectionY = 0;
        public float KickCooldown = 0;
        public float InvincibleTime = 0; // 피격 직후 연속으로 체력이 깎이지 않게 한다.
    }
}
