/*
Title: IOperationExecutor
Type: Source Code
Availability: https://github.com/dotnet/efcore/blob/5ac0582edbeafc1ccfa3348fa5eb2b0fccc60cc4/src/ef/IOperationExecutor.cs
*/

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Atlas.Provider.Core;

internal interface IOperationExecutor : IDisposable
{
    IDictionary AddMigration(string name, string? outputDir, string? contextType, string? @namespace);
    IDictionary RemoveMigration(string? contextType, bool force);
    IEnumerable<IDictionary> GetMigrations(string? contextType, string? connectionString, bool noConnect);
    void DropDatabase(string? contextType);
    IDictionary GetContextInfo(string? name);
    void UpdateDatabase(string? migration, string? connectionString, string? contextType);
    IEnumerable<IDictionary> GetContextTypes();
    void OptimizeContext(string? outputDir, string? modelNamespace, string? contextType);

    IDictionary ScaffoldContext(
        string provider,
        string connectionString,
        string? outputDir,
        string? outputDbContextDir,
        string? dbContextClassName,
        IEnumerable<string> schemaFilters,
        IEnumerable<string> tableFilters,
        bool useDataAnnotations,
        bool overwriteFiles,
        bool useDatabaseNames,
        string? entityNamespace,
        string? dbContextNamespace,
        bool suppressOnConfiguring,
        bool noPluralize);

    string ScriptMigration(string? fromMigration, string? toMigration, bool idempotent, bool noTransactions, string? contextType);

    string ScriptDbContext(string? contextType);
    void HasPendingModelChanges(string? contextType);
}
