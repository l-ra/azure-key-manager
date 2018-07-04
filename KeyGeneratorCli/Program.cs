using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Formatting;
using Gnu.Getopt;
using System.Text;
using KeyGenerator;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.KeyVault;


namespace KeyGeneratorCli
{
    public class Startup
    {
        public static TaskCompletionSource<string> Oauth2Code = new TaskCompletionSource<string>();
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.Run(async context =>
            {
                if (!String.IsNullOrEmpty(context.Request.Query["code"]))
                {
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync($"Code: {context.Request.Query["code"]}");
                    Oauth2Code.SetResult(context.Request.Query["code"]);
                }
                else
                {
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    context.Response.StatusCode = 400;
                    var error = context.Request.Query["error"];
                    var errorDesc = context.Request.Query["error_description"];
                    await context.Response.WriteAsync($"Bad request\n{error}\n{errorDesc}");
                    if (!String.IsNullOrEmpty(error)){
                        Oauth2Code.SetException(new Exception(errorDesc));
                    }
                }
            });

        }

        static IWebHost webHost;

        public static void startWeb(string url)
        {
            if (webHost == null)
            {
                webHost = new WebHostBuilder()
                    .UseKestrel()
                    .UseStartup<Startup>()
                    .UseUrls(url)
                    .Build();
                webHost.Start();
            }
        }

