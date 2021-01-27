namespace TestCode
{
    public static class Program
    {
        public static void Main()
        {
            bool[] array = new bool[100];
            string data = "hello world";
            int a = 10;
            int b = 20;

            if (a < b)
            {
                array[50] = true;
                a = b;
            }
            else
            {
                array[20] = false;
                b = a;
            }

            a++;
            b++;
        }
    }
}
