# Findings & Decisions

## Requirements

- Windows 10 2004+、.NET 10、WPF、SkiaSharp、SQLite。
- 像素风传统桌面放置经营增量游戏，而非传统全屏经营游戏。
- 第一阶段必须是完整可玩 Demo，不是静态窗口或技术样例。
- Domain、Application、Infrastructure、Rendering、Desktop 分层，业务规则禁止进入窗口代码。
- 桌面窗口 420×280；管理窗口 1180×720。
- 一间便利店、十种商品、三种货架、顾客购买流程、收银员、补货员、资金与利润、自动存档。
- 每秒模拟 Tick，支持暂停/1x/2x/4x，窗口不活跃时降低渲染与模拟开销。
- 按版本迭代、维护 Changelog、阶段总结、下一步计划并清理工作区。
- 2026-08-03 产品方向变更：取消玩家可见的暂停/2x/4x经营速度；经营时间固定为现实 1x，使持续桌面挂机、长期规划和回访有真实意义。系统休眠、退出与维护状态不算玩家倍速功能。
- 2026-08-03 正式版本从 0.1.0 重新开始；此前 1.0.0 作为可运行原型历史保留。
- 玩法底层新增：初始一家店，等级解锁商品和新店；商品毛利差异；员工效率和小时工资差异。
- Windows 壳新增：通知区域常驻图标，桌面/管理窗口不在普通任务栏显示。
- 开发排序：玩法与数据模型优先，正式前端和 UI 最后制作。

## Source Brief Gap Audit (updated for v0.1.8 on 2026-08-04)

- 原始桌面文件同时描述第一阶段与中后期愿景；0.1.0～0.1.8 已增量完成双窗口、多店经营、挂机存档、采购、员工、成长、管理前端、正式像素底座与商业街 Beta，不能把原型时期的缺口继续当成当前状态。
- 用户后续决策覆盖原始速度建议：正式产品已移除玩家可见的暂停/1x/2x/4x 控件、命令和存档字段，经营固定现实 1x；原型速度记录只作为历史保留。
- 真实离线批量结算、回归报告、八小时上限、自动补货、店铺成长与三渠道供应链已经交付，挂机收益来自经营配置而不是倍速捷径。
- 管理窗已有店铺、商品、进货、员工、成长、财务和商业街七区真实页面；商业街显示共享客流、天气、路人/车辆及逐店份额，底部状态栏可收起。0.1.9 已交付七步核心经营新手任务链，剩余交互缺口主要是场景对象点击、分段动作教学和未来的深度教程润色。
- 0.1.7 已用项目专用 256×256 嵌入图集、三角色四帧、三类货架、十种商品、八类芯片提示音和品牌托盘图标补齐正式像素资产底座；资产位于 Rendering 的 `Assets/PixelArt`，构建器位于 `tools/pixel-assets`。
- 顾客当前有进店→找货→排队→结账→离开的经营状态和四帧短动作，但尚无逐货架路径、拿货/结账/离店的完整分段动作；商业街路人是有预算的环境角色，不冒充店内完整路径。
- 需求已包含价格接受度、服务、队列、整洁度、时段、成长和促销整数分项；0.1.8 增加共享路过判定、店铺吸引力加权、开店协同、天气、车辆和路人。尚未加入节庆/事故等长期街道事件。
- 多显示器基础并非完全缺失：`WindowInteractionService` 按最近显示器工作区吸附并校验虚拟桌面边界；仍缺不同排列、负坐标和混合 DPI 的系统化实机矩阵验收。
- Serilog 当前仍只有包引用，源码没有正式异常/模拟日志管线；这是原始文件尚未兑现的基础设施缺口。
- 0.1.9 已完成平衡、长时稳定、安装/便携发布、多显示器矩阵与正式日志；当前原始愿景差距主要集中在更深的场景对象交互、顾客分段动作/路径与后续经营内容。

## Research Findings

- 当前 solution 有五个生产项目和五个测试项目；已有单店经济闭环、模拟、双窗口和 JSON 商品目录。
- 当前项目已初始化本地 Git；0.1.0 在 `feature/v0.1-gameplay-foundation` 分支实现，但尚未配置远端。
- 已安装的 sprite、imagegen、产品设计和数据分析 skills 足以支撑像素资产与经济平衡工作。
- WinForms `NotifyIcon` 可满足 WPF 应用的通知区域常驻需求；Desktop 项目必须显式隔离两套 UI 命名空间。
- 0.2 多店运行时可以完全建立在 `BusinessGameService` 的命令/快照边界上，不需要让 Application 取得 Domain 聚合的可变引用。
- 需求概率、平均队列和价格指数均可使用整数基点表达，满足可解释性和确定性，无需浮点经营状态。

## Technical Decisions