        public static void stopWeb()
        {
            if (webHost != null)
                webHost.StopAsync();
        }
    }

    class Program
    {
        static int quorum = -1;
        static int shareCount = -1;
        static int keySize = 2048;
        static string output = null;
        static string kid = null;
        static bool testModeFlag = false;
        static string testMode = "";

        static string tenant = "ludekraseklogica.onmicrosoft.com";
        static string appId = "c35c4ab0-2d8a-4e1c-8eab-3633c82c9f52";
        static string appSecret = null;
        static string redirectUrl = "http://localhost:7373";
        static string resourceId = "https://vault.azure.net/";
        static string vaultUrl = "https://lravault.vault.azure.net/";
        static bool skipshareverify = false;

        static void processOpts(string[] args)
        {
            var longOpts = new LongOpt[]{
                new LongOpt("quorum",Argument.Required,null,'k'),
                new LongOpt("count",Argument.Required,null,'n'),
                new LongOpt("kid",Argument.Required,null,'i'),
                new LongOpt("size",Argument.Required,null,'s'),
                new LongOpt("output",Argument.Required,null,'o'),
                new LongOpt("test",Argument.No,null,'t'),
                new LongOpt("tenant",Argument.No,null,'e'),
                new LongOpt("appid",Argument.No,null,'a'),
                new LongOpt("appsecret",Argument.No,null,'z'),
                new LongOpt("redirecturl",Argument.No,null,'u'),
                new LongOpt("resource",Argument.No,null,'r'),
                new LongOpt("vaulturl",Argument.No,null,'v'),
                new LongOpt("skipshareverify",Argument.No,null,'x'),
            };
            var opts = new Getopt("KeyGeneratorCli", args, "k:n:i:s:o:te:a:u:r:v:xz:", longOpts);

            var c = 0;
            while ((c = opts.getopt()) != -1)
            {
                switch (c)
                {
                    case 'k':
                        quorum = Int32.Parse(opts.Optarg); break;
                    case 'n':
                        shareCount = Int32.Parse(opts.Optarg); break;
                    case 's':
                        keySize = Int32.Parse(opts.Optarg); break;
                    case 'i':
                        kid = opts.Optarg; break;
                    case 'o':
                        output = opts.Optarg; break;
                    case 't':
                        testModeFlag = true;
                        testMode = "<<!! TEST MODE !! - no permanent changes will be performed>>";
                        break;
                    case 'e':
                        tenant = opts.Optarg; break;
                    case 'a':
                        appId = opts.Optarg; break;
                    case 'z':
                        appSecret = opts.Optarg; break;
                    case 'u':
                        redirectUrl = opts.Optarg; break;
                    case 'r':
                        resourceId = opts.Optarg; break;
                    case 'v':
                        vaultUrl = opts.Optarg; break;
                    case 'x':
                        skipshareverify = true; break;
                    case '?':
                    default:
                        //Console.WriteLine("Unkonwn option")
                        break;
                }
                //Console.WriteLine(String.Format("c: {0}, arg: {1}, ind {2}",(char) c, opts.Optarg, opts.Optind));
            }
            if (quorum == -1 || shareCount == -1)
            {
                Console.WriteLine("both -k (--quorum) and -n (--count)  must bespecified");
                Environment.Exit(1);
            }

            if (quorum > shareCount)
            {
                Console.WriteLine("k must be less than or equal to n (k<=n)");
                Environment.Exit(1);
            }

            if (kid == null)
            {
                Console.WriteLine("key identifier must be provided as -i (--kid) option");
                Environment.Exit(1);
            }

            if (output == null)
            {
                output = kid + ".backup";
            }

        }

        static void Main(string[] args)
        {

            processOpts(args);

            Console.Clear();
            displayInitialInfo();
            Console.ReadLine();


            var key = SharedSecretGenerator.genKey(kid);
            var shares = SharedSecretGenerator.generateSharedSecret(32, shareCount, quorum);
            var encryptedKey = SharedSecretGenerator.encryptKey(key,shares);

            displayShareHolderInvitation(shares.Length);
            Console.ReadLine();

            var idx = 1;
            foreach (var share in shares)
            {
                var verified = false;
                while (!verified)
                {
                    Console.Clear();
                    displayShare(idx++, share);

                    // verification
                    verified = readVerifyShare(share.shareIndex, share.shareValue, share.shareHash);
                    if (!verified){
                        displayInvalidShare();
                    }
                }
            }

            Console.Clear();
            displayStoreKeyStorePrompt();
            Console.ReadLine();

            if (!testModeFlag) File.WriteAllText(output,encryptedKey,Encoding.UTF8);
            Console.Clear();
            displayAzureVaultPrompt();
            Console.ReadLine();
            string token=null;
            if (!testModeFlag) token = getToken();
            Console.WriteLine($"Token:\n{token}");
            displayVaultImportConfirm();
            Console.ReadLine();
            if (!testModeFlag) importKeyToVault(key,token,vaultUrl);
            Console.WriteLine("Press [Enter] to continue");
            Console.ReadLine();

            Console.Clear();
            displayFinishInfo();
            Console.ReadLine();
        }


        static void displayShare(int idx, Share share)
        {
            Console.WriteLine($@"

Hi share keeper ___{idx++}____ 

The 'shareValue' consists of 16 groups 
of lowercase letters, in a form cvcvc (consonant/vovel).
The 'shareHash' consists of 2 similar groups.

Write down following information:

  * total number of shares   : {share.n}
  * quorum needed to decrypt : {share.k}
  * key identifier           : {kid}
  * share index (number)     : {share.shareIndex}
  * share value              : {share.shareValue}
  * share hash               : {share.shareHash}
  * secret hash              : {share.secretHash}
--------------------------------------------------------------------------
Press [Enter] when done.

");
            Console.ReadLine();
        }

        static bool readVerifyShare(int shareIndex, string shareValue, string shareHash)
        {
            if (testModeFlag || skipshareverify) return true;
            Console.Clear();
            Console.WriteLine("To verify your record, enter your share information. Confirm every etry with [Enter].");
            Console.Write("Enter shareIndex (a number):");
            var shareIndexVerify = Console.ReadLine();
            Console.Write("Enter shareValue (cvcvc-cvcvc...):");
            var shareValueVerify = Console.ReadLine().ToLower();
            Console.Write("Enter shareHash (cvcvc-cvcvc):");
            var shareHashVerify = Console.ReadLine().ToLower();

            return (shareIndex == Int32.Parse(shareIndexVerify))
                && shareValue.Equals(shareValueVerify)
                && shareHash.Equals(shareHash);
        }
        public static void OpenBrowser(string url)
        {
            //https://stackoverflow.com/a/38604462/1777150

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}")); // Works ok on windows
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);  // Works ok on linux
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url); // Not tested
            }
        }

        static string createLoginUrl()
        {
            return $"https://login.microsoftonline.com/{tenant}/oauth2/authorize?client_id={appId}&response_type=code&redirect_uri={HttpUtility.UrlEncode(redirectUrl)}&response_mode=query&resource={HttpUtility.UrlEncode(resourceId)}";
        }

        static string getToken()
        {
            var loginUrl = createLoginUrl();
            Console.WriteLine($@"
Waiting for token. 
If the browser failed to open, navigate to:
{loginUrl}");
            Startup.startWeb(redirectUrl);
            OpenBrowser(loginUrl);
            var code = Startup.Oauth2Code.Task.Result;
            var parameters = new List<KeyValuePair<string,string>>();
            parameters.AddRange(new []{
                                 new KeyValuePair<string,string>("grant_type","authorization_code"),
                                 new KeyValuePair<string,string>("client_id",appId),
                                 new KeyValuePair<string,string>("code",code),
                                 new KeyValuePair<string,string>("redirect_uri",redirectUrl),
                                 new KeyValuePair<string,string>("resource",resourceId)
                            });
            if (!String.IsNullOrEmpty(appSecret)) parameters.Add( new KeyValuePair<string,string>("client_secret",appSecret));
            var postContent = new FormUrlEncodedContent(parameters);
            var result = new HttpClient().PostAsync(
                $"https://login.microsoftonline.com/{tenant}/oauth2/token",
                postContent).Result;
            var response = result.Content.ReadAsStringAsync().Result;
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(response);
            return tokenResponse.access_token;
        }
        static void importKeyToVault(JwtRsaKey key, string token, string vaultUrl){
            var importRequestUrl = $"{vaultUrl}/keys/{kid}?api-version=2016-10-01";
            var req = new HttpClient();
            req.DefaultRequestHeaders.Add("Authorization",new[]{$"Bearer {token}"});
            var content = new ObjectContent<ImportKeyRequest>(new ImportKeyRequest{key=key},new JsonMediaTypeFormatter());
            var result = new HttpClient().PutAsync(importRequestUrl,content).Result;
            var response = result.Content.ReadAsStringAsync().Result;
            Console.WriteLine($"Import key result {result.StatusCode} {result.ReasonPhrase}\n===\n{response}\n===\n");
        }

        static void importKeyToVault2(JwtRsaKey key, string token, string vaultUrl){
            var client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(async (authority, resource, scope)=>await Task.FromResult(token)));
            var keyBnd = new Microsoft.Azure.KeyVault.Models.KeyBundle();
            keyBnd.Key=new Microsoft.Azure.KeyVault.WebKey.JsonWebKey{
                Kty=key.kty,
                Kid=key.kid,
                E = Encoding.UTF8.GetBytes(key.e),
                P = Encoding.UTF8.GetBytes(key.p),
                Q = Encoding.UTF8.GetBytes(key.q),
                QI = Encoding.UTF8.GetBytes(key.qi),
                DP = Encoding.UTF8.GetBytes(key.dp),
                DQ = Encoding.UTF8.GetBytes(key.dq)
            };
            var result = client.ImportKeyAsync(vaultUrl,key.kid,keyBnd).Result;
        }


        static void displayInitialInfo(){
                        Console.WriteLine($@"

=== Summary ===
Will generate RSA key of size {keySize} identified by '{kid}', 
split into {shareCount} shares from which at least {quorum} need to be available
to decrypt the key backup.
The key backup will be stored to '{output}'
Press [Enter] to continue.
======

");
        }

        static void displayShareHolderInvitation(int shareHoldersCount){
                        Console.WriteLine($@"
Key generated, decryption shares generated.


Bring {shareHoldersCount} share keepers one by one. 


Press [Enter] when ready.");

        }


        static void displayStoreKeyStorePrompt(){
                        Console.WriteLine($@"
All shares distributed. The key backup will be stored on 
the disk in a file: {output} 

Press [Enter] to store key backup {testMode}.
");

        }


        static void displayInvalidShare(){
                                    Console.WriteLine(@"
                        
Share values invalid. Verify your record and try again.

Press [Enter] when ready.");
                        Console.ReadLine();

        }

        static void displayAzureVaultPrompt(){
                        Console.WriteLine($@"
            
The key will be imported into the Azure Key Vault using following parameters:
* tenant for token     : {tenant}
* client id for token  : {appId}
* redirect URL         : {redirectUrl}
* resource             : {resourceId}
* vault URL            : {vaultUrl}
* key id               : {kid}

Press [Enter] to proceed {testMode}.      
");

        }

        static void displayFinishInfo(){
                        Console.WriteLine($@"
All done.

Press [Enter] to finish.

{testMode}
");

        }


        static void displayVaultImportConfirm(){
            Console.WriteLine($@"
Token acquired, ready to proceed to vault import.

Press [Enter] to continue {testMode}");
        }

    }
    public class ImportKeyRequest {
        public JwtRsaKey key;
    }

    public class TokenResponse {
        public string token_type;
        public long expires_in;
        public string scope;
        public long expires_on;
        public long notbefore;
        public string resource;
        public string access_token;
    }




}
