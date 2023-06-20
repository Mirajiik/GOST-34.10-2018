using System.Numerics;

public class Program
{
    private static void Main(string[] args)
    {
        Curve gost3410 = new Curve();

        Console.WriteLine("Enter message:");
        string message = Console.ReadLine();
        Console.WriteLine();

        (BigInteger r, BigInteger s) = gost3410.SignMessage(message, gost3410.dA);// Подпись сообщения

        Console.WriteLine("Enter message to check:");
        message = Console.ReadLine();
        Console.WriteLine($"Verification = {gost3410.VerificationSignature(r, s, gost3410.Q, message)}"); //Проверка подписи
    }
}