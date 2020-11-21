namespace TestCode
{
    public static class Program
    {
        static ulong[] Registers;

        public static void Main(string[] args)
        {
            Registers = new ulong[2];

            ulong flag;

            Registers[0] = 1;
            Registers[1] = 1;

            do
            {
                flag = Registers[0] + Registers[1];
                Registers[1] = flag;
                flag = Registers[0] + Registers[1];
                Registers[0] = flag;
            }
            while (flag < uint.MaxValue);
        }
    }
}
