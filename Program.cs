using System;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// Получить имя файла.
        /// </summary>
        /// <returns></returns>
        protected string GetFileName()
        {
            return Path.GetFileName(FilePath);
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
            Console.WriteLine("Converting to base64...");
            string base64String = Convert.ToBase64String(bytes);
            Console.WriteLine("Saving...");
            WriteBase64String(GetFileName(), base64String);
            Console.WriteLine("Done.");
        }

        /// <summary>
        /// Получить путь до файла, который будет хранить base64 строку
        /// </summary>
        /// <returns>Путь до файла с base64 строкой</returns>
        private string GetBase64FilePath()
        {
            string outputFileName = Path.GetFileName(FilePath).Replace(".", "_") + ".txt";
            return Path.Combine(new[] { Path.GetPathRoot(FilePath), outputFileName });
        }

        /// <summary>
        /// Сохранить base64 строку (преобразованный оригинальный файл) в файл
        /// </summary>
        /// <param name="base64String">Преобразованный в base64 оригинальный файл</param>
        private void WriteBase64String(string fileName, string base64String)
        {
            string str = $"{fileName};{base64String}";

            if (Source == Source.Buffer)
                Clipboard.SetText(str, TextDataFormat.Text);
            else
                File.WriteAllText(Base64FilePath, base64String);
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

            Console.WriteLine("Reading base64 string...");
            string base64String = ReadBase64String(out string fileName);

            if (string.IsNullOrEmpty(base64String))
            {
                throw new Exception("Base64 string read is empty");
            }

            Console.WriteLine("Converting to file...");
            byte[] file = Convert.FromBase64String(base64String);
            Console.WriteLine("Saving file...");
            File.WriteAllBytes(fileName == null ? BinaryFilePath : fileName, file);
            Console.WriteLine("Done.");
        }

        /// <summary>
        /// Получить путь, куда сохранить оригинальный файл после преобразования
        /// </summary>
        /// <returns>Путь до будущего ориггинального файла</returns>
        private string GetBinaryFilePath()
        {
            string fileName = Path.GetFileNameWithoutExtension(BinaryFilePath).Replace("_", ".");
            return Path.Combine(new[] { Path.GetPathRoot(fileName), fileName });
        }

        /// <summary>
        /// Считать base64 строку
        /// </summary>
        /// <returns>base64 строку - закодированный файл</returns>
        private string ReadBase64String(out string fileName)
        {
            fileName = null;
            if (Source == Source.Buffer)
            {
                string str = Clipboard.GetText(TextDataFormat.Text);
                string[] parts = str.Split(new char[] { ';' });
                fileName = parts[0];
                string base64String = parts[1];
                return base64String;
            }
            else
            {
                return File.ReadAllText(Base64FilePath);
            }
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
            var command = CommandFactory.GetCommand(args);

            Console.WriteLine($"Created {command.GetType().Name}.");
            command.Execute();
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