﻿using MetadataManager;
using PageManager;
using System;
using System.Diagnostics;

namespace QueryProcessing
{
    static class QueryProcessingAccessors
    {
        public static IComparable MetadataColumnRowsetHolderFetcher(MetadataColumn mc, RowHolderFixed rowHolder)
        {
            // TODO: Can't use ColumnId as fetcher.
            if (mc.ColumnType.ColumnType == ColumnType.Int)
            {
                return rowHolder.GetField<int>(mc.ColumnId);
            }
            else if (mc.ColumnType.ColumnType == ColumnType.Double)
            {
                return rowHolder.GetField<double>(mc.ColumnId);
            }
            else if (mc.ColumnType.ColumnType == ColumnType.String)
            {
                // TODO: Since char[] doesn't implement IComparable need to cast it to string.
                // This is super slow...
                // Consider creating your own string type.
                return new string(rowHolder.GetStringField(mc.ColumnId));
            }
            else
            {
                Debug.Fail("Invalid column type");
                throw new InvalidProgramException("Invalid state.");
            }
        }

        // TODO: This is just bad.
        // It is very hard to keep all type -> agg mappings.
        // Needs refactoring.
        public static void ApplyAgg(MetadataColumn mc, ref RowHolderFixed inputRowHolder, Sql.aggType aggType, ref RowHolderFixed stateRowHolder) 
        {
            // TODO: Can't use ColumnId as fetcher.
            if (mc.ColumnType.ColumnType == ColumnType.Int)
            {
                IComparable inputValue = inputRowHolder.GetField<int>(mc.ColumnId);
                IComparable stateValue = stateRowHolder.GetField<int>(mc.ColumnId);

                if (aggType.IsMax)
                {
                    if (inputValue.CompareTo(stateValue) == 1)
                    {
                        // Update state.
                        // TODO: boxing/unboxing hurts perf.
                        stateRowHolder.SetField<int>(mc.ColumnId, (int)inputValue);
                    }
                }
                else if (aggType.IsMin)
                {
                    if (inputValue.CompareTo(stateValue) == -1)
                    {
                        // TODO: boxing/unboxing hurts perf.
                        stateRowHolder.SetField<int>(mc.ColumnId, (int)inputValue);
                    }
                }
                else
                {
                    throw new InvalidProgramException("Aggregate not supported.");
                }
            }
            else if (mc.ColumnType.ColumnType == ColumnType.Double)
            {
                IComparable inputValue = inputRowHolder.GetField<double>(mc.ColumnId);
                IComparable stateValue = stateRowHolder.GetField<double>(mc.ColumnId);

                if (aggType.IsMax)
                {
                    if (inputValue.CompareTo(stateValue) == 1)
                    {
                        // Update state.
                        // TODO: boxing/unboxing hurts perf.
                        stateRowHolder.SetField<double>(mc.ColumnId, (double)inputValue);
                    }
                }
                else if (aggType.IsMin)
                {
                    if (inputValue.CompareTo(stateValue) == -1)
                    {
                        // TODO: boxing/unboxing hurts perf.
                        stateRowHolder.SetField<double>(mc.ColumnId, (double)inputValue);
                    }
                }
                else
                {
                    throw new InvalidProgramException("Aggregate not supported.");
                }
            }
            else if (mc.ColumnType.ColumnType == ColumnType.String)
            {
                // TODO: Since char[] doesn't implement IComparable need to cast it to string.
                // This is super slow...
                // Consider creating your own string type.
                string inputValue = new string(inputRowHolder.GetStringField(mc.ColumnId));
                string stateValue = new string(stateRowHolder.GetStringField(mc.ColumnId));

                if (aggType.IsMax)
                {
                    if (inputValue.CompareTo(stateValue) == 1)
                    {
                        // Update state.
                        // TODO: boxing/unboxing hurts perf.
                        stateRowHolder.SetField(mc.ColumnId, inputValue.ToCharArray());
                    }
                }
                else if (aggType.IsMin)
                {
                    if (inputValue.CompareTo(stateValue) == -1)
                    {
                        // TODO: boxing/unboxing hurts perf.
                        stateRowHolder.SetField(mc.ColumnId, inputValue.ToCharArray());
                    }
                }
                else
                {
                    throw new InvalidProgramException("Aggregate not supported.");
                }
            }
            else
            {
                Debug.Fail("Invalid column type");
                throw new InvalidProgramException("Invalid state.");
            }
        }
    }
}