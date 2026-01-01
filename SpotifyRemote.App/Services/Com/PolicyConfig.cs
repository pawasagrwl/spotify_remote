using System.Runtime.InteropServices;

namespace SpotifyRemote.App.Services.Com
{
    [ComImport]
    [Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")]
    internal class PolicyConfig
    {
    }

    [ComImport]
    [Guid("f8679f50-850a-41cf-9c72-430f290290c8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPolicyConfig
    {
        [PreserveSig]
        int GetMixFormat(string pszDeviceName, out IntPtr ppFormat);

        [PreserveSig]
        int GetDeviceFormat(string pszDeviceName, int bDefault, out IntPtr ppFormat);

        [PreserveSig]
        int ResetDeviceFormat(string pszDeviceName);

        [PreserveSig]
        int SetDeviceFormat(string pszDeviceName, IntPtr pEndpointFormat, IntPtr pMixFormat);

        [PreserveSig]
        int GetProcessingPeriod(string pszDeviceName, int bDefault, out long pmftDefaultPeriod, out long pmftMinimumPeriod);

        [PreserveSig]
        int SetProcessingPeriod(string pszDeviceName, long pmftPeriod);

        [PreserveSig]
        int GetShareMode(string pszDeviceName, out IntPtr pMode);

        [PreserveSig]
        int SetShareMode(string pszDeviceName, IntPtr mode);

        [PreserveSig]
        int GetPropertyValue(string pszDeviceName, IntPtr key, out IntPtr value);

        [PreserveSig]
        int SetPropertyValue(string pszDeviceName, IntPtr key, IntPtr value);

        [PreserveSig]
        int SetDefaultEndpoint(string pszDeviceName, int role);

        [PreserveSig]
        int SetEndpointVisibility(string pszDeviceName, int bVisible);
    }
}
