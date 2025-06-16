using System;
using System.IO;
using System.Text.Json;


namespace Minify
{
    internal class TokenStorage
    {
        // Define a subfolder and filename in AppData
        private static readonly string appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Minify"
        );

        private static readonly string tokenFilePath = Path.Combine(appDataFolder, "token.json");

        // Save token to file in AppData
        public static void SaveToken(TokenInfo token)
        {
            try
            {
                if (!Directory.Exists(appDataFolder))
                {
                    Directory.CreateDirectory(appDataFolder); // make if not exist
                }
                var json = JsonSerializer.Serialize(token);   // make json
                File.WriteAllText(tokenFilePath, json);       // write
            }
            catch ( Exception ex )
            {
                System.Diagnostics.Debug.WriteLine("Error saving token: " + ex.Message);
            }
        }

        public static TokenInfo? LoadToken()
        {
            try
            {
                if (!File.Exists(tokenFilePath))
                {
                    return null; // Return null if the file does not exist
                }
                var json = File.ReadAllText(tokenFilePath); // Read the file
                return JsonSerializer.Deserialize<TokenInfo>(json); // Deserialize the JSON to TokenInfo object
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading token: " + ex.Message);
                return null; // Return null if there's an error
            }
        }
    }
}
