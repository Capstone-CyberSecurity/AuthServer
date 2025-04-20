using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace AuthenticationServer.Utility
{
    public class Crypto
    {
        private RSA rsa;
        private byte[] aesKey;

        public Crypto()
        {
            //rsa private 키 생성
            rsa = RSA.Create(2048);
        }

        //RSA 관련
        public byte[] GetPublicKeyBytes()
        {
            return rsa.ExportRSAPublicKey();
        }

        public void LoadPublicKey(byte[] publicKeyBytes)
        {
            rsa.ImportRSAPublicKey(publicKeyBytes, out _);
        }

        public byte[] RsaEncrypt(byte[] data)
        {
            return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
        }

        public byte[] RsaDecrypt(byte[] encryptedData)
        {
            return rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
        }

        // AES-GCM 관련
        public void SetAesGcmKey(byte[] key)
        {
            aesKey = key;
        }

        public (byte[] ciphertext, byte[] tag) AesGcmEncrypt(byte[] iv, byte[] plaintext)
        {
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16];
            using var aesGcm = new AesGcm(aesKey);
            aesGcm.Encrypt(iv, plaintext, ciphertext, tag);
            return (ciphertext, tag);
        }

        public byte[] AesGcmDecrypt(byte[] iv, byte[] ciphertext, byte[] tag)
        {
            byte[] plaintext = new byte[ciphertext.Length];
            using var aesGcm = new AesGcm(aesKey);
            aesGcm.Decrypt(iv, ciphertext, tag, plaintext);
            return plaintext;
        }

        // AES-GCM 키, IV 생성
        public static byte[] GenerateRandomKey(int size = 32) => RandomNumberGenerator.GetBytes(size);
        public static byte[] GenerateRandomIV(int size = 12) => RandomNumberGenerator.GetBytes(size);
    }
}
