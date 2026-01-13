namespace SnowflakeExample;

// ============================================================================
// SNOWFLAKE MAIN APP - All functionality in one component with UseState
// ============================================================================

[App(icon: Icons.Database, title: "Snowflake")]
public class SnowflakeApp : ViewBase
{
    private record SnowflakeCredentialsRequest
    {
        [Required] public string Account { get; init; } = "";
        [Required] public string User { get; init; } = "";
        [Required] public string Password { get; init; } = "";
    }

    public override object? Build()
    {
        var refreshToken = this.UseRefreshToken();
        var configuration = this.UseService<IConfiguration>();
        
        // ========== SHARED CREDENTIALS STATE (UseState - available to all sections!) ==========
        var account = this.UseState("");
        var user = this.UseState("");
        var password = this.UseState("");
        var isVerified = this.UseState(false);
        
        // UI state for credentials form
        var isDialogOpen = this.UseState(false);
        var credentialsForm = this.UseState(() => new SnowflakeCredentialsRequest());
        var verificationStatus = this.UseState<string?>(() => null);
        var isVerifying = this.UseState(false);
        
        // Active tab: 0 = Settings, 1 = Database Explorer, 2 = Brand Dashboard
        var activeTab = this.UseState(0);

        // ========== DATABASE EXPLORER STATE ==========
        var databases = this.UseState<List<string>>(() => new List<string>());
        var selectedDatabase = this.UseState<string?>(() => null);
        var schemas = this.UseState<List<string>>(() => new List<string>());
        var selectedSchema = this.UseState<string?>(() => null);
        var tables = this.UseState<List<string>>(() => new List<string>());
        var selectedTable = this.UseState<string?>(() => null);
        var tableInfo = this.UseState<TableInfo?>(() => null);
        var tablePreview = this.UseState<System.Data.DataTable?>(() => null);
        var dataTab = this.UseState(0);
        var isLoadingStats = this.UseState(false);
        var isLoadingSchemas = this.UseState(false);
        var isLoadingTables = this.UseState(false);
        var isLoadingTableData = this.UseState(false);
        var errorMessage = this.UseState<string?>(() => null);
        var totalDatabases = this.UseState(0);
        var totalSchemas = this.UseState(0);
        var totalTables = this.UseState(0);
        var totalSchemasAll = this.UseState(0);
        var totalTablesAll = this.UseState(0);
        var totalTablesInSchema = this.UseState(0);

        // ========== BRAND DASHBOARD STATE ==========
        var brandData = this.UseState<List<BrandStats>>(() => new List<BrandStats>());
        var totalBrandsCount = this.UseState<long>(() => 0);
        var totalItemsCount = this.UseState<long>(() => 0);
        var totalAvgItemsPerBrand = this.UseState<double>(() => 0);
        var totalAvgPrice = this.UseState<double>(() => 0);
        var totalMinPrice = this.UseState<double>(() => 0);
        var totalMaxPrice = this.UseState<double>(() => 0);
        var totalInventoryValue = this.UseState<double>(() => 0);
        var totalTotalSize = this.UseState<long>(() => 0);
        var totalAvgSize = this.UseState<double>(() => 0);
        var isLoadingBrands = this.UseState(false);
        var errorMessageBrands = this.UseState<string?>(() => null);
        var sortBy = this.UseState<string>("ItemCount");
        var sortOrder = this.UseState<string>("DESC");
        var limit = this.UseState<int>(7);

        // ========== LOAD CREDENTIALS FROM SECRETS ON INIT ==========
        this.UseEffect(() =>
        {
            // Try to load credentials from configuration (user secrets)
            var configAccount = configuration["Snowflake:Account"];
            var configUser = configuration["Snowflake:User"];
            var configPassword = configuration["Snowflake:Password"];
            
            if (!string.IsNullOrWhiteSpace(configAccount) 
                && !string.IsNullOrWhiteSpace(configUser) 
                && !string.IsNullOrWhiteSpace(configPassword)
                && !isVerified.Value)
            {
                account.Value = configAccount;
                user.Value = configUser;
                password.Value = configPassword;
                // Auto-verify credentials from secrets
                isVerifying.Value = true;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var connectionString = BuildConnectionString(configuration, configAccount, configUser, configPassword);
                        var snowflakeService = new SnowflakeService(connectionString);
                        var valid = await snowflakeService.TestConnectionAsync();

                        if (valid)
                        {
                            isVerified.Value = true;
                            verificationStatus.Value = "success";
                            refreshToken.Refresh();
                        }
                        else
                        {
                            account.Value = "";
                            user.Value = "";
                            password.Value = "";
                            verificationStatus.Value = "error";
                            refreshToken.Refresh();
                        }
                    }
                    catch
                    {
                        account.Value = "";
                        user.Value = "";
                        password.Value = "";
                        verificationStatus.Value = "error";
                        refreshToken.Refresh();
                    }
                    finally
                    {
                        isVerifying.Value = false;
                        refreshToken.Refresh();
                    }
                });
            }
        }, []);

        // ========== CREDENTIAL VERIFICATION ==========
        this.UseEffect(async () =>
        {
            var form = credentialsForm.Value;
            if (!string.IsNullOrWhiteSpace(form.Account) && !isDialogOpen.Value && !isVerifying.Value && verificationStatus.Value == null)
            {
                isVerifying.Value = true;
                try
                {
                    var connectionString = BuildConnectionString(configuration, form.Account, form.User, form.Password);
                    var snowflakeService = new SnowflakeService(connectionString);
                    var valid = await snowflakeService.TestConnectionAsync();

                    if (valid)
                    {
                        account.Value = form.Account;
                        user.Value = form.User;
                        password.Value = form.Password;
                        isVerified.Value = true;
                        verificationStatus.Value = "success";
                        // Auto-switch to Database Explorer after successful login
                        activeTab.Value = 1;
                    }
                    else
                    {
                        verificationStatus.Value = "error";
                    }
                }
                catch
                {
                    verificationStatus.Value = "error";
                }
                isVerifying.Value = false;
                refreshToken.Refresh();
            }
        }, [credentialsForm, isDialogOpen]);

        // ========== DATABASE EXPLORER EFFECTS ==========
        this.UseEffect(async () =>
        {
            if (!isVerified.Value || activeTab.Value != 1) return;
            var service = CreateSnowflakeService(configuration, account, user, password);
            if (service == null) return;
            
            errorMessage.Value = null;
            var dbList = await TryAsync(() => service.GetDatabasesAsync(), "Error loading databases");
            if (dbList != null) databases.Value = dbList;
            if (databases.Value.Count > 0) await LoadStatistics(service, null);
        }, [isVerified, activeTab]);

        this.UseEffect(async () =>
        {
            if (!isVerified.Value || activeTab.Value != 1) return;
            var service = CreateSnowflakeService(configuration, account, user, password);
            if (service == null) return;
            
            if (string.IsNullOrEmpty(selectedDatabase.Value))
            {
                schemas.Value = new List<string>();
                ClearSelection();
                if (databases.Value.Count > 0) await LoadStatistics(service, null);
            }
            else
            {
                await LoadSchemas(service, selectedDatabase.Value);
                await LoadStatistics(service, selectedDatabase.Value);
            }
        }, selectedDatabase);

        this.UseEffect(async () =>
        {
            if (!isVerified.Value || activeTab.Value != 1) return;
            var service = CreateSnowflakeService(configuration, account, user, password);
            if (service == null) return;
            
            if (!string.IsNullOrEmpty(selectedDatabase.Value) && !string.IsNullOrEmpty(selectedSchema.Value))
            {
                await LoadTables(service, selectedDatabase.Value, selectedSchema.Value);
            }
            else
            {
                tables.Value = new List<string>();
                selectedTable.Value = null;
                tableInfo.Value = null;
                tablePreview.Value = null;
                totalTablesInSchema.Value = 0;
            }
        }, selectedSchema);

        // Auto-load table data when table is selected
        this.UseEffect(async () =>
        {
            if (!isVerified.Value || activeTab.Value != 1) return;
            if (string.IsNullOrEmpty(selectedDatabase.Value) || string.IsNullOrEmpty(selectedSchema.Value) || string.IsNullOrEmpty(selectedTable.Value)) return;
            
            var service = CreateSnowflakeService(configuration, account, user, password);
            if (service == null) return;
            
            dataTab.Value = 0;
            await LoadTablePreview(service, selectedDatabase.Value, selectedSchema.Value, selectedTable.Value);
        }, selectedTable);

        // ========== BRAND DASHBOARD EFFECTS ==========
        this.UseEffect(async () =>
        {
            if (!isVerified.Value || activeTab.Value != 2) return;
            var service = CreateSnowflakeService(configuration, account, user, password);
            if (service == null) return;
            
            isLoadingBrands.Value = true;
            errorMessageBrands.Value = null;

            try
            {
                refreshToken.Refresh();

                var brandsCountSql = @"
                    SELECT COUNT(DISTINCT P_BRAND) as TotalBrands
                    FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF001.PART
                    WHERE P_BRAND IS NOT NULL";

                var brandsCountResult = await service.ExecuteScalarAsync(brandsCountSql);
                totalBrandsCount.Value = brandsCountResult != null ? Convert.ToInt64(brandsCountResult) : 0;

                var itemsCountSql = @"
                    SELECT COUNT(*) as TotalItems
                    FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF001.PART
                    WHERE P_BRAND IS NOT NULL";

                var itemsCountResult = await service.ExecuteScalarAsync(itemsCountSql);
                totalItemsCount.Value = itemsCountResult != null ? Convert.ToInt64(itemsCountResult) : 0;

                var overallMetricsSql = @"
                    SELECT 
                        AVG(P_RETAILPRICE) as AvgPrice,
                        MIN(P_RETAILPRICE) as MinPrice,
                        MAX(P_RETAILPRICE) as MaxPrice,
                        SUM(P_RETAILPRICE) as TotalValue,
                        SUM(P_SIZE) as TotalSize,
                        AVG(P_SIZE) as AvgSize
                    FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF001.PART
                    WHERE P_BRAND IS NOT NULL";

                var overallMetricsTable = await service.ExecuteQueryAsync(overallMetricsSql);
                if (overallMetricsTable.Rows.Count > 0)
                {
                    var row = overallMetricsTable.Rows[0];
                    totalAvgPrice.Value = Convert.ToDouble(row["AvgPrice"] ?? 0);
                    totalMinPrice.Value = Convert.ToDouble(row["MinPrice"] ?? 0);
                    totalMaxPrice.Value = Convert.ToDouble(row["MaxPrice"] ?? 0);
                    totalInventoryValue.Value = Convert.ToDouble(row["TotalValue"] ?? 0);
                    totalTotalSize.Value = Convert.ToInt64(row["TotalSize"] ?? 0);
                    totalAvgSize.Value = Convert.ToDouble(row["AvgSize"] ?? 0);
                }

                totalAvgItemsPerBrand.Value = totalBrandsCount.Value > 0
                    ? totalItemsCount.Value / (double)totalBrandsCount.Value
                    : 0;

                await LoadBrandData(service);
            }
            catch (Exception ex)
            {
                errorMessageBrands.Value = $"Error loading brand data: {ex.Message}";
            }
            finally
            {
                isLoadingBrands.Value = false;
            }
        }, [isVerified, activeTab]);
        
        this.UseEffect(async () =>
        {
            if (brandData.Value.Count == 0 || activeTab.Value != 2) return;
            var service = CreateSnowflakeService(configuration, account, user, password);
            if (service == null) return;
            await LoadBrandData(service);
        }, sortBy, sortOrder, limit);

        // Helper methods for effects
        async Task<T?> TryAsync<T>(Func<Task<T>> action, string errorPrefix)
        {
            try
            {
                refreshToken.Refresh();
                return await action();
            }
            catch (Exception ex)
            {
                errorMessage.Value = $"{errorPrefix}: {ex.Message}";
                return default;
            }
            finally
            {
                refreshToken.Refresh();
            }
        }

        void ClearSelection()
        {
            selectedSchema.Value = null;
            tables.Value = new List<string>();
            selectedTable.Value = null;
            tableInfo.Value = null;
            tablePreview.Value = null;
        }

        async Task LoadStatistics(SnowflakeService service, string? database = null)
        {
            isLoadingStats.Value = true;
            errorMessage.Value = null;
            try
            {
                refreshToken.Refresh();
                int databasesCount = 0;
                int schemasCount = 0;
                int tablesCount = 0;
                int schemasAllCount = 0;
                int tablesAllCount = 0;
                
                if (database == null)
                {
                    databasesCount = databases.Value.Count;
                    foreach (var db in databases.Value)
                    {
                        try
                        {
                            var schemaList = await service.GetSchemasAsync(db);
                            schemasAllCount += schemaList.Count;
                            foreach (var sch in schemaList)
                            {
                                try
                                {
                                    var tableList = await service.GetTablesAsync(db, sch);
                                    tablesAllCount += tableList.Count;
                                }
                                catch { }
                            }
                        }
                        catch { }
                    }
                    schemasCount = schemasAllCount;
                    tablesCount = tablesAllCount;
                }
                else
                {
                    databasesCount = databases.Value.Count;
                    var schemaList = await service.GetSchemasAsync(database);
                    schemasCount = schemaList.Count;
                    foreach (var sch in schemaList)
                    {
                        try
                        {
                            var tableList = await service.GetTablesAsync(database, sch);
                            tablesCount += tableList.Count;
                        }
                        catch { }
                    }
                    schemasAllCount = totalSchemasAll.Value;
                    tablesAllCount = totalTablesAll.Value;
                }
                
                await Task.Yield();
                totalDatabases.Value = databasesCount;
                totalSchemas.Value = schemasCount;
                totalTables.Value = tablesCount;
                if (database == null)
                {
                    totalSchemasAll.Value = schemasAllCount;
                    totalTablesAll.Value = tablesAllCount;
                }
            }
            catch (Exception ex)
            {
                errorMessage.Value = $"Error loading statistics: {ex.Message}";
            }
            finally
            {
                isLoadingStats.Value = false;
                refreshToken.Refresh();
            }
        }

        async Task LoadSchemas(SnowflakeService service, string database)
        {
            isLoadingSchemas.Value = true;
            errorMessage.Value = null;
            ClearSelection();
            var schemaList = await TryAsync(() => service.GetSchemasAsync(database), "Error loading schemas");
            if (schemaList != null) schemas.Value = schemaList;
            isLoadingSchemas.Value = false;
        }

        async Task LoadTables(SnowflakeService service, string database, string schema)
        {
            isLoadingTables.Value = true;
            errorMessage.Value = null;
            selectedTable.Value = null;
            tableInfo.Value = null;
            tablePreview.Value = null;
            var tableList = await TryAsync(() => service.GetTablesAsync(database, schema), "Error loading tables");
            if (tableList != null)
            {
                tables.Value = tableList;
                totalTablesInSchema.Value = tableList.Count;
            }
            else
            {
                totalTablesInSchema.Value = 0;
            }
            isLoadingTables.Value = false;
        }

        async Task LoadTablePreview(SnowflakeService service, string database, string schema, string table)
        {
            isLoadingTableData.Value = true;
            errorMessage.Value = null;
            var info = await TryAsync(() => service.GetTableInfoAsync(database, schema, table), "Error loading table info");
            if (info != null) tableInfo.Value = info;
            var preview = await TryAsync(() => service.GetTablePreviewAsync(database, schema, table, 1000), "Error loading table preview");
            if (preview != null) tablePreview.Value = preview;
            isLoadingTableData.Value = false;
        }

        async Task LoadBrandData(SnowflakeService service)
        {
            isLoadingBrands.Value = true;
            errorMessageBrands.Value = null;
            try
            {
                refreshToken.Refresh();

                var orderByColumn = sortBy.Value switch
                {
                    "AvgPrice" => "AVG(P_RETAILPRICE)",
                    "MinPrice" => "MIN(P_RETAILPRICE)",
                    "MaxPrice" => "MAX(P_RETAILPRICE)",
                    "TotalSize" => "SUM(P_SIZE)",
                    "AvgSize" => "AVG(P_SIZE)",
                    "Brand" => "P_BRAND",
                    _ => "COUNT(*)"
                };
                
                var orderDirection = sortOrder.Value == "ASC" ? "ASC" : "DESC";
                
                var sql = $@"
                    SELECT 
                        P_BRAND as Brand,
                        COUNT(*) as ItemCount,
                        AVG(P_RETAILPRICE) as AvgPrice,
                        MIN(P_RETAILPRICE) as MinPrice,
                        MAX(P_RETAILPRICE) as MaxPrice,
                        SUM(P_SIZE) as TotalSize,
                        AVG(P_SIZE) as AvgSize
                    FROM SNOWFLAKE_SAMPLE_DATA.TPCH_SF001.PART
                    WHERE P_BRAND IS NOT NULL
                    GROUP BY P_BRAND
                    ORDER BY {orderByColumn} {orderDirection}
                    LIMIT {limit.Value}";

                var dataTable = await service.ExecuteQueryAsync(sql);

                var brands = new List<BrandStats>();
                foreach (DataRow row in dataTable.Rows)
                {
                    brands.Add(new BrandStats
                    {
                        Brand = row["Brand"]?.ToString() ?? "Unknown",
                        ItemCount = Convert.ToInt64(row["ItemCount"] ?? 0),
                        AvgPrice = Convert.ToDouble(row["AvgPrice"] ?? 0),
                        MinPrice = Convert.ToDouble(row["MinPrice"] ?? 0),
                        MaxPrice = Convert.ToDouble(row["MaxPrice"] ?? 0),
                        TotalSize = Convert.ToInt64(row["TotalSize"] ?? 0),
                        AvgSize = Convert.ToDouble(row["AvgSize"] ?? 0)
                    });
                }

                brandData.Value = brands;
                refreshToken.Refresh();
            }
            catch (Exception ex)
            {
                errorMessageBrands.Value = $"Error loading brand data: {ex.Message}";
            }
            finally
            {
                isLoadingBrands.Value = false;
            }
        }

        // ========== RENDER BASED ON ACTIVE TAB ==========
        return Layout.Horizontal().Gap(2)
            | (Layout.Vertical().Gap(2)
            | (Layout.Vertical().Gap(2)
                | BuildTabs(activeTab, isVerified.Value)).Height(Size.Fraction(0.1f))
            | (Layout.Vertical().Gap(2)
                | (activeTab.Value == 0
                    ? BuildSettingsTab(isDialogOpen, credentialsForm, verificationStatus, isVerifying, account, user, password, isVerified)
                    : activeTab.Value == 1
                    ? BuildDatabaseExplorerTab(account, user, password, isVerified, configuration, databases, selectedDatabase, schemas, selectedSchema, tables, selectedTable, tableInfo, tablePreview, dataTab, isLoadingStats, isLoadingSchemas, isLoadingTables, isLoadingTableData, errorMessage, totalDatabases, totalSchemas, totalTables, totalSchemasAll, totalTablesAll, totalTablesInSchema)
                    : BuildBrandDashboardTab(account, user, password, isVerified, configuration, brandData, totalBrandsCount, totalItemsCount, totalAvgItemsPerBrand, totalAvgPrice, totalMinPrice, totalMaxPrice, totalInventoryValue, totalTotalSize, totalAvgSize, isLoadingBrands, errorMessageBrands, sortBy, sortOrder, limit))
                ).Height(Size.Fraction(0.9f))
            );
    }

    // ========== HELPER METHODS ==========
    string BuildConnectionString(IConfiguration config, string acc, string usr, string pwd)
    {
        var warehouse = config["Snowflake:Warehouse"] ?? "";
        var database = config["Snowflake:Database"] ?? "SNOWFLAKE_SAMPLE_DATA";
        var schema = config["Snowflake:Schema"] ?? "TPCH_SF1";
        return $"account={acc};user={usr};password={pwd};warehouse={warehouse};db={database};schema={schema};";
    }

    SnowflakeService? CreateSnowflakeService(IConfiguration config, IState<string> account, IState<string> user, IState<string> password)
    {
        if (string.IsNullOrWhiteSpace(account.Value)) return null;
        return new SnowflakeService(BuildConnectionString(config, account.Value, user.Value, password.Value));
    }

    object BuildTabs(IState<int> activeTab, bool isVerified)
    {
        return Layout.Horizontal().Gap(2)
            | new Button("Settings").Icon(Icons.Settings)
                .Variant(activeTab.Value == 0 ? ButtonVariant.Primary : ButtonVariant.Outline)
                .HandleClick(() => activeTab.Value = 0)
            | new Button("Database Explorer").Icon(Icons.Database)
                .Variant(activeTab.Value == 1 ? ButtonVariant.Primary : ButtonVariant.Outline)
                .Disabled(!isVerified)
                .HandleClick(() => activeTab.Value = 1)
            | new Button("Brand Dashboard").Icon(Icons.ChartBar)
                .Variant(activeTab.Value == 2 ? ButtonVariant.Primary : ButtonVariant.Outline)
                .Disabled(!isVerified)
                .HandleClick(() => activeTab.Value = 2);
    }

    object BuildSettingsTab(
        IState<bool> isDialogOpen,
        IState<SnowflakeCredentialsRequest> credentialsForm,
        IState<string?> verificationStatus,
        IState<bool> isVerifying,
        IState<string> account,
        IState<string> user,
        IState<string> password,
        IState<bool> isVerified)
    {
        var refreshToken = this.UseRefreshToken();
        var configuration = this.UseService<IConfiguration>();
        var showSuccessMessage = isVerified.Value && verificationStatus.Value == null;
        
        return Layout.Center()
            | new Card(
                Layout.Vertical().Gap(4).Padding(4)
                | Text.H3("Getting Started with Snowflake")
                | Text.Muted("Follow these steps to configure your Snowflake connection:")
                | Layout.Vertical().Gap(3)
                    | Text.Markdown("**1. Sign up or log in** to [Snowflake](https://www.snowflake.com/)")
                    | Text.Markdown("**2. Navigate to your Account** settings")
                    | Text.Markdown("**3. Copy your Account Identifier** (e.g., `xy12345.us-east-1`)")
                    | Text.Markdown("**4. Note your Username and Password**")
                    | Text.Markdown("**5. Enter your credentials below or load from user secrets**")
                | (isVerified.Value
                    ? new Button("Clear Credentials")
                        .Icon(Icons.LogOut)
                        .Variant(ButtonVariant.Secondary)
                        .HandleClick(_ =>
                        {
                            account.Value = "";
                            user.Value = "";
                            password.Value = "";
                            isVerified.Value = false;
                            verificationStatus.Value = null;
                            refreshToken.Refresh();
                        })
                    : new Button("Enter Credentials")
                        .Icon(Icons.Key)
                        .Variant(ButtonVariant.Primary)
                        .Disabled(isVerifying.Value)
                        .HandleClick(_ =>
                        {
                            isDialogOpen.Value = true;
                            verificationStatus.Value = null;
                        }))
                | new Spacer()
                | (showSuccessMessage
                    ? new Callout("Connection verified successfully! All Snowflake apps are now available.", "Success", CalloutVariant.Success)
                    : verificationStatus.Value == "success"
                    ? new Callout("Connection successful! All Snowflake apps are now available.", "Success", CalloutVariant.Success)
                    : verificationStatus.Value == "error"
                    ? new Callout("Connection failed. Please check your credentials and try again.", "Error", CalloutVariant.Error)
                    : new Callout("Never publish credentials in public repositories or share them with unauthorized parties.", "Important Notice", CalloutVariant.Warning, Icons.TriangleAlert))
                | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Snowflake .NET Connector](https://github.com/snowflakedb/snowflake-connector-net)")
            ).Width(Size.Fraction(0.4f))
            | (isDialogOpen.Value
                ? credentialsForm.ToForm()
                    .Builder(e => e.Account, e => e.ToTextInput())
                    .Label(e => e.Account, "Account Identifier")
                    .Builder(e => e.User, e => e.ToTextInput())
                    .Label(e => e.User, "Username")
                    .Builder(e => e.Password, e => e.ToPasswordInput())
                    .Label(e => e.Password, "Password")
                    .ToDialog(isDialogOpen,
                        title: "Enter Snowflake Credentials",
                        submitTitle: isVerifying.Value ? "Verifying..." : "Save",
                        width: Size.Fraction(0.3f)
                    )
                : null);
    }

    object BuildDatabaseExplorerTab(
        IState<string> account,
        IState<string> user,
        IState<string> password,
        IState<bool> isVerified,
        IConfiguration configuration,
        IState<List<string>> databases,
        IState<string?> selectedDatabase,
        IState<List<string>> schemas,
        IState<string?> selectedSchema,
        IState<List<string>> tables,
        IState<string?> selectedTable,
        IState<TableInfo?> tableInfo,
        IState<System.Data.DataTable?> tablePreview,
        IState<int> dataTab,
        IState<bool> isLoadingStats,
        IState<bool> isLoadingSchemas,
        IState<bool> isLoadingTables,
        IState<bool> isLoadingTableData,
        IState<string?> errorMessage,
        IState<int> totalDatabases,
        IState<int> totalSchemas,
        IState<int> totalTables,
        IState<int> totalSchemasAll,
        IState<int> totalTablesAll,
        IState<int> totalTablesInSchema)
    {
        var refreshToken = this.UseRefreshToken();
        if (!isVerified.Value)
        {
            return Layout.Center()
                | new Card(
                    Layout.Vertical().Gap(4).Padding(4)
                    | Text.H3("Authentication Required")
                    | Text.Muted("Please enter your Snowflake credentials in the Settings tab.")
                ).Width(Size.Fraction(0.5f));
        }

        var snowflakeService = CreateSnowflakeService(configuration, account, user, password);
        if (snowflakeService == null) return Text.Muted("Error creating service");
        

       
        
        var hasDatabase = !string.IsNullOrEmpty(selectedDatabase.Value);
        var hasSchema = hasDatabase && !string.IsNullOrEmpty(selectedSchema.Value);
        var hasTable = hasSchema && !string.IsNullOrEmpty(selectedTable.Value);
        var isLoadingData = isLoadingStats.Value || databases.Value.Count == 0;
        
        var currentSelectedDb = selectedDatabase.Value;
        var currentSelectedSchema = selectedSchema.Value;
        var currentHasDatabase = !string.IsNullOrEmpty(currentSelectedDb);
        var currentHasSchema = currentHasDatabase && !string.IsNullOrEmpty(currentSelectedSchema);
        
        var databasesDisplayValue = currentHasDatabase ? 1 : totalDatabases.Value;
        var schemasDisplayValue = totalSchemas.Value;
        var tablesDisplayValue = totalTables.Value;
        var tablesInSchemaDisplayValue = totalTablesInSchema.Value;
        
        var metricsList = new List<object>
        {
            new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Layout.Horizontal().Gap(2).Align(Align.Center)
                        | Text.H3(databasesDisplayValue.ToString("N0"))
            ).Title(currentHasDatabase ? $"Databases: {currentSelectedDb}" : "Databases").Icon(Icons.Database)
                .Key($"databases-{totalDatabases.Value}-{currentSelectedDb ?? "none"}"),
            new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Layout.Horizontal().Gap(2).Align(Align.Center)
                        | Text.H3(schemasDisplayValue.ToString("N0"))
            ).Title(currentHasDatabase ? $"Schemas in {currentSelectedDb}" : "Schemas").Icon(Icons.Layers)
                .Key($"schemas-{totalSchemas.Value}-{totalSchemasAll.Value}-{currentSelectedDb ?? "all"}"),
            new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Layout.Horizontal().Gap(2).Align(Align.Center)
                        | Text.H3(tablesDisplayValue.ToString("N0"))
            ).Title(currentHasDatabase ? $"Tables in {currentSelectedDb}" : "Tables").Icon(Icons.Table)
                .Key($"tables-{totalTables.Value}-{totalTablesAll.Value}-{currentSelectedDb ?? "all"}")
        };
        
        if (currentHasSchema)
        {
            metricsList.Add(
                new Card(
                    Layout.Vertical().Gap(2).Padding(3)
                        | Layout.Horizontal().Gap(2).Align(Align.Center)
                            | Text.H3(tablesInSchemaDisplayValue.ToString("N0"))
                ).Title($"Tables in {currentSelectedSchema}").Icon(Icons.Table)
                    .Key($"tables-in-schema-{totalTablesInSchema.Value}-{currentSelectedSchema}")
            );
        }
        
        var shouldShowSkeleton = isLoadingStats.Value || databases.Value.Count == 0;
        object statsCards;
        if (shouldShowSkeleton)
        {
            // statsCards = BuildStatsSkeletons();
            statsCards = new Spacer();
        }
        else
        {
            var layout = Layout.Horizontal().Gap(4).Align(Align.TopCenter);
            foreach (var metric in metricsList)
            {
                layout = layout | metric;
            }
            statsCards = layout;
        }
        
        var databaseOptions = databases.Value
            .Select(db => new Option<string>(db, db))
            .Prepend(new Option<string>("-- Select Database --", ""))
            .ToArray();
        
        var schemaOptions = schemas.Value
            .Select(s => new Option<string>(s, s))
            .Prepend(new Option<string>("-- Select Schema --", ""))
            .ToArray();
        
        var tableOptions = tables.Value
            .Select(t => new Option<string>(t, t))
            .Prepend(new Option<string>("-- Select Table --", ""))
            .ToArray();
        
        // Simple SelectInput components that directly update states
        // When database changes -> schemas are loaded via existing UseEffect (line 191)
        // When schema changes -> tables are loaded via existing UseEffect (line 210)
        var formView = Layout.Vertical().Gap(3)
            | selectedDatabase.ToSelectInput(databaseOptions)
                .Placeholder("-- Select Database --")
                .WithField()
                .Label("Database")
            | selectedSchema.ToSelectInput(schemaOptions)
                .Placeholder("-- Select Schema --")
                .Disabled(isLoadingSchemas.Value || !hasDatabase)
                .WithField()
                .Label("Schema")
            | selectedTable.ToSelectInput(tableOptions)
                .Placeholder("-- Select Table --")
                .Disabled(isLoadingTables.Value || !hasSchema)
                .WithField()
                .Label("Table");
        
        var leftSection = new Card(
            Layout.Vertical().Gap(4).Padding(3)
            | Text.H2("Database Explorer")
            | Text.Muted("Select database, schema, and table (auto-loads on selection)")
            // | (isLoadingData
            //     ? BuildSkeletons(3)
            //     : formView)
                    
        ).Width(Size.Fraction(0.3f));
        
        // Show table data
        var hasLoadedTableData = tablePreview.Value != null || tableInfo.Value != null;
        
        var rightSection = new Card(
            Layout.Vertical().Gap(4).Padding(3)
            | (hasTable
                ? (isLoadingTableData.Value || !hasLoadedTableData
                    ? Layout.Vertical().Gap(4)
                        | Text.H2($"{selectedDatabase.Value}.{selectedSchema.Value}.{selectedTable.Value}")
                        | Text.Muted("Loading table data...")
                        // | BuildSkeletons(7)
                    : Layout.Vertical().Gap(3)
                        | Text.H2($"{selectedDatabase.Value}.{selectedSchema.Value}.{selectedTable.Value}")
                        | BuildDataTabs(dataTab, tableInfo.Value)
                        | (dataTab.Value == 0
                            ? (tablePreview.Value?.Rows.Count > 0
                                ? ConvertDataTableToDataTable(tablePreview.Value)
                                : Text.Muted("No data available"))
                            : (tableInfo.Value != null
                                ? Layout.Vertical().Gap(3)
                                    | Text.Muted($"{tableInfo.Value.ColumnCount} columns, {tableInfo.Value.RowCount:N0} rows")
                                    | BuildColumnsTable(tableInfo.Value.Columns)
                                : Text.Muted("No structure information available"))))
                : Layout.Vertical().Gap(4)
                    | Text.H2("Table Preview")
                    | Text.Muted("Select database, schema, and table to load data"))
                    // | (isLoadingData ? BuildSkeletons(3) : new Spacer()))
        ).Width(Size.Fraction(0.7f));

        return Layout.Vertical().Gap(2)
            | (Layout.Vertical().Gap(2).Align(Align.TopCenter)
            | Text.H1("Snowflake Database Explorer")
            | Text.Muted("Explore your Snowflake databases, schemas, and tables"))
            | statsCards
            | (errorMessage.Value != null
                ? new Card(
                    Layout.Vertical().Gap(2).Padding(2)
                        | Text.Small($"Error: {errorMessage.Value}")
                )
                : new Spacer())
            | (Layout.Horizontal().Gap(4)
                | leftSection
                | rightSection)
            | (Layout.Vertical().Gap(4).Align(Align.TopCenter)
            | Text.Small("This demo uses Snowflake to explore databases, schemas, and tables.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [Snowflake](https://www.snowflake.com/)"))
            ;
    }

    object BuildBrandDashboardTab(
        IState<string> account,
        IState<string> user,
        IState<string> password,
        IState<bool> isVerified,
        IConfiguration configuration,
        IState<List<BrandStats>> brandData,
        IState<long> totalBrandsCount,
        IState<long> totalItemsCount,
        IState<double> totalAvgItemsPerBrand,
        IState<double> totalAvgPrice,
        IState<double> totalMinPrice,
        IState<double> totalMaxPrice,
        IState<double> totalInventoryValue,
        IState<long> totalTotalSize,
        IState<double> totalAvgSize,
        IState<bool> isLoadingBrands,
        IState<string?> errorMessageBrands,
        IState<string> sortBy,
        IState<string> sortOrder,
        IState<int> limit)
    {
        var refreshToken = this.UseRefreshToken();
        if (!isVerified.Value)
        {
            return Layout.Center()
                | new Card(
                    Layout.Vertical().Gap(4).Padding(4)
                    | Text.H3("Authentication Required")
                    | Text.Muted("Please enter your Snowflake credentials in the Settings tab.")
                ).Width(Size.Fraction(0.5f));
        }

        var snowflakeService = CreateSnowflakeService(configuration, account, user, password);
        if (snowflakeService == null) return Text.Muted("Error creating service");

        var controlsHeader = Layout.Vertical().Align(Align.TopCenter)
            |(Layout.Vertical().Gap(3).Padding(3).Width(Size.Fraction(0.8f))
                | (Layout.Horizontal().Align(Align.TopCenter)
                        | sortBy.ToSelectInput(new[] { "ItemCount", "AvgPrice", "MinPrice", "MaxPrice", "TotalSize", "AvgSize", "Brand" }.ToOptions())
                            .WithField()
                            .Label("Sort by:")
                            .Width(Size.Full())
                        | sortOrder.ToSelectInput(new[] { "DESC", "ASC" }.ToOptions())
                            .WithField()
                            .Label("Sort order:")
                            .Width(Size.Full())
                        | new NumberInput<int>(limit).Min(1).Max(100)
                            .WithField()
                            .Label("Limit (number of brands):")
                            .Width(Size.Full())));

        var pageHeader = Layout.Vertical().Gap(3)
            | Layout.Horizontal().Gap(3).Width(Size.Full())
                | Layout.Vertical().Gap(1)
                    | Text.H3("Brand Analytics Dashboard")
                    | Text.Muted($"Top {limit.Value} Brands (sorted by {sortBy.Value} {sortOrder.Value})");

        if (errorMessageBrands.Value != null)
        {
            return new HeaderLayout(
                header: controlsHeader,
                content: Layout.Vertical().Gap(4).Padding(4)
                    | pageHeader
                    | new Card(
                        Layout.Vertical().Gap(2).Padding(3)
                            | Text.Small($"Error: {errorMessageBrands.Value}")
                    )
            );
        }

        // if (isLoadingBrands.Value || brandData.Value.Count == 0)
        // {
        //     return new HeaderLayout(
        //         header: controlsHeader,
        //         content: Layout.Vertical().Gap(3).Padding(4).Align(Align.TopCenter)
        //             | pageHeader.Width(Size.Fraction(0.8f))
        //             | (Layout.Grid().Columns(4).Gap(3).Width(Size.Fraction(0.8f))
        //                 | new Skeleton().Height(Size.Units(60))
        //                 | new Skeleton().Height(Size.Units(60))
        //                 | new Skeleton().Height(Size.Units(60))
        //                 | new Skeleton().Height(Size.Units(60))
        //                 | new Skeleton().Height(Size.Units(80))
        //                 | new Skeleton().Height(Size.Units(80))
        //                 | new Skeleton().Height(Size.Units(80))
        //                 | new Skeleton().Height(Size.Units(80)))
        //             | (Layout.Vertical().Gap(3).Width(Size.Fraction(0.8f))
        //                 | new Skeleton().Height(Size.Units(80)))
        //             | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(0.8f))
        //                 | new Skeleton().Height(Size.Units(80))
        //                 | new Skeleton().Height(Size.Units(80)))
        //     );
        // }

        var totalItems = brandData.Value.Sum(b => b.ItemCount);
        var avgItemsPerBrand = brandData.Value.Count > 0 ? totalItems / (double)brandData.Value.Count : 0;
        var avgPrice = brandData.Value.Count > 0 ? brandData.Value.Average(b => b.AvgPrice) : 0;
        var minPrice = brandData.Value.Count > 0 ? brandData.Value.Min(b => b.MinPrice) : 0;
        var maxPrice = brandData.Value.Count > 0 ? brandData.Value.Max(b => b.MaxPrice) : 0;
        var totalSize = brandData.Value.Sum(b => b.TotalSize);
        var avgSize = brandData.Value.Count > 0 ? brandData.Value.Average(b => b.AvgSize) : 0;
        var totalValue = brandData.Value.Sum(b => (double)b.ItemCount * b.AvgPrice);

        var filteredBrands = brandData.Value;

        double? CalculateTrend(double current, double baseline) => baseline != 0
            ? (current - baseline) / baseline
            : null;

        var expectedItemsForLoadedBrands = totalBrandsCount.Value > 0
            ? (totalItemsCount.Value / (double)totalBrandsCount.Value) * brandData.Value.Count
            : 0;
        var expectedValueForLoadedBrands = totalBrandsCount.Value > 0
            ? (totalInventoryValue.Value / (double)totalBrandsCount.Value) * brandData.Value.Count
            : 0;

        var totalItemsTrend = CalculateTrend(totalItems, expectedItemsForLoadedBrands);
        var avgItemsPerBrandTrend = CalculateTrend(avgItemsPerBrand, totalAvgItemsPerBrand.Value);
        var totalValueTrend = CalculateTrend(totalValue, expectedValueForLoadedBrands);
        var avgPriceTrend = CalculateTrend(avgPrice, totalAvgPrice.Value);
        var minPriceTrend = CalculateTrend(minPrice, totalMinPrice.Value);
        var maxPriceTrend = CalculateTrend(maxPrice, totalMaxPrice.Value);

        var expectedSizeForLoadedBrands = totalBrandsCount.Value > 0
            ? (totalTotalSize.Value / (double)totalBrandsCount.Value) * brandData.Value.Count
            : 0;
        var totalSizeTrend = CalculateTrend(totalSize, expectedSizeForLoadedBrands);
        var avgSizeTrend = CalculateTrend(avgSize, totalAvgSize.Value);

        var overallMetrics = Layout.Grid().Columns(4).Gap(3)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Layout.Horizontal().Gap(2).Align(Align.Center)
                        | Text.H3(totalItems.ToString("N0"))
            ).Title("Total Items").Icon(Icons.Database)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Layout.Horizontal().Gap(2).Align(Align.Center)
                        | Text.H3(minPrice.ToString("C2"))
            ).Title("Min Price").Icon(Icons.ArrowDown)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Layout.Horizontal().Gap(2).Align(Align.Center)
                        | Text.H3(maxPrice.ToString("C2"))
            ).Title("Max Price").Icon(Icons.ArrowUp)
            | new Card(
                Layout.Vertical().Gap(2).Padding(3)
                    | Layout.Horizontal().Gap(2).Align(Align.Center)
                        | Text.H3(totalValue.ToString("C0"))
            ).Title("Total Inventory Value").Icon(Icons.DollarSign)
            | new MetricView("Avg Price", Icons.CreditCard,
                (ctx) => ctx.UseQuery<MetricRecord, string>(
                    key: "avgPrice",
                    fetcher: async (key, ct) => new MetricRecord(
                        avgPrice.ToString("C2"),
                        avgPriceTrend,
                        totalAvgPrice.Value > 0 ? avgPrice / (totalAvgPrice.Value + totalAvgPrice.Value/2) : null,
                        $"{avgPrice:C2} loaded"
                    )))
            | new MetricView("Brands Analyzed", Icons.Tag,
                (ctx) => ctx.UseQuery<MetricRecord, string>(
                    key: "brandsAnalyzed",
                    fetcher: async (key, ct) => new MetricRecord(
                        brandData.Value.Count.ToString(),
                        null,
                        totalBrandsCount.Value > 0 ? (double)brandData.Value.Count / totalBrandsCount.Value : null,
                        $"{brandData.Value.Count} loaded of {totalBrandsCount.Value} total"
                    )))
            | new MetricView("Avg Items/Brand", Icons.ChartBar,
                (ctx) => ctx.UseQuery<MetricRecord, string>(
                    key: "avgItemsPerBrand",
                    fetcher: async (key, ct) => new MetricRecord(
                        avgItemsPerBrand.ToString("N0"),
                        avgItemsPerBrandTrend,
                        totalAvgItemsPerBrand.Value > 0 ? (double)avgItemsPerBrand / totalAvgItemsPerBrand.Value : null,
                        $"{avgItemsPerBrand:N0} loaded"
                    )))
            | new MetricView("Total Size", Icons.Box,
                (ctx) => ctx.UseQuery<MetricRecord, string>(
                    key: "totalSize",
                    fetcher: async (key, ct) => new MetricRecord(
                        totalSize.ToString("N0"),
                        totalSizeTrend,
                        totalTotalSize.Value > 0 ? (double)totalSize / totalTotalSize.Value : null,
                        $"{totalSize:N0} loaded"
                    )));

        var pieChart = filteredBrands.ToPieChart(
            dimension: b => b.Brand,
            measure: b => b.Sum(f => f.ItemCount),
            PieChartStyles.Dashboard,
            new PieChartTotal(Format.Number(@"[<1000]0;[<10000]0.0,""K"";0,""K""", filteredBrands.Sum(b => b.ItemCount)), "Total Items"));

        var barChartData = filteredBrands
            .Select(b => new { Brand = b.Brand, Count = (double)b.ItemCount })
            .ToList();

        var barChart = barChartData.ToLineChart()
            .Dimension("Brand", e => e.Brand)
            .Measure("Count", e => e.Sum(f => f.Count));

        var barChartCard = new Card(
            Layout.Horizontal().Gap(3).Padding(3)
                | pieChart
                | barChart
        ).Title("Brand Popularity");

        var minPriceChartData = filteredBrands
            .Select(b => new { Brand = b.Brand, Price = b.MinPrice })
            .ToList();

        var minPriceChart = minPriceChartData.ToBarChart()
            .Dimension("Brand", e => e.Brand)
            .Measure("Price", e => e.Sum(f => f.Price));
        
        var minPriceChartCard = new Card(
            Layout.Vertical().Gap(3).Padding(3)
                | minPriceChart
        ).Title("Min Price by Brand");

        var maxPriceChartData = filteredBrands
            .Select(b => new { Brand = b.Brand, Price = b.MaxPrice })
            .ToList();
            
        var maxPriceChart = maxPriceChartData.ToBarChart()
            .Dimension("Brand", e => e.Brand)
            .Measure("Price", e => e.Sum(f => f.Price));

        var maxPriceChartCard = new Card(
            Layout.Vertical().Gap(3).Padding(3)
                | maxPriceChart
        ).Title("Max Price by Brand");

        var content = Layout.Vertical().Gap(4).Padding(4).Align(Align.TopCenter)
            | pageHeader.Width(Size.Fraction(0.8f))
            | overallMetrics.Width(Size.Fraction(0.8f))
            | barChartCard.Width(Size.Fraction(0.8f))
            | (Layout.Grid().Columns(2).Gap(3).Width(Size.Fraction(0.8f))
                | minPriceChartCard
                | maxPriceChartCard);

        return new HeaderLayout(
            header: controlsHeader,
            content: content
        );
    }

    // ========== UI HELPER METHODS ==========
    // private object BuildSkeletons(int count)
    // {
    //     var layout = Layout.Vertical().Gap(2);
    //     foreach (var _ in Enumerable.Range(0, count))
    //     {
    //         layout = layout | new Skeleton().Height(Size.Units(24)).Width(Size.Full());
    //     }
    //     return layout;
    // }
    
    // private object BuildStatsSkeletons()
    // {
    //     var layout = Layout.Horizontal().Gap(4).Align(Align.TopCenter);
    //     foreach (var _ in Enumerable.Range(0, 3))
    //     {
    //         layout = layout | new Skeleton().Height(Size.Units(50));
    //     }
    //     return layout;
    // }
    
    private object BuildDataTabs(IState<int> activeTab, TableInfo? tableInfo)
    {
        var dataTab = new Button("Data")
            .Icon(Icons.Table)
            .Variant(activeTab.Value == 0 ? ButtonVariant.Primary : ButtonVariant.Outline)
            .HandleClick(() => activeTab.Value = 0);
        
        var structureTab = new Button("Structure")
            .Icon(Icons.Layers)
            .Variant(activeTab.Value == 1 ? ButtonVariant.Primary : ButtonVariant.Outline)
            .HandleClick(() => activeTab.Value = 1)
            .Disabled(tableInfo == null);
        
        return Layout.Horizontal().Gap(2)
            | dataTab
            | structureTab;
    }
    
    private object BuildColumnsTable(List<ColumnInfo> columns)
    {
        var key = $"columns-{columns.Count}-{string.Join("-", columns.Select(c => c.Name))}";
        return columns.AsQueryable()
            .ToDataTable()
            .Header(c => c.Name, "Column Name")
            .Header(c => c.Type, "Type")
            .Header(c => c.NullableText, "Nullable Text")
            .Width(c => c.Nullable, Size.Px(65))
            .Height(Size.Units(100))
            .Key(key);
    }
    
    private object ConvertDataTableToDataTable(System.Data.DataTable dataTable)
    {
        if (dataTable == null || dataTable.Rows.Count == 0)
        {
            return Text.Muted("No data available");
        }
        
        var columns = dataTable.Columns.Cast<DataColumn>().ToList();
        var columnCount = Math.Min(columns.Count, 20);
        
        var rows = dataTable.Rows.Cast<DataRow>().Select(row =>
        {
            var values = new string[20];
            for (int i = 0; i < columnCount; i++)
            {
                var value = row[columns[i]];
                values[i] = value == DBNull.Value ? "" : value?.ToString() ?? "";
            }
            return new DynamicRow(
                values[0], values[1], values[2], values[3], values[4],
                values[5], values[6], values[7], values[8], values[9],
                values[10], values[11], values[12], values[13], values[14],
                values[15], values[16], values[17], values[18], values[19]
            );
        }).ToList();
        
        var builder = rows.AsQueryable().ToDataTable();
        
        for (int i = 0; i < columnCount; i++)
        {
            builder = builder.Header(CreatePropertyExpression<DynamicRow, object>($"C{i}"), columns[i].ColumnName);
        }
        
        for (int i = columnCount; i < 20; i++)
        {
            builder = builder.Hidden(CreatePropertyExpression<DynamicRow, object>($"C{i}"));
        }
        
        return builder
            .Config(c =>
            {
                c.AllowSorting = true;
                c.AllowFiltering = true;
                c.ShowSearch = true;
            })
            .Height(Size.Units(190))
            .Key($"datatable-{dataTable.TableName}-{columns.Count}-{dataTable.Rows.Count}");
    }
    
    private static Expression<Func<T, TResult>> CreatePropertyExpression<T, TResult>(string propertyName)
    {
        var parameter = Expression.Parameter(typeof(T), "r");
        var property = Expression.Property(parameter, propertyName);
        var converted = typeof(TResult) == typeof(object) && property.Type != typeof(object)
            ? Expression.Convert(property, typeof(object))
            : (Expression)property;
        return Expression.Lambda<Func<T, TResult>>(converted, parameter);
    }


    private class BrandStats
    {
        public string Brand { get; set; } = "";
        public long ItemCount { get; set; }
        public double AvgPrice { get; set; }
        public double MinPrice { get; set; }
        public double MaxPrice { get; set; }
        public long TotalSize { get; set; }
        public double AvgSize { get; set; }
    }


// Typed record for DataTable widget (supports up to 20 columns)
public record DynamicRow(
    string C0, string C1, string C2, string C3, string C4,
    string C5, string C6, string C7, string C8, string C9,
    string C10, string C11, string C12, string C13, string C14,
    string C15, string C16, string C17, string C18, string C19
);
}
