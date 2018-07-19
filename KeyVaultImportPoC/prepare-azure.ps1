#Import-Module AzureRM
#Import-Module AzureAD

. (Join-Path $PSScriptRoot prepare-azure-config.ps1)

#display context info
Get-AzureRmSubscription
Get-AzureADCurrentSessionInfo

Read-Host -Prompt "Press [Enter] to continue in this context"

#create RG
$rg = New-AzureRmResourceGroup -Name $rgName -Location $location
echo "Resource group created $rgName"

#create keyvault
$kv = New-AzureRmKeyVault -Name $keyVaultName -ResourceGroupName $rgName -Location $location
echo "Key vault create $keyVaultName"

#prepare app reg
$azureKeyVaultSp = Get-AzureADServicePrincipal | ? { $_.DisplayName -match "Azure Key Vault"}
$permissionId =  $azureKeyVaultSp.Oauth2Permissions.Id

$requiredResourceAccess = New-Object -TypeName "Microsoft.Open.AzureAD.Model.RequiredResourceAccess"
$requiredResourceAccess.ResourceAppId = $azureKeyVaultSp.AppId

$resourceAccess = New-Object -TypeName "Microsoft.Open.AzureAD.Model.ResourceAccess"
$resourceAccess.Id = $permissionId
$resourceAccess.Type = "Scope"

$requiredResourceAccess.ResourceAccess = $resourceAccess

#create app & service principal
$app = New-AzureADApplication -DisplayName KeyApp -PublicClient $true -RequiredResourceAccess $requiredResourceAccess -ReplyUrls https://nourl
$appSp = New-AzureADServicePrincipal -appid $app.AppId

echo "app created $appName"

#grant app access to key vault
Set-AzureRmKeyVaultAccessPolicy -VaultName $keyVaultName -ResourceGroupName $rgName -ServicePrincipalName $appSp.AppId  -PermissionsToKeys import,create

echo "app granted access to vault"

$tenantId = (Get-AzureRmSubscription).tenantId
$kvDetail = Get-AzureRmKeyVault -vaultname $keyVaultName -ResourceGroupName $rgName
$vaultUrl = $kvDetail.VaultUri
$appId = $app.AppId
echo "Use following: `
tenant: $tenantId `
vaultUri: $vaultUri `
appId: $appId"
      


















