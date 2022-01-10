# bgDB

[![.NET](https://github.com/dasatomic/bgdb/actions/workflows/dotnet.yml/badge.svg)](https://github.com/dasatomic/bgdb/actions/workflows/dotnet.yml)
[![Coverage Status](https://coveralls.io/repos/github/dasatomic/bgdb/badge.svg?branch=master)](https://coveralls.io/github/dasatomic/bgdb?branch=master)

`bgDB` is a tiny relational database that aims to make end to end database development more approachable.
The starting point is that database development is fun and that it can help us learn many fundamental computer science concepts. To name a few:
1) Parser/Lexer/Compiler development
2) Data Structures
3) Asynchronous programming
4) Concurrency
5) Performance
6) Working with hardware

`bgDB` doesn't aim to be the fastest nor to provide features that other RDBMSs already provide for decades. It main goal is to be approachable and easily extendable. Language of choice is `C#` for most of the engine and `F#` for Parser and Lexer. Even though managed languages are not the best choice for Database development, given that garbage collection can induce unwanted performance problems and that for many parts developer would want more control, for what we aim to do here they serve the purpose.

# Getting started.

## Dependencies
After cloning the enlistment you will need `dotnet core 3.1` or above. It can be found [here](https://dotnet.microsoft.com/download/dotnet-core/3.1).

The engine it self doesn't have any dependencies, besides .net core. Tests and perf benchmarks will pull additional nugets. For running perf tests and updating plots you will need to install R. Instructions are in perf [readme.md](https://github.com/dasatomic/bgdb/tree/master/UnitBenchmark/readme.md).

## Read eval print loop

To try it out you can start bgdbRepl (read-eval-print-loop) by starting project in bgdbRepl folder. From root enlistment do following:

```cmd
cd bgdbRepl
dotnet run
```

```
Booted Page Manager with file name repl.db. File size 10MB. Creating db file from scratch.
Booted Log Manager. Using in memory stream.
Booted Metadata Manager.
Query entry gate ready.
Transactions are implicit for now. Every command will be executed in separate transaction.
====================
>CREATE TABLE T1 (TYPE_INT a, TYPE_DOUBLE b, TYPE_STRING(20) c)


Total rows returned 0
```

Inserting rows
```
>INSERT INTO T1 VALUES (1, 1.1, 'somerandomstring1')
Total rows returned 0
>INSERT INTO T1 VALUES (2, 2.2, 'somerandomstring1')
Total rows returned 0
>INSERT INTO T1 VALUES (3, 2.2, 'somerandomstring2')
Total rows returned 0
>INSERT INTO T1 VALUES (5, 14.2, 'somerandomstring2')
Total rows returned 0
```

Querying
```
SELECT MAX(a), MIN(b), c FROM T1 WHERE a > 1 GROUP BY c

|     a_Max |     b_Min |                     c |
-------------------------------------------------
|         2 |       2.2 |     somerandomstring1 |
-------------------------------------------------
|         5 |       2.2 |     somerandomstring2 |
-------------------------------------------------

Total rows returned 2
```

Or let us create one more table and test out joins.

```
CREATE TABLE T2 (TYPE_INT a, TYPE_STRING(10) c)
INSERT INTO T2 VALUES (1, 'somerandomstring2')
INSERT INTO T2 VALUES (100, 'somerandomstring2')
```

Needlessly complex query that illustrates data flow:

```
SELECT MAX(T1.a), MIN(T1.b), T2.c 
FROM T1
JOIN T2 ON T1.c = T2.c
WHERE T2.a = 100
GROUP BY T2.c

| TR1.A_Max | TR1.B_Min |             TR2.C |
---------------------------------------------
|         5 |       2.2 | somerandomstring2 |
---------------------------------------------

Total rows returned 1
```

To experiment with slightly larger datasets you can also load titanic dataset by passing set_load_path argument

```
dotnet run --set_load_path .\datasets\titanic-passengers.csv
```

```
>SELECT TOP 10 * FROM Passengers

|PASSENGERID |SURVIVED |     CLASS |                                                                    NAME |     SEX |       AGE |  SIBLINGS |   PARENTS |EMBARKEDPORT |
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------
|       343  |      No |         2 |                                              Collander, Mr. Erik Gustaf |    male |        28 |         0 |         0 |           S |
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------
|        76  |      No |         3 |                                                 Moen, Mr. Sigurd Hansen |    male |        25 |         0 |         0 |           S |
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------
|       641  |      No |         3 |                                                  Jensen, Mr. Hans Peder |    male |        20 |         0 |         0 |           S |
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------
|       568  |      No |         3 |                             Palsson, Mrs. Nils (Alma Cornelia Berglund) |  female |        29 |         0 |         4 |           S |
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------
|       672  |      No |         1 |                                                  Davidson, Mr. Thornton |    male |        31 |         1 |         0 |           S |
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------
|       105  |      No |         3 |                                          Gustafsson, Mr. Anders Vilhelm |    male |        37 |         2 |         0 |           S |
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------
|       576  |      No |         3 |                                                    Patchett, Mr. George |    male |        19 |         0 |         0 |           S |
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------
|       382  |     Yes |         3 |                                         "Nakid, Miss. Maria (""Mary"")" |  female |         1 |         0 |         2 |           C |
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------
|       228  |      No |         3 |                                     "Lovell, Mr. John Hall (""Henry"")" |    male |      20.5 |         0 |         0 |           S |
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------
|       433  |     Yes |         2 |                     Louch, Mrs. Charles Alexander (Alice Adelaide Slow) |  female |        42 |         1 |         0 |           S |
--------------------------------------------------------------------------------------------------------------------------------------------------------------------------

Total rows returned 10
```
Or something a bit more complex.
```
>SELECT COUNT(PassengerId), Sex, Survived, MIN(Age), MAX(Age) FROM Passengers WHERE EmbarkedPort = 'S' AND Class = 3 GROUP BY Sex, Survived

|PassengerId_Count |     Sex |Survived |   Age_Min |   Age_Max |
----------------------------------------------------------------
|       183        |    male |   No    |      1    |        74 |
----------------------------------------------------------------
|        45        |  female |   No    |      2    |        48 |
----------------------------------------------------------------
|        31        |  female |  Yes    |      1    |        63 |
----------------------------------------------------------------
|        30        |    male |  Yes    |      1    |        45 |
----------------------------------------------------------------

Total rows returned 4
```

If you want to play with more data you can start repl with argument `--rep_load_count N` that will duplicate input dataset N times. E.g.
```
dotnet run --set_load_path .\datasets\titanic-passengers.csv --rep_load_count 1000
```

Will load ~800k rows.

# Working with non structured data
Bgdb also supports accessing row file system:

```
SELECT Extension, SUM(FileSize) FROM FILESYSTEM('./assets') GROUP BY Extension"
```

```
Hint: For file system operation start bgdbrepl with format_list option. This way querying long strings is more readable:
cd bgdbRepl
dotnet run --use_list_format
```

One idea for the future of BgDb is support for extracting information from data types such as image, video and audio.

## Video operations

Currently there is a support for basic video querying. You can either list info about video file or split it in chunks for future processing:
```
SELECT * FROM VIDEO_CHUNKER(300, SELECT * FROM FILESYSTEM('E:/bgdb_video_examples') WHERE Extension = '.mkv')
```

This example will split all the *.mkv files in 300 second chunks:
```
FilePath -> E:\bgdb_video_examples\Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special.mkv
FileName -> Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special.mkv
Extension -> .mkv
FileSize -> 313687893
chunk_path -> C:\Users\Aleksandar\projects\bgdb_github\bgdb\bgdbRepl\bin\Debug\net6.0\temp\d8ec89d7-50a9-41d0-8a94-091e12934cab\Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special000.mkv
NbStreams -> 2
NbPrograms -> 0
StartTimeInSeconds -> 0.067
DurationInSeconds -> 304.337
FormatName -> matroska,webm
---------------------
FilePath -> E:\bgdb_video_examples\Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special.mkv
FileName -> Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special.mkv
Extension -> .mkv
FileSize -> 313687893
chunk_path -> C:\Users\Aleksandar\projects\bgdb_github\bgdb\bgdbRepl\bin\Debug\net6.0\temp\d8ec89d7-50a9-41d0-8a94-091e12934cab\Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special001.mkv
NbStreams -> 2
NbPrograms -> 0
StartTimeInSeconds -> 304.28
DurationInSeconds -> 602.101
FormatName -> matroska,webm
---------------------
FilePath -> E:\bgdb_video_examples\Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special.mkv
FileName -> Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special.mkv
Extension -> .mkv
FileSize -> 313687893
chunk_path -> C:\Users\Aleksandar\projects\bgdb_github\bgdb\bgdbRepl\bin\Debug\net6.0\temp\d8ec89d7-50a9-41d0-8a94-091e12934cab\Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special002.mkv
NbStreams -> 2
NbPrograms -> 0
StartTimeInSeconds -> 602.05
DurationInSeconds -> 900.967
FormatName -> matroska,webm
...
```

Note that all the temporary files (e.g. video chunks) will leave as long as transaction is opened. On commit/rollback all the files will be purged.

## Image operations
Bgdb supports image classification through CLASSIFY_IMAGE function:

```
SELECT CLASSIFY_IMAGE(FilePath), FilePath, FileName FROM FILESYSTEM('../tests/E2EQueryExecutionTests/assets/pics') WHERE EXTENSION = '.jpg' OR EXTENSION = '.jfif'
```

```
---------------------
Object_Classification_Result -> basketball
FilePath -> C:\Users\Aleksandar\projects\bgdb_github\bgdb\tests\E2EQueryExecutionTests\assets\pics\basketball.jpg
FileName -> basketball.jpg
---------------------
Object_Classification_Result -> hippopotamus
FilePath -> C:\Users\Aleksandar\projects\bgdb_github\bgdb\tests\E2EQueryExecutionTests\assets\pics\hippo.jfif
FileName -> hippo.jfif
---------------------
```

You can also combine image classification with video operations.
Following example will print classification result for every frame, extracted every 60s of video chunked into 300s pieces:

```
SELECT chunk_path, FilePath, frame_path, CLASSIFY_IMAGE(frame_path) 
FROM VIDEO_TO_IMAGE(
    1, 60, SELECT * FROM VIDEO_CHUNKER(300, SELECT * FROM FILESYSTEM('E:/bgdb_video_examples')))
```

Output:
```
---------------------
chunk_path -> C:\Users\Aleksandar\projects\bgdb_github\bgdb\bgdbRepl\bin\Debug\net6.0\temp\ff4862cd-97fa-445a-b42b-2b5762336314\Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special000.mkv
FilePath -> E:\bgdb_video_examples\Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special.mkv
frame_path -> C:\Users\Aleksandar\projects\bgdb_github\bgdb\bgdbRepl\bin\Debug\net6.0\temp\f3b4bb86-c687-4560-957a-d5065e45c985\Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special000001.bmp
Object_Classification_Result -> panpipe
---------------------
chunk_path -> C:\Users\Aleksandar\projects\bgdb_github\bgdb\bgdbRepl\bin\Debug\net6.0\temp\ff4862cd-97fa-445a-b42b-2b5762336314\Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special000.mkv
FilePath -> E:\bgdb_video_examples\Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special.mkv
frame_path -> C:\Users\Aleksandar\projects\bgdb_github\bgdb\bgdbRepl\bin\Debug\net6.0\temp\f3b4bb86-c687-4560-957a-d5065e45c985\Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special000002.bmp
Object_Classification_Result -> oboe
---------------------
chunk_path -> C:\Users\Aleksandar\projects\bgdb_github\bgdb\bgdbRepl\bin\Debug\net6.0\temp\ff4862cd-97fa-445a-b42b-2b5762336314\Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special000.mkv
FilePath -> E:\bgdb_video_examples\Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special.mkv
frame_path -> C:\Users\Aleksandar\projects\bgdb_github\bgdb\bgdbRepl\bin\Debug\net6.0\temp\f3b4bb86-c687-4560-957a-d5065e45c985\Anthony.Bourdain.No.Reservations.S04E00.Holiday.Special000003.bmp
Object_Classification_Result -> American lobster
---------------------
...
```

## From code init

Repl is currently pretty limited. There is also no support for transactions in parser layer (transactions are implicit and to single command, `BEGIN/COMMIT/ROLLBACK TRAN` support will be added). To get a feeling how things are working under the hood it is best to take a look at end to end tests.

For example:
```cs
[Test]
public async Task MultiTableGroupBy()
{
    await using (ITransaction tran = this.logManager.CreateTransaction(pageManager))
    {
        await this.queryEntryGate.Execute("CREATE TABLE T1 (TYPE_INT A, TYPE_INT B)", tran).AllResultsAsync();
        await this.queryEntryGate.Execute("CREATE TABLE T2 (TYPE_INT A, TYPE_INT B)", tran).AllResultsAsync();
        await this.queryEntryGate.Execute("INSERT INTO T1 VALUES (1, 1)", tran).AllResultsAsync();
        await this.queryEntryGate.Execute("INSERT INTO T1 VALUES (1, 2)", tran).AllResultsAsync();
        await this.queryEntryGate.Execute("INSERT INTO T2 VALUES (2, 3)", tran).AllResultsAsync();
        await this.queryEntryGate.Execute("INSERT INTO T2 VALUES (2, 4)", tran).AllResultsAsync();
        await tran.Commit();
    }

    await using (ITransaction tran = this.logManager.CreateTransaction(pageManager, isReadOnly: true, "SELECT"))
    {
        RowHolder[] result = await this.queryEntryGate.Execute("SELECT MAX(B), A FROM T1 GROUP BY A", tran).ToArrayAsync();
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(2, result[0].GetField<int>(0));
        Assert.AreEqual(1, result[0].GetField<int>(1));
        result = await this.queryEntryGate.Execute("SELECT MAX(B), A FROM T2 GROUP BY A", tran).ToArrayAsync();
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(4, result[0].GetField<int>(0));
        Assert.AreEqual(2, result[0].GetField<int>(1));
    }
}
```

E2E tests can be found [here](https://github.com/dasatomic/bgdb/tree/master/tests/E2EQueryExecutionTests).

# Supported features and project ramp-up
At this point list of features is rather limited but, hopefully, the list will keep growing.

## Language features
1) `CREATE TABLE`
2) `INSERT INTO TABLE`
3) FILTERS (`WHERE` statement with basic arithmetic)
4) `GROUP BY` statement
5) Aggregates (`MAX`, `MIN`, `SUM`, `COUNT`)
6) Support for wildcard select (`SELECT * FROM`)
7) Support for TOP clause (`SELECT TOP N * FROM`)
8) Support for JOIN clause (only `INNER JOIN` for now)
9) Support for functions (`SELECT CONCAT(str1, str2) FROM WHERE ADD(x, y) > 10`)
10) Support for nested subqueries (`SELECT * FROM (SELECT a, b FROM T) WHERE b > 42`)
11) Support for filesystem operations (`SELECT * FROM FILESYSTEM('./assets') WHERE Extension = '.txt'`)
12) Support for image classification (`SELECT CLASSIFY_IMAGE(FilePath), FilePath, FileName FROM FILESYSTEM('./assets/pics') WHERE EXTENSION = '.jpg' OR EXTENSION = '.jfif'`)
13) Support for video operations (`SELECT * FROM VIDEO_CHUNKER(10, SELECT * FROM FILESYSTEM('./assets/videos') WHERE Extension = '.mkv')`)

