using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

// Все напихано в один файл для удобства копирования программы за один присест

namespace FileEncoder
{
    /// <summary>
    /// Общий класс для всех команд
    /// </summary>
    public abstract class Command
    {
        /// <summary>
        /// Выполнить команду
        /// </summary>
        public abstract void Execute();
    }

    /// <summary>
    /// Общая команда для всех команд конвертации
    /// </summary>
    public abstract class ConvertCommand : Command
    {
        /// <summary>
        /// Путь до файла, указанный пользователем
        /// </summary>
        protected string FilePath { get; }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="filePath">Путь до файла, с которым будет работать команда</param>
        protected ConvertCommand(string filePath)
        {
            FilePath = filePath;
        }
    }

    /// <summary>
    /// Команда преобразования оригинального файла в base64 строку
    /// </summary>
    public class EncodeCommand : ConvertCommand
    {
        public EncodeCommand(string filePath) : base(filePath) { }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (!File.Exists(FilePath))
            {
                throw new Exception($"File {FilePath} not found");
            }

            string base64FilePath = FilePath + ".base64";
            string compressedBase64FilePath = base64FilePath + ".compressed";
            string b64CompressedBase64FilePath = compressedBase64FilePath + ".base64";
            
            Console.WriteLine($"Reading file {FilePath} and converting to base64 string...");
            Base64Converter.FileToBase64String(FilePath, base64FilePath);
            Console.WriteLine("Compressing...");
            Compressor.Zip(base64FilePath, compressedBase64FilePath);
            Base64Converter.FileToBase64String(compressedBase64FilePath, compressedBase64FilePath + ".base64");
            string b64 = File.ReadAllText(b64CompressedBase64FilePath);
            SaveToClipboard(b64);

            // Console.WriteLine("Converting compressed file to base64 string...");
            // string compressedBase64 = FileToBase64String(TempFileName);
            //
            // Console.WriteLine("Saving...");
            // WriteBase64String(compressedBase64);
            // Console.WriteLine("Done.");
        }

