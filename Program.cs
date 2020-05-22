using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FileEncoder
{
    public enum Command
    {
        Encode, Decode
    }

    public enum Source
    {
        FromFile, FromBuffer
    }

    // FileEncoder -h|--help
    // FileEncoder -v|--version
    // FileEncoder -s file -c encode -f filename.ext
    // FileEncoder --source buffer --command decode -file filename_ext.txt
    public class FileEncoder
    {
        private readonly string binaryFilePath;
        private readonly string base64FilePath;
        private readonly Command command;
        private readonly Source source;

        public FileEncoder(Command command, Source source, string filePath)
        {
            if (command == Command.Decode && source == Source.FromFile && !File.Exists(filePath))
            {
                Console.WriteLine("Specified file doesn't exist! Change source to buffer or enter valid file name.");
                Environment.Exit(0);
            }

            if (command == Command.Encode)
            {
                base64FilePath = GetBase64FilePath(filePath);
                binaryFilePath = filePath;
            }
            else
            {
                if (source == Source.FromFile)
                {
                    binaryFilePath = GetBinaryFilePath(filePath);
                    base64FilePath = filePath;
                }
                else
                {
                    binaryFilePath = filePath;
                }
            }

            this.command = command;
            this.source = source;
        }

        private string GetBase64FilePath(string filePath)
        {
            string outputFileName = Path.GetFileName(filePath).Replace(".", "_") + ".txt";
            return Path.Combine(new[] { Path.GetPathRoot(filePath), outputFileName });
        }

        private string GetBinaryFilePath(string filePath)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath).Replace("_", ".");
            return Path.Combine(new[] { Path.GetPathRoot(fileName), fileName });
        }

        private string ReadBase64String(Source source)
        {
            if (source == Source.FromBuffer)
                return Clipboard.GetText(TextDataFormat.Text);
            return File.ReadAllText(base64FilePath);
        }

        private void WriteBase64String(Source source, string base64String)
        {
            if (source == Source.FromBuffer)
                Clipboard.SetText(base64String, TextDataFormat.Text);
            else
                File.WriteAllText(base64FilePath, base64String);
        }

        public void Execute()
        {
            if (command == Command.Decode)
            {
                string base64String = ReadBase64String(source);
                byte[] file = Convert.FromBase64String(base64String);
                File.WriteAllBytes(binaryFilePath, file);
            }
            else // encode
            {
                Byte[] bytes = File.ReadAllBytes(binaryFilePath);
                string base64String = Convert.ToBase64String(bytes);
                WriteBase64String(source, base64String);
            }
        }
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("FileEncoder needs params! Call -h or --help for more info.");
                return;
            }

            if (args.Length == 1)
            {
                if (args[0] == "-h" || args[0] == "--help")
                {
                    Console.WriteLine("\t-h|--help\t\tHelp information");
                    Console.WriteLine("\t-v|--version\t\tProgram version");
                    Console.WriteLine("\t-s|--source\t\tsource of input: buffer or file");
                    Console.WriteLine("\t-f|--file\t\tOutput file name");
                    Console.WriteLine("\t-c|--command\t\tCommand source execute: encode or decode");
                    Console.WriteLine("\nExamples:");
                    Console.WriteLine("\tFileEncoder -s file -c encode -f filename.ext");
                    Console.WriteLine("\tFileEncoder --source buffer --command decode -file filename_ext.txt");
                    return;
                }
                else if (args[0] == "-v" || args[0] == "--version")
                {
                    Console.WriteLine($"{typeof(Program).Assembly.GetName().Version}v");
                    return;
                }
                else
                {
                    Console.WriteLine("Unknown argument! Call -h or --help for more info.");
                    return;
                }
            }

            Command cmd;
            Source source = Source.FromBuffer;
            string fileName;

            // get command
            if (args.Contains("-c") || args.Contains("--command"))
            {
                int index = Array.IndexOf(args, "-c");
                if(index == -1) index = Array.IndexOf(args, "--command");

                string str = args[index + 1];
                if (str != "decode" && str != "encode")
                {
                    Console.WriteLine("Unknown command! Call -h or --help for more info.");
                    return;
                }

                cmd = str == "encode" ? Command.Encode : Command.Decode;
            }
            else
            {
                Console.WriteLine("You need source specify the command! Call -h or --help for more info.");
                return;
            }

            // get source
            if (args.Contains("-s") || args.Contains("--source"))
            {
                int index = Array.IndexOf(args, "-s");
                if(index == -1) index = Array.IndexOf(args, "--source");

                string str = args[index + 1];
                if (str != "buffer" && str != "file")
                {
                    Console.WriteLine("Unknown source! Call -h or --help for more info.");
                    return;
                }

                source = str == "buffer" ? Source.FromBuffer : Source.FromFile;
            }

            //get file name
            if (args.Contains("-f") || args.Contains("--file"))
            {
                int index = Array.IndexOf(args, "-f");
                if(index == -1) index = Array.IndexOf(args, "--file");

                fileName = args[index + 1];
            }
            else
            {
                Console.WriteLine("You need source specify the file source write source or source read from! Call -h or --help for more info.");
                return;
            }

            new FileEncoder(cmd, source, fileName).Execute();
        }
    }
}
