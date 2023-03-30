using System.Numerics;

public static class BigIntegerExtension
{
    public static BigInteger Next(this Random random, BigInteger max)
    {
        if (max < 0)
            throw new ArgumentOutOfRangeException(nameof(max));
        int n = max.GetByteCount() + 1;
        byte[] result = new byte[n];
        BigInteger bigInteger;
        do
        {
            random.NextBytes(result);
            result[n - 1] = 0;
            bigInteger = new BigInteger(result);
        } while (bigInteger >= max);
        return bigInteger;
    }
}