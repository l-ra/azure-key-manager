# Azure Key Manager

This tool aims to secure backup for an encryption key stored in the Azure Key Vault.
The encryption key usually needs a backup. The tool is not intended for signature key.

## Process
Use the command line tool KeyGeneratorCli to generate key, import it into the KeyVault 
and create secure backup. The KeyGeneratorCli tool shall be used on a secure computer preferrably 
a clean installation. .NET Core 2 is required.  