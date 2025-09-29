# sqlite_clone_csharp

A minimal **SQLite clone written in C#** for learning purposes.
This project explores how SQLite works under the hood by parsing the SQLite database file format and supporting a few basic commands.

---

## ✨ Features

* Read SQLite database files directly (no dependency on the official SQLite engine).
* Supports:

  * `.dbinfo` → shows database page size and number of tables.
  * `.tables` → lists all tables in the database.
  * `SELECT COUNT(*) FROM <table>` → counts rows in a given table.
  * `SELECT <columns> FROM <table>` → fetches table rows with optional `WHERE` filtering.
* Parses:

  * Page headers
  * B-tree structures (interior and leaf pages)
  * Table schemas
  * Records and serial types

---

## 📦 Installation

Clone the repository:

```bash
git clone https://github.com/rahulsenna/sqlite_clone_csharp.git
cd sqlite_clone_csharp
```

Build the project:

```bash
dotnet build
```

Run it:

```bash
dotnet run -- <database file> <command>
```

---

## 🖥️ Usage Examples

### Show database info

```bash
dotnet run -- sample.db .dbinfo
```

### List tables

```bash
dotnet run -- sample.db .tables
```

### Count rows in a table

```bash
dotnet run -- sample.db "SELECT COUNT(*) FROM users"
```

### Select specific columns

```bash
dotnet run -- sample.db "SELECT id, name FROM users"
```

### With `WHERE` filtering

```bash
dotnet run -- sample.db "SELECT id, name FROM users WHERE name = 'Alice'"
```

---

## 📖 Learning Goals

This project is **not a full SQLite replacement**.
Instead, it’s a tool for understanding:

* How SQLite stores tables, rows, and indexes on disk.
* How B-tree pages are structured (leaf vs. interior nodes).
* How to parse variable-length integers and serial types.
* How a simple SQL execution engine can be built from scratch.

---

## 🛠️ Project Structure

* `Program.cs` → main entry point with command parsing and execution
* `TABLE` struct → represents table schema and row storage
* Helper functions:

  * `ReadInt16`, `ReadVarInt` → binary parsing
  * `ProcessTable` → traverses B-tree pages
  * `GetTables` / `GetTable` → extracts schema from `sqlite_schema`

---

## 🚧 Limitations

* Only supports a subset of SQL (`.dbinfo`, `.tables`, `SELECT`, `COUNT`, simple `WHERE`).
* Does not support `INSERT`, `UPDATE`, `DELETE`.
* Not optimized for performance.
* Tested on small SQLite databases only.

---

## 📚 References

* [SQLite File Format](https://www.sqlite.org/fileformat.html)
* [SQLite B-Tree Overview](https://www.sqlite.org/fileformat2.html)
* [How SQLite Works](https://sqlite.org/arch.html)

---


## 📜 License

MIT License.
See [LICENSE](LICENSE) for details.