## Supported types
1) `TYPE_INT` (32bit signed)
2) `TYPE_DOUBLE` (64 bit)
3) `TYPE_STRING(SIZE)` (strings of fixed length)
4) `TYPE_STRING(MAX)` (work in progress, strings of unlimited length)

## Transactions and locking
Engine currently supports page level locking and read committed isolation. On startup a pool of locks is created and page id maps to lock id through simple modular arithmetic. Lock Manager does deadlock detection and rollbacks deadlock victims.

Logging is traditional [Write Ahead Logging](https://en.wikipedia.org/wiki/Write-ahead_logging). To ramp-up, recommendation is to take a look at `PageManager` [constructor](https://github.com/dasatomic/bgdb/tree/master/PageManager/PageManager.cs).

```cs
public PageManager(uint defaultPageSize, IPersistedStream persistedStream, IBufferPool bufferPool, ILockManager lockManager, InstrumentationInterface logger)
```

Log Manager only sits on top of .NET stream that is used for logging.
```cs
public LogManager(BinaryWriter storage)
```

[Lock Manager](https://github.com/dasatomic/bgdb/tree/master/LockManager/LockImplementation/AsyncReadWriterLock.cs) currently supports only Read and Write locks that prioritize Writers.

## Data Structures
Tables can be organized in two data structures:
1) [linked list](https://github.com/dasatomic/bgdb/tree/master/DataStructures/PageListCollection.cs)
2) [btree] (https://github.com/dasatomic/bgdb/blob/master/DataStructures/BTreeCollection.cs)

## Query Processing
Query tree is currently assembled through a set of rules that can be found [here](https://github.com/dasatomic/bgdb/tree/master/QueryProcessing/AstToOpTreeBuilder.cs). When work on Query Optimizer starts this will have to change.

Operators follow this simple interface:
```cs
public interface IPhysicalOperator<T>
{
    IAsyncEnumerable<T> Iterate(ITransaction tran);
}
```

Caller gets root operator and keeps draining iterators that are placed deeper in the tree. On the leaf nodes there is `Scan` operator (or, in future, `Seek`, when support for indexes comes).
Operators work on [RowHolder](https://github.com/dasatomic/bgdb/tree/master/PageManager/RowHolder.cs) instances which represent single `Row` fetched from Storage Engine.

## Parser/Lexer
Currently we use F# and `FsLexYacc` [library](https://github.com/fsprojects/FsLexYacc). Grammar can be easily understood by looking at [type definitions](https://github.com/dasatomic/bgdb/tree/master/ParserLexerFSharp/Sql.fs) and [Parser](https://github.com/dasatomic/bgdb/tree/master/ParserLexerFSharp/SqlParser.fsp).

# Testing and Benchmarks
Tests are written using `NUNIT` framework. You can run them either through Visual Studio or simply running `dotnet test` from root folder.

For Benchmarking we use excellent [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet). Benchmarks and results can be found [here](https://github.com/dasatomic/bgdb/tree/master/UnitBenchmark).

There are also simple [stress tests](https://github.com/dasatomic/bgdb/tree/master/tests/E2EQueryExecutionTests/ConcurrentInsertWithEviction.cs) in E2E folder.

Project also has set up Continuous Integration [pipeline](https://github.com/dasatomic/bgdb/actions) on GitHub that is running unit tests on every push.

You can build docker container by using provided docker [file](https://github.com/dasatomic/bgdb/tree/master/Dockerfile).
