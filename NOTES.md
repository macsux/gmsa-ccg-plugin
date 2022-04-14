```
bin\Debug> tlbexp CcgPlugin.dll
regasm CcgPlugin.dll /tlb:CcgPlugin.tlb
az aks update -g myResourceGroup -n myAKSCluster --windows-admin-password $WINDOWS_ADMIN_PASSWORD
```



```
MANAGED_ID=$(az aks show -g playaks2 -n MyAKS --query "identityProfile.kubeletidentity.objectId" -o tsv)
az keyvault secret set --vault-name macsux-vault --name "GMSADomainUserCred" --value "ALMIREX\\gmsa1:P@ssw0rd"

```

Confirm dll is 64bit

```
dumpbin /header CCGAKVPlugin.dll
```





Azure Plugin registrations

```
PS C:\ProgramData\docker\credentialspecs> reg query "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\CCG\COMClasses" /s

HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\CCG\COMClasses\{CCC2A336-D7F3-4818-A213-272B7924213E}
    (Default)    REG_SZ

reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Classes\CLSID\{CCC2A336-D7F3-4818-A213-272B7924213E}" /s

reg query "HKEY_CLASSES_ROOT" /f "CCC2A336-D7F3-4818-A213-272B7924213E" /s 

reg query HKEY_LOCAL_MACHINE\SOFTWARE /f "{CCC2A336-D7F3-4818-A213-272B7924213E}" /s

HKEY_LOCAL_MACHINE\SOFTWARE\Classes\CLSID\{CCC2A336-D7F3-4818-A213-272B7924213E}
    AppID    REG_SZ    {557110E1-88BC-4583-8281-6AAC6F708584}
    
reg export "HKEY_LOCAL_MACHINE\SOFTWARE\Classes\AppID\{557110E1-88BC-4583-8281-6AAC6F708584}" 1.reg
reg export "HKEY_LOCAL_MACHINE\SOFTWARE\Classes\WOW6432Node\AppID\{557110E1-88BC-4583-8281-6AAC6F708584}" 2.reg
reg export "HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Classes\AppID\{557110E1-88BC-4583-8281-6AAC6F708584}" 3.reg


HKEY_LOCAL_MACHINE\SOFTWARE\Classes\CLSID\{CCC2A336-D7F3-4818-A213-272B7924213E}\InprocServer32
    (Default)    REG_SZ    C:\Windows\System32\CCGAKVPlugin.dll
    ThreadingModel    REG_SZ    Both
```

```
kubectl exec -it node-debugger-aks-nodepool1-39078785-vmss000000-669sp -- /bin/bash
ssh -l macsux 10.240.0.97 
```

```powershell
docker run --security-opt "credentialspec=file://vault.json" --hostname webapp01 -it mcr.microsoft.com/windows/servercore:ltsc2019 powershell

docker run --security-opt "credentialspec=file://ccg.json" --hostname almirex.dc -it kerberos-demo

docker run --security-opt "credentialspec=file://ccg.json" --hostname webapp01 -it mcr.microsoft.com/windows/servercore:ltsc2019 powershell

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe CcgPlugin.dll /codebase /tlb
```



