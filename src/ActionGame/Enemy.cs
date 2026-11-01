namespace ActionGame
{
    // 적마다 new Enemy로 별도 객체를 만든다. 한 적의 Hp를 바꿔도 다른 적의 Hp는 그대로다.
    public class Enemy
    {
        public float X;
        public float Y;
        public float Radius = 18;
        public int Hp = 2;
        public float VelocityX;
        public float VelocityY;
        public float FlyTime = 0; // 0보다 크면 킥을 맞고 날아가는 중이다.

        // 같은 적과 겹쳐 있는 동안 매 프레임 피해를 주지 않도록 기록한다.
        public HashSet<Enemy> HitEnemies = new HashSet<Enemy>();

        // 생성자: 객체를 만들 때 전달받은 좌표를 필드에 저장한다.
        public Enemy(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
