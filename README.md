# Azure Key Manager

This tool aims to secure backup for an encryption key stored in the Azure Key Vault.
The encryption key usually needs a backup. The tool is not intended for signature key.

## Process
Use the command line tool KeyGeneratorCli to generate key, import it into the KeyVault 
and create secure backup. The KeyGeneratorCli tool shall be used on a secure computer preferrably 
a clean installation. .NET Core 2 is required.  

After the tool is installed just run it with folowing parameters:
* `-n|--count` - total number of shares
* `-k|--quorum`	- number of shares needed to decrypt the key backup
* `-i|--kid` - key identifier
* `-s|--size` - RSA key size (not implemented, defaults to 2048)
* `-o|--output` - name of output file. Defaults to `kid.backup`

Example `KeyGeneratorCli --count=5 --quorum=3 --kid=sec.enc` will output 
the key into `sec.enc.backup` file and will generate 5 shares.

## Install
```
git clone https://github.com/l-ra/azure-key-manager.git
cd azure-key-manager
dotnet build
cd KeyGeneratorCli
dotnet run -n 5 -k 3 -i sec.enc
```