| Decision | Rationale |
| --- | --- |
| `Money` 使用不可变 `long Cents` | 精确、可比较、可序列化 |
| `Shop` 作为首个经营聚合 | 原子维护现金、库存和账本一致性 |
| 操作返回状态结果而非 UI 文本 | Domain 不依赖本地化和表现层 |
| 先测试进货与销售闭环 | 它是可玩性的最小真实经济循环 |
| `ShopGameService` 为双窗口唯一经营入口 | 串行化命令并保证桌面窗与管理窗读取同一不可变快照 |
| 商品内容使用带 `schemaVersion` 的 JSON | 内容可迭代，同时为未来迁移提供明确契约 |
| 货架类别先固定为 ambient/chilled/frozen | 恰好满足 Demo 的三种货架并保持内容配置简单 |
| 每名角色每 Tick 最多一次状态转换 | 保证像素动画能观察到进入、找货、排队、结账和离开，不发生瞬移 |
| 收银与补货均使用 FIFO 队列 | 员工不逐帧扫描全店，行为确定且便于测试 |
| 模拟接受外部随机源和现实秒批量数 | 自动测试可复现，Desktop 可在后台或低活跃模式批量推进 |
| Desktop 和 Management 共用一个 `GameViewModel` 与 `ShopSceneControl` | 两个窗口的经营数字、人物状态与操作结果不会分叉 |
| 管理窗展开期间暂停桌面窗 Topmost | 保留桌面窗运行但不遮挡高密度管理操作，关闭管理窗后恢复桌面常驻 |
| WPF 正文使用 Segoe UI，金额使用 Consolas | 中文可读性优先，数字仍保持稳定等宽；像素感由硬边框、网格和场景承担 |
| 存档契约属于 Application，SQLite 只保存版本化 JSON 载荷 | 经营状态可脱离数据库测试，未来数据库/云存档适配器不影响领域和模拟 |
| 完整保存顾客、收银/补货队列和进行中任务 | 重启不是只恢复数字，而是连续恢复可观察的店铺现场和员工状态 |
| SQLite 连接关闭池化 | 自动保存频率低，换取 Windows 下存档文件可立即复制、备份和迁移 |
| Skia 渲染器消费 `SimulationSnapshot` | Rendering 不依赖 Desktop，双窗口保持同源、确定性和整数像素绘制 |
| 鼠标穿透不跨启动恢复 | 避免重启后玩家无法重新获得桌面窗交互 |
| 桌面小窗整体扁平为一个 Skia 表面 | 远程/软件合成环境中，多层 WPF 壳即使静态也会持续占用渲染线程；管理窗才保留复杂原生控件 |
| 桌面 0.5 Hz、管理窗 4 Hz 刷新 | 模拟仍每秒运行，桌面放置状态允许两秒视觉延迟，展开管理时保持即时反馈 |
| `SKElement.IgnorePixelScaling=True` | 使用 WPF 逻辑尺寸消除 150% DPI 下画面缩小与命中错位 |
| `BusinessWallet` 由所有 `Shop` 实例共享 | 开店和销售都原子改变同一资金池，逐店报表仍保留独立经营数据 |
| 等级从累计经验推导 | 存档不保存可能与曲线冲突的冗余等级，调整平衡时仍可确定重算 |
| 工资按分钟累积余数 | 只使用整数分仍能长期精确结清小时工资，不因每分钟截断而少发 |
| 托盘退出是唯一显式退出路径 | 隐藏窗口时模拟和自动存档继续，符合桌面挂机产品语义 |
| 多店 Tick 使用店铺 ID、员工 ID 序 | 新店加入不改变已有店的处理相对顺序，同输入可稳定复现 |
| 商品参考售价独立于当前售价 | 调价后仍能计算价格指数，不把上一次玩家价格误当作市场基准 |
| 工资支付先预览后提交 | 钱包扣款、工时和逐店工资成本共同成功或共同失败 |
| 日结只依赖不可变应用快照 | 0.3 可持久化或离线回放，WPF/SQLite 不反向渗入玩法层 |
| 表现帧与经营 Tick 分离 | 250 ms 动画刷新和系统减少动态效果只影响绘制，不改变固定现实 1x、离线收益或存档 |
| 正式像素资源使用单嵌入图集 | 固定逻辑资源名、显式帧区域和最近邻采样降低运行时 IO、资源扫描与跨层耦合 |
| 音效在内存中确定性生成 | 八类短 PCM 无外部媒体依赖，每个提示受 16 KiB 预算约束并由单一输出对象释放 |
| 商业街每分钟只路由一名共享访客 | 多店不再各自复制客流；开店协同提高总客流，吸引力权重决定店间分配 |
| 街道天气与可见角色从经营分钟和快照派生 | 在线、离线和恢复完全同管线，不为纯表现状态升级 schema 或增加渲染器存档 |
| 商业街渲染独立于店内渲染 | 两个场景只共享正式图集和资源预算，不共享可变状态或把 WPF 引入玩法层 |
| 店铺定义负责开店权限，商业街负责完整呈现 | `ShopDefinition`/`RetailBusiness` 保持唯一解锁权威；Application 将街区阶段提升到能容纳所有已开店铺的最低阶段，renderer 拒绝快照声明容量不足的损坏状态 |
| 正式等级曲线延伸至 Lv.10 | 原曲线最高 Lv.6 会让完整街区永远不可达；保留 Lv.1～Lv.6 阈值并追加 2,000/3,200/5,000/7,500 经验目标 |