# 120 Get-AzureADServicePrincipalOAuth2PermissionGrant
#  121 Get-AzureADServicePrincipal -filter "displayname eq 'keyapp'"|Get-AzureADServiceAppRoleAssignment
#  122 Get-AzureADServicePrincipal -filter "displayname eq 'keyapp'"|Get-AzureADServicePrincipalOAuth2PermissionGrant
#  123 Get-AzureADServicePrincipal -filter "displayname eq 'keyapp'"|Get-AzureADServicePrincipal
#  124 Get-AzureADServicePrincipal -filter "displayname eq 'keyapp'"|Get-AzureADServicePrincipalOAuth2PermissionGrant
#  125 $app = New-AzureADApplication
#  126 $app = New-AzureADApplication -DisplayName KeyApp -PublicClient
#  127 $app = New-AzureADApplication -DisplayName KeyApp -PublicClient true
#  128 $app = New-AzureADApplication -DisplayName KeyApp -PublicClient $true
#  129 $app
#  130 Get-AzureADServicePrincipal
#  131 Get-AzureADServicePrincipal -Filter "displayname eq 'Azure key Vault'"
#  132 $azureKeyVaultSp = Get-AzureADServicePrincipal -Filter "displayname eq 'Azure key Vault'"
#  133 get-help | New-AzureADServicePrincipal
#  134 get-help  New-AzureADServicePrincipal
#  135 $keyapp = Get-AzureADApplication -filter "displayname eq 'keyapp'"
#  136 $keyapp | New-AzureADServicePrincipal
#  137 New-AzureADServicePrincipal -appid $keyapp
#  138 New-AzureADServicePrincipal -appid $keyapp.AppId
#  139 $keyappSp = Get-AzureADServicePrincipal -filter "displayname eq 'keyapp'"
#  140 $keyappSp
#  141 $keyapp
#  142 $azureKeyVaultSp.AppRoles
#  143 $azureKeyVaultSp
#  144 $azureKeyVaultSp.AppRoles
#  145 $sshrpntSp = Get-AzureADServicePrincipal -All $true | ? { $_.DisplayName -match "Office 365 SharePoint Online" }
#  146 $sshrpntSp
#  147 Get-AzureADServicePrincipal -All $true | ? { $_.DisplayName -match "Office 365 SharePoint Online" }
#  148 Get-AzureADServicePrincipal -All $true | ? { $_.DisplayName -match "Azure Key Vault" }
#  149 $xx = Get-AzureADServicePrincipal -All $true | ? { $_.DisplayName -match "Azure Key Vault" }
#  150 $xx.AppRoles
#  151 $xx.AddIns
#  152 $xx.AlternativeNames
#  153 $xx.AppId
#  154 Get-AzureADServicePrincipal -All $true
#  155 $xx = Get-AzureADServicePrincipal -All $true | ? { $_.DisplayName -match "Windows Azure Active Directory" }
#  156 $xx
#  157 $xx.AppDisplayName
#  158 $xx.AppRoles
#  159 $xx = Get-AzureADServicePrincipal -All $true | ? { $_.DisplayName -match "Azure Key Vault" }
#  160 $reqPerms = New-Object -TypeName "Microsoft.Open.AzureAD.Model.RequiredResourceAccess"
#  161 $reqPerms.ResourceAppId=$xx.AppId
#  162 $keyapp
#  163 $keyapp.RequiredResourceAccess=$reqPerms
#  164 $keyapp | Set-AzureADApplication
#  165  $kk = Get-AzureADApplication -filter "displayname eq 'keyapp'"
#  166 $kk
#  167 $kk.RequiredResourceAccess
#  168 Get-History
#  169 $xx
#  170 $xx.AppRoleAssignmentRequired
#  171 $xx.Oauth2Permissions
#  172 get-history
#  173 $reqPerms
#  174 $perm=New-Object -TypeName "Microsoft.Open.AzureAD.Model.ResourceAccess"
#  175 $perm
#  176 $perm.Id="f53da476-18e3-4152-8e01-aec403e6edc0"
#  177 $perm.Type = "User"
#  178 $reqPerms.ResourceAccess=$perm
#  179 $reqPerms
#  180 $reqPerms.ResourceAccess
#  181 $app
#  182 $app.RequiredResourceAccess=$reqPerms
#  183 Set-AzureADApplication -apid $app.AppId -RequiredResourceAccess $reqPerms
#  184 Set-AzureADApplication -appid $app.AppId -RequiredResourceAccess $reqPerms
#  185 $app| Set-AzureADApplication -RequiredResourceAccess $reqPerms
#  186 $perm.Type = "Scope"
#  187 $reqPerms.ResourceAccess=$perm
#  188 $reqPerms
#  189 $reqPerms.ResourceAccess
#  190 $app| Set-AzureADApplication -RequiredResourceAccess $reqPerms
#  191 Get-AzureRmKeyVault
#  192 $kv=Get-AzureRmKeyVault
#  193 $kv
#  194 Get-AzureRmKeyVault
#  195 get-help Set-AzureRmKeyVault
#  196 get-help Set-AzureRmKeyVaultAccessPolicy
#  197 $keyappSp
#  198 $kv| Set-AzureRmKeyVaultAccessPolicy -ServicePrincipalName $keyappSp.Id -PermissionsToKeys import,create
#  199 $kv| Set-AzureRmKeyVaultAccessPolicy -ServicePrincipalName $keyappSp.ObjectId -PermissionsToKeys import,create
#  200 $keyappSp = Get-AzureADServicePrincipal -filter "displayname eq 'keyapp'"
#  201 $keyappSp
#  202 $kv| Set-AzureRmKeyVaultAccessPolicy -ServicePrincipalName keyapp -PermissionsToKeys import,create
#  203 Set-AzureRmKeyVaultAccessPolicy -ServicePrincipalName keyapp -PermissionsToKeys import,create
#  204 Set-AzureRmKeyVaultAccessPolicy -VaultName fanis-larag -ResourceGroupName testvault -ServicePrincipalName  4085...
#  205 $keyapp | Set-AzureADApplication -ReplyUrls https://nourl