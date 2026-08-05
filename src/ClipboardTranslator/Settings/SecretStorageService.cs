using System.Runtime.InteropServices;
using System.Text;

namespace ClipboardTranslator.Settings;

/// <summary>
/// 使用 Windows DPAPI（CryptProtectData）加密/解密敏感信息。
/// 加密数据与当前 Windows 用户和本机绑定，即使文件整体泄漏，其他用户/机器无法解密。
/// 不引入第三方包，直接 P/Invoke crypt32.dll。
/// </summary>
public static class SecretStorageService
{
    private const uint CRYPTPROTECT_UI_FORBIDDEN = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int cbData;
        public IntPtr pbData;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(
        ref DataBlob pDataIn,
        string szDataDescr,
        IntPtr pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        uint dwFlags,
        out DataBlob pDataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptUnprotectData(
        ref DataBlob pDataIn,
        IntPtr ppszDataDescr,
        IntPtr pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        uint dwFlags,
        out DataBlob pDataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] encryptedBytes = Protect(plainBytes, uiForbidden: true);
        return Convert.ToBase64String(encryptedBytes);
    }

    public static string Decrypt(string base64Text)
    {
        if (string.IsNullOrWhiteSpace(base64Text))
            return string.Empty;

        byte[] encryptedBytes = Convert.FromBase64String(base64Text);
        byte[] plainBytes = Unprotect(encryptedBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }

    private static byte[] Protect(byte[] data, bool uiForbidden)
    {
        DataBlob input = ToBlob(data);
        DataBlob output = default;

        try
        {
            uint flags = uiForbidden ? CRYPTPROTECT_UI_FORBIDDEN : 0;
            bool ok = CryptProtectData(ref input, string.Empty, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, flags, out output);
            if (!ok)
                throw new InvalidOperationException($"DPAPI 加密失败，错误码 {Marshal.GetLastWin32Error()}。");
            return FromBlob(ref output);
        }
        finally
        {
            FreeBlob(ref input);
            FreeBlob(ref output);
        }
    }

    private static byte[] Unprotect(byte[] data)
    {
        DataBlob input = ToBlob(data);
        DataBlob output = default;

        try
        {
            bool ok = CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, CRYPTPROTECT_UI_FORBIDDEN, out output);
            if (!ok)
                throw new InvalidOperationException($"DPAPI 解密失败，错误码 {Marshal.GetLastWin32Error()}。");
            return FromBlob(ref output);
        }
        finally
        {
            FreeBlob(ref input);
            FreeBlob(ref output);
        }
    }

    private static DataBlob ToBlob(byte[] bytes)
    {
        IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        return new DataBlob { cbData = bytes.Length, pbData = ptr };
    }

    private static byte[] FromBlob(ref DataBlob blob)
    {
        if (blob.pbData == IntPtr.Zero || blob.cbData <= 0)
            return Array.Empty<byte>();
        var bytes = new byte[blob.cbData];
        Marshal.Copy(blob.pbData, bytes, 0, blob.cbData);
        return bytes;
    }

    private static void FreeBlob(ref DataBlob blob)
    {
        if (blob.pbData != IntPtr.Zero)
        {
            LocalFree(blob.pbData);
            blob.pbData = IntPtr.Zero;
            blob.cbData = 0;
        }
    }
}
