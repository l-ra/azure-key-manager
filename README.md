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
* `-t|--test` - test mode - skips share verifications, no key output, no key imported into vault
* `-e|--tenant` - tenant to use for OAuth2 token request
* `-a|--appid` - appid to use for OAuth2 token request
* `-u|redirecturl` - oauth2 redirect url - defuelts to http://localhost:7373
* `-r|resource` - oauth2 request resource defaults to https://vault.azure.net
* `-v|vaulturl` - the target vault URL, defaults to https://lravault.vault.azure.net/
* `-x|skipshareverify` - skips the verification of the shares DANGEROUS for production use, share may be misstyped,


Example `KeyGeneratorCli --count=5 --quorum=3 --kid=sec.enc -v https://somevault.vault.azure.net/` will output 
the key into `sec.enc.backup` file and will generate 5 shares and import key into "somevault"

The backup need to be stored separated from shares. Each share should be 
in possession of different person. 


## KeyVault Access



## Install
```
git clone https://github.com/l-ra/azure-key-manager.git
cd azure-key-manager
dotnet build
cd KeyGeneratorCli
dotnet run -n 5 -k 3 -i sec.enc
```