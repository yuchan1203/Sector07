using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// 계정 정보를 PlayerPrefs에 저장하는 간단한 로컬 저장소.
// 비밀번호는 원문 대신 솔트 + SHA256 해시로 저장한다. (서버 없이 쓰는 학습용 수준의 보호)
public class PlayerPrefsAccountRepository
{
    private const string KeyPrefix = "Account_";
    private const int SaltSize = 16;

    private static string GetKey(string id)
    {
        return KeyPrefix + id;
    }

    public bool Exists(string id)
    {
        return PlayerPrefs.HasKey(GetKey(id));
    }

    // 계정을 새로 만든다. 이미 있는 아이디면 false.
    public bool Register(string id, string password)
    {
        if (Exists(id))
        {
            return false;
        }

        byte[] salt = new byte[SaltSize];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        string saltText = Convert.ToBase64String(salt);
        PlayerPrefs.SetString(GetKey(id), saltText + ":" + Hash(saltText, password));
        PlayerPrefs.Save();
        return true;
    }

    public bool CheckPassword(string id, string password)
    {
        if (!Exists(id))
        {
            return false;
        }

        string[] parts = PlayerPrefs.GetString(GetKey(id)).Split(':');
        if (parts.Length != 2)
        {
            return false;
        }
        return parts[1] == Hash(parts[0], password);
    }

    private static string Hash(string salt, string password)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(salt + password));
            return Convert.ToBase64String(bytes);
        }
    }
}
