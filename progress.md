# Progress Log

## 2026-08-03 · 0.1.0 玩法底层

- 正式版本重置为 0.1.0，原 1.0.0 发布事实、报告和制品归档保留。
- 固定现实 1x并完成 SQLite schema v1→v2 兼容迁移；活动模拟、快照、UI 和存档契约移除速度。
- 完成玩家等级、商品/店铺解锁、多店共享资金、逐店财务、商品毛利和员工效率/精确工时费。
- 新增 `BusinessGameService`，串联销售经验、升级解锁、开店收费和新店商品注册。
- 新增通知区域图标；桌面与管理窗口均不显示任务栏按钮，关闭桌面窗口后继续挂机。
- WinForms 引入后曾导致 WPF 类型歧义；通过 Desktop 禁用隐式 using 和显式全局 using 修复模块边界。
- Release 门禁：76/76 测试通过，构建 0 警告/0 错误，10 个项目无已知 NuGet 漏洞。
- 运行验收：Release 进程保持响应且无普通主窗口；通知区域界面无法被当前自动化定位，托盘菜单鼠标点击列为一次人工验收项。
- 阶段报告：`docs/progress/v0.1.0-gameplay-foundation.md`。
- 下一阶段为 0.2.0 多店模拟与经营深度，继续玩法/API 优先。

## 2026-08-03 · 原始需求差距审计与后续路线

- 重新读取桌面原始需求、当前规划文件、产品愿景、路线、架构、1.0 报告和关键实现。
- 确认用户最新决策覆盖原速度建议：1.1.0 起固定现实 1x，不提供玩家暂停/2x/4x。
- 代码自检确认倍速横跨 Application 模拟/快照/存档、Desktop ViewModel/XAML、Rendering 与测试，必须做 schema v1→v2 迁移，不能只隐藏按钮。
- 确认当前 1.0 是可靠的第一阶段 Demo；主要剩余差距为离线结算、需求因子、成长、三渠道采购、员工深度、桌面交互、正式素材、日志与商业街。
- 新增 `docs/audits/2026-08-03-source-brief-gap-audit.md`，逐项记录完成、部分与未完成状态。
- 更新产品时间原则、1.1.0–2.0.0 路线与 Unreleased Changelog，保留 1.0 历史记录。
- 新增 `docs/superpowers/plans/2026-08-03-fixed-time-idle-economy.md`，作为 1.1.0 的跨层执行计划。
- 审计过程中一次猜测了不存在的 WindowPlacementService 路径；已记录并改用 `rg --files` 定位实际 `WindowInteractionService`。
- 最终文档门禁通过：8 个必需文件存在、1.1.0–2.0.0 共 9 个路线版本齐全、计划中 24 个 Modify/Delete 路径有效、占位词 0；同时确认当前 1.0 源码仍有 28 处倍速相关引用，未把“计划移除”误报为“已经移除”。

## Session: 2026-08-03

### Phase 1: Demo 设计与执行基线

- **Status:** complete
- Actions taken:
  - 重新检查当前 solution 和全部产品约束。
  - 建立完整 Demo 六阶段计划与可验证完成标准。
  - 决定第一条实现闭环为商品、库存、进货、销售、资金与账本。
  - 保存 `2026-08-03-core-economy.md`，包含测试、目标 API 和完整实现步骤。
- Files created/modified:
  - `task_plan.md`
  - `findings.md`
  - `progress.md`
  - `docs/superpowers/plans/2026-08-03-core-economy.md`

### Phase 2: 经营领域核心

