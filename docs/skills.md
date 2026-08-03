# Skill 选型与使用策略

审计日期：2026-08-03

本机共发现 170 个 `SKILL.md` 文件。项目只启用与当前任务直接相关的最小组合，避免多个相似 skill 同时抢占上下文。

## 核心交付组合

| 阶段 | 首选 Skill | 使用边界 |
| --- | --- | --- |
| 需求与版本计划 | `writing-plans` | 把一个版本拆成可测试、可提交的小任务 |
| 复杂阶段跟踪 | `planning-with-files` | 仅在跨多日、跨多个子系统的阶段使用 |
| 领域模型与模拟 | `test-driven-development` | 商品、库存、经济、顾客状态机、员工队列先写失败测试 |
| 故障定位 | `systematic-debugging` | 编译、窗口、存档、模拟和性能异常必须先找根因 |
| 阶段执行 | `executing-plans` | 按已确认计划在当前会话执行 |
| 完成验证 | `verification-before-completion` | 构建、测试、漏洞审计和需求清单有新证据后才能交付 |
| 里程碑审查 | `requesting-code-review` | 每个重要版本结束前做架构与回归审查 |

## 专业游戏设计组合

| 设计工作 | 首选 Skill | 产物 |
| --- | --- | --- |
| 像素概念探索 | `product-design:ideate` + `imagegen` | 桌面小店、人物比例、货架和 UI 方向图 |
| 像素资产生产 | `game-studio:sprite-pipeline` | 统一网格、透明边界、动画帧、图集和 QA 结果 |
| 交互与可用性审查 | `product-design:audit` | 桌面角落模式、管理窗口和高频操作问题清单 |
| 创意精修 | `creative-production:produce` | 经确认方向的正式视觉资产与变体 |
| 经济平衡表 | `spreadsheets:Spreadsheets` | 价格、需求、库存、工资、成长曲线和敏感性分析 |
| 试玩数据诊断 | `data-analytics:metric-diagnostics` | 流失点、缺货率、等待时间、利润异常原因 |
| 平衡图表 | `data-analytics:visualize-data` | 收益曲线、时间墙、升级成本和资源流图表 |

`ui-ux-pro-max` 可辅助配色、间距和可访问性，但其主要知识面向 Web/移动端；采用其原则时必须重新验证 WPF、像素整数缩放和桌面常驻窗口约束。

## 不采用的候选

| 候选 | 结论 | 原因 |
| --- | --- | --- |
| OpenAI `develop-web-game` | 不安装 | 官方且质量高，但以浏览器 Canvas、Playwright 和 JavaScript hook 为核心 |
| `game-studio:phaser-2d-game` | 不使用 | Phaser 浏览器运行时与 WPF/SkiaSharp 架构不匹配 |
| `game-studio:three-webgl-game` | 不使用 | 3D WebGL，不符合 2D WPF 像素路线 |
| `game-studio:react-three-fiber-game` | 不使用 | React 3D 浏览器技术栈不匹配 |
| `game-studio:game-playtest` | 暂不直接使用 | 自动化接口依赖浏览器；其确定性步进思想可在本项目测试中借鉴 |
| 第三方 `simota/agent-skills` 的 `dot` | 不安装 | 面向 SVG、Canvas、Phaser；来源与脚本仍需单独安全审查 |
| 公共 skill 聚合站 | 不直接安装 | 索引规模不代表质量，需逐个检查许可证、脚本、网络和凭据权限 |

## 网络检索结论

- OpenAI 官方 skills 仓库提供 curated、experimental 和 system 三类来源，但本次 GitHub API 列表请求返回 HTTP 403。
- Firecrawl CLI 已安装但未认证，因此没有用它下载或安装任何内容。
- 公开搜索没有找到同时覆盖 WPF 桌面常驻、像素渲染、放置增量经济和版本化交付的成熟 skill。
- 项目专属约束继续维护在 `AGENTS.md`；不创建未经基线测试的自定义 skill。

## 安装安全门槛

安装任何新 skill 前必须完成：

1. 阅读完整 `SKILL.md` 及其直接引用文件。
2. 检查脚本是否写文件、联网、读取凭据或修改全局环境。
3. 检查许可证、维护活跃度和目标技术栈。
4. 确认它解决现有组合无法解决的具体问题。
5. 在隔离任务中验证后，才能纳入项目流程。

当前结论：现有本机 skills 已足够启动专业开发，不需要为了数量安装额外第三方包。
