using System;
using System.Text;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;

namespace AutoTable.Services
{
    /// <summary>
    /// ECDSA signing/verification (Secp256k1, SHA-256 hash) for the license
    /// file integrity layer. Reuses the instance key pair created by KeyManager
    /// ("unified cryptography").
    /// Signature stored as "r:s" hex (r then s, each 32-byte padded).
    /// </summary>
    public class EcdsaSigningService
    {
        private static readonly Lazy<EcdsaSigningService> _instance = new(() => new EcdsaSigningService());
        public static EcdsaSigningService Instance => _instance.Value;

        /// <summary>The canonical byte representation of the license file fields being signed (field order matters).</summary>
        public byte[] BuildSigningPayload(AutoTable.Models.LicenseFileData license)
        {
            var sb = new StringBuilder();
            sb.Append(license.RegistrationTime).Append('|');
            sb.Append(license.RegistrationCounter).Append('|');
            sb.Append(license.LastValidatedTime).Append('|');
            sb.Append(license.LastValidatedCounter).Append('|');
            sb.Append(license.LicensePeriodMinutes).Append('|');
            sb.Append(license.GracePeriodMinutes).Append('|');
            sb.Append(license.InstanceAddress).Append('|');
            sb.Append(license.ActivationCode).Append('|');
            sb.Append(license.SchoolCode);
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        /// <summary>Sign the payload hash with the instance private key. Returns "r:s" hex.</summary>
        public string Sign(byte[] payload, string privateKeyHex)
        {
            var domain = GetDomain();
            var d = new BigInteger(1, FromHex(privateKeyHex));
            var privKey = new ECPrivateKeyParameters(d, domain);

            var hash = System.Security.Cryptography.SHA256.HashData(payload);

            var signer = new ECDsaSigner();
            signer.Init(true, privKey);
            var sig = signer.GenerateSignature(hash);
            var r = sig[0];
            var s = sig[1];

            // Normalize s (low-s) for deterministic verification.
            var halfOrder = domain.N.ShiftRight(1);
            if (s.CompareTo(halfOrder) > 0)
                s = domain.N.Subtract(s);

            return Pad32(r) + ":" + Pad32(s);
        }

        /// <summary>Verify a "r:s" hex signature over the payload using the instance public key.</summary>
        public bool Verify(byte[] payload, string signatureHex, string publicKeyHex)
        {
            try
            {
                var parts = signatureHex.Split(':');
                if (parts.Length != 2) return false;

                var domain = GetDomain();

                // Rebuild public key (uncompressed 0x04||X||Y) from KeyManager's public key hex.
                var pubRaw = FromHex(publicKeyHex.Replace("0x", ""));
                if (pubRaw.Length == 65 && pubRaw[0] == 0x04)
                {
                    var x = new BigInteger(1, SubArray(pubRaw, 1, 32));
                    var y = new BigInteger(1, SubArray(pubRaw, 33, 32));
                    var Q = domain.Curve.CreatePoint(x, y);
                    var pubKey = new ECPublicKeyParameters(Q, domain);

                    var hash = System.Security.Cryptography.SHA256.HashData(payload);
                    var r = new BigInteger(1, FromHex(parts[0]));
                    var s = new BigInteger(1, FromHex(parts[1]));

                    var signer = new ECDsaSigner();
                    signer.Init(false, pubKey);
                    return signer.VerifySignature(hash, r, s);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private ECDomainParameters GetDomain()
        {
            var ec = SecNamedCurves.GetByName("secp256k1");
            return new ECDomainParameters(ec.Curve, ec.G, ec.N, ec.H);
        }

        private static string Pad32(BigInteger value) =>
            value.ToByteArrayUnsigned().Length switch
            {
                32 => Convert.ToHexString(value.ToByteArrayUnsigned()).ToLower(),
                var len when len < 32 => "0".PadLeft((32 - len) * 2, '0') + Convert.ToHexString(value.ToByteArrayUnsigned()).ToLower(),
                _ => Convert.ToHexString(value.ToByteArrayUnsigned()).ToLower()
            };

        private static byte[] FromHex(string hex) => Convert.FromHexString(hex.Replace("0x", ""));

        private static byte[] SubArray(byte[] src, int offset, int length)
        {
            var result = new byte[length];
            Array.Copy(src, offset, result, 0, length);
            return result;
        }
    }
}