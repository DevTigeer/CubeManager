using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace CubeManager.Core.Helpers;

/// <summary>
/// Windows DPAPI 기반 자격증명 암호화/복호화.
/// CurrentUser 스코프: 암호화한 PC+사용자만 복호화 가능.
/// </summary>
[SupportedOSPlatform("windows")]
public static class CredentialHelper
{
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(plainText);
        var encrypted = ProtectedData.Protect(bytes, null,
            DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string encryptedBase64)
    {
        if (string.IsNullOrEmpty(encryptedBase64)) return string.Empty;

        try
        {
            var encrypted = Convert.FromBase64String(encryptedBase64);
            var bytes = ProtectedData.Unprotect(encrypted, null,
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException)
        {
            // 다른 PC/사용자에서 암호화된 경우 복호화 불가
            return string.Empty;
        }
        catch (FormatException)
        {
            // Base64가 아닌 평문이 저장된 경우 (마이그레이션 전 데이터)
            return encryptedBase64;
        }
    }
}
