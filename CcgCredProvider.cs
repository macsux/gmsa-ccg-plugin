using System;
using System.EnterpriseServices;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace CcgPlugin
{

    [Guid("6ECDA518-2010-4437-8BC3-46E752B7B172")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface ICcgDomainAuthCredentials
    {
        // [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
        void GetPasswordCredentials(
            [MarshalAs(UnmanagedType.LPWStr), In] string pluginInput,
            [MarshalAs(UnmanagedType.LPWStr)] out string domainName,
            [MarshalAs(UnmanagedType.LPWStr)] out string username,
            [MarshalAs(UnmanagedType.LPWStr)] out string password);
    }
    
    // [Guid("defff03c-3245-465f-8391-cc586a2d1f31")]
    [Guid("defff03c-3245-465f-8391-cc586a2d1f32")]
    // [ClassInterface(ClassInterfaceType.None)]
    [ProgId("CcgCredProvider")]
    public class CcgCredProvider : ServicedComponent, ICcgDomainAuthCredentials
    {
        public CcgCredProvider()
        {
            File.WriteAllText(@"c:\temp\ccglog2.txt", "test");
        }
        public void GetPasswordCredentials(
            [MarshalAs(UnmanagedType.LPWStr), In] string pluginInput,
            [MarshalAs(UnmanagedType.LPWStr)] out string domainName,
            [MarshalAs(UnmanagedType.LPWStr)] out string username,
            [MarshalAs(UnmanagedType.LPWStr)] out string password)
        {
            
            var temp = pluginInput.Split(':');
            password = temp[1];
            temp = temp[0].Split('@');
            username = temp[0];
            domainName = temp[1];
            var results = $@"
INPUT={pluginInput}
DOMAIN={domainName}
USERNAME={username}
PASSWORD={password}";
            File.WriteAllText(@"c:\temp\ccglog.txt", results);
        }
        
    }
}