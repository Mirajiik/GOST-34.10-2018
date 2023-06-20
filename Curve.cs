using System.Numerics;
using System.Security.Cryptography;
using System.Text;

public class Curve
{
    private readonly BigInteger _a, _b;
    private readonly BigInteger _p;
    private readonly BigInteger _q;
    private readonly static RandomNumberGenerator rnd = RandomNumberGenerator.Create();
    private readonly (BigInteger _Px, BigInteger _Py) _P;
    public BigInteger a { get => _a; }
    public BigInteger b { get => _b; }
    /// <summary>
    /// Module Elliptic Curve
    /// </summary>
    public BigInteger p { get => _p; }
    /// <summary>
    /// Order Cyclic Subgroup
    /// </summary>
    public BigInteger q { get => _q; }
    /// <summary>
    /// Start Point
    /// </summary>
    public (BigInteger Px, BigInteger Py) P { get => _P; }

    /// <summary>
    /// Private Signature Key
    /// </summary>
    public BigInteger dA { get; set; } //Закрытый ключ
    /// <summary>
    /// Point Curve. Signature Verification Public Key
    /// </summary>
    public (BigInteger Qx, BigInteger Qy) Q { get; set; }

    /// <summary>
    /// Curve GOST 34.10-2018
    /// </summary>
    public Curve()
    {
        _a = 7;
        _b = BigInteger.Parse("05FBFF498AA938CE739B8E022FBAFEF40563F6E6A3472FC2A514C0CE9DAE23B7E", System.Globalization.NumberStyles.HexNumber);
        _p = BigInteger.Parse("08000000000000000000000000000000000000000000000000000000000000431", System.Globalization.NumberStyles.HexNumber);
        _q = BigInteger.Parse("08000000000000000000000000000000150FE8A1892976154C59CFC193ACCF5B3", System.Globalization.NumberStyles.HexNumber);
        _P = (BigInteger.Parse("02", System.Globalization.NumberStyles.HexNumber),
            BigInteger.Parse("08E2A8A0E65147D4BD6316030E16D19C85C97F0A9CA267122B96ABBCEA7E8FC8", System.Globalization.NumberStyles.HexNumber));
        Console.WriteLine("Curve GOST 34.10-2018\n");
        PrintParam();
        KeyGen();
    }
    /// <summary>
    /// Create custom curve
    /// </summary>
    public Curve(BigInteger a, BigInteger b, BigInteger p, BigInteger q, (BigInteger Px, BigInteger Py) P, BigInteger d = new())
    {
        _a = a;
        _b = b;
        _p = p;
        _q = q;
        _P = P;
        PrintParam();
        dA = d;
        KeyGen();
    }

    public void PrintParam()
    {
        Console.WriteLine($"a = {a}, b = {b}\np = {p}\nq = {q}\nPx = {P.Px}\nPy = {P.Py}\n");
    }

    public bool VerificationSignature(BigInteger r, BigInteger s, (BigInteger Qx, BigInteger Qy) Q, string message)
    {
        if (Q == (0, 0))
            throw new NullReferenceException("Keys not generated");
        byte[] hash = new byte[33]; //Создаем массив под хэш на 1 байт больше, потому что в BigInteger'e последний байт отводится под знак числа
                                    //(положительный < 128, отрицательный >= 128)
        Streebog.HashFunc(Encoding.Default.GetBytes(message), Streebog.OutSize.bit256, Streebog.TypeInput.Chars).Reverse().ToArray().CopyTo(hash, 0);
        BigInteger h = new BigInteger(hash);
        BigInteger e = h % q;
        if (e == 0)
            e = 1;
        BigInteger v = inverse(e, q);
        BigInteger z1 = s * v % q;
        BigInteger z2 = -1 * r * v % q + q;
        (BigInteger Cx, BigInteger Cy) = AddPoints(Mult(P, z1), Mult(Q, z2));
        BigInteger R = Cx % q;
        return r == R;
    }

