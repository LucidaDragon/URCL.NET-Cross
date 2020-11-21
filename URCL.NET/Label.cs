namespace URCL.NET
{
    public class Label
    {
        private static ulong CurrentId;

        public readonly ulong Id = NewId();

        public override int GetHashCode()
        {
            return (int)Id;
        }

        private static ulong NewId()
        {
            return CurrentId++;
        }
    }
}
