namespace LuC
{
    public class Reference<T>
    {
        public T Value { get; private set; }

        public static implicit operator T(Reference<T> r)
        {
            return r.Value;
        }

        public static implicit operator Reference<T>(T v)
        {
            return new Reference<T> { Value = v };
        }
    }
}
