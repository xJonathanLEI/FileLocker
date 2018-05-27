using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace FileLocker
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("You must specify two parameters");
                return;
            }

            bool unlock;
            if (args[0] == "lock")
                unlock = false;
            else if (args[0] == "unlock")
                unlock = true;
            else
            {
                Console.WriteLine("Invalid operation");
                return;
            }

            string fileName = args[1];
            if (!File.Exists(fileName))
            {
                Console.WriteLine($"File {fileName} does not exist");
                return;
            }

            byte[] fileContent = File.ReadAllBytes(fileName);

            Console.Write("Enter password: ");
            string password = ConsoleReadPassword();

            if (!unlock)
            {
                Console.Write("Repeat password: ");
                string repeatPwd = ConsoleReadPassword();
                if (password != repeatPwd)
                {
                    Console.WriteLine("Passwords do not match");
                    return;
                }
            }

            byte[] key;
            byte[] iv = new byte[16];
            using (var sha256 = SHA256.Create())
            {
                key = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                Array.Copy(sha256.ComputeHash(Encoding.UTF8.GetBytes("File Locker")), 0, iv, 0, iv.Length);
            }

            string backupName = fileName + ".backup";
            if (File.Exists(backupName))
                File.Delete(backupName);

            File.Move(fileName, backupName);

            try
            {
                using (var aes = Aes.Create())
                using (var transformer = unlock ? aes.CreateDecryptor(key, iv) : aes.CreateEncryptor(key, iv))
                using (var fs = File.Create(fileName))
                using (var c = new CryptoStream(fs, transformer, CryptoStreamMode.Write))
                    c.Write(fileContent, 0, fileContent.Length);
                File.Delete(backupName);
                Console.WriteLine("Done");
            }
            catch
            {
                Console.WriteLine("Failed. Recovering...");
                if (File.Exists(fileName))
                    File.Delete(fileName);
                File.Move(backupName, fileName);
                Console.WriteLine("Recovered");
            }
        }

        private static string ConsoleReadPassword()
        {
            string pass = "";
            while (true)
            {
                var key = Console.ReadKey();
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("\b*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace)
                    {
                        if (pass.Length > 0)
                        {
                            pass = pass.Substring(0, (pass.Length - 1));
                            Console.Write(" \b");
                        }
                        else
                        {
                            Console.Write(" ");
                        }
                    }
                    else if (key.Key == ConsoleKey.Enter)
                    {
                        Console.Write("\r\n");
                        break;
                    }
                }
            }
            return pass;
        }

    }
}
