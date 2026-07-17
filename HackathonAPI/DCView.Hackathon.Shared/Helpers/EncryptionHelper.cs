using System.Security.Cryptography;
using System.Text;

namespace DCView.Hackathon.Shared.Helpers;

public static class EncryptionHelper
{
    public static string Encrypt(string plainText, string base64Key)
    {
        byte[] key = Convert.FromBase64String(base64Key);
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        byte[] result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string cipherText, string base64Key)
    {
        byte[] key = Convert.FromBase64String(base64Key);
        byte[] fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = key;

        byte[] iv = new byte[aes.BlockSize / 8];
        byte[] cipher = new byte[fullCipher.Length - iv.Length];
        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        byte[] plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
