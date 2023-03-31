using System.Numerics;

public class Program
{
    private static void Main(string[] args)
    {
        Curve secp256k1 = new Curve();

        Console.WriteLine("Enter message:");
        string message = Console.ReadLine();
        Console.WriteLine();

        (BigInteger r, BigInteger s) = secp256k1.SignMessage(message, secp256k1.dA);// Подпись сообщения

        Console.WriteLine($"Verification = {secp256k1.VerificationSignature(r, s, secp256k1.Q, message)}"); //Проверка подписи
    }
}
