using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace FileEncoder.Tests
{
	public class ErrorTest
	{
		[Fact]
		public void TestErrorCommand()
		{
			string[] args = { "-s", "folder", "-c", "encode", "-f", "filename.ext" };
			var command = CommandFactory.GetCommand(args);
			Assert.Equal(command, new ErrorCommand("Unknown source"));
		}
		[Fact]
		public void TestErrorEncodeCommand()
		{
			string arg = "C:\\proga\\text.txg";
			var command = new EncodeCommand(Source.Buffer, arg);
			Assert.Throws<Exception>(()=> command.Execute());
		}
		[Theory]
		[MemberData(nameof(EncoderErrorCommands))]
		public void TestEncoderCommandsError(string[] args)
		{
			var command = CommandFactory.GetCommand(args);
			Assert.Throws<Exception>(() => command.Execute());
		}
		public static IEnumerable<object[]> EncoderErrorCommands()
		{
			yield return new object[] { new string[] { "-s", "file", "-c", "encode" } };
			yield return new object[] { new string[] { "--comand", "encode" } };
		}
	}
}
