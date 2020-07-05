﻿using PageManager;

namespace Test.Common
{
    public static class TestGlobals
    {
        public static IPageEvictionPolicy DefaultEviction
        {
            get
            {
                return new FifoEvictionPolicy(10, 5);
            }
        }

        public static IPageEvictionPolicy RestrictiveEviction = new FifoEvictionPolicy(1, 1);

        public static PersistedStream DefaultPersistedStream
        {
            get
            {
                return new PersistedStream(1024 * 1024, "temp.data", createNew: true);
            }
        }

        public static ITransaction DummyTran = new DummyTran();

        public static InstrumentationInterface TestFileLogger = new Instrumentation.Logger("SharedTestFile", "mainrep");
    }
}