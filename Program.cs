﻿using System.Numerics;
using System.Text;

public class Program
{
    static readonly int a = 0, b = 7; //Curve secp256k1 
    static readonly BigInteger p = BigInteger.Parse("0 ffffffff ffffffff ffffffff ffffffff ffffffff ffffffff fffffffe fffffc2f".Replace(" ", ""),
            System.Globalization.NumberStyles.HexNumber);
    static readonly BigInteger q = BigInteger.Parse("0 ffffffff ffffffff ffffffff fffffffe baaedce6 af48a03b bfd25e8c d0364141".Replace(" ", ""),
        System.Globalization.NumberStyles.HexNumber);
    static readonly Random rnd = new Random();
    static readonly BigInteger Px = BigInteger.Parse("0 79be667e f9dcbbac 55a06295 ce870b07 029bfcdb 2dce28d9 59f2815b 16f81798".Replace(" ", ""),
            System.Globalization.NumberStyles.HexNumber);
    static readonly BigInteger Py = BigInteger.Parse("0 483ada77 26a3c465 5da4fbfc 0e1108a8 fd17b448 a6855419 9c47d08f fb10d4b8".Replace(" ", ""),
        System.Globalization.NumberStyles.HexNumber);
    private static void Main(string[] args)
    {
        Console.WriteLine($"Curve secp256k1\na = {a}, b = {b}\np = {p}\nq = {q}\nPx = {Px}\nPy = {Py}\n");

        BigInteger dA; //Закрытый ключ
        BigInteger Qx, Qy; //Точка, открытый ключ
        (dA, (Qx, Qy)) = KeyGen(); // Генерация открытого и закрытого ключа
        Console.WriteLine($"Qx = {Qx}\nQy = {Qy}");

        Console.WriteLine("Enter message:");
        string message = Console.ReadLine();

        (BigInteger r, BigInteger s) = SignMessage(message, dA);// Подпись сообщения

        Console.WriteLine($"Verification = {VerificationSignature(r, s, Qx, Qy, message)}"); //Проверка подписи
    }

    public static bool VerificationSignature(BigInteger r, BigInteger s, BigInteger Qx, BigInteger Qy, string message)
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
        (BigInteger Cx, BigInteger Cy) = AddPoints(Mult(Px, Py, z1), Mult(Qx, Qy, z2));
        BigInteger R = Cx % q;
        return r == R;
    }

    public static (BigInteger, BigInteger) SignMessage(string message, BigInteger dA)
    {
        BigInteger k, Cx, Cy; //Закрытый ключ
        byte[] hash = new byte[33]; //Создаем массив под хэш на 1 байт больше, потому что в BigInteger'e последний байт отводится под знак числа
                                    //(положительный < 128, отрицательный >= 128)
        Streebog.HashFunc(Encoding.Default.GetBytes(message), Streebog.OutSize.bit256, Streebog.TypeInput.Chars).Reverse().ToArray().CopyTo(hash, 0);
        BigInteger h = new BigInteger(hash);
        Console.WriteLine($"Hash = {BitConverter.ToString(hash.SkipLast(1).ToArray())}\nh = {h}");
        BigInteger e = h % q;
        if (e == 0)
            e = 1;

        BigInteger r;
        BigInteger s;
        do
        {
            do
            {
                k = rnd.Next(q);
                (Cx, Cy) = Mult(Px, Py, k);
                r = Cx % q;
                Console.WriteLine($"k = {k}\nPx = {Cx}\nPy = {Cy}\nr = {r}");
            } while (r == 0);
            //s = inverse(k, q) * (z + r * dA) % q;
            s = (r * dA + k * e) % q;
            Console.WriteLine($"s = {s}\n");
        } while (s == 0);

        return (r, s);
    }

    public static (BigInteger, (BigInteger, BigInteger)) KeyGen()
    {
        var dA = rnd.Next(q);
        return (dA, Mult(Px, Py, dA));
    }

    public static (BigInteger, BigInteger) Mult(BigInteger x, BigInteger y, BigInteger k)
    {
        while ((k & 1) == 0)
        {
            (x, y) = AddPoints((x, y), (x, y));
            k >>= 1;
        }
        (BigInteger resultX, BigInteger resultY) = (x, y);
        while (k != 0)
        {
            (x, y) = AddPoints((x, y), (x, y));
            k >>= 1;
            if ((k & 1) == 1)
            {
                (resultX, resultY) = AddPoints((x, y), (resultX, resultY));
            }
        }


        return (resultX, resultY);
    }

    static (BigInteger, BigInteger) AddPoints((BigInteger x, BigInteger y) firstPoint, (BigInteger x, BigInteger y) secondPoint)
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
