using System;
using System.Collections.Generic;
using System.Text;

namespace SourceAFIS.Tuning
{
    public abstract class MultiFingerPolicy
    {
        public int ExpectedCount = 1;
        public abstract float Combine(float[] partial);

        public sealed class TakeKofN : MultiFingerPolicy
        {
            public int TakenIndex = 0;

            public TakeKofN(int index, int total)
            {
                TakenIndex = index;
                ExpectedCount = total;
            }

            public override float Combine(float[] partial)
            {
                if (TakenIndex >= partial.Length)
                    return 0;
                else
                {
                    Array.Sort(partial);
                    return partial[partial.Length - TakenIndex - 1];
                }
            }
        }

        public static readonly TakeKofN Take1Of1 = new TakeKofN(0, 1);
        public static readonly TakeKofN Take1Of2 = new TakeKofN(0, 2);
        public static readonly TakeKofN Take2Of3 = new TakeKofN(1, 3);
        public static readonly TakeKofN Take2Of4 = new TakeKofN(1, 4);
        public static readonly TakeKofN Take3Of5 = new TakeKofN(2, 5);
    }
}
