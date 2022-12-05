# IWA in Containers without domain joining

This is a proof of concept of Container Credential Guard plugin that allows launching containers for IWA (Kerberos) on non-domain joined machines.



## How it works

Docker runtime on Windows allow containers to be launched under gMSA account. You need a special user account that has been granted access to retrieve password for gMSA account (lets call it `gmsa.plugin`). A plugin is registered on the host, which provides docker runtime with credentials to `gmsa.plugin`. A special json launch file (CredentialsSpec) is used to instruct docker runtime of the domain controller addresses, the gMSA account to assign to the container as the identity, the plugin to be used. When container is lauched, an optional parameter can be passed into `docker run` to instruct it to use credential spec json file in order to authenticate under gMSA and set it as container identity.

### Reference info: 

https://docs.microsoft.com/en-us/virtualization/windowscontainers/manage-containers/manage-serviceaccounts

https://docs.microsoft.com/en-us/windows/win32/api/ccgplugins/nf-ccgplugins-iccgdomainauthcredentials-getpasswordcredentials



## How to use

1. Compile the plugin (Requires windows machine with .NET 6 SDK installed)
   ```
   dotnet build
   ```

2. Register the plugin on the non-domain joined host

   1. From the resulting DLL directory (`bin\Debug`), run the following

   ```
   C:\Windows\Microsoft.NET\Framework64\v4.0.30319\regsvcs /fc CcgPlugin.dll
   ```

   2. Add the following registry key (You may need to change permissions first before being able to modify `HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\CCG` registry path)

   ```
   Windows Registry Editor Version 5.00
   
   [HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\CCG\COMClasses\{DEFFF03C-3245-465F-8391-CC586A2D1F32}]
   ```

