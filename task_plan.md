# Task Plan: Hajimao 版本化开发

## Goal

在已交付的 Windows 桌面便利店 Demo 上，持续完成真实时间挂机、经营深度、单店成长和商业街愿景。

## Current Phase

执行 0.1.0 玩法底层：固定现实时间、玩家等级、多店铺、商品解锁/毛利、员工效率/工时费和托盘常驻。

## Phases

### Phase 1: Demo 设计与执行基线

- [x] 读取产品需求、架构、路线与现有项目
- [x] 固定 Demo 完成标准和模块边界
- [x] 保存第一批领域实现计划
- **Status:** complete

### Phase 2: 经营领域核心

- [x] 商品、货币、库存、定价、进货、销售和账本
- [x] 十种商品 JSON 配置契约
- [x] 领域与应用测试覆盖核心不变量
- **Status:** complete

### Phase 3: 模拟引擎

- [x] 1 秒 Tick 与暂停/1x/2x/4x
- [x] 顾客购买状态机
- [x] 收银员与补货员任务队列
- [x] 线程安全场景快照和后台批量模拟
- **Status:** complete

### Phase 4: 可玩双窗口

- [x] DesktopShopWindow 透明、无边框、拖动、锁定、穿透与吸附
- [x] ManagementWindow 商品、进货、员工、财务与速度控制
- [x] UI 只调用 Application 命令/查询，不持有经营规则
- **Status:** complete

### Phase 5: 存档、内容与像素表现

- [x] SQLite 自动存档与版本迁移
- [x] 十种商品、三种货架、一名收银员和一名补货员
- [x] SkiaSharp 像素场景、角色状态和基础音效
- **Status:** complete

### Phase 6: Demo 验收

- [x] 从新存档可完成进货、调价、顾客购买、结账和盈利
- [x] 1,800 Tick/30 游戏分钟压力运行无异常，桌面空闲刷新降至 0.5 Hz
- [x] 完整构建、测试、漏洞审计、资源采样和人工试玩
- [x] 更新 Changelog、版本总结、发布包并清理工作区
- **Status:** complete

### Phase 7: 玩法底层重建（0.1.0）

- [ ] 将正式版本重置为 0.1.0，原 1.0.0 作为原型历史归档
- [ ] 固定现实 1x并迁移原型存档
- [ ] 玩家等级、商品/店铺解锁、多店铺共享资金底层
- [ ] 商品毛利指标、员工效率与精确工时费
- [ ] 系统托盘图标，窗口不占外部任务栏
- **Status:** in_progress

### Phase 8: 经营决策深度（1.2.0）

- [ ] 价格、需求、队列、服务、整洁度、时段影响进店与购买
- [ ] 日结与可解释经营反馈
- **Status:** pending

### Phase 9: 单店成长与采购（1.3.0–1.4.0）

- [ ] 店铺等级、面积、货架、商品和容量解锁
- [ ] 三采购渠道、送货时间、最低采购量与自动补货
- **Status:** pending

### Phase 10: 员工经营（1.5.0）

- [ ] 员工属性、工资、体力、效率、满意度
- [ ] 导购/清洁、招聘、排班、培训与任务优先级
- **Status:** pending

### Phase 11: 桌面交互与正式内容（1.6.0–1.7.0）

- [ ] 场景对象点击、右键菜单、控件淡出、真实管理导航
- [ ] 正式 sprite、角色动作、音效、日志和长时资源验证
- **Status:** pending

### Phase 12: 装修、促销与扩建（1.8.0）

- [ ] 家具/吸引力、促销活动、相邻店铺扩建
- **Status:** pending

### Phase 13: 商业街 MVP（2.0.0）

- [ ] 底部商业街窗口、逐段解锁、共享客流与收起状态栏
- [ ] 路人、车辆和天气表现
- **Status:** pending

## 1.0 Demo 完成标准（历史）

1. 启动后显示桌面角落像素便利店，经营模拟持续运行。
2. 玩家可展开管理窗口，完成进货、调价和速度控制。
3. 顾客能进店、选货、排队、结账、离开，并真实改变库存与资金。
4. 收银员处理结账，补货员执行补货任务。
5. 关闭并重启后，SQLite 恢复资金、库存、价格、时间和员工状态。
6. Demo 包含十种商品、三种货架、收入/支出/利润反馈和缺货警告。
7. 视觉为清晰整数缩放的像素风，管理界面保持可读。

## Decisions Made

| Decision | Rationale |
| --- | --- |
| 先建立纯 Domain 经营闭环 | 后续模拟、存档和 UI 都依赖稳定规则 |
| 金额以 long 分存储 | 避免浮点误差，SQLite 与 JSON 也易于持久化 |
| UI 通过命令、查询和只读快照交互 | 保持模块化和低耦合 |
| 使用确定性随机源和显式 Tick | 便于自动化测试与离线批量模拟 |
| 当前直接在项目目录工作 | 当前目录不是 Git 仓库，无法建立 worktree |
| 1.0.0 保留为历史发布 | 已发布事实不可因新方向被改写；变更从 1.1.0 通过兼容迁移落地 |
| 固定现实 1x | 用户明确取消倍速；长期效率改由经营策略、自动化与升级提供 |
| 离线结算默认最多 8 小时 | 让关闭期间仍有积累，同时保持桌面常驻和后续离线上限升级的价值 |
| 正式版本从 0.1.0 重启 | 用户认为原版本跨度过大；旧交付改称原型历史，文件和证据保留 |
| 多店共享业务钱包 | 玩家只管理一套资金，店铺各自保留收入、采购与毛利统计 |
| 等级由累计经验与配置曲线推导 | 存档只保存累计经验，调整曲线时不会产生矛盾的等级字段 |
| 先做玩法底层，最后做正式 UI | 当前 WPF 仅作为兼容适配器，不为未完成玩法提前制作页面 |

## Errors Encountered

| Error | Attempt | Resolution |
| --- | --- | --- |
| `git status` 报告不是 Git 仓库 | 1 | 记录约束，不执行分支/worktree 操作 |
| PowerShell `rg` 占位符检查命令退出 1 | 1 | 命令字符串含反引号且无匹配；改用 `Select-String` 与显式状态处理 |
| 并行 Domain 测试争用同一个 `obj` 输出 | 1 | 相同项目的测试命令顺序执行，跨项目且无共享输出时才并行 |
| WPF `App : Application` 构建为 CS0118 | 1 | 同根 Application 命名空间发生遮蔽；使用 `System.Windows.Application` 全限定类型 |
| Win32 `LibraryImport` 触发 SYSLIB1062/CS0227 | 1 | 窗口 API 无需不安全指针，改用 `DllImport`，不开放全项目 unsafe |
| ManagementWindow 首次展开触发 XamlParseException | 1 | `Run.Text` 对只读属性推断 TwoWay；内联格式化/员工字段显式使用 OneWay |
| 差距审计读取了不存在的 `Windows/WindowPlacementService.cs` | 1 | 实际类型名为 `WindowInteractionService`；改用 `rg --files` 定位，不重复猜测路径 |
| 底层审计猜测了不存在的 `TransactionEntry.cs` 与 `ShopFinancials.cs` | 1 | 实际文件为 `Economy/LedgerEntry.cs` 与 `Shops/ShopFinancialState.cs`；已通过 `rg --files` 定位 |

## Notes

- 每完成一个阶段更新 `CHANGELOG.md` 和 `docs/progress/`。
- 每个新行为严格执行 RED → GREEN → REFACTOR。
- 不以“能编译”替代完整可玩 Demo 验收。