- **Status:** complete
- Actions taken:
  - 准备按 TDD 依次实现 Money、Product/InventorySlot 和 Shop 交易聚合。
  - Money RED 因类型缺失按预期失败；实现后 2 个 Money 测试通过。
  - Product/InventorySlot RED 因命名空间和类型缺失按预期失败；实现后 3 个测试通过。
  - Shop 交易聚合 RED 因 `Domain.Shops`、`Shop` 缺失按预期失败。
  - 实现 Shop 原子交易行为后，3 个交易测试和全部 8 个领域测试通过。
  - 完整 solution 构建为 0 警告、0 错误；所有直接与间接 NuGet 依赖均未发现已知漏洞。
  - Shop 调价入口 RED 因 `TryChangePrice` 与 `PriceChangeStatus` 缺失按预期失败。
  - 财务累计 RED 因 `TotalRevenue`、`TotalStockPurchaseCost`、`TotalGrossProfit` 缺失按预期失败。
  - 应用经营服务 RED 因 `Catalog`、`Game`、`ShopGameService` 缺失按预期失败。
  - JSON 商品目录 RED 因 `Infrastructure.Configuration` 缺失按预期失败。
  - 应用服务 3 个测试通过；JSON 目录 2 个测试通过。
  - Phase 2 最终验证：16/16 测试通过、构建 0 警告/0 错误、发行输出含 10 个唯一商品和 3 类货架、8 个项目均无已知漏洞。
  - 版本提升至 0.2.0，程序集元数据确认为 0.2.0.0；工作区杂项文件检查为 0。
- Files created/modified:
  - `tests/HajimaoDesktopShop.Domain.Tests/Economy/MoneyTests.cs`
  - `src/HajimaoDesktopShop.Domain/Economy/Money.cs`
  - `tests/HajimaoDesktopShop.Domain.Tests/Products/ProductTests.cs`
  - `tests/HajimaoDesktopShop.Domain.Tests/Inventory/InventorySlotTests.cs`
  - `src/HajimaoDesktopShop.Domain/Products/ProductId.cs`
  - `src/HajimaoDesktopShop.Domain/Products/Product.cs`
  - `src/HajimaoDesktopShop.Domain/Inventory/StockChangeStatus.cs`
  - `src/HajimaoDesktopShop.Domain/Inventory/InventorySlot.cs`
  - `tests/HajimaoDesktopShop.Domain.Tests/Shops/ShopTests.cs`
  - `src/HajimaoDesktopShop.Domain/Economy/LedgerEntryType.cs`
  - `src/HajimaoDesktopShop.Domain/Economy/LedgerEntry.cs`
  - `src/HajimaoDesktopShop.Domain/Shops/StockPurchaseResult.cs`
  - `src/HajimaoDesktopShop.Domain/Shops/SaleResult.cs`
  - `src/HajimaoDesktopShop.Domain/Shops/Shop.cs`
  - `src/HajimaoDesktopShop.Domain/Shops/PriceChangeResult.cs`
  - `tests/HajimaoDesktopShop.Application.Tests/Game/ShopGameServiceTests.cs`
  - `src/HajimaoDesktopShop.Application/Catalog/ProductDefinition.cs`
  - `src/HajimaoDesktopShop.Application/Game/ProductSnapshot.cs`
  - `src/HajimaoDesktopShop.Application/Game/ShopSnapshot.cs`
  - `src/HajimaoDesktopShop.Application/Game/ShopGameService.cs`
  - `tests/HajimaoDesktopShop.Infrastructure.Tests/HajimaoDesktopShop.Infrastructure.Tests.csproj`
  - `tests/HajimaoDesktopShop.Infrastructure.Tests/Configuration/JsonProductCatalogTests.cs`
  - `src/HajimaoDesktopShop.Application/Catalog/IProductCatalog.cs`
  - `src/HajimaoDesktopShop.Infrastructure/Configuration/JsonProductCatalog.cs`
  - `src/HajimaoDesktopShop.Desktop/Assets/Config/products.json`

## Test Results

