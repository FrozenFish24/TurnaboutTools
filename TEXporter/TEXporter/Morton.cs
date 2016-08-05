using System.Drawing;

namespace TEXporter
{
    static class Morton
    {
        private static uint deinterleave(uint x)
        {
            x = x & 0x55555555;
            x = (x | (x >> 1)) & 0x33333333;
            x = (x | (x >> 2)) & 0x0F0F0F0F;
            x = (x | (x >> 4)) & 0x00FF00FF;
            x = (x | (x >> 8)) & 0x0000FFFF;
            return x;
        }

        public static Point ZtoXY(uint z)
        {
            uint x = deinterleave(z);
            uint y = deinterleave(z >> 1);

            return new Point((int)x, (int)y);
        }

        public static uint XYtoZ(uint x, uint y)
        {
            uint[] B = { 0x55555555, 0x33333333, 0x0F0F0F0F, 0x00FF00FF };
            uint[] S = { 1, 2, 4, 8 };

            uint z;

            x = (x | (x << (int)S[3])) & B[3];
            x = (x | (x << (int)S[2])) & B[2];
            x = (x | (x << (int)S[1])) & B[1];
            x = (x | (x << (int)S[0])) & B[0];

            y = (y | (y << (int)S[3])) & B[3];
            y = (y | (y << (int)S[2])) & B[2];
            y = (y | (y << (int)S[1])) & B[1];
            y = (y | (y << (int)S[0])) & B[0];

            z = x | (y << 1);

            return z;
        }
    }
}
