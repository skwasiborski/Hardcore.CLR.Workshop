using System;

namespace SampleLibrary
{
    public class SampleClassWithInstanceMethod
    {
        public void DoSomething()
        {
            Console.WriteLine("Hello {0}", "Slawek");
            //Console.WriteLine("Hello {0}", 1);

            //Console.WriteLine("Hello World!");

            //IntExpecting(1);
            IntRetExpecting2(1);
        }

        public static void IntExpecting(int i)
        {
            Console.WriteLine(i);
        }

        public int IntRetExpecting(int i)
        {
            return i;
        }

        public object IntRetExpecting2(int i)
        {
            return i;
        }
    }
}