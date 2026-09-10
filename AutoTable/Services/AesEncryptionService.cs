using System;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace AutoTable.Services
{
    /// <summary>
    /// AES-256-GCM encryption for the license file.
    /// The 256-bit key is derived deterministically from the instance private
    /// key (never stored), so only the instance that owns the key pair can
    /// decrypt its license file. The AAD binds the ciphertext to the instance
    /// address + school code so a file copied to another machine fails
    /// authentication before decryption.
    /// </summary>
    public class AesEncryptionService
    {
        private static readonly Lazy<AesEncryptionService> _instance = new(() => new AesEncryptionService());
        public static AesEncryptionService Instance => _instance.Value;

        private const string KeyPurpose = "AUTOTABLE_LICENSE_AES_KEY_v1";
        private const int KeyBytes = 32;   // 256-bit
        private const int NonceBytes = 12; // 96-bit GCM nonce
        private const int TagBytes = 16;   // 128-bit auth tag

        /// <summary>
        /// Derive the 256-bit AES key from the instance private key hex string.
        /// Deterministic for a given private key.
        /// </summary>
        public byte[] DeriveKey(string instancePrivateKey)
        {
            var input = Encoding.UTF8.GetBytes(instancePrivateKey + KeyPurpose);
            return SHA256.HashData(input);
        }

        /// <summary>Encrypt the given plaintext; returns base64 nonce|ciphertext|tag.</summary>
        public string Encrypt(byte[] plaintext, string instancePrivateKey, string aad)
        {
            var key = DeriveKey(instancePrivateKey);

            var nonce = new byte[NonceBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }

            var aadBytes = Encoding.UTF8.GetBytes(aad ?? string.Empty);

            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(true, new ParametersWithIV(new KeyParameter(key), nonce));
            if (aadBytes.Length > 0) cipher.ProcessAadBytes(aadBytes, 0, aadBytes.Length);

            var ciphertext = new byte[plaintext.Length];
            var len = cipher.ProcessBytes(plaintext, 0, plaintext.Length, ciphertext, 0);
            cipher.DoFinal(ciphertext, len);
            var tag = cipher.GetMac();

            var combined = new byte[NonceBytes + plaintext.Length + tag.Length];
            System.Array.Copy(nonce, 0, combined, 0, nonce.Length);
            System.Array.Copy(ciphertext, 0, combined, nonce.Length, plaintext.Length);
            System.Array.Copy(tag, 0, combined, nonce.Length + plaintext.Length, tag.Length);

            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// Decrypt a combined base64 blob. Throws on tampering (wrong key,
        /// wrong AAD, corrupted data, or copy from another instance).
        /// </summary>
        public byte[] Decrypt(string combinedBase64, string instancePrivateKey, string aad)
        {
            var combined = Convert.FromBase64String(combinedBase64);
            if (combined.Length < NonceBytes + TagBytes)
                throw new InvalidOperationException("Encrypted license file is corrupt.");

            var nonce = new byte[NonceBytes];
            System.Array.Copy(combined, 0, nonce, 0, NonceBytes);

            var ciphertextLen = combined.Length - NonceBytes - TagBytes;
            var ciphertext = new byte[ciphertextLen];
            System.Array.Copy(combined, NonceBytes, ciphertext, 0, ciphertextLen);

            var tag = new byte[TagBytes];
            System.Array.Copy(combined, NonceBytes + ciphertextLen, tag, 0, TagBytes);

            var key = DeriveKey(instancePrivateKey);
            var aadBytes = Encoding.UTF8.GetBytes(aad ?? string.Empty);

            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(false, new ParametersWithIV(new KeyParameter(key), nonce));
            if (aadBytes.Length > 0) cipher.ProcessAadBytes(aadBytes, 0, aadBytes.Length);

            var plaintext = new byte[ciphertextLen];
            var len = cipher.ProcessBytes(ciphertext, 0, ciphertextLen, plaintext, 0);
            cipher.DoFinal(plaintext, len);

            // GCM verification: computed MAC must match the stored tag,
            // otherwise the ciphertext/AAD/key was wrong (tamper or foreign copy).
            var expected = cipher.GetMac();
            var mismatch = expected == null || expected.Length != tag.Length;
            for (var i = 0; !mismatch && i < tag.Length; i++)
                mismatch = expected[i] != tag[i];

            if (mismatch)
                throw new InvalidOperationException("License authentication failed — file tampered or copied from another instance.");

            return plaintext;
        }
    }
}