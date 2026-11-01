namespace ActionGame
{
    // 점수는 GameMain에서 실제로 더한다. 이 클래스는 화면에 떠오르는 글자만 관리한다.
    internal class ScorePopup
    {
        public float X;
        public float Y;
        public string Text;
        public float Life = 0.8f;

        public ScorePopup(float x, float y, int points, string reason)
        {
            X = x;
            Y = y;
            Text = reason + " +" + points;
        }

        public void Update(float dt)
        {
            Y -= 30 * dt; // 화면의 Y는 아래로 커지므로 빼면 위로 움직인다.
            Life -= dt;
        }
    }
}
