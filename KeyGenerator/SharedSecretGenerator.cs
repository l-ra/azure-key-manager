using System;
using System.Text;
using System.Collections.Generic;
using ShamirSecretShareSchema;
using ProquintUtil;
using System.Security.Cryptography;
using Jose.jwe;
using Jose;
using Newtonsoft.Json;


namespace KeyGenerator
{
    public class JwtRsaKey
    {
        public string kty;
        public string kid;
        public string use;
        public string m;
        public string e;
        public string d;
        public string p;
        public string q;
        public string dp;
        public string dq;
        public string qi;
    }
    public class Share
    {
        public int n;
        public int k;
        public int shareIndex;
        public string shareValue;
        public string shareHash;
    }
    public static class Base64Url
    {
        public static string Encode(byte[] arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException("arg");
            }

            var s = Convert.ToBase64String(arg);
            return s
                .Replace("=", "")
                .Replace("/", "_")
                .Replace("+", "-");
        }

        public static string ToBase64(string arg)
        {
            if (arg == null)
            {
                throw new ArgumentNullException("arg");
            }

            var s = arg
                    .PadRight(arg.Length + (4 - arg.Length % 4) % 4, '=')
                    .Replace("_", "/")
                    .Replace("-", "+");

            return s;
        }

        public static byte[] Decode(string arg)
        {
            var decrypted = ToBase64(arg);

            return Convert.FromBase64String(decrypted);
        }
    }

    public class SharedSecretGenerator
    {


        /* Formats text in form lowercase(HHHHHHHH:CVCVC-..-CVCVC), where 
           HHHHHHHH is a hex form of shareIndex left zero padded 
           and : is colon and
           CV.. is proquint encoded share value  
        */
        public static string computeShareHash(int shareIndex, string shareValue)
        {
            var bytes = Encoding.UTF8.GetBytes(String.Format("{0:x08}:{1}", shareIndex, shareValue).ToLower());
            var hash = SHA256.Create();
            var hashValue = hash.ComputeHash(bytes);
            var hashValuePrefix = new byte[4];
            Array.Copy(hashValue, hashValuePrefix, 4);
            return ProquintUtil.PQ.bytes2quints(hashValuePrefix);
        }

        public static Share[] generateSharedSecret(int length, int n, int k)
        {
            var secret = new byte[length];
            var random = RandomNumberGenerator.Create();
            random.GetBytes(secret);
            var scheme = Scheme.of(n, k);
            var sharesBinary = scheme.split(secret);
            var shares = new Share[n];
            var idx = 0;
            foreach (var shareBinary in sharesBinary)
            {
                var shareValue = PQ.bytes2quints(shareBinary.Value);
                shares[idx++] = new Share
                {
                    n = n,
                    k = k,
                    shareIndex = shareBinary.Key,
                    shareValue = shareValue,
                    shareHash = computeShareHash(shareBinary.Key, shareValue)
                };
            }
            return shares;
        }

        public static byte[] joinShares(Share[] shares)
        {
            if (shares == null || shares.Length == 0) throw new Exception("shares must not be null or empty array");
            var n = shares[0].n;
            var k = shares[0].k;
            if (shares.Length < k) throw new Exception(String.Format("too little shares, at least {0} shares must bu provided", k));
            var s = new Dictionary<int, byte[]>();
            foreach (var share in shares)
            {
                if (share.n != n || share.k != k)
                    throw new Exception("inconsistent shares - n,k must be the same in all shares");
                if (share.shareValue == null || share.shareHash == null)
                    throw new Exception("shareValue and shareHash can't be null");
                if (!computeShareHash(share.shareIndex, share.shareValue).Equals(share.shareHash))
                    throw new Exception("inconsistent share hash - typing error?");
                s.Add(share.shareIndex, ProquintUtil.PQ.quints2bytes(share.shareValue));
            }
            //validations done
            return Scheme.of(n, k).join(s);
        }

        static string toB64(byte[] b)
        {
            return Base64Url.Encode(b);
        }

        public static JwtRsaKey genKey(string kid)
        {
            var rsa = RSA.Create();
            var rsaParams = rsa.ExportParameters(true);
            var jwk = new JwtRsaKey
            {
                kty = "RSA",
                use = "enc",
                kid = kid,
                e = toB64(rsaParams.Exponent),
                m = toB64(rsaParams.Modulus),
                d = toB64(rsaParams.D),
                p = toB64(rsaParams.P),
                q = toB64(rsaParams.Q),
                dp = toB64(rsaParams.DP),
                dq = toB64(rsaParams.DQ),
                qi = toB64(rsaParams.InverseQ)
            };
            return jwk;
            //Console.WriteLine(JsonConvert.SerializeObject(jwk));
        }

        public static string encryptKey(JwtRsaKey key, Share[] sharesOfSecret)
        {
            return Jose.JWT.Encode(key, joinShares(sharesOfSecret), JweAlgorithm.DIR, JweEncryption.A128CBC_HS256);
        }

        public static string decryptKey(string encrypted, Share[] sharesOfSecret)
        {
            return Jose.JWT.Decode(encrypted, joinShares(sharesOfSecret), JweAlgorithm.DIR, JweEncryption.A128CBC_HS256);
        }
    }
}
