# CLAUDE.md

本檔提供 Claude Code (claude.ai/code) 在此儲存庫工作時的指引。

## 專案說明

OrderHub — 內部訂單管理 web app，作為初級 AI-agent 程式訓練系列的練習專案。**部分行為刻意不完美**（訓練活動包含重現 bug、新增功能）：勿假設既有程式正確 — 修改前依下方領域規則與 pages/tests 驗證。UI 字串、seed data、錯誤訊息皆為繁體中文；新增使用者可見文字時比照。

訓練教材位於本儲存庫上一層 `../documents/`（`README.md`、`PROCESS.md`、`activities/`、`references/`）。完整執行/設定/疑難排解指南為 `../documents/README.md`。

## 指令

於 `training-repo` 目錄（本檔所在目錄）執行。

```powershell
dotnet run --project src/OrderHub.Web     # run the site (auto-migrates + seeds on first start)
dotnet build                              # build the solution
dotnet test                               # run all xUnit tests
dotnet test --filter "FullyQualifiedName~OrderServicePricingTests"   # one test class
dotnet test --filter "FullyQualifiedName~OrderServicePricingTests.MethodName"  # one test
```

將開發資料庫重設回 seed data：

```powershell
dotnet ef database drop -f -p src/OrderHub.Infrastructure -s src/OrderHub.Web
dotnet run --project src/OrderHub.Web
```

需 .NET 8 SDK 與本機 SQL Server（連線字串於 `src/OrderHub.Web/appsettings*.json`，DB `OrderHubTraining`）。測試使用 EF Core InMemory，**不需** SQL Server。`Program.cs` 於啟動時執行 `db.Database.Migrate()` + `DbSeeder.SeedAsync`，app 自行建立 schema 與 20 customers / 50 products / 200 orders（固定 random seed）。

## 工作流程

- **任務完成前先驗證**：執行 `dotnet build` 與 `dotnet test` 並顯示輸出。兩者皆須通過。先以 `--filter` 測試迭代，最後執行完整測試一次。
- 編輯前先探索：閱讀相關 service 與其 test，確認領域規則，再修改刻意不完美的程式。
- 修 bug 時，於對應 test class 新增回歸測試（見下方 Testing）。
- 由 `main` 開分支，commit 聚焦。僅在使用者要求時 commit 或 push。

## 架構

三專案 clean-architecture 切分加測試；相依方向朝內（Web -> Core <- Infrastructure）。

- **OrderHub.Core** — domain models (`Domain/`)、service interfaces + 商業邏輯 (`Services/`)、repository interfaces (`Interfaces/`)、result wrappers `ServiceResult<T>` / `PagedResult<T>` (`Common/`)。無 EF Core 相依。discount / stock / status-transition 規則於此。
- **OrderHub.Infrastructure** — `OrderHubDbContext`、repository 實作、EF migrations、`DbSeeder`。唯一接觸資料庫的專案。
- **OrderHub.Web** — ASP.NET Core MVC：thin controllers、手動對應 ViewModels、Razor views、本機 Bootstrap 5（來自 `wwwroot/lib`，無 CDN）。DI 於 `Program.cs` 接線（所有 services/repos 註冊為 `Scoped`）。

請求流程：Controller -> `IXxxService` (Core) -> `IXxxRepository`（Core interface，Infrastructure impl）-> `OrderHubDbContext`。

### 分層慣例（新增功能時遵循）

- controllers 保持 thin；商業邏輯放入介面後的 Core service。
- 資料存取經 repository — service 或 controller 絕不直接碰 `DbContext`。
- Views 綁定 ViewModel（手動對應，通常於 controller），絕不綁定 domain model。
- 伺服器端驗證用 DataAnnotations + `ModelState`；於表單顯示錯誤。
- Action-result 訊息用 `TempData["Success"]` / `TempData["Error"]`（共用 alert 區塊於 `Views/Shared/_Layout.cshtml`）。
- 新 service/repository 註冊於 `Program.cs`。

### 須知領域規則

- **Order status**：`Pending -> Confirmed -> Shipped -> Delivered`，或 `Cancelled`。僅 `Pending`/`Confirmed` 訂單可取消；取消會回補 stock。
- **Pricing/discounts**：customer tiers 為 `Standard` (0%)、`Silver` (5%)、`Gold` (10%)。`OrderItem.UnitPriceSnapshot` 擷取下單當時價格。折扣邏輯於 `OrderService`（`GetDiscountRate`、`CalculateSubtotal`、`CalculateTotal`）— 折扣套用的位置與次數須驗證，勿假設。
- 建立訂單會驗證 customer 存在、明細非空、數量為正、product 不重複、product active 且 stock 足夠；成功時扣減 stock。

## 慣例

- C#，file-scoped namespaces、nullable enabled、implicit usings。縮排與樣式由 `.editorconfig` 強制（`.cs`/`.cshtml` 用 4-space，json/js/css 用 2-space）。
- **Line endings 須為 CRLF** — 此為 Windows 儲存庫；混合 endings 造成雜亂 diff 並可能破壞工具。

## 測試

- xUnit；`tests/OrderHub.Tests/TestSetup.cs` 提供 InMemory-context 與 service/entity factory helpers — 重用而非自行手刻設定。
- Test classes 依關注點分組：`OrderServiceCreateTests`、`OrderServiceCancelTests`、`OrderServicePricingTests`、`OrderServiceQueryTests`、`ProductServiceTests`。新測試放入對應關注點的 class。