| Test | Input | Expected | Actual | Status |
| --- | --- | --- | --- | --- |
| Money RED | `dotnet test ... --filter FullyQualifiedName~MoneyTests` | 因 `Money` 缺失而失败 | CS0234：Domain.Economy 不存在 | ✓ expected RED |
| Money GREEN | `dotnet test ... --filter FullyQualifiedName~MoneyTests` | 2 tests pass | 2 passed, 0 failed | ✓ |
| Product/Inventory RED | selected domain tests | 因类型缺失失败 | CS0234/CS0246 | ✓ expected RED |
| Product/Inventory GREEN | selected domain tests | 3 tests pass | 3 passed, 0 failed | ✓ |
| Shop RED | `dotnet test ... --filter FullyQualifiedName~ShopTests` | 因交易聚合类型缺失失败 | CS0234/CS0246 | ✓ expected RED |
| Shop GREEN | `dotnet test ... --filter FullyQualifiedName~ShopTests` | 3 tests pass | 3 passed, 0 failed | ✓ |
| Domain regression | `dotnet test tests/HajimaoDesktopShop.Domain.Tests` | 8 tests pass | 8 passed, 0 failed | ✓ |
| Solution build | `dotnet build HajimaoDesktopShop.slnx` | 0 warnings/errors | 0 warnings, 0 errors | ✓ |
| NuGet audit | `dotnet list ... --vulnerable --include-transitive` | no known vulnerabilities | all 7 projects clean | ✓ |
| Shop price RED | `dotnet test ... --filter FullyQualifiedName~ChangePrice` | 因调价 API 缺失失败 | CS1061/CS0103 | ✓ expected RED |
| Financial totals RED | `dotnet test ... --filter FullyQualifiedName~TracksFinancialTotals` | 因累计字段缺失失败 | CS1061 | ✓ expected RED |
| Application service RED | `dotnet test ... --filter FullyQualifiedName~ShopGameServiceTests` | 因应用类型缺失失败 | CS0234/CS0246 | ✓ expected RED |
| JSON catalog RED | `dotnet test tests/HajimaoDesktopShop.Infrastructure.Tests` | 因配置适配器缺失失败 | CS0234 | ✓ expected RED |
| Application GREEN | `dotnet test tests/HajimaoDesktopShop.Application.Tests` | 3 tests pass | 3 passed, 0 failed | ✓ |
| JSON catalog GREEN | `dotnet test tests/HajimaoDesktopShop.Infrastructure.Tests` | 2 tests pass | 2 passed, 0 failed | ✓ |
| Phase 2 full test | `dotnet test HajimaoDesktopShop.slnx --no-build` | 16 tests pass | 16 passed, 0 failed | ✓ |
| Phase 2 build | `dotnet build HajimaoDesktopShop.slnx` | 0 warnings/errors | 0 warnings, 0 errors | ✓ |
| Catalog output | inspect Desktop build output JSON | schema 1, 10 unique products, 3 shelves | matched | ✓ |
| Phase 2 audit | full direct/transitive audit | no known vulnerabilities | all 8 projects clean | ✓ |
| Version metadata | inspect built Domain assembly | 0.2.0.0 | 0.2.0.0 | ✓ |
| Workspace hygiene | scan outside bin/obj for tmp/bak/orig/rej | 0 junk files | 0 | ✓ |

### Phase 3: 模拟引擎

- **Status:** complete
- Next actions:
  - 确定性 1 秒 Tick 和暂停/1x/2x/4x。
  - 顾客状态机与收银员任务队列。
  - 补货员任务队列与线程安全场景快照。
- Actions taken:
  - SimulationClock RED 因 `Application.Simulation` 与速度类型缺失按预期失败。
  - 顾客模拟 RED 因随机源、顾客状态和 `ShopSimulation` 缺失按预期失败。
  - 补货员 RED 因 `QueueRestock` 与队列快照缺失按预期失败。
  - 批量模拟 RED 因 `AdvanceRealSeconds` 缺失按预期失败。
  - SimulationClock selected 5/5、顾客 selected 3/3、补货 selected 3/3、批量 selected 3/3 均通过。
  - Application 全量回归为 17 passed、0 failed。
  - Phase 3 最终门禁：30/30 测试通过、完整构建 0 警告/0 错误、8 个项目无已知漏洞。
  - Application 程序集版本确认为 0.3.0.0；工作区杂项文件为 0。
