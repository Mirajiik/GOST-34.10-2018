using System.Numerics;
using System.Text;

public class Program
{
    private static void Main(string[] args)
    {
        Curve secp256k1 = new Curve();
        Console.WriteLine($"Curve secp256k1\na = {secp256k1.a}, b = {secp256k1.b}\np = {secp256k1.p}\nq = {secp256k1.q}\n" +
            $"Px = {secp256k1.P.Px}\nPy = {secp256k1.P.Py}\n");
        
        secp256k1.KeyGen(); // Генерация открытого и закрытого ключа
        Console.WriteLine($"Qx = {secp256k1.Q.Qx}\nQy = {secp256k1.Q.Qy}");

        Console.WriteLine("Enter message:");
        string message = Console.ReadLine();

        (BigInteger r, BigInteger s) = secp256k1.SignMessage(message, secp256k1.dA);// Подпись сообщения

        Console.WriteLine($"Verification = {secp256k1.VerificationSignature(r, s, secp256k1.Q, message)}"); //Проверка подписи
    }
}

public class Curve
{
    private readonly int _a, _b; 
    private readonly BigInteger _p;
    private readonly BigInteger _q;
    private static readonly Random rnd = new Random();
    private readonly (BigInteger _Px, BigInteger _Py) _P;
    public int a { get => _a; }
    public int b { get => _b; }
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
    /// Curve secp256k1
    /// </summary>
    public Curve() //Curve secp256k1 
    {
        _a = 0;
        _b = 7;
        _p = BigInteger.Parse("0 ffffffff ffffffff ffffffff ffffffff ffffffff ffffffff fffffffe fffffc2f".Replace(" ", ""), System.Globalization.NumberStyles.HexNumber);
        _q = BigInteger.Parse("0 ffffffff ffffffff ffffffff fffffffe baaedce6 af48a03b bfd25e8c d0364141".Replace(" ", ""), System.Globalization.NumberStyles.HexNumber);
        _P = (BigInteger.Parse("0 79be667e f9dcbbac 55a06295 ce870b07 029bfcdb 2dce28d9 59f2815b 16f81798".Replace(" ", ""), System.Globalization.NumberStyles.HexNumber),
        BigInteger.Parse("0 483ada77 26a3c465 5da4fbfc 0e1108a8 fd17b448 a6855419 9c47d08f fb10d4b8".Replace(" ", ""), System.Globalization.NumberStyles.HexNumber));
    }

    public Curve(int a, int b, BigInteger p, BigInteger q, (BigInteger Px, BigInteger Py) P)
    {
        _a = a;
        _b = b;
        _p = p;
        _q = q;
        _P = P;
    }

    public bool VerificationSignature(BigInteger r, BigInteger s, (BigInteger Qx, BigInteger Qy) Q, string message)
    {
        message = "Hello!";
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
        Console.WriteLine($"Hash = {BitConverter.ToString(hash.SkipLast(1).ToArray())}\nh = {h}");
        BigInteger e = h % q;
        if (e == 0)
            e = 1;

        BigInteger r, s;
        do
        {
            do
            {
                k = rnd.Next(q);
                (Cx, Cy) = Mult(P, k);
                r = Cx % q;
                Console.WriteLine($"k = {k}\nPx = {Cx}\nPy = {Cy}\nr = {r}");
            } while (r == 0);
            s = (r * dA + k * e) % q;
            Console.WriteLine($"s = {s}\n");
        } while (s == 0);
        return (r, s);
    }

    public void KeyGen()
    {
        var k = rnd.Next(q);
        (dA, Q) = (k, Mult(P, k));
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

    private (BigInteger, BigInteger) AddPoints((BigInteger x, BigInteger y) firstPoint, (BigInteger x, BigInteger y) secondPoint)
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
