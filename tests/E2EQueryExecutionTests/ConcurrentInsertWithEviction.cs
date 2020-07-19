﻿using DataStructures;
using LockManager;
using LogManager;
using NUnit.Framework;
using PageManager;
using PageManager.Exceptions;
using QueryProcessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Test.Common;

namespace E2EQueryExecutionTests
{
    public class ConcurrentInsertWithEviction
    {
        [Test]
        public async Task ConcurrentInsertWithEvictionTest()
        {
            var lockManager = new LockManager.LockManager(new LockMonitor(), TestGlobals.TestFileLogger);
            var pageManager =  new PageManager.PageManager(4096, new FifoEvictionPolicy(7, 2), TestGlobals.DefaultPersistedStream, new BufferPool(), lockManager, TestGlobals.TestFileLogger);

            var logManager = new LogManager.LogManager(new BinaryWriter(new MemoryStream()));
            StringHeapCollection stringHeap = null;
            StringHeapCollection metadataStringHeap = null;

            await using (ITransaction tran = logManager.CreateTransaction(pageManager, isReadOnly: false, "SETUP"))
            {
                stringHeap = new StringHeapCollection(pageManager, tran);
                metadataStringHeap = new StringHeapCollection(pageManager, tran);
                await tran.Commit();
            }

            var metadataManager = new MetadataManager.MetadataManager(pageManager, metadataStringHeap, pageManager, logManager);
            AstToOpTreeBuilder treeBuilder = new AstToOpTreeBuilder(metadataManager, stringHeap, pageManager);

            var queryEntryGate = new QueryEntryGate(
                statementHandlers: new ISqlStatement[]
                {
                    new CreateTableStatement(metadataManager),
                    new InsertIntoTableStatement(treeBuilder),
                    new SelectStatement(treeBuilder),
                });

            await using (ITransaction tran = logManager.CreateTransaction(pageManager))
            {
                string createTableQuery = "CREATE TABLE ConcurrentTableWithEviction (TYPE_INT a, TYPE_DOUBLE b, TYPE_STRING c)";
                await queryEntryGate.Execute(createTableQuery, tran).ToArrayAsync();
                await tran.Commit();
            }

            const int rowCount = 10;
            const int workerCount = 100;
            int totalSum = 0;
            int totalInsert = 0;

            async Task insertAction()
            {
                for (int i = 1; i <= rowCount; i++)
                {
                    using (ITransaction tran = logManager.CreateTransaction(pageManager, "GET_ROWS"))
                    {
                        try
                        {
                            string insertQuery = $"INSERT INTO ConcurrentTableWithEviction VALUES ({i}, {i + 0.001}, mystring)";
                            await queryEntryGate.Execute(insertQuery, tran).ToArrayAsync();
                            await tran.Commit();
                            Interlocked.Add(ref totalSum, i);
                            Interlocked.Increment(ref totalInsert);
                        }
                        catch (TransactionRollbackException)
                        {
                            await tran.Rollback();
                            TestContext.Out.Write("Transaction rollback exception");
                        }
                        catch (DeadlockException)
                        {
                            await tran.Rollback();
                            TestContext.Out.Write("Transaction deadlock exception");
                        }
                    }
                }
            }

            List<Task> tasks = new List<Task>();
            for (int i = 0; i < workerCount; i++)
            {
                tasks.Add(Task.Run(insertAction));
            }

            await Task.WhenAll(tasks);

            await using (ITransaction tran = logManager.CreateTransaction(pageManager, "GET_ROWS"))
            {
                string query = @"SELECT a, b, c FROM ConcurrentTableWithEviction";
                Row[] result = await queryEntryGate.Execute(query, tran).ToArrayAsync();

                Assert.AreEqual(result.Length, totalInsert);

                int sum = result.Sum(r => r.IntCols[0]);
                Assert.AreEqual(totalSum, sum);
                await tran.Commit();
            }

            LockStats lockStats = lockManager.GetLockStats();
            TestContext.Out.WriteLine(lockStats);
        }
    }
}