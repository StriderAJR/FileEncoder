using System;
using System.Collections.Generic;
using Xunit;

namespace FileEncoder.Tests
{
	public class FileEncoderTests
	{
		[Theory]
		[MemberData(nameof(CommandData))]
		public void TestBasicCommand(string[] args, Command testCommand)
		{	
			var command = CommandFactory.GetCommand(args);
			Assert.IsType(command.GetType() ,testCommand);
		}
		public static IEnumerable<object[]> CommandData()
		{
			yield return new object[] { new string[] { }, new EmptyCommand() };
			yield return new object[] { new string[] { "--help" }, new PrintHelpCommand() };
			yield return new object[] { new string[] { "--version" }, new PrintVersionCommand() };
			yield return new object[] { new string[] { "--Get300$" }, new EmptyCommand() };
		}		
		[Theory]
		[MemberData(nameof(EncodeCommands))]
		public void TestEncodeCommands(string[] args)
		{
			var command = CommandFactory.GetCommand(args);
			Assert.IsType<EncodeCommand>(command);
		}
		public static IEnumerable<object[]> EncodeCommands()
		{
			yield return new object[] { new string[] { "C:\\proga\\text.txt" } };
			yield return new object[] { new string[] { "-s", "file", "-c", "encode", "-f", "C:\\proga\\text.txt" } };
			yield return new object[] { new string[] { "--comand", "encode", "-file", "C:\\proga\\text.txt" } };
		}
		[Theory]
		[MemberData(nameof(DecodeCommands))]
		public void TestDecodeCommands(string[] args)
		{
			var command = CommandFactory.GetCommand(args);
			Assert.IsType<DecodeCommand>(command);
		}
		public static IEnumerable<object[]> DecodeCommands()
		{
			yield return new object[] { new string[] { "--source", "buffer", "--command", "decode", "-file", "filename_ext.txt" } };
			yield return new object[] { new string[] { "-s", "file", "-c", "decode", "-f", "C:\\proga\\text.txt" } };
			yield return new object[] { new string[] { "--comand", "decode", "-file", "C:\\proga\\text.txt" } };
		}
	}
}
