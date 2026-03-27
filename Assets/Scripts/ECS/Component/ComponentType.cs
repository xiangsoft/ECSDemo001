namespace Xiangsoft.Lib.ECS.Component
{
    public enum ComponentMask : ulong
    {
        None = 0,
        Transform = 1,
        Movement = 2,
        Projectile = 4,
        AI = 8,
    }
}