- Files created/modified:
  - `tests/HajimaoDesktopShop.Application.Tests/Simulation/SimulationClockTests.cs`
  - `src/HajimaoDesktopShop.Application/Simulation/SimulationSpeed.cs`
  - `src/HajimaoDesktopShop.Application/Simulation/SimulationClock.cs`
  - `tests/HajimaoDesktopShop.Application.Tests/Simulation/ScriptedRandomSource.cs`
  - `tests/HajimaoDesktopShop.Application.Tests/Simulation/ShopSimulationCustomerTests.cs`
  - `src/HajimaoDesktopShop.Application/Simulation/IRandomSource.cs`
  - `src/HajimaoDesktopShop.Application/Simulation/Customers/CustomerState.cs`
  - `src/HajimaoDesktopShop.Application/Simulation/Customers/CustomerSnapshot.cs`
  - `src/HajimaoDesktopShop.Application/Simulation/Employees/EmployeeRole.cs`
  - `src/HajimaoDesktopShop.Application/Simulation/Employees/EmployeeState.cs`
  - `src/HajimaoDesktopShop.Application/Simulation/Employees/EmployeeSnapshot.cs`
  - `src/HajimaoDesktopShop.Application/Simulation/SimulationSnapshot.cs`
  - `src/HajimaoDesktopShop.Application/Simulation/ShopSimulation.cs`
  - `tests/HajimaoDesktopShop.Application.Tests/Simulation/ShopSimulationRestockerTests.cs`
  - `tests/HajimaoDesktopShop.Application.Tests/Simulation/ShopSimulationBatchTests.cs`

### Phase 4: 可玩双窗口

- **Status:** complete
- Actions taken:
  - GameViewModel RED 因 Desktop ViewModels 缺失按预期失败。
  - ui-ux-pro-max 设计系统已生成，并为两个 WPF 窗口建立深色像素风页面覆盖规则。
  - SeededRandomSource RED 因 Infrastructure.Simulation 缺失按预期失败。
  - SimulationLoop RED 因 Desktop.Services 缺失按预期失败。
  - 双窗口首次构建因 `LibraryImport` 生成代码要求 unsafe 报 SYSLIB1062/CS0227；改用无需 unsafe 的 `DllImport` 窗口互操作签名。
  - 第二次构建因 WPF 临时项目未隐式导入 `System.IO` 报 `Path` 缺失；在组合根显式引用。
  - 首次真实展开管理窗因 `Run.Text` 向只读格式化属性建立 TwoWay 绑定而崩溃；所有内联只读文本显式改为 OneWay。
  - 第二次启动成功展开管理窗并真实提交“矿泉水补货 ×5”；状态提示与采购支出均更新。
  - 视觉 QA 发现 Topmost 桌面窗遮挡管理窗左上角；管理窗打开期间临时取消 Topmost，关闭后恢复。
  - 视觉 QA 发现 4x 被静态高亮会误导当前速度；补充 `CurrentSpeedText` RED/GREEN，并取消假选中样式。
  - 真实操作验证补货、调价、2x 速度和鼠标穿透开关；管理窗打开时桌面窗不再遮挡。
  - 最终门禁：35/35 测试通过，完整 solution 构建 0 警告、0 错误，9 个项目无已知漏洞。
  - Desktop 程序集版本确认为 0.4.0.0；产品配置正确复制，工作区杂项文件检查为 0。

### Phase 5: 存档、内容与像素表现

