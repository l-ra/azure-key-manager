using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace KeyVaultImportPoC
{
    class Program
    {
        static string resourceId = "https://vault.azure.net/";
        static string appId = "2af6052f-e630-4f7c-977f-47459db41cf0";
        static string tenant = "c72651f1-0bdc-4774-ab6a-00a5a628d8dc";
        static string vaultUrl = " https://fanis-larag-0001.vault.azure.net/";
        static string kid = "xxx";


        static void Main(string[] args)
        {
            try {
                var key = genKey(kid);
                Console.WriteLine("key generated");
                var token = getToken(tenant).Result.AccessToken;
                Console.WriteLine("token acquired");
                importKeyToVault(key,token,vaultUrl);
                Console.WriteLine("import finished");
            }
            catch (Exception e){
                Console.WriteLine($"Exception: {e}");
            }
        }


        static void importKeyToVault(JwtRsaKey key, string token, string vaultUrl)
        {
            var client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(async (authority, resource, scope) => await Task.FromResult(token)));
            var keyBnd = new Microsoft.Azure.KeyVault.Models.KeyBundle();
            keyBnd.Key = new Microsoft.Azure.KeyVault.WebKey.JsonWebKey
            {
                Kty = key.kty,
                Kid = key.kid,
                E = Encoding.UTF8.GetBytes(key.e),
                P = Encoding.UTF8.GetBytes(key.p),
                Q = Encoding.UTF8.GetBytes(key.q),
                QI = Encoding.UTF8.GetBytes(key.qi),
                DP = Encoding.UTF8.GetBytes(key.dp),
                DQ = Encoding.UTF8.GetBytes(key.dq)
            };
            var result = client.ImportKeyAsync(vaultUrl, key.kid, keyBnd).Result;
        }

        static async Task<AuthenticationResult> getToken(string tenant)
        {
            AuthenticationContext ctx = null;
            if (tenant != null)
                ctx = new AuthenticationContext("https://login.microsoftonline.com/" + tenant);
            else
            {
                ctx = new AuthenticationContext("https://login.microsoftonline.com/common");
                if (ctx.TokenCache.Count > 0)
                {
                    string homeTenant = ctx.TokenCache.ReadItems().First().TenantId;
                    ctx = new AuthenticationContext("https://login.microsoftonline.com/" + homeTenant);
                }
            }
            AuthenticationResult result = null;
            try
            {
                result = await ctx.AcquireTokenSilentAsync(resourceId, appId);
            }
            catch (AdalSilentTokenAcquisitionException)
            {
                result = await GetTokenViaCode(ctx);
            }
            catch (AdalException exc)
            {
                PrintError(exc);
            }
            return result;

        }



        private static void PrintError(Exception exc)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Something went wrong.");
            Console.WriteLine("Message: " + exc.Message + "\n");
        }

        static async Task<AuthenticationResult> GetTokenViaCode(AuthenticationContext ctx)
        {
            AuthenticationResult result = null;
            try
            {
                DeviceCodeResult codeResult = await ctx.AcquireDeviceCodeAsync(resourceId, appId);
                Console.ResetColor();
                Console.WriteLine("You need to sign in.");
                Console.WriteLine("Message: " + codeResult.Message + "\n");
                result = await ctx.AcquireTokenByDeviceCodeAsync(codeResult);
            }
            catch (Exception exc)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Something went wrong.");
                Console.WriteLine("Message: " + exc.Message + "\n");
            }
            return result;
        }


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

}
/*

New-AzureADApplication [-AddIns <System.Collections.Generic.List`1[Microsoft.Open.AzureAD.Model.AddIn]>] 
[-AppRoles <System.Collections.Generic.List`1[Microsoft.Open.AzureAD.Model.AppRole]>] 
[-AvailableToOtherTenants <Boolean>] 
-DisplayName <String> 
[-ErrorUrl <String>] [-GroupMembershipClaims <String>] 
[-Homepage <String>] 
[-IdentifierUris <System.Collections.Generic.List`1[System.String]>] 
[-KeyCredentials <System.Collections.Generic.List`1[Microsoft.Open.AzureAD.Model.KeyCredential]>] 
[-KnownClientApplications <System.Collections.Generic.List`1[System.String]>] 
[-LogoutUrl <String>] 
[-Oauth2AllowImplicitFlow <Boolean>] 
[-Oauth2AllowUrlPathMatching <Boolean>] 
[-Oauth2Permissions  <System.Collections.Generic.List`1[Microsoft.Open.AzureAD.Model.OAuth2Permission]>] 
[-PasswordCredentials <System.Collections.Generic.List`1[Microsoft.Open.AzureAD.Model.PasswordCredential]>] 
[-PublicClient <Boolean>] [-RecordConsentConditions <String>] 
[-ReplyUrls <System.Collections.Generic.List`1[System.String]>] 
[-RequiredResourceAccess <System.Collections.Generic.List`1[Microsoft.Open.AzureAD.Model.RequiredResourceAccess]>] 
[-SamlMetadataUrl <String>]
[-Oauth2RequirePostResponse <Boolean>] [<CommonParameters>]




 */