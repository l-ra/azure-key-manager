using System;
using Gnu.Getopt;
using System.Text;
using KeyGenerator;

namespace KeyGeneratorCli
{
    class Program
    {
        static int kOpt = -1;
        static int nOpt = -1;
        static int sOpt = 2048;
        static string oOpt = null;
        static string iOpt = null;


        static void processOpts(string[] args)
        {
            var longOpts = new LongOpt[]{
                new LongOpt("quorum",Argument.Required,null,'k'),
                new LongOpt("count",Argument.Required,null,'n'),
                new LongOpt("kid",Argument.Required,null,'i'),
                new LongOpt("size",Argument.Required,null,'s'),
                new LongOpt("output",Argument.Required,null,'o'),
            };
            var opts = new Getopt("KeyGeneratorCli", args, "k:n:i:", longOpts);

            var c = 0;
            while ((c = opts.getopt()) != -1)
            {
                switch (c)
                {
                    case 'k':
                        kOpt = Int32.Parse(opts.Optarg); break;
                    case 'n':
                        nOpt = Int32.Parse(opts.Optarg); break;
                    case 's':
                        sOpt = Int32.Parse(opts.Optarg); break;
                    case 'i':
                        iOpt = opts.Optarg; break;
                    case 'o':
                        oOpt = opts.Optarg; break;
                    case '?':
                    default:
                        //Console.WriteLine("Unkonwn option")
                        break;
                }
                //Console.WriteLine(String.Format("c: {0}, arg: {1}, ind {2}",(char) c, opts.Optarg, opts.Optind));
            }
            if (kOpt == -1 || nOpt == -1)
            {
                Console.WriteLine("both -k (--quorum) and -n (--count)  must bespecified");
                Environment.Exit(1);
            }

            if (kOpt > nOpt)
            {
                Console.WriteLine("k must be less than or equal to n (k<=n)");
                Environment.Exit(1);
            }

            if (iOpt == null)
            {
                Console.WriteLine("key identifier must be provided as -i (--kid) option");
                Environment.Exit(1);
            }

            if (oOpt == null)
            {
                oOpt = iOpt + ".backup";
            }

        }



        static void Main(string[] args)
        {
            Console.Clear();
            processOpts(args);

            Console.WriteLine(String.Format(@"

=== Summary ===
Will generate RSA key of size {0} identified by '{1}', 
split into {2} shares from which at least {3} need to be available
to decrypt the key backup.
The key backup will be stored to '{4}'
Press [Enter] to continue.
======

", sOpt, iOpt, nOpt, kOpt, oOpt));

            Console.ReadLine();


            var key = SharedSecretGenerator.genKey(iOpt);
            var shares = SharedSecretGenerator.generateSharedSecret(32, nOpt, kOpt);
            Console.WriteLine(String.Format(@"
Key generated, decryption shares generated.


Bring {0} share keepers one by one. 


Press [Enter] when ready.", shares.Length));
            Console.ReadLine();

            var idx = 1;
            foreach (var share in shares)
            {
                var verified = false;
                while (!verified)
                {
                    Console.Clear();
                    displayShare(idx++,share);

                    // verification
                    verified = readVerifyShare(share.shareIndex, share.shareValue, share.shareHash);
                    if (!verified)
                    {
                        Console.WriteLine(@"
                        
Share values invalid. Verify your record and try again.

Press [Enter] when ready.");
                        Console.ReadLine();
                    }
                }
            }
        }


        static void displayShare(int idx, Share share)
        {
            Console.WriteLine(String.Format(@"

Hi share keeper ___{0}____ 

The 'shareValue' consists of 16 groups 
of lowercase letters, in a form cvcvc (consonant/vovel).
The 'shareHash' consists of 2 similar groups.

Write down following information:

  * total number of shares   : {1}
  * quorum needed to decrypt : {2}
  * key identifier           : {3}
  * share index (number)     : {4}
  * share value              : {5}
  * share hash               : {6}
  * secret hash              : {7}
--------------------------------------------------------------------------
Press [Enter] when done.


", idx++, share.n, share.k, iOpt, share.shareIndex, share.shareValue, share.shareHash, share.secretHash));
                    Console.ReadLine();
        }
        
        static bool readVerifyShare(int shareIndex, string shareValue, string shareHash)
        {

            Console.Clear();
            Console.WriteLine("To verify your record, enter your share information. Confirm rvery etry with [Enter].");
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
    }
}