    public (BigInteger, BigInteger) SignMessage(string message, BigInteger dA)
    {
        if (dA == 0)
            throw new NullReferenceException("Keys not generated");
        BigInteger k, Cx, Cy; //Закрытый ключ
        byte[] hash = new byte[33]; //Создаем массив под хэш на 1 байт больше, потому что в BigInteger'e последний байт отводится под знак числа
                                    //(положительный < 128, отрицательный >= 128)
        Streebog.HashFunc(Encoding.Default.GetBytes(message), Streebog.OutSize.bit256, Streebog.TypeInput.Chars).Reverse().ToArray().CopyTo(hash, 0);
        BigInteger h = new BigInteger(hash);
        BigInteger e = h % q;
        if (e == 0)
            e = 1;
        Console.WriteLine($"Hash = {BitConverter.ToString(hash.SkipLast(1).ToArray())}\ne = {e}");

        BigInteger r, s;
        do
        {
            do
            {
                k = rnd.Next(q);
                (Cx, Cy) = Mult(P, k);
                r = Cx % q;
            } while (r == 0);
            s = (r * dA + k * e) % q;
        } while (s == 0);
        Console.WriteLine($"k = {k}\nPx = {Cx}\nPy = {Cy}\n");
        Console.WriteLine($"Signature:\nr = {r}\ns = {s}\n");
        return (r, s);
    }

    public void KeyGen()
    {
        if (dA == 0)
        {
            var k = rnd.Next(q);
            dA = k;
        }
        Q = Mult(P, dA);
        Console.WriteLine("Keys generated");
        Console.WriteLine($"dA = {dA}\nQx = {Q.Qx}\nQy = {Q.Qy}\n");
    }

    public (BigInteger, BigInteger) Mult((BigInteger x, BigInteger y) point, BigInteger k)
    {
        while ((k & 1) == 0)
        {
            point = AddPoints(point, point);
            k >>= 1;
        }
        (BigInteger resultX, BigInteger resultY) = point;
        while (k != 0)
        {
            point = AddPoints(point, point);
            k >>= 1;
            if ((k & 1) == 1)
            {
                (resultX, resultY) = AddPoints(point, (resultX, resultY));
            }
        }
        return (resultX, resultY);
    }

    public (BigInteger, BigInteger) AddPoints((BigInteger x, BigInteger y) firstPoint, (BigInteger x, BigInteger y) secondPoint)
    {
        BigInteger m;
        if (firstPoint.x == secondPoint.x && firstPoint.y == secondPoint.y)
        {
            m = (3 * firstPoint.x * firstPoint.x + a) * inverse(2 * firstPoint.y, p) % p;
        }
        else
        {
            BigInteger diffY = firstPoint.y - secondPoint.y >= 0 ? firstPoint.y - secondPoint.y : firstPoint.y - secondPoint.y + p;
            BigInteger diffX = firstPoint.x - secondPoint.x >= 0 ? firstPoint.x - secondPoint.x : firstPoint.x - secondPoint.x + p;
            m = diffY * inverse(diffX, p) % p;
        }
        BigInteger resultX, resultY;
        resultX = (m * m - firstPoint.x - secondPoint.x) % p;
        if (resultX < 0)
            resultX += p;
        resultY = -1 * (firstPoint.y + m * (resultX - firstPoint.x)) % p;
        if (resultY < 0)
            resultY += p;
        return (resultX, resultY);
    }

    public static BigInteger inverse(BigInteger num, BigInteger mod)
    {
        BigInteger x, y;
        BigInteger g = GCD(num, mod, out x, out y);
        if (g != 1)
            throw new ArgumentException();
        return (x % mod + mod) % mod;
    }

    private static BigInteger GCD(BigInteger firstNum, BigInteger secondNum, out BigInteger x, out BigInteger y)
    {
        if (firstNum == 0)
        {
            x = 0;
            y = 1;
            return secondNum;
        }
        BigInteger x1, y1;
        BigInteger d = GCD(secondNum % firstNum, firstNum, out x1, out y1);
        x = y1 - (secondNum / firstNum) * x1;
        y = x1;
        return d;
    }
}