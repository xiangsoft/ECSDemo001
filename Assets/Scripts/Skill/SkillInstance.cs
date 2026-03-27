namespace Xiangsoft.Game.Skill
{
    public class SkillInstance
    {
        public SkillData Data { get; private set; }

        public float CurrentCD;

        public bool IsReady { get { return CurrentCD <= 0f; } }

        public SkillInstance(SkillData data)
        {
            Data = data;
            CurrentCD = 0f;
        }

        public void UpdateCD(float deltaTime)
        {
            if (CurrentCD <= 0f)
                return;

            CurrentCD -= deltaTime;
            
            if (CurrentCD < 0f)
                CurrentCD = 0f;
        }

        public void ResetCD()
        {
            CurrentCD = Data.Cooldown;
        }
    }
}