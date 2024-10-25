using System.Runtime.InteropServices;

namespace YoshiMoshi.LabelConverter;

internal static class Interop
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    public static bool CreateFileHardLink(string linkFileName, string existingFileName)
    {
        return CreateHardLink(linkFileName, existingFileName, IntPtr.Zero);
    }
}
