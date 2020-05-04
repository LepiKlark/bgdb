﻿using MetadataManager;
using PageManager;
using System.Linq;

namespace QueryProcessing
{
    public class PhyOpTableInsert : IPhysicalOperator<Row>
    {
        private MetadataTable mdTable;
        private PageListCollection pageCollection;
        private IAllocateMixedPage pageAllocator;
        private HeapWithOffsets<char[]> stringHeap;

        public PhyOpTableInsert(MetadataTable mdTable, IAllocateMixedPage pageAllocator, HeapWithOffsets<char[]> stringHeap)
        {
            this.mdTable = mdTable;
            this.pageAllocator = pageAllocator;

            IPage rootPage = this.pageAllocator.GetMixedPage(this.mdTable.RootPage);
            this.pageCollection = new PageListCollection(this.pageAllocator, mdTable.Columns.Select(c => c.ColumnType).ToArray(), rootPage);
            this.stringHeap = stringHeap;
        }

        public void Invoke(Row row)
        {
            this.pageCollection.Add(row.ToRowsetHolder(mdTable.Columns.Select(c => c.ColumnType).ToArray(), stringHeap));
        }
    }
}
