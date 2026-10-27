using System.Security.Cryptography;

namespace DocFlow.Documents;

public static class Hashing
{
    public static string Sha256Hex(byte[] data) => Convert.ToHexString(SHA256.HashData(data));
}