        /// <summary>
        /// Сохранить base64 строку (преобразованный оригинальный файл) в файл
        /// </summary>
        /// <param name="base64String">Преобразованный в base64 оригинальный файл</param>
        private void SaveToClipboard(string base64String)
        {
            Clipboard.SetText(base64String, TextDataFormat.Text);
        }
    }

    /// <summary>
    /// Команда преобразования base64 строки в файл
    /// </summary>
    public class DecodeCommand : ConvertCommand
    {
        public DecodeCommand(string filePath) : base(filePath) { }

        /// <inheritdoc/>
        public override void Execute()
        {
            // Console.WriteLine("Reading base64 string of compressed file...");
            // string compressedBase64String = ReadBase64StringFromClipboard();
            // Console.WriteLine("Converting to bytes...");
            // byte[] compressedFile = Convert.FromBase64String(compressedBase64String);
            // Console.WriteLine("Decompressing to base64 string...");
            // Compressor.Unzip(compressedFile);
            // Console.WriteLine("Reading uncompressed file base64 string...");
            // string base64String = FileToBase64String("");
            // Console.WriteLine("Converting to bytes...");
            // byte[] file = Convert.FromBase64String(base64String);
            //
            // Console.WriteLine("Saving file...");
            // File.WriteAllBytes(FilePath, file);
            // Console.WriteLine("Done.");
        }

        /// <summary>
        /// Считать base64 строку
        /// </summary>
        /// <returns>base64 строку - закодированный файл</returns>
        private string ReadBase64StringFromClipboard()
        {
            return Clipboard.GetText(TextDataFormat.Text);
        }
    }

    /// <summary>
    /// Фабрика создания команд
    /// </summary>
    public static class CommandFactory
    {
        /// <summary>
        /// Получить команды
        /// </summary>
        /// <param name="args">Параметры для создания команды</param>
        /// <returns>Команда</returns>
        public static Command GetCommand(string[] args)
        {
            if (args.Length == 0)
            {
                return new EmptyCommand();
            }

            string fileName;
            if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "-h":
                    case "--help":
                        return new PrintHelpCommand();
                    case "-v":
                    case "--version":
                        return new PrintVersionCommand();
                    default:
                        if (File.Exists(args[0]))
                        {
                            return new EncodeCommand(args[0]);
                        }

                        return new EmptyCommand();
                }
            }

            try
            {
                fileName = GetFileName(args);
            }
            catch (NoFilePathException)
            {
                return new ErrorCommand("File path needed");
            }
            catch (UnknownSourceException)
            {
                return new ErrorCommand("Unknown source");
            }
            catch (Exception e)
            {
                return new ErrorCommand(e.Message);
            }

            if (args.Contains("encode"))
            {
                return new EncodeCommand(fileName);
            }

            if (args.Contains("decode"))
            {
                return new DecodeCommand(fileName);
            }

            return new EncodeCommand(fileName);
        }

        /// <summary>
        /// Получить путь до фафйла
        /// </summary>
        /// <param name="args">Параметры команды</param>
        /// <returns>Путь до файла</returns>
        private static string GetFileName(string[] args)
        {
            if (args.Contains("-f") || args.Contains("--file"))
            {
                int index = Array.IndexOf(args, "-f");
                if (index == -1) index = Array.IndexOf(args, "--file");

                return args[index + 1];
            }

            return args[args.Length - 1];
        }
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            args = new[] {"datagrip-2021.1.1.exe"};

            var command = CommandFactory.GetCommand(args);
            Console.WriteLine($"Created {command.GetType().Name}.");
            command.Execute();
        }
    }

    /// <summary>
    /// Команда вывода версии программы
    /// </summary>
    public class PrintVersionCommand : Command
    {
        /// <inheritdoc/>
        public override void Execute()
        {
            Console.WriteLine($"{typeof(Program).Assembly.GetName().Version}v");
        }
    }

    /// <summary>
    /// Команда вывода помощи
    /// </summary>
    public class PrintHelpCommand : Command
    {
        /// <inheritdoc/>
        public override void Execute()
        {
            Console.WriteLine("\t-h|--help\t\tHelp information");
            Console.WriteLine("\t-v|--version -v\t\tProgram version");
            Console.WriteLine("\t-f|--file -f\t\tOutput file name");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("\tFileEncoder file -c encode -f filename.ext");
            Console.WriteLine("\tFileEncoder --command decode -file filename_ext.txt");
        }
    }

    /// <summary>
    /// Пустая команда, в которой не заданы параметры
    /// </summary>
    public class EmptyCommand : Command
    {
        /// <inheritdoc/>
        public override void Execute()
        {
            Console.WriteLine("Unknown or empty argument");
        }
    }

    /// <summary>
    /// Команда вывода сообщения об ошибке
    /// </summary>
    public class ErrorCommand : Command
    {
        /// <summary>
        /// Текст сообщения об ошибке
        /// </summary>
        private readonly string message;

        public ErrorCommand(string message)
        {
            this.message = message;
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            Console.WriteLine(message);
        }
    }

    public static class Compressor
    {
        private const string TempFileName = "temp.bin";

        // Сохраняем поток в файл, чтобы избежать OutOfMemoryException при выполнении mso.ToArray()

        public static void Zip(string base64File, string compressedFileName)
        {
            var base64String = File.ReadAllText(base64File);
            var bytes = Encoding.UTF8.GetBytes(base64String);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new FileStream(compressedFileName, FileMode.Create))
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    CopyTo(msi, gs);
                }

                mso.Close();
            }
        }

        public static void Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new FileStream(TempFileName, FileMode.Create))
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }

                mso.Close();
            }
        }

        private static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }
    }
    
    public static class Base64Converter
    {
        public static void FileToBase64String(string filePath, string outputFilePath)
        {
            using (FileStream fs = File.Open(outputFilePath, FileMode.Create))
            using (var cs = new CryptoStream(fs, new ToBase64Transform(), CryptoStreamMode.Write))
            using (var fi = File.Open(filePath, FileMode.Open))
            {
                fi.CopyTo(cs);
            }
        }

        public static void Base64ToFile(string filePath)
        {
            string outputFilePath = filePath + ".base64";
            
            using (FileStream f64 = File.Open(outputFilePath, FileMode.Open))
            using (var cs = new CryptoStream(f64, new FromBase64Transform(), CryptoStreamMode.Read))
            using (var fo = File.Open(filePath + ".orig", FileMode.Create))
            {
                cs.CopyTo(fo);
            }
        }
        
        public static string FileToBase64String222(string fileName)
        {
            byte[] packet = new byte[10 * 1024 * 1024];
            StringBuilder b64sb = new StringBuilder();
            int j = 0;
            using (FileStream fs = new FileStream(fileName, FileMode.Open))
            {
                int i = packet.Length;
                while (i == packet.Length)
                {
                    Console.Write($"\rReading {j++} of {fs.Length / packet.Length}");
                    i = fs.Read(packet, 0, packet.Length);
                    b64sb.Append(Convert.ToBase64String(packet, 0, i));
                }
            }

            return b64sb.ToString();
        }
    }

    /// <summary>
    /// Ошибка в случае, если был указан неизвестный источник для работы
    /// </summary>
    public class UnknownSourceException : Exception { }

    /// <summary>
    /// Ошибка в случае, если не был указан путь до файла
    /// </summary>
    public class NoFilePathException : Exception { }
}