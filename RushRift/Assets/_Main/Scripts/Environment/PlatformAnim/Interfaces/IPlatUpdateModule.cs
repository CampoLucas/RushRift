namespace RushRift.Environment.Interfaces
{
    public interface IPlatUpdateModule
    {
        void OnUpdate(float progress, bool inverse, float delta);
    }
}