3. Perform steps for **non domain-joined hosts** in [this article](https://docs.microsoft.com/en-us/virtualization/windowscontainers/manage-containers/manage-serviceaccounts) to setup gMSA account, gMSA plugin account, and create credentials spec.

   1. You should run `New-CredentialSpec` powershell commandlet on domain joined machine to ensure correct values are generated. This commandlet requires that you have an existing directory `C:\ProgramData\Docker\CredentialSpecs`.

4. Copy the resulting `credspec.json` to the non domain joined host's `C:\ProgramData\Docker\CredentialSpecs` directory

5. Edit `credspec.json` and MERGE in the following values (under existing `ActiveDirectoryConfig` section)

   ```
   {
       "ActiveDirectoryConfig": {
           "HostAccountConfig": {
             "PortableCcgVersion": "1",
             "PluginGUID": "{DEFFF03C-3245-465F-8391-CC586A2D1F32}",
             "PluginInput": "gmsa.plugin@mydomain.com:P@ssw0rd"
           }
       }
   }
   ```

   Change `PluginInput` to use account you created in step 3 that is allowed to retrieve gMSA credentials.

6. Change COM-component settings to run as NETWORK SERVICE LOCAL SYSTEM.
   Run this:
   ```
   $comAdmin = New-Object -comobject COMAdmin.COMAdminCatalog
   $apps = $comAdmin.GetCollection("Applications")
   $apps.Populate()
   $app = $apps | Where-Object {$_.Name -eq "CcgPlugin"}
   $app.Value("Identity") = "NT AUTHORITY\NetworkService"
   $apps.SaveChanges()
   ```

7. Ensure that the domain controller is reachable on the following ports

   | Protocol and port | Purpose  |
   | :---------------- | :------- |
   | TCP and UDP 53    | DNS      |
   | TCP and UDP 88    | Kerberos |
   | TCP 139           | NetLogon |
   | TCP and UDP 389   | LDAP     |
   | TCP 636           | LDAP SSL |

8. Launch container by passing in name of credential spec json as parameter like this (only filename - not path)

   ```
   docker run --security-opt "credentialspec=file://credspec.json" -it mcr.microsoft.com/windows/servercore:ltsc2019 powershell
   ```

9. Verify that Kerberos ticket can be obtained by invoking. 

   ```
   klist get <MY_GMSA_ACCOUNT>
   ```



### Current problem - SOLVED (below info for historic reference): 

Cannot get CCM to activate COM plugin component

### What has been tried:

1. Implemented COM interface as C# project

2. Compile in x64 mode (Debug config)

3. Register to COM from `bin\Debug` folder via `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\RegAsm.exe CcgPlugin.dll /tlb /codebase`

4. Add COM registration to CCG

   ```
   Windows Registry Editor Version 5.00
   
   [HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\CCG\COMClasses\{DEFFF03C-3245-465F-8391-CC586A2D1F32}]
   ```

   

5. Place `ccg.json` into `C:\ProgramData\Docker\credentialspecs`

6. Try to start container with `docker run --security-opt "credentialspec=file://ccg.json" --hostname webapp01 -it mcr.microsoft.com/windows/servercore:ltsc2019 powershell`

### Expected result:

1. Some kinda logs created in `c:\temp` (first on component activation, and method invocation)

### Actual result:

1. No logs in `c:\temp`

## Additional info

### Validate COM registration

COM registration seems to be successful, as it can be activated like this:

```c#
 var otherType = Type.GetTypeFromCLSID(new Guid("DEFFF03C-3245-465F-8391-CC586A2D1F32"));
var otherInstance = Activator.CreateInstance(otherType).Dump();
object[] args = new object[] { "test","","","" };
ParameterModifier pMod = new ParameterModifier(4);
pMod[1] = true;
pMod[2] = true;
pMod[3] = true;
ParameterModifier[] mods = { pMod };

otherType.InvokeMember("GetPasswordCredentials", BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public , null, otherInstance, args, mods, null, null);

```

### Confirm correct COM signature

1. Windows SDK installed to get original `IDL` for `ccgplugins`. Necessary files are in `C:\Program Files (x86)\Windows Kits\10\Include\10.0.20348.0\um`

2. IDL converted to `TLB` as following:

   ```
   midl ccgplugins-lib.idl /tlb ccgplugins.tlb
   
   ```

3. `TLB` converted to `.NET` assembly DLL via `tlbimp ccgplugins.tlb /out:ccgplugins-net.dll`

   ```
   tlbimp ccgplugins.tlb /out:ccgplugins-net.dll
   ```

4. Reverse decompilation of the resulting DLL via Jetbrains DotPeek confirms that the correct signature for the interface as:

   ```c#
   namespace ccgplugins\u002Dnet
   {
     [Guid("6ECDA518-2010-4437-8BC3-46E752B7B172")]
     [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
     [ComImport]
     public interface ICcgDomainAuthCredentials
     {
       [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
       void GetPasswordCredentials(
         [MarshalAs(UnmanagedType.LPWStr), In] string pluginInput,
         [MarshalAs(UnmanagedType.LPWStr)] out string domainName,
         [MarshalAs(UnmanagedType.LPWStr)] out string username,
         [MarshalAs(UnmanagedType.LPWStr)] out string password);
     }
   }
   
   ```

   

### AKS plugin

It was confirmed that AKS plugin that implements the above interface does work and is able to be spun up and start container with GMSA creds as per this article. https://docs.microsoft.com/en-us/azure/aks/use-group-managed-service-accounts

### Other observations

Given that COM plugin has constructor logic that writes a marker file to `c:\temp`, the lack of said file when attempting to start container with `--security-opt` would imply that COM method signature conforming to an interface is not a problem, as COM object would be instantiated before the method call. Since it's not being created, seems like CCG subsystem is not picking up the plugin registration properly. 

The experiment was repeated by copying the DLL to an AKS node where Azure's Vault CCG plugin is installed, registering it and adding the necessary CCG registry entries. No COM activation was observed in that environment either.

## Primary test Environment

```
PS C:\projects> [Environment]::OSVersion

Platform ServicePack Version      VersionString
-------- ----------- -------      -------------
 Win32NT             10.0.19043.0 Microsoft Windows NT 10.0.19043.0


PS C:\projects> docker version
Client:
 Cloud integration: v1.0.20
 Version:           20.10.10
 API version:       1.41
 Go version:        go1.16.9
 Git commit:        b485636
 Built:             Mon Oct 25 07:47:53 2021
 OS/Arch:           windows/amd64
 Context:           default
 Experimental:      true

Server: Docker Engine - Community
 Engine:
  Version:          20.10.10
  API version:      1.41 (minimum version 1.24)
  Go version:       go1.16.9
  Git commit:       e2f740d
  Built:            Mon Oct 25 07:43:13 2021
  OS/Arch:          windows/amd64
  Experimental:     false
PS C:\projects>
```

