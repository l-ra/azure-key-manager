using System;
using Xunit;
using KeyGenerator;
using ProquintUtil;
using Newtonsoft.Json;

namespace KeyGenerator.Tests
{
    

    public class UnitTest1
    {

        [Fact]
        public void Test1()
        {
            var n = 6;
            var k = 3;
            var shares = SharedSecretGenerator.generateSharedSecret(32,n,k);
            Assert.NotNull(shares);
            Assert.Equal(shares.Length,n);

            foreach (var share in shares){
                Assert.NotNull(share);
                Assert.NotNull(share.shareValue);
                Console.WriteLine(String.Format("{0:x08}:{1}|{2}",
                         share.shareIndex, share.shareValue, share.shareHash));
            }
        }

        [Fact]
        public void Test2(){
            var key = SharedSecretGenerator.genKey("sec.oper");
            var shares = SharedSecretGenerator.generateSharedSecret(32,6,3);
            var encryptedKey = SharedSecretGenerator.encryptKey(key,shares);
            var secret = PQ.bytes2hex(SharedSecretGenerator.joinShares(shares));
            var secretMac = secret.Substring(0,secret.Length/2);
            var secretEnc = secret.Substring(secret.Length/2);

            Console.WriteLine("Secret:\n"+secret);
            Console.WriteLine("SecretMac:\n"+secretMac);
            Console.WriteLine("SecretEnc:\n"+secretEnc);
            Console.WriteLine("----");

            Console.WriteLine("Encrypted key:\n"+encryptedKey);
            Console.WriteLine("----");

            string[] parts=encryptedKey.Split('.');
            Console.WriteLine("--Authenticated header:\n"+parts[0]);
            Console.WriteLine("--Encrypted key:\n"+parts[1]);
            Console.WriteLine("--IV:\n"+parts[2]);
            Console.WriteLine("--Cipher text:\n"+parts[3]);
            Console.WriteLine("--Auth Tag:\n"+parts[4]);
            Console.WriteLine("----");

            var authenticatedHeader = Base64Url.Decode(parts[0]);
            byte[] iv = Base64Url.Decode(parts[2]);
            var cipherText = Base64Url.Decode(parts[3]);

            Console.WriteLine("You can test decryption using followinf openssl command.");
            Console.WriteLine(String.Format("echo -n {0} | xxd -r -p | openssl enc -aes-128-cbc -d -K {1} -iv {2} ",
                           PQ.bytes2hex(cipherText), secretEnc, PQ.bytes2hex(iv)));
            
            var decrypted = SharedSecretGenerator.decryptKey(encryptedKey,shares);
            Console.WriteLine("Decrypted key: \n"+decrypted);
            Console.WriteLine("----");
        }
    }
}
