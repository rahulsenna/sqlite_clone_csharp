using System.Text;
using static System.Buffers.Binary.BinaryPrimitives;


const byte INTERIOR_INDEX = 0x02;
const byte INTERIOR_TABLE = 0x05;
const byte LEAF_INDEX = 0x0a;
const byte LEAF_TABLE = 0x0D;

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
else if (command == ".tables")
{
  var tables = GetTables(databaseFile);
  foreach (var table in tables)
  {
    Console.WriteLine(table.tblName);
  }
}
else if (command.StartsWith("select count(*) from"))
{
  string tableName = command[(command.LastIndexOf(' ') + 1)..];
  TABLE table = GetTables(databaseFile).First(t => t.tblName == tableName);

  int tablePageNumber = table.rootpage - 1;
  int pageSize = ReadInt16(databaseFile, 16);
  int tableOffset = pageSize * tablePageNumber;

  databaseFile.Seek(tableOffset, SeekOrigin.Begin);
  byte pageType = (byte)databaseFile.ReadByte();

  if (pageType == LEAF_TABLE)
  {
    int cell_count = ReadInt16(databaseFile, tableOffset + 3);
    Console.WriteLine(cell_count);
  }
}
else
{
  throw new InvalidOperationException($"Invalid command: {command}");
}

static int ReadInt16(FileStream bufStream, int offset)
{
  Span<byte> buffer = stackalloc byte[2];
  bufStream.Seek(offset, SeekOrigin.Begin);
  bufStream.ReadExactly(buffer);
  return ReadUInt16BigEndian(buffer);
}

static int ReadVarInt(FileStream bufStream, ref int offset)
{
  Span<byte> buffer = stackalloc byte[9];
  bufStream.Seek(offset, SeekOrigin.Begin);
  bufStream.ReadExactly(buffer);

  int value = 0;
  for (int i = 0; i < 8; i++)
  {
    offset++;
    value = (value << 7) | (buffer[i] & 0x7F);
    if ((buffer[i] & 0x80) == 0)
      return value;
  }

  // Handle last byte specially
  value = (value << 8) | buffer[8];
  offset++;
  return value;
};

static TABLE[] GetTables(FileStream bufStream)
{
  int cellCount = ReadInt16(bufStream, 103);
  List<TABLE> tables = [];
  for (int i = 0; i < cellCount; ++i)
  {
    int cellPtrOffset = 108;
    int cellContentOffset = ReadInt16(bufStream, cellPtrOffset + i * 2);
    TABLE tableName = GetTable(bufStream, cellContentOffset);
    tables.Add(tableName);
  }
  return [.. tables];
}

static TABLE GetTable(FileStream bufStream, int offset)
{
  int cellOffset = offset;
  int payloadSize = ReadVarInt(bufStream, ref cellOffset);
  int rowid = ReadVarInt(bufStream, ref cellOffset);

  bufStream.Seek(cellOffset, SeekOrigin.Begin);
  int headerSize = bufStream.ReadByte();
  int savedOffset = cellOffset;
  int dataOffset = savedOffset + headerSize;

  cellOffset += 1; // Skip header size byte
  TABLE res = new();
  Span<byte> stackBuf = stackalloc byte[512];

  for (int col = 0; col < 5 && ((cellOffset - savedOffset) < headerSize); ++col)
  {
    int serialType = ReadVarInt(bufStream, ref cellOffset);

    if (serialType >= 13 && serialType % 2 == 1)
    {
      int strLen = (serialType - 13) / 2;
      Span<byte> buffer = strLen <= 512 ? stackBuf[..strLen] : new byte[strLen];
      bufStream.Seek(dataOffset, SeekOrigin.Begin);
      bufStream.ReadExactly(buffer);
      var str = Encoding.ASCII.GetString(buffer);
      switch (col)
      {
        case 0: res.type = str; break;    // sqlite_schema.type
        case 1: res.name = str; break;    // sqlite_schema.name
        case 2: res.tblName = str; break; // sqlite_schema.tblName
        case 4: res.sql = str; break;     // sqlite_schema.sql
      }
      dataOffset += strLen;
    }
    else if (col == 3)                    // sqlite_schema.rootpage
    {
      var (value, bytesRead) = ParseRootPage(bufStream, dataOffset, serialType);
      res.rootpage = value;
      dataOffset += bytesRead;
    }
  }
  return res;
}

static (int, int) ParseRootPage(FileStream bufStream, int offset, int serialType)
{
  if (serialType == 0 || serialType == 8 || serialType == 9 || serialType == 12 || serialType == 13)
    return (0, 0);

  int res = -1;

  bufStream.Seek(offset, SeekOrigin.Begin);
  if (serialType == 1)
  {
    res = bufStream.ReadByte();
  }
  else if (serialType == 2)
  {
    Span<byte> buffer = stackalloc byte[2];
    bufStream.ReadExactly(buffer);
    res = (buffer[0] << 8) | buffer[1];
  }
  else if (serialType == 3)
  {
    Span<byte> buffer = stackalloc byte[3];
    bufStream.ReadExactly(buffer);
    res = (buffer[0] << 16) | (buffer[1] << 8) | buffer[2];

  }
  else if (serialType == 4)
  {
    Span<byte> buffer = stackalloc byte[4];
    bufStream.ReadExactly(buffer);
    res = (buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3];
  }
  else
  {
    throw new Exception("unknown serialType");
  }

  return (res, serialType);
}

struct TABLE
{
  public string type = "";
  public string name = "";
  public string tblName = "";
  public string sql = "";
  public int rootpage = 0;
  public int rowCount = 0;
  public List<int> cells = [];
  public List<int> indexRowids = [];

  public TABLE()
  {
  }
}
