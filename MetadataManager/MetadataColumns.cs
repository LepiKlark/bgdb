﻿using DataStructures;
using PageManager;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MetadataManager
{
    public struct MetadataColumn
    {
        public const int NotPartOfClusteredIndex = -1;

        public const int ColumnIdColumnPos = 0;
        public readonly int ColumnId;
        public const int TableIdColumnPos = 1;
        public readonly int TableId;
        public const int ColumnNameColumnPos = 2;
        public readonly string ColumnName;
        public const int ColumnTypeColumnPos = 3;
        public readonly ColumnInfo ColumnType;
        public const int ColumnTypeLength = 4;
        public const int ClusteredIndexPartPos = 5;
        public readonly int ClusteredIndexPart;

        public MetadataColumn(int columnId, int tableId, string columnName, ColumnInfo columnInfo, int clusteredIndexPart = NotPartOfClusteredIndex)
        {
            this.ColumnId = columnId;
            this.TableId = tableId;
            this.ColumnName = columnName;
            this.ColumnType = columnInfo;
            this.ClusteredIndexPart = clusteredIndexPart;
        }

        public MetadataColumn SetNewColumnPosition(int columnId)
        {
            return new MetadataColumn(columnId, this.TableId, this.ColumnName, this.ColumnType, this.ClusteredIndexPart);
        }
    }

    /// <summary>
    ///  Definition of one column.
    ///  Table id and column id are unique identifiers.
    /// </summary>
    public struct ColumnCreateDefinition
    {
        public ColumnCreateDefinition(int tableId, string columnName, ColumnInfo columnInfo, int columnId, int clusteredIndexPosition)
        {
            this.TableId = tableId;
            this.ColumnName = columnName;
            this.ColumnType = columnInfo;
            this.ColumnId = columnId;
            this.ClusteredIndexPosition = clusteredIndexPosition;
        }

        /// <summary>
        /// Table id this column belongs to.
        /// </summary>
        public int TableId { get; }

        /// <summary>
        /// Name of this column.
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Type of this column.
        /// </summary>
        public ColumnInfo ColumnType { get; }

        /// <summary>
        /// Column position in table.
        /// </summary>
        public int ColumnId { get; }

        /// <summary>
        /// Position in clustered index, if this column is part of it.
        /// -1 otherwise.
        /// </summary>
        public int ClusteredIndexPosition { get; }
    }

    public class MetadataColumnsManager : IMetadataObjectManager<MetadataColumn, ColumnCreateDefinition, Tuple<int, int> /* column id - table id */>
    {
        public const string MetadataTableName = "sys.columns";

        private IPageCollection<RowHolder> pageListCollection;
        private HeapWithOffsets<char[]> stringHeap;

        private const int MAX_NAME_LENGTH = 20;

        private static ColumnInfo[] columnDefinitions = new ColumnInfo[]
        {
            new ColumnInfo(ColumnType.Int), // Column id
            new ColumnInfo(ColumnType.Int), // Table id
            new ColumnInfo(ColumnType.String, MAX_NAME_LENGTH), // pointer to name
            new ColumnInfo(ColumnType.Int), // column type
            new ColumnInfo(ColumnType.Int), // column type length
            new ColumnInfo(ColumnType.Int), // clustered index position.
        };

        public static ColumnInfo[] GetSchemaDefinition() => columnDefinitions;

        public MetadataColumnsManager(IAllocateMixedPage pageAllocator, MixedPage firstPage, HeapWithOffsets<char[]> stringHeap)
        {
            if (pageAllocator == null || firstPage == null)
            {
                throw new ArgumentNullException();
            }

            this.pageListCollection = new PageListCollection(pageAllocator, columnDefinitions, firstPage.PageId());
            this.stringHeap = stringHeap;
        }

        public async IAsyncEnumerable<MetadataColumn> Iterate(ITransaction tran)
        {
            await foreach (RowHolder rh in pageListCollection.Iterate(tran))
            {
                PagePointerOffsetPair stringPointer = rh.GetField<PagePointerOffsetPair>(MetadataColumn.ColumnNameColumnPos);
                char[] columnName = await this.stringHeap.Fetch(stringPointer, tran);

                yield return new MetadataColumn(
                    rh.GetField<int>(MetadataColumn.ColumnIdColumnPos),
                        rh.GetField<int>(MetadataColumn.TableIdColumnPos),
                        new string(columnName),
                        new ColumnInfo((ColumnType)rh.GetField<int>(MetadataColumn.ColumnTypeColumnPos), rh.GetField<int>(MetadataColumn.ColumnTypeLength)),
                        rh.GetField<int>(MetadataColumn.ClusteredIndexPartPos));
            }
        }

        public async Task<int> CreateObject(ColumnCreateDefinition def, ITransaction tran)
        {
            if (await this.Exists(def, tran))
            {
                throw new ElementWithSameNameExistsException();
            }

            RowHolder rh = new RowHolder(columnDefinitions);
            PagePointerOffsetPair namePointer =  await this.stringHeap.Add(def.ColumnName.ToCharArray(), tran);

            rh.SetField<int>(MetadataColumn.ColumnIdColumnPos, def.ColumnId);
            rh.SetField<int>(MetadataColumn.TableIdColumnPos, def.TableId);
            rh.SetField<PagePointerOffsetPair>(MetadataColumn.ColumnNameColumnPos, namePointer);
            rh.SetField<int>(MetadataColumn.ColumnTypeColumnPos, (int)def.ColumnType.ColumnType);
            rh.SetField<int>(MetadataColumn.ColumnTypeLength, def.ColumnType.RepCount);
            rh.SetField<int>(MetadataColumn.ClusteredIndexPartPos, def.ClusteredIndexPosition);

            await pageListCollection.Add(rh, tran);

            return def.ColumnId;
        }

        public async Task<bool> Exists(ColumnCreateDefinition def, ITransaction tran)
        {
            await foreach (RowHolder rh in pageListCollection.Iterate(tran))
            {
                int tableId = rh.GetField<int>(MetadataColumn.TableIdColumnPos);

                if (tableId == def.TableId)
                {
                    PagePointerOffsetPair stringPointer = rh.GetField<PagePointerOffsetPair>(MetadataColumn.ColumnNameColumnPos);

                    char[] strContent = await stringHeap.Fetch(stringPointer, tran);
                    if (CharrArray.Compare(strContent, def.ColumnName) == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public async Task<MetadataColumn> GetById(Tuple<int, int> id, ITransaction tran)
        {
            await foreach (var column in this.Iterate(tran))
            {
                if (column.ColumnId == id.Item1 && column.TableId == id.Item2)
                {
                    return column;
                }
            }

            throw new KeyNotFoundException();
        }
    }
}
