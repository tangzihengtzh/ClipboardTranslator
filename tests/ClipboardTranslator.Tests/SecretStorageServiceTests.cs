using ClipboardTranslator.Settings;

namespace ClipboardTranslator.Tests;

public class SecretStorageServiceTests
{
    [Fact]
    public void Encrypt_Decrypt_RoundTrip()
    {
        const string key = "sk-test-1234567890abcdef-abc";
        string encrypted = SecretStorageService.Encrypt(key);
        Assert.NotEqual(key, encrypted);
        Assert.Equal(key, SecretStorageService.Decrypt(encrypted));
    }

    [Fact]
    public void Encrypt_Empty_Returns_Empty()
    {
        Assert.Equal(string.Empty, SecretStorageService.Encrypt(string.Empty));
    }

    [Fact]
    public void Encrypt_Produces_NonDeterministic_Output()
    {
        const string key = "sk-abcdef";
        Assert.NotEqual(SecretStorageService.Encrypt(key), SecretStorageService.Encrypt(key));
    }

    [Fact]
    public void Decrypt_Invalid_Base64_Throws()
    {
        Assert.Throws<FormatException>(() => SecretStorageService.Decrypt("not base64 !!!"));
    }
}
