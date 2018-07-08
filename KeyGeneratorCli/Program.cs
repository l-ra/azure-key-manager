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
using System.Text.RegularExpressions;
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
        const string proquintPattern = "[bcdfghjklmnpqrstvwxz][aeiouy][bcdfghjklmnpqrstvwxz][aeiouy][bcdfghjklmnpqrstvwxz]-[bcdfghjklmnpqrstvwxz][aeiouy][bcdfghjklmnpqrstvwxz][aeiouy][bcdfghjklmnpqrstvwxz](-[bcdfghjklmnpqrstvwxz][aeiouy][bcdfghjklmnpqrstvwxz][aeiouy][bcdfghjklmnpqrstvwxz]-[bcdfghjklmnpqrstvwxz][aeiouy][bcdfghjklmnpqrstvwxz][aeiouy][bcdfghjklmnpqrstvwxz])*";
        const string CMD_GENERATE = "generate";
        const string CMD_RECOVER = "recover";
        static string[] COMMANDS = new string[]{CMD_GENERATE,CMD_RECOVER};
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
        static string command = CMD_GENERATE;
        static string backupToRecover;

        static void processOpts(string[] args)
        {
            var longOpts = new LongOpt[]{
                new LongOpt("quorum",Argument.Required,null,'k'),
                new LongOpt("count",Argument.Required,null,'n'),
                new LongOpt("kid",Argument.Required,null,'i'),
                new LongOpt("size",Argument.Required,null,'s'),
                new LongOpt("output",Argument.Required,null,'o'),
                new LongOpt("test",Argument.No,null,'t'),
                new LongOpt("tenant",Argument.Required,null,'e'),
                new LongOpt("appid",Argument.Required,null,'a'),
                new LongOpt("appsecret",Argument.Required,null,'z'),
                new LongOpt("redirecturl",Argument.Required,null,'u'),
                new LongOpt("resource",Argument.Required,null,'r'),
                new LongOpt("vaulturl",Argument.Required,null,'v'),
                new LongOpt("skipshareverify",Argument.No,null,'x'),
                new LongOpt("help",Argument.No,null,'h'),
                new LongOpt("command",Argument.Required,null,'c'),
                new LongOpt("backup",Argument.Required,null,'b')
            };
            var opts = new Getopt("KeyGeneratorCli", args, "k:n:i:s:o:te:a:u:r:v:xz:hc:b:", longOpts);

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
                    case 'c':
                        command = opts.Optarg; break;
                    case 'b':
                        backupToRecover = opts.Optarg; break;
                    case 'h':
                        displayHelp();break;
                    case '?':
                    default:
                        //Console.WriteLine("Unkonwn option")
                        break;
                }
                //Console.WriteLine(String.Format("c: {0}, arg: {1}, ind {2}",(char) c, opts.Optarg, opts.Optind));
            }

            if ( Array.Find(COMMANDS,validCommand=>validCommand.Equals(command))==null){
                Console.WriteLine($"Bad command {command}. Allowed commands: [{String.Join(",",COMMANDS)}]");
                Environment.Exit(1);
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

            if ( backupToRecover == null ){
                backupToRecover = kid + ".backup";
            }

        }

        static void Main(string[] args)
        {

            processOpts(args);

            switch (command){
                case CMD_GENERATE: doGenerate(); break;
                case CMD_RECOVER: doRecover(); break;
            }

        }


        static void doGenerate(){
            if ( File.Exists(output) ){
                Console.WriteLine("Output exists. Quitting.");
                Environment.Exit(1);
            }

            Console.Clear();
            displayGenerateInitialInfo();
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
                    verified = readVerifyShare(share.shareIndex, share.shareValue, share.shareHash, shareCount, idx);
                    if (!verified){
                        displayInvalidShare();
                    }
                }
            }

            Console.Clear();
            displayStoreKeyStorePrompt();
            Console.ReadLine();

            if (!testModeFlag){
                File.WriteAllText(output,encryptedKey,Encoding.UTF8);
            } 
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

        static void doRecover(){
            if ( ! File.Exists(backupToRecover) ){
                Console.WriteLine($"Backup file {backupToRecover} does not exists. Quitting.");
                Environment.Exit(1);
            }

            Console.Clear();
            displayRecoverInitialInfo();
            Console.ReadLine();

            displayShareHolderInvitation(quorum);
            Console.ReadLine();

            Share[] shares = new Share[quorum];
            

            for( var i=0; i< shares.Length; i++)
            {
                var share = shares[i] = new Share();
                share.n=quorum+1; // just to satisfy validations in SharedSecretGenerator
                share.k=quorum;
                readShare(share,quorum,i+1,true);
            }     

            var secret = SharedSecretGenerator.joinShares(shares);
            var encryptedKey = File.ReadAllText(backupToRecover,Encoding.UTF8);
            var keyJson = SharedSecretGenerator.decryptKey(encryptedKey,shares);
            var key = JsonConvert.DeserializeObject<KeyGenerator.JwtRsaKey>(keyJson);

            //=========================

            Console.Clear();
            displayKeyRecoveredInfo();
            Console.ReadLine();

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

        static void displayKeyRecoveredInfo(){
            Console.Clear();
            Console.WriteLine($@"
Key was recovered and is ready to import to secure destination.

Press [Enter] to proceed {testMode}
            ");
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

        static void readShare(Share share, int count, int current, bool withSecretHash=false){
            Console.Clear();

            Console.WriteLine($@"
Share {current} of {count}:
Enter your share information. Confirm every entry with [Enter].");
            
            while (true){

                while(true){
                    Console.Write("Enter shareIndex (a number):");
                    var input = Console.ReadLine();
                    if (Int32.TryParse(input,out share.shareIndex)) 
                        break;
                    else 
                        Console.WriteLine("..Bad fromat - enter decimal number");
                }
                
                while(true){
                    Console.Write("Enter shareValue (cvcvc-cvcvc...):");
                    share.shareValue = Console.ReadLine().ToLower();
                    if (Regex.IsMatch(share.shareValue,proquintPattern)) break;
                    else {
                        Console.WriteLine("Bad share value format ...");
                    }
                }
                
                while(true){
                    Console.Write("Enter shareHash (cvcvc-cvcvc):");
                    share.shareHash = Console.ReadLine().ToLower();
                    if (Regex.IsMatch(share.shareHash,proquintPattern)) {
                        break;
                    }
                    else {
                        Console.WriteLine("Bad share hash format ...");
                    }
                }

                // check hash of the share 
                var shareHash = SharedSecretGenerator.computeShareHash(share.shareIndex,share.shareValue);
                if (shareHash.Equals(share.shareHash)){
                    break;
                }
                else {
                    Console.WriteLine("Bad share hash - typing error ? Type all this share info again.");    
                }
            }

            if (withSecretHash){
                while(true){
                    Console.Write("Enter secretHash (cvcvc-cvcvc):");
                    share.secretHash = Console.ReadLine().ToLower();                
                    if (Regex.IsMatch(share.secretHash,proquintPattern)) break;
                    else {
                        Console.WriteLine("Bad secret hash format ...");
                    }
                }
            }
        }

        static bool readVerifyShare(int shareIndex, string shareValue, string shareHash, int count, int current)
        {
            if (testModeFlag || skipshareverify) return true;
            Share verify = new Share();

            readShare(verify,count,current);

            return (shareIndex == verify.shareIndex)
                && shareValue.Equals(verify.shareValue)
                && shareHash.Equals(verify.shareHash);
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


        static void displayGenerateInitialInfo(){
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

        static void displayRecoverInitialInfo(){
                        Console.WriteLine($@"

=== Summary ===
Will recover RSA key identified by '{kid}', 
expecting {quorum} shares will be entered
to decrypt the key backup.
The key backup will be read from '{backupToRecover}'
Press [Enter] to continue.
======

");
        }


        static void displayShareHolderInvitation(int shareHoldersCount){
                        Console.WriteLine($@"

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


        static void displayHelp(){
            Console.WriteLine(@"

* `-c|--command` - available commands: 
  * `generate` - will generate new key - the default command
  * `recover` - will recover from encrypted key backup (see --backup)
* `-n|--count` - total number of shares
* `-k|--quorum`	- number of shares needed to decrypt the key backup
* `-i|--kid` - key identifier
* `-s|--size` - RSA key size (not implemented, defaults to 2048)
* `-o|--output` - name of output file. Defaults to `kid.backup`
* `-b|--backup` - name of input file containing key backup. Defaults to `kid.backup`
* `-t|--test` - test mode - skips share verifications, no key output, no key imported into vault
* `-e|--tenant` - tenant to use for OAuth2 token request
* `-a|--appid` - appid (client_id) to use for OAuth2 token request
* `-c|--appsecret` - app secret (client_secret) to use for OAuth2 token request
* `-u|--redirecturl` - oauth2 redirect url - defuelts to http://localhost:7373
* `-r|--resource` - oauth2 request resource defaults to https://vault.azure.net
* `-v|--vaulturl` - the target vault URL, defaults to https://lravault.vault.azure.net/
* `-x|--skipshareverify` - skips the verification of the shares DANGEROUS for production use, share may be misstyped
* `-h|--help` - display help
            ");
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
