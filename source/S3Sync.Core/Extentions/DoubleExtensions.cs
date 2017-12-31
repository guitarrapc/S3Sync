using System;
using System.Collections.Generic;
using System.Text;

namespace S3Sync.Core
{
    public static class DoubleExtensions
    {
        public static double ToRound(this double number, int effectiveDigit = 0)
        {
            var pow = Math.Pow(10, effectiveDigit);
            var result = number > 0
                ? Math.Floor((number * pow) + 0.5) / pow
                : Math.Ceiling((number * pow) - 0.5) / pow;
            return result;
        }
    }
}
