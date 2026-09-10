using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AutoTable.Services
{
    public class KeyManager
    {
        private static readonly Lazy<KeyManager> _instance = new(() => new KeyManager());
        public static KeyManager Instance => _instance.Value;

        private const string KeyFileName = "autotable_key.dat";

        public string? PrivateKey { get; private set; }
        public string? PublicKey { get; private set; }
        public string? InstanceAddress { get; private set; }
        public bool HasKey => !string.IsNullOrEmpty(PrivateKey);

        private KeyManager() { }

        public void Initialize()
        {
            if (TryLoadKey()) return;
            GenerateNewKey();
        }

        public void GenerateNewKey()
        {
            var ecParams = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
            var domainParams = new Org.BouncyCastle.Crypto.Parameters.ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H);
            var keyGen = new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator();
            keyGen.Init(new Org.BouncyCastle.Crypto.Parameters.ECKeyGenerationParameters(domainParams, new Org.BouncyCastle.Security.SecureRandom()));
            var keyPair = keyGen.GenerateKeyPair();
            var priv = (Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters)keyPair.Private;
            var pub = (Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters)keyPair.Public;
            PrivateKey = priv.D.ToString(16).PadLeft(64, '0');
            PublicKey = "0x" + Convert.ToHexString(pub.Q.GetEncoded(false)).ToLower();
            InstanceAddress = DeriveAddress(pub);
            SaveKey();
        }

        private string DeriveAddress(Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters pub)
        {
            return DeriveAddressFromUncompressedHex("0x" + Convert.ToHexString(pub.Q.GetEncoded(false)).ToLower());
        }

        /// <summary>Derives the 20-byte Ethereum-style address (0x-prefixed) from a 0x-prefixed uncompressed public key.</summary>
        private string DeriveAddressFromUncompressedHex(string pubHex)
        {
            var full = Convert.FromHexString(pubHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? pubHex.Substring(2) : pubHex);
            var pubNoPrefix = new byte[64];
            Array.Copy(full, 1, pubNoPrefix, 0, 64);
            var hash = Keccak256(pubNoPrefix);
            var addr = new byte[20];
            Array.Copy(hash, 12, addr, 0, 20);
            return "0x" + Convert.ToHexString(addr).ToLower();
        }

        /// <summary>Derives the 0x-prefixed uncompressed public key from a 0x-padded private key hex string.</summary>
        private string DerivePublicKeyHex(string privateKeyHex)
        {
            var ec = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
            var dp = new Org.BouncyCastle.Crypto.Parameters.ECDomainParameters(ec.Curve, ec.G, ec.N, ec.H);
            var pk = new Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters(
                new Org.BouncyCastle.Math.BigInteger(1, Convert.FromHexString(privateKeyHex)), dp);
            var q = ec.G.Multiply(pk.D);
            return "0x" + Convert.ToHexString(q.GetEncoded(false)).ToLower();
        }

        private byte[] Keccak256(byte[] data)
        {
            var d = new Org.BouncyCastle.Crypto.Digests.KeccakDigest(256);
            d.BlockUpdate(data, 0, data.Length);
            var hash = new byte[d.GetDigestSize()];
            d.DoFinal(hash, 0);
            return hash;
        }

        public string SignMessage(string message)
        {
            return SignMessage(Keccak256(Encoding.UTF8.GetBytes(message)));
        }

        public string SignMessage(byte[] hash)
        {
            if (string.IsNullOrEmpty(PrivateKey)) throw new InvalidOperationException("No key");
            var ec = Org.BouncyCastle.Asn1.Sec.SecNamedCurves.GetByName("secp256k1");
            var dp = new Org.BouncyCastle.Crypto.Parameters.ECDomainParameters(ec.Curve, ec.G, ec.N, ec.H);
            var pk = new Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters(new Org.BouncyCastle.Math.BigInteger(1, Convert.FromHexString(PrivateKey)), dp);
            var signer = new Org.BouncyCastle.Crypto.Signers.ECDsaSigner();
            signer.Init(true, pk);
            var sig = signer.GenerateSignature(hash);
            var r = sig[0].ToByteArrayUnsigned();
            var s = sig[1].ToByteArrayUnsigned();
            var rp = new byte[32]; var sp = new byte[32];
            Array.Copy(r, 0, rp, 32 - r.Length, r.Length);
            Array.Copy(s, 0, sp, 32 - s.Length, s.Length);
            var result = new byte[65];
            Array.Copy(rp, 0, result, 0, 32);
            Array.Copy(sp, 0, result, 32, 32);
            result[64] = 27;
            return "0x" + Convert.ToHexString(result).ToLower();
        }

        private string GetPath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoTable");

        private void SaveKey()
        {
            var dir = GetPath(); Directory.CreateDirectory(dir);
            var data = new KeyData { PrivateKey = PrivateKey, PublicKey = PublicKey, InstanceAddress = InstanceAddress, CreatedAt = DateTime.UtcNow };
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));
            File.WriteAllBytes(Path.Combine(dir, KeyFileName), System.Security.Cryptography.ProtectedData.Protect(bytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser));
        }

        private bool TryLoadKey()
        {
            var path = Path.Combine(GetPath(), KeyFileName);
            if (!File.Exists(path)) return false;
            try
            {
                var dec = System.Security.Cryptography.ProtectedData.Unprotect(File.ReadAllBytes(path), null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                var kd = JsonSerializer.Deserialize<KeyData>(Encoding.UTF8.GetString(dec));
                if (kd == null || string.IsNullOrEmpty(kd.PrivateKey)) return false;
                PrivateKey = kd.PrivateKey; PublicKey = kd.PublicKey; InstanceAddress = kd.InstanceAddress;

                // Backfill: key files saved by older app versions do not contain the
                // derived instance address (and possibly not the public key either).
                // Re-derive them from the stored private key so first-run setup is not blocked.
                if (string.IsNullOrEmpty(InstanceAddress) || string.IsNullOrEmpty(PublicKey))
                {
                    try
                    {
                        PublicKey = DerivePublicKeyHex(PrivateKey);
                        InstanceAddress = DeriveAddressFromUncompressedHex(PublicKey);
                        SaveKey(); // persist the backfilled fields for future launches
                    }
                    catch
                    {
                        // Corrupt key material — treat as "no key" so a fresh one is generated.
                        return false;
                    }
                }
                return true;
            }
            catch { return false; }
        }

        public void ClearKey()
        {
            var path = Path.Combine(GetPath(), KeyFileName);
            if (File.Exists(path)) File.Delete(path);
            PrivateKey = null; PublicKey = null; InstanceAddress = null;
        }

        private class KeyData { public string? PrivateKey { get; set; } public string? PublicKey { get; set; } public string? InstanceAddress { get; set; } public DateTime CreatedAt { get; set; } }
    }
}