## 0.1.9 发布候选范围发现

- 日志与稳定性阶段基线：`Serilog.Extensions.Hosting` 10.0.0 只被 Desktop 引用，源码除退出失败的 `Debug.WriteLine` 外没有正式日志；Infrastructure 当前没有 Logging 目录或 Serilog 依赖。正式实现需把抽象事件契约放在 Application、文件适配器放在 Infrastructure、配置与生命周期放在 Desktop，Domain 保持零日志依赖。
- 现有 `OfflineSettlementService` 是静态无副作用入口，`BusinessSimulation` 已可批量确定性推进；稳定性报告应组合现有不可变快照与推进 API，不在 Domain 增加测试捷径，也不复制 Tick 逻辑。
- NuGet 官方页面确认当前稳定版 `Serilog` 为 4.4.0、`Serilog.Sinks.File` 为 7.0.0；文件 sink 支持按日滚动、文件大小限制和保留数量。现有应用未使用 Generic Host，因此计划移除 Desktop 的闲置 `Serilog.Extensions.Hosting`，由 Infrastructure 封装 Serilog core + file sink，Desktop 只依赖 Application 日志契约。
- 稳定性审计发现正式接线缺口：`OfflineSettlementService` 有在线/离线一致性与默认八小时性能测试，但 `DesktopBusinessSessionFactory.Create` 恢复存档后只构造 `BusinessSession`，没有调用离线结算；全仓生产代码也无其他调用点。因此当前正式 WPF 启动不会应用关闭期间收益，README 所述离线挂机只在底层能力/测试成立。必须先写 Desktop 工厂回归测试并在恢复后调用同一离线管线，再把结果送入结构化日志。
- 现有 SQLite 测试已覆盖 v1～v6 逐级迁移、未来版本拒绝和窗口位置往返；稳定性子阶段无需重写迁移，只需增加从真实旧存档恢复后执行离线结算并重新捕获 v6 的组合测试。
- 2026-08-04 项目报告目标为桌面纯文本 `Hajimao DesktopShop项目进度与底层逻辑报告.txt`，当前不存在同名文件，可直接新建而不会覆盖用户资料。报告必须明确：远端已发布版本是 0.1.8；本地 `agent/v0.1.9-release-candidate` 已完成教程与品牌子阶段，但尚未发布、合并或打标签。
- 报告事实基线使用 README、产品定位、技术基础、路线图、task_plan、progress 与实际源码交叉核对；历史原型中的暂停/倍速不代表正式产品，正式 0.1.x 经营始终固定现实 1x。
- 领域层关键公式已从源码确认：进店基础概率 3,000 基点、购买基础概率 9,000 基点、基础收银 2 分钟；需求总分由价格、服务、队列、整洁度、时段、成长吸引力和促销修正组成并夹在 0～10,000。员工有效效率使用基础效率×培训×体力×满意度的纯整数乘法，工资以“时薪分×已工作分钟÷60”并保留 0～59 分余数精确结算。
- 单店成长上限为扩建 4、货架 5、装修 5；成本分别以 600/250/300 元为一级基数乘以目标等级平方。商业街在 Lv.1/Lv.3/Lv.5/Lv.10 解锁 1/2/4/5 个铺位，天气每 360 经营分钟按晴/多云/雨/风循环。
- 采购渠道为本地批发 1250‰、最低 1、即时；区域配送 1000‰、最低 6、30 分钟；厂家直供 850‰、最低 24、120 分钟。离线结算默认上限 28,800 秒、每批最多 10,000 秒，并调用同一 `BusinessSimulation.AdvanceRealSeconds` 管线。
- 0.1.9 新手任务是无可变状态投影：从真实快照判断进货、调价、自动补货、首笔销售、员工培训、店铺成长和第二家店七项，不增加存档字段或 schema。
- 正式内容基线：等级经验阈值为 0/40/120/300/650/1200/2000/3200/5000/7500；三店分别为 Lv.1 免费街角便利店、Lv.3 花费 800 元车站便利店、Lv.5 花费 2,000 元社区生活店。初始员工为收银员小葵（1000‰、60 元/小时）与补货员阿澄（950‰、54 元/小时）。
- 十种商品从 Lv.1～Lv.6 逐级开放，覆盖常温/冷藏/冷冻三类货架；当前配置毛利从矿泉水 0.80 元到速冻饺子 3.00 元不等。游戏存档当前 schema v6，SQLite 从 v1～v5 逐级事务迁移，数据库连接关闭池化并序列化访问。
- 每分钟 Tick 的真实顺序为：同步新店与员工 → 按店铺 ID 支付当班员工分钟工资 → 清洁与服务计算 → 收银推进 → 生成共享商业街需求并至多路由一名访客 → 顾客购买判定/入队 → 记录队列样本 → 采购到货与自动补货推进 → 促销倒计时 → 每 1,440 分钟生成逐店日结。资金不足则员工本分钟不工作并累计失败次数。
- 顾客只有在有货时随机选择一件商品，再根据价格、服务、有效队列、整洁度、时段和促销做购买判定；成功后进入 FIFO 收银队列。收银员效率把基础 2 分钟任务向上取整缩放，结账完成才实际扣库存、增加收入/毛利和玩家经验。
- 员工系统候选池固定 3 人，覆盖收银、补货、导购、清洁、店长、采购六岗位；招聘费为 40 小时时薪，默认新员工班次 08:00～16:00，常规排班最长 480 分钟且支持跨午夜。培训最高 5 级，第 N 次培训费用为时薪×N×8。
- 三种促销分别为：本地传单 150 元/240 分钟/+1200 进店；折扣券 250 元/360 分钟/+600 进店/+800 购买；节庆活动 500 元/480 分钟/+1600 进店/+600 购买，并有扩建/装修前置。同店同时只允许一个活动。
- Desktop 启动时加载 JSON 商品、SQLite 存档与窗口位置；新局自动为前三种商品走本地批发补入最多 5 件。后台模拟每秒推进一次、每 5 秒尝试自动存档；管理窗打开时画面 250 ms 刷新，仅桌面小店时 2 秒刷新，但表现刷新永不改变经营 Tick。关闭桌面窗只隐藏到通知区域，明确退出时停止循环并强制刷盘。
- 存档默认位于 `%LocalAppData%/HajimaoDesktopShop/hajimao.db`，可用 `HAJIMAO_DATA_DIRECTORY` 隔离测试/便携数据。多店共用一个 `BusinessWallet`，开店、采购、工资、升级、促销均先原子扣款，销售再把收入记回共享钱包；逐店继续独立记录收入、进货、毛利、工资与运营成本。
- Rendering 只消费不可变快照：店内逻辑画布 420×180、商业街 420×160，整数缩放、最近邻采样、关闭抗锯齿；单张 256×256 图集、角色 4 帧、最多 5 位店内顾客、6 位路人、2 辆车，图集预算 256 KiB、单音效预算 16 KiB。
- 桌面报告已生成并通过结构/编码检查：25,204 字节、644 行、22 个编号章节，严格 UTF-8 解码成功、无替换字符；版本边界、核心 Tick、架构、schema v6 和下一步计划均存在。SHA-256 为 `A412F8C6CB9D4526EA748B18FDE2E27567BE38D35172BBE22A74366A4DA636DA`。
- 2026-08-04 用户将正式项目显示名从 `Hajimao Market` 恢复为 `Hajimao DesktopShop`。当前代码中的程序集、命名空间、解决方案目录、存档目录与数据库技术标识已经使用兼容形式 `HajimaoDesktopShop`，应只统一用户可见名称、现行文档和远端仓库名，不迁移持久化身份。
- 旧显示名仍出现在 README、Desktop 程序集元数据、两个窗口标题、管理页品牌头、托盘菜单/提示、启动错误、新手任务完成文案及像素资产说明。CHANGELOG 与历史阶段/计划还包含当时的名称事实；正式迁移应新增覆盖决定，历史发布记录只在不会歪曲历史时更新。
- 现有 WPF 测试只验证窗口不进任务栏和管理结构，托盘测试只验证图标生命周期，新手任务测试反而锁定了旧完成文案；品牌迁移需要先补充可观察的产品标识、窗口标题/品牌头和托盘文本契约，再修改生产字符串。
- README 与现行资产说明应直接使用新名称；历史 CHANGELOG 需要在 Unreleased 新增覆盖决定，并将 0.1.3 条目表述为“当时采用、后续恢复”，避免改写版本历史。历史 PR 标题保持原样，仓库 URL 在远端改名后更新为新地址。
- Codex 任务标题已同步为 `Hajimao DesktopShop`。GitHub 当前远端为私有仓库 `kirakimo2000-dot/Hajimao-Market`，默认分支 `main`，`gh` 已以仓库所有者身份认证；可安全把远端仓库改名为 `Hajimao-DesktopShop` 并更新本地 origin，旧 GitHub URL 会由平台重定向。
- GitHub 仓库已成功改名为私有仓库 `kirakimo2000-dot/Hajimao-DesktopShop`；默认分支仍为 `main`，本地共享 origin 和历史 PR 文档链接已更新到新地址。
- 品牌全仓扫描后，活跃源码、测试期望、README、程序集元数据、当前资产说明和新 GitHub URL 均使用 `Hajimao DesktopShop`。旧名保留在当时采用该名称的 0.1.3～0.1.8 阶段报告/计划、真实历史 PR 标题与迁移审计记录中，属于有意保留的历史事实。
- 全方案格式失败源为 `AssemblyInfo.cs` 自 0.1.0 (`209d1a0`) 起的既有注释对齐，当前工作树对该文件无 diff；限定到本次全部已跟踪和未跟踪 C# 变更后，格式校验退出 0。品牌 diff 自检未发现版本号、schema、存档路径或业务规则改动。
- 独立品牌审阅结果为 Critical 0、Important 1、Minor 0：实现与兼容边界无缺口，唯一问题是旧阶段文档被重写成新显示名。已恢复 0.1.3～0.1.8 的当时名称，只在 0.1.9 覆盖说明和当前资料使用 `Hajimao DesktopShop`。
- 原始需求中的七步正式教程、Serilog 日志、多显示器矩阵和安装/便携发布物已在 0.1.9 完成；仍缺完整顾客分段动作与更深场景交互，后续继续限定在玩家店铺和共享商业街，不扩张为开放城市或其他商店内部模拟。
- 教程必须通过真实 Application 命令与只读进度快照观察玩家行为，不能靠 UI 私有布尔值、暂停或倍速推进。
- 平衡与长时稳定应固化为确定性场景测试和报告生成边界，优先验证新局、中期多店与八小时离线，而不是为测试添加生产捷径。
- 正式日志属于 Infrastructure 适配器与 Desktop 组合根职责；Domain/Application 只发布结构化事件或调用抽象端口，不引用 Serilog。
- 安装包、多显示器和签名验证与玩法/日志相互独立，按同一 0.1.9 版本下的后续子计划交付。
- `BusinessSession` 已是创建、恢复和完整存档捕获的唯一组合边界；若教程需要顺序进度，最稳妥的持久化位置是与 Business/Simulation 并列的 Application save record，并由 session 组合教程服务。
- `BusinessGameService` 已集中所有玩家主动经营命令，适合由薄命令观察器在成功结果后通知教程，而不是让教程反查按钮或修改 Domain 经济规则。
- SQLite 目前逐级迁移到 schema v6；0.1.9 若保存教程状态，应增加 v6→v7 迁移，同时保留 v1～v6 的逐级兼容测试。
- `WindowInteractionService` 直接依赖静态 `SystemParameters` 与 Win32 monitor API，现有恢复校验只检查虚拟桌面矩形；多显示器矩阵需要先抽出可测试的纯几何放置策略，再由 WPF 适配器提供显示器工作区。
- 当前没有 Serilog 或通用日志端口；正式日志应避免污染 Domain，并通过 Desktop 组合根把 Infrastructure logger 传给 session/autosave/simulation loop 等应用边界。
- 教程进度无需升级 schema：采购成本、当前/参考售价、自动补货策略、营业收入、员工培训级别、店铺成长和已开店数都已存在于正式快照及 v6 存档中。教程可做成无可变状态的 Application 投影，恢复旧存档时自然重建进度。
- 教程任务只返回稳定 `OnboardingTaskId` 与完成状态；中文标题、引导文字和管理页导航映射留在 Desktop ViewModel，Application 不引用 WPF 或界面枚举。
- 第一条任务应为真实进货而不是“查看页面”；完整任务链依次覆盖进货、调价、自动补货、首笔销售、培训、成长和开设第二家店，持续强化挂机经营而非速度控制。
- 0.1.9 新手任务最终架构为七项无状态 Application 投影：`RestockProduct`、`AdjustPrice`、`EnableAutoRestock`、`CompleteFirstSale`、`TrainEmployee`、`UpgradeStore`、`OpenSecondStore` 均从 schema v6 已持久化的 business/simulation/procurement 快照派生，不新增迁移、不保存教程进度、不让 Desktop 或 WPF 持有可变教程状态。
- `OnboardingSnapshot` 的公开契约要求完整七项任务集合、稳定顺序、无 null entry、无重复 ID、`CompletedTasks` 与实际完成数一致，且完成时 `CurrentTaskId` 必须为 null、未完成时必须指向第一项未完成任务。
- 新手任务审阅修复已覆盖两轮防御性缺口：先补齐前缀任务集/null entry 拒绝，再把校验测试改为完整七项数组以精确命中错误顺序、重复 ID、错误计数和错误 current 状态分支。
- Desktop 表现层仅把稳定任务 ID 映射为中文标题、引导文案和 `ManagementSection` 导航；`GoToOnboardingTaskCommand` 只切换管理页，不推进经营分钟。WPF 面板只绑定 ViewModel 属性，并通过 `AutomationProperties.Name="前往当前新手任务"` 暴露可访问按钮。