- **Status:** complete
- Actions taken:
  - Shop 恢复 RED/GREEN：无交易重放恢复现金、财务累计和初始库存，领域测试增至 12。
  - 完整模拟存档 RED/GREEN：售价、库存、时间、速度、顾客、收银队列、补货队列、员工进行中任务均可往返。
  - SQLite RED/GREEN：覆盖新建数据库 v0→v1 迁移、事务往返、窗口位置和未来版本拒绝。
  - Windows 文件句柄测试暴露默认连接池持有 `hajimao.db`；低频存档连接改为 `Pooling=False`。
  - 自动保存协调器 RED/GREEN：重叠定时保存被合并，退出刷新始终等待并捕获最新状态。
  - 桌面窗口恢复坐标与锁定状态；越界位置回退角落，穿透状态固定恢复为关闭。
  - 新增独立 Rendering 测试项目和 SkiaSharp 420×180 逻辑像素渲染器；场景使用整数倍缩放和无抗锯齿绘制。
  - 新增可替换音效输出、补货/调价/成交反馈以及管理窗静音控制。
  - 实机存档往返：将矿泉水改为 ¥1.90、速度设为 2x，等待自动存档后退出；重启明确恢复 ¥1.90、当前 2x、资金、时间与现场角色。
  - v0.5 最终门禁：44/44 测试通过、构建 0 警告/0 错误、10 个项目无已知漏洞。
  - Desktop 程序集版本确认为 0.5.0.0；输出含 10 商品/3 货架，工作区杂项文件为 0。

### Phase 6: Demo 验收

- **Status:** complete
- Actions taken:
  - 新增从新局完成补货、调价、顾客购买、收银结账和产生毛利的完整验收测试。
  - 新增 1,800 Tick/30 游戏分钟连续顾客压力测试，顾客数、库存、资金和销售保持边界有效。
  - 桌面单窗刷新降至 0.5 Hz，管理窗展开时恢复 4 Hz；模拟循环始终保持每秒运行。
  - Release 初始资源采样发现可见桌面窗 30 秒约消耗 14.8 CPU 秒；系统化排除模拟、SQLite、Skia、透明窗和刷新定时器。
  - 根因定位为当前远程/软件合成环境下的多层 WPF 桌面壳；改为单一完整 Skia 表面和三个透明可访问命中区域。
  - 扁平化后同环境 30 秒 CPU 降至 5.70 秒（约降低 62%），峰值工作集约 156 MB，进程始终响应。
  - 修复 150% DPI 逻辑/物理像素错位和 ToolTip 白底白字；重新验证经营、锁定、穿透和恢复交互。
  - 最终 Release 门禁：50/50 测试通过、构建 0 警告/0 错误、10 项目无已知漏洞。
  - 生成 Windows x64 自包含目录与 ZIP；程序集 1.0.0.0，ZIP 含 446 项、大小 102,794,982 字节。
  - ZIP SHA-256：`0A6410D057FE83E5BD36553630E9396CBD3F412A06BBA328F74E1097D47A58F4`。
  - 工作区检查：0 个诊断钩子、0 个临时/备份/补丁杂项文件、0 个残留运行实例。

## Error Log

| Timestamp | Error | Attempt | Resolution |
| --- | --- | --- | --- |
| 2026-08-03 | 当前目录不是 Git 仓库 | 1 | 继续在现有目录工作并记录限制 |
| 2026-08-03 | `rg` 占位符检查命令退出 1 | 1 | 改用 PowerShell `Select-String` 检查 |
| 2026-08-03 | 并行测试同时写入 Domain `obj` 导致 CS2012 文件锁 | 1 | 同一项目的 selected/full 测试改为顺序执行 |
| 2026-08-03 | 全量测试通过后 WPF build 报 CS0118：`Application` 被当作命名空间 | 1 | 根因是同根 `HajimaoDesktopShop.Application` 遮蔽 WPF 类型；将 App 基类全限定为 `System.Windows.Application` |
| 2026-08-03 | 读取预期的 `docs/progress/v0.1.0-phase-1.md` 失败 | 1 | 实际文件名为 `v0.1.0-bootstrap.md`；后续先用 `rg --files` 定位 |

## 5-Question Reboot Check

| Question | Answer |
| --- | --- |
| Where am I? | 第一阶段完整可玩 Demo 1.0.0 已完成 |
| Where am I going? | 等待真实试玩反馈，再规划 1.1 内容与成长系统 |
| What's the goal? | Windows 桌面像素便利店完整可玩 Demo |
| What have I learned? | 桌面常驻窗的渲染架构必须针对远程/软件合成优化；单表面渲染比堆叠 WPF 控件稳定得多 |
| What have I done? | 完成并打包 1.0.0 第一阶段完整可玩 Demo |
