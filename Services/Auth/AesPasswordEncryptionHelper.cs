using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Services.Auth
{
    public static class AesPasswordEncryptionHelper
    {
        private const int KeySize = 32; // 256-bit key
        private const int IvSize = 16;  // 128-bit IV
        private const int SaltSize = 16; // 128-bit salt
        private const int Iterations = 100_000;

        public static string Encrypt(string plainText, string password)
        {
            byte[] salt = GenerateRandomBytes(SaltSize);
            byte[] iv = GenerateRandomBytes(IvSize);
            var key = DeriveKeyFromPassword(password, salt);

            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            using MemoryStream ms = new();
            using (CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (StreamWriter sw = new(cs))
            {
                sw.Write(plainText);
            }

            byte[] encryptedBytes = ms.ToArray();

            // Combine salt + iv + ciphertext
            byte[] result = new byte[SaltSize + IvSize + encryptedBytes.Length];
            Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
            Buffer.BlockCopy(iv, 0, result, SaltSize, IvSize);
            Buffer.BlockCopy(encryptedBytes, 0, result, SaltSize + IvSize, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }

        public static string Decrypt(string cipherTextBase64, string password)
        {
            byte[] cipherData = Convert.FromBase64String(cipherTextBase64);

            byte[] salt = new byte[SaltSize];
            byte[] iv = new byte[IvSize];
            byte[] cipherBytes = new byte[cipherData.Length - SaltSize - IvSize];

            Buffer.BlockCopy(cipherData, 0, salt, 0, SaltSize);
            Buffer.BlockCopy(cipherData, SaltSize, iv, 0, IvSize);
            Buffer.BlockCopy(cipherData, SaltSize + IvSize, cipherBytes, 0, cipherBytes.Length);

            var key = DeriveKeyFromPassword(password, salt);

            using Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            using MemoryStream ms = new(cipherBytes);
            using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using StreamReader sr = new(cs);
            return sr.ReadToEnd();
        }

        private static byte[] DeriveKeyFromPassword(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(KeySize);
        }

        private static byte[] GenerateRandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }
    }
}