## 0.2 Demand Formula

- 进店基础概率默认 3,000 基点，购买基础概率默认 9,000 基点。
- 价格指数以当前售价/目录参考售价计算；高价同时压低进店和购买，购买阶段惩罚更强。
- 收银/导购/店长的已支付工时效率形成服务分；无人或付不起工资时服务为 0。
- 队列每增加一人都会产生显式惩罚；整洁度低于 1,000 时产生惩罚。
- 早高峰、午间、晚高峰加成分别为 800/700/1,000 基点；凌晨为 -1,500 基点。
- 最终概率统一夹在 0–10,000 基点，所有分项随快照公开。

## 0.3 Persistence And Offline Decisions

- schema v3 根存档同时保留旧 `Shop`/`Simulation` 字段和新的 `Business`/`BusinessSimulation` 字段；旧 WPF 可继续读取首店兼容投影，新底层读取完整状态。
- v1→v2→v3 必须按版本逐级、同事务升级，禁止数据库版本和 JSON 载荷版本暂时不一致。
- 员工工时恢复同时验证已工作分钟、累计工资和 0–59 分余数与小时工资完全相符。
- 随机源保存下一个输出所需的 64 位内部状态；恢复不能用当前时间重新播种。
- 已分配给未来新店的员工同样保存；未知店铺 ID 则视为损坏存档并拒绝。
- 离线结算只计算现实 UTC 整秒差并调用在线 Tick；默认上限 28,800 秒，分批大小 10,000 秒。
- 时间倒退不产生经营分钟；不足一秒的小数时间截断，不四舍五入。
- 0.3 不重写现有 WPF 页面。正式管理前端接入仍属于 0.7，避免玩法底层与临时 UI 再次耦合。

