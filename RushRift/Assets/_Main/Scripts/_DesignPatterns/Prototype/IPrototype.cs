namespace Game
{
    public interface IPrototype<out TOut>
    {
        TOut Clone();
    }
    
    public interface IPrototype<in TIn, out TOut>
    {
        TOut Clone(TIn args);
    }
}