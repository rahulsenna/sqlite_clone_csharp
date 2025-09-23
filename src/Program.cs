using static System.Buffers.Binary.BinaryPrimitives;

// Parse arguments
var (path, command) = args.Length switch
{
  0 => throw new InvalidOperationException("Missing <database path> and <command>"),
  1 => throw new InvalidOperationException("Missing <command>"),
  _ => (args[0], args[1])
};

var databaseFile = File.OpenRead(path);

// Parse command and act accordingly
if (command == ".dbinfo")
{
  Console.Error.WriteLine("Logs from your program will appear here!");

  Console.WriteLine($"database page size: {ReadInt16(databaseFile, 16)}");
  Console.WriteLine($"number of tables: {ReadInt16(databaseFile, 103)}");
}
else
{
  throw new InvalidOperationException($"Invalid command: {command}");
}

static int ReadInt16(FileStream bufStream, int offset)
{
  Span<byte> pageSizeBytes = stackalloc byte[2];
  bufStream.Seek(offset, SeekOrigin.Begin);
  bufStream.ReadExactly(pageSizeBytes);
  return ReadUInt16BigEndian(pageSizeBytes);
}