## Issues Encountered

| Issue | Resolution |
| --- | --- |
| 早期 .NET 8 构建残留 | 已用旧 TFM 的 MSBuild CoreClean 清除编译文件 |
| WPF `Application` 类型被同根命名空间遮蔽 | 使用 `System.Windows.Application` 全限定基类 |
| 损坏存档中的员工可引用未定义店铺 | 回归测试确认恢复未校验员工店铺；增加定义查询并拒绝未知 ID |
| 完整存档遗漏尚未开业店铺的预分配员工 | 动态开店回归测试复现服务效率丢失；改为保存完整员工映射而非仅枚举已开店运行时 |

## Resources

- `C:\Users\86427\Desktop\hajimao小店.txt`
- `docs/product-vision.md`
- `docs/architecture/technical-foundation.md`
- `docs/roadmap.md`

## Visual/Browser Findings

- 暂无新的视觉参考；像素风方向已记录为统一网格、整数缩放、关闭插值和高辨识轮廓。
- 2026-08-04：复核文件布局时确认 `BusinessSession` 位于 `Application/Business`，离线结算位于 `Application/Business/Offline`，桌面组合测试位于 `Desktop.Tests/Services`；后续计划与实现均以实际命名空间为准。
- 2026-08-04：`OfflineSettlementResult` 已提供 requested/applied/capped/anomaly 及结算前后现金、营收、毛利、工资、净利、销量总计，足够直接作为启动结果与结构化日志载荷，无需修改 Domain。
- 2026-08-04：现有桌面工厂测试只验证新局与无时间流逝恢复；将用显式 `nowUtc` 新增失败测试，锁定恢复时必须执行真实离线推进，同时避免测试依赖系统时钟。
- 2026-08-04：`BusinessSimulationSnapshot` 已包含全局游戏分钟、业务总览、逐店经营指标、员工、商业街和最后日结，正式稳定性审计可只比较不可变快照并调用公开批量推进，不需要测试专用入口。
- 2026-08-04：查阅旧计划时误猜了 `2026-08-03-v0.1.9-onboarding-core.md`；实际文件为 `2026-08-04-v0.1.9-onboarding.md`，已改用 `rg`/目录枚举定位。
- 2026-08-04：桌面项目已有受控 `GlobalUsings.cs`，生命周期日志组合可复用基础集合/线程类型；日志 sink 的创建、异常吞吐和释放仍由 `App` 明确负责，避免后台模拟或 ViewModel 直接依赖 Infrastructure。
- 2026-08-04：`ApplicationDataPathPolicy` 的存档与日志目录现共享同一基目录解析，因此 `HAJIMAO_DATA_DIRECTORY` 隔离配置会同时隔离数据库与日志，便于便携包和自动化验收。
- 2026-08-04：`BusinessSimulation.AdvanceRealSeconds` 明确拒绝 0，但稳定性审计需要“零时长基线报告”；审计服务会对 0 直接返回同快照/0 批次，对负值报错，不改变模拟引擎的正数推进契约。
- 2026-08-04：模拟快照的逐店运行状态已按 `StoreId` 排序，业务快照未承诺同样的呈现顺序；审计投影仍将显式按 ID 连接并排序，避免报告稳定性依赖内部集合顺序。
- 2026-08-04：收尾审阅发现日志初始化仍处于启动主 `try` 内，目录不可写会让非关键诊断阻止游戏；已改为安全降级空 sink。同时增加完整启动标记，避免启动失败路径写入“正常退出”。
- 2026-08-04：离线结算若只等待五秒自动存档，早期异常退出可能从旧 `SavedAtUtc` 重复结算；Desktop 在实际应用正数离线秒后会立即保存以启动 UTC 为时间戳的 schema v6 检查点，时钟回退不覆盖时间戳。
- 2026-08-04：最终依赖/边界复核确认 Serilog 实现引用只存在于 Infrastructure（Desktop 组合根仅构造适配器）；Domain、Rendering、ViewModel/WPF 均无日志实现依赖。发布目标仍为 0.1.9，`VersionPrefix` 按候选阶段策略暂留 0.1.8，存档 schema 保持 v6。
- 2026-08-04：最终版本复核首次误猜 `Persistence/GameSaveSchema.cs`；实际常量位于 `Persistence/GameSaveData.cs`，`CurrentVersion = 6`，已用 `rg --files` 校正。
- 2026-08-04：最终审阅 Critical 0、Important 0、Minor 0；重点复核 UTC/封顶、结算后检查点、sink 所有权/并发释放、异常降级、保留上限、报告排序/checked 差值及固定现实 1x，未发现剩余阻断项。
- 2026-08-05：多显示器基线确认 `WindowInteractionService` 仍直接混合 WPF/Win32、DPI 换算和几何判断；恢复只检查整个虚拟桌面矩形，会把“落在两块显示器之间空洞区域”的窗口误判为可见。应先抽出无 WPF 的工作区矩形/放置策略，再让 Win32 适配器提供各显示器工作区。
- 2026-08-05：仓库没有 `WindowInteractionServiceTests.cs`，现有窗口测试没有锁定负坐标、显示器空洞、拔插或混合 DPI；本阶段必须先新增纯几何 RED 测试，不能继续依赖真实机器的单一显示器配置。
- 2026-08-05：现有正式历史只生成过自包含 win-x64 目录与 ZIP，当前仓库没有安装器工程；0.1.9 需要新增可重复发布脚本与安装器定义，同时保持 `%LocalAppData%/HajimaoDesktopShop` 数据目录不被卸载删除。
- 2026-08-05：官方 WiX 文档显示 SDK-style `.wixproj` 可由 `dotnet build` 直接生成 MSI，`Files` 支持命名 bind path 与 `**` 递归收集；WiX v6 最新维护版为 6.0.2。为避免依赖机器全局安装，本项目应把 WiX SDK 精确固定在安装器工程，而不是要求全局 `wix` 命令。
- 2026-08-05：WiX 官方明确指出纯 per-user `Files` 自动收集会产生 ICE 校验问题，推荐 per-user-or-machine/per-machine-or-user；v6 支持 `perUserOrMachine`，适合默认当前用户且无需强制管理员的桌面摆件安装，同时程序数据继续写 LocalAppData 而非安装目录。
- 2026-08-05：Windows 安装包不应包含或删除 `%LocalAppData%/HajimaoDesktopShop`；MSI 只拥有 Program Files/开始菜单中的应用文件与快捷方式，因此卸载天然保留存档和日志。
- 2026-08-05：桌面小店固定 420×280 DIP、无边框且不可缩放；现有存档保存 WPF `Left/Top`，本阶段应保持 schema v6，纯几何策略以逻辑坐标接收“窗口矩形 + 每块显示器工作区”，只改变验证/夹取算法，不迁移已保存字段。
- 2026-08-05：现有恢复在虚拟桌面边界内允许最少 48 DIP 可见，但未验证具体工作区；新策略需以“任一真实工作区与窗口交集的宽高均至少 48 DIP”为可恢复条件，否则夹取到最近工作区角落。显示器列表适配与几何选择必须分离，便于覆盖负坐标、上下排列和空洞。
- 2026-08-05：`artifacts/` 已被忽略，适合只保留生成发布物；可重复发布定义与 PowerShell 脚本应放入受版本控制的 `installer/`/`scripts/`，解决方案文件可不包含 WiX 项目，避免普通应用测试/构建隐式生成安装包。
- 2026-08-05：Desktop 当前没有 app manifest 或 DPI 声明；Microsoft 文档说明 WPF 默认仅系统 DPI aware，而 `dpiAwareness=PerMonitorV2` 可在 Windows 10 1703+ 获得逐显示器 DPI 上下文。项目目标最低 Windows 10 2004，因此应新增 manifest 并同时声明 Windows 10 支持。
- 2026-08-05：.NET 10 的 WFO0003 对任何启用 WinForms 且 manifest 含 DPI 节点的项目告警；`ApplicationHighDpiMode` 生成 WinForms `ApplicationConfiguration`，但本项目由 WPF 启动且只借用 WinForms 托盘。保留 WPF 在 HWND 创建前生效的 manifest，并按 Microsoft 官方规则只抑制 WFO0003，避免重复且不会执行的 DPI 初始化来源。
- 2026-08-05：WiX v6 `Files` 的排除规则不是 `Exclude` 属性，而是子 `<Exclude Files="..." />` 元素；已在写入安装器前修正规划与契约测试，避免为错误 schema 编写实现。
- 2026-08-05：MSI 表取证确认 `Scope=perUserOrMachine` 默认设置 `ALLUSERS=2`、`MSIINSTALLPERUSER=1`，但首版 `INSTALLFOLDER` 仍指向 Program Files；自定义临时目录的冒烟会掩盖普通用户默认写入权限问题。默认二进制目录应改为 `%LocalAppData%\Programs\Hajimao DesktopShop`，与 `%LocalAppData%\HajimaoDesktopShop` 存档目录明确隔离。
- 2026-08-05：将自包含 payload 改到用户配置目录后，WiX 完整 ICE 验证按预期产生 500+ 个 ICE38/ICE64：每个用户配置文件组件都需要 HKCU KeyPath，所有递归目录也需 RemoveFile 表项。为避免压制验证或生成数百个用户注册表项，正式 MSI 采用标准 per-machine Program Files 安装；无管理员场景使用便携 ZIP，存档仍留在用户 LocalAppData 且不归 MSI 所有。
- 2026-08-05：发布构建若保留 SDK 默认 SourceRevision 信息，文档收尾提交会让同一 0.1.9 二进制的产品信息随 HEAD 改变；发布脚本显式关闭 `IncludeSourceRevisionInInformationalVersion`，版本/哈希只由发布输入决定，提交溯源由 Git 标签和 Release 负责。
- 2026-08-05：当前机器只有 `msiexec` 与 GitHub CLI；没有全局 `wix`、Inno Setup、MakeAppx 或 SignTool，亦未发现 Windows Kits bin。发布方案必须可由 NuGet 恢复的项目级 WiX SDK构建；本机无法执行 Authenticode 签名，只能生成明确标注 unsigned 的 MSI/ZIP 与 SHA-256。
- 2026-08-05：混合 DPI 不只需要几何矩阵，进程还必须在创建 UI 前声明 PerMonitorV2；manifest 属于 Desktop 组合/平台边界，不改变 Rendering 的逻辑像素、固定 420×280 DIP 或经营 Tick。
- 2026-08-05：WiX MSBuild 官方契约支持 `<BindPath Include="$(PublishDir)" BindName="PublishDir" />`，WXS 可用 `!(bindpath.PublishDir)\**` 递归收集并精确排除显式主 EXE；`ProductVersion` 等 MSBuild 属性需加入 `DefineConstants` 后才能在 WXS 中以 `$(ProductVersion)` 使用。
- 2026-08-05：发布脚本独立审阅发现 MSI 冒烟可能触碰机器上同 ProductCode 的既有安装。最终门禁改为安装前枚举所有安装上下文、安装后核对 Windows Installer 注册目录精确属于随机临时根，只有核对成功才记录卸载所有权；MSI 参数改为逐参数传递，finally 的进程、MSI、临时目录清理互不阻断。
- 2026-08-05：当前 Codex 进程不在管理员令牌中，正式 per-machine MSI 的静默安装会以 1603 拒绝。验收脚本因此区分两条诚实路径：非管理员用标准 `/a` administrative image 验证 MSI 解包内容并真实启动其中程序；GitHub Windows runner 通过 `-RequireFullMsiInstall` 强制执行 `ALLUSERS=1` 注册、目录归属核对、运行和按 ProductCode 卸载，不能退化成 per-user 安装。
- 2026-08-05：发布安全最终复审为 Critical 0、Important 0、Minor 0；确认 ProductCode 所有权在安装成功后、注册断言前建立，per-user 参数完全移除，非管理员分支不注册产品，管理员 workflow 不能退化为 `/a`。
- 2026-08-05：GitHub Actions run `30968984051` 已在 Windows 管理员 runner 通过完整 per-machine MSI 生命周期，验证 `-RequireFullMsiInstall` 未退化为本地 `/a` 分支；本机权限差距不再是发布证据缺口。
