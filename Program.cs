using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;

// Все напихано в один файл для удобства копирования программы за один присест

namespace FileEncoder
{
    /// <summary>
    /// Источник получения данных
    /// </summary>
    public enum Source
    {
        /// <summary>
        /// Из файла
        /// </summary>
        File,
        /// <summary>
        /// Из буфера обмена
        /// </summary>
        Buffer
    }
    
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
        /// Источник данных
        /// </summary>
        protected Source Source { get; }
        /// <summary>
        /// Путь до файла, указанный пользователем
        /// </summary>
        protected string FilePath { get; }
        /// <summary>
        /// Путь до оригинального файла 
        /// </summary>
        protected string BinaryFilePath;
        /// <summary>
        /// Путь до файла, в котором хранится base64 строка, полученная из оригинального файла
        /// </summary>
        protected string Base64FilePath;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="source">Источник данных</param>
        /// <param name="filePath">Путь до файла, с которым будет работать команда</param>
        protected ConvertCommand(Source source, string filePath)
        {
            Source = source;
            FilePath = filePath;
        }
    }

    /// <summary>
    /// Команда преобразования оригинального файла в base64 строку
    /// </summary>
    public class EncodeCommand : ConvertCommand
    {
        public EncodeCommand(Source source, string filePath) : base(source, filePath) { }

        /// <inheritdoc/>
        public override void Execute()
        {
            Base64FilePath = GetBase64FilePath();
            BinaryFilePath = FilePath;

            if (!File.Exists(BinaryFilePath))
            {
                throw new Exception($"File {BinaryFilePath} not found");
            }
            
            Console.WriteLine($"Reading file {BinaryFilePath}...");
            Byte[] bytes = File.ReadAllBytes(BinaryFilePath);
            Console.WriteLine("Converting to base64 string...");
            string base64String = Convert.ToBase64String(bytes);
            Console.WriteLine("Compressing...");
            byte[] compressedFile = Compressor.Zip(base64String);
            Console.WriteLine("Converting to base64 string compressed file...");
            string compressedBase64 = Convert.ToBase64String(compressedFile);
            
            Console.WriteLine("Saving...");
            WriteBase64String(compressedBase64);
            Console.WriteLine("Done.");
        }

        /// <summary>
        /// Получить путь до файла, который будет хранить base64 строку
        /// </summary>
        /// <param name="filePath">Путь до оригинального файла</param>
        /// <returns>Путь до файла с base64 строкой</returns>
        private string GetBase64FilePath()
        {
            string outputFileName = Path.GetFileName(FilePath).Replace(".", "_") + ".txt";
            return Path.Combine(new[] {Path.GetPathRoot(FilePath), outputFileName});
        }

        /// <summary>
        /// Сохранить base64 строку (преобразованный оригинальный файл) в файл
        /// </summary>
        /// <param name="source">Куда сохранить base64 строку</param>
        /// <param name="base64String">Преобразованный в base64 оригинальный файл</param>
        private void WriteBase64String(string base64String)
        {
            if (Source == Source.Buffer)
                Clipboard.SetText(base64String, TextDataFormat.Text);
            else
                File.WriteAllText(Base64FilePath, base64String);
        }

        private string ConvertToBase64String(byte[] bytes)
        {
            byte[] Packet = new byte[4096];
            string b64str = "";
            using (FileStream fs = new FileStream(file, FileMode.Open))
            {
                int i = Packet.Length;
                while (i == Packet.Length)
                {
                    i = fs.Read(Packet, 0, Packet.Length);
                    b64str = Convert.ToBase64String(Packet, 0, i);
                }
            }
        }
    }

    /// <summary>
    /// Команда преобразования base64 строки в файл
    /// </summary>
    public class DecodeCommand : ConvertCommand
    {
        public DecodeCommand(Source source, string filePath) : base(source, filePath) { }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (Source == Source.File)
            {
                BinaryFilePath = GetBinaryFilePath();
                Base64FilePath = FilePath;
                
                if (!File.Exists(Base64FilePath))
                {
                    throw new Exception($"File {Base64FilePath} not found");
                }
            }
            else
            {
                BinaryFilePath = FilePath;
            }

            Console.WriteLine("Reading base64 string of compressed file...");
            string compressedBase64String = ReadBase64String();
            Console.WriteLine("Converting to bytes...");
            byte[] compressedFile = Convert.FromBase64String(compressedBase64String);
            Console.WriteLine("Decompressing to base64 string...");
            string base64String = Compressor.Unzip(compressedFile);
            Console.WriteLine("Converting to bytes...");
            byte[] file = Convert.FromBase64String(base64String);
            
            Console.WriteLine("Saving file...");
            File.WriteAllBytes(BinaryFilePath, file);
            Console.WriteLine("Done.");
        }

        /// <summary>
        /// Получить путь, куда сохранить оригинальный файл после преобразования
        /// </summary>
        /// <param name="filePath">Путь до файла с base64 строкой</param>
        /// <returns>Путь до будущего ориггинального файла</returns>
        private string GetBinaryFilePath()
        {
            string fileName = Path.GetFileNameWithoutExtension(BinaryFilePath).Replace("_", ".");
            return Path.Combine(new[] {Path.GetPathRoot(fileName), fileName});
        }

        /// <summary>
        /// Считать base64 строку
        /// </summary>
        /// <param name="source">Откуда считать base64 строку</param>
        /// <returns>base64 строку - закодированный файл</returns>
        private string ReadBase64String()
        {
            return Source == Source.Buffer 
                ? Clipboard.GetText(TextDataFormat.Text) 
                : File.ReadAllText(Base64FilePath);
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

            Source source = Source.Buffer;
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
                            return new EncodeCommand(Source.Buffer, args[0]);
                        }

                        return new EmptyCommand();
                }
            }

            try
            {
                source = GetSource(args);
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
                return new EncodeCommand(source, fileName);
            }

            if (args.Contains("decode"))
            {
                return new DecodeCommand(source, fileName);
            }

            return new EncodeCommand(source, fileName);
        }

        /// <summary>
        /// Получить источник работы
        /// </summary>
        /// <param name="args">Параметры команды</param>
        /// <returns>Источник, откуда команда должна считывать данные</returns>
        private static Source GetSource(string[] args)
        {
            if (args.Contains("-s") || args.Contains("--source"))
            {
                int index = Array.IndexOf(args, "-s");
                if (index == -1) index = Array.IndexOf(args, "--source");

                string str = args[index + 1];
                if (str != "buffer" && str != "file")
                {
                    throw new UnknownSourceException();
                }

                return str == "buffer" ? Source.Buffer : Source.File;
            }

            return Source.Buffer;
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
            Console.WriteLine("\t-s|--source -s\t\tsource of input: buffer or file");
            Console.WriteLine("\t-f|--file -f\t\tOutput file name");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("\tFileEncoder -s file -c encode -f filename.ext");
            Console.WriteLine("\tFileEncoder --source buffer --command decode -file filename_ext.txt");
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
        
        public static byte[] Zip(string str) {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new FileStream(TempFileName, FileMode.Create)) {
                using (var gs = new GZipStream(mso, CompressionMode.Compress)) {
                    CopyTo(msi, gs);
                }

                mso.Close();
            }

            byte[] result = File.ReadAllBytes(TempFileName);
            File.Delete(TempFileName);
            return result;
        }

        public static string Unzip(byte[] bytes) {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new FileStream(TempFileName, FileMode.Create)) {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress)) {
                    CopyTo(gs, mso);
                }

                mso.Close();
            }
            
            byte[] result = File.ReadAllBytes(TempFileName);
            File.Delete(TempFileName);
            
            return Encoding.UTF8.GetString(result);
        }
        
        private static void CopyTo(Stream src, Stream dest) {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0) {
                dest.Write(bytes, 0, cnt);
            }
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