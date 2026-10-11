using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SellFlowApi.Interfaces;

namespace SellFlowApi.Implementation
{
    
// AES-256-GCM. Key = 32 random bytes, base64, in config "Encryption:Key". Never log tokens.
public class AesTokenEncryptor(IConfiguration cfg) : ITokenEncryptor
{
    private byte[] Key => Convert.FromBase64String(cfg["Encryption:Key"]
        ?? throw new InvalidOperationException("Encryption:Key is not configured."));

    public string Encrypt(string plainText)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var data = Encoding.UTF8.GetBytes(plainText);
        var cipher = new byte[data.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(Key, 16);
        aes.Encrypt(nonce, data, cipher, tag);
        return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }

    public string Decrypt(string cipherText)
    {
        var all = Convert.FromBase64String(cipherText);
        var nonce = all[..12]; var tag = all[12..28]; var cipher = all[28..];
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(Key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
}