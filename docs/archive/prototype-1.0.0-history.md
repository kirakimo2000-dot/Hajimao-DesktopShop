# Hajimao 原型历史（正式 0.1.0 之前）

## 定位

2026-08-03 完成的原 `0.1.0`–`1.0.0` 序列属于技术与玩法原型，用于证明 .NET 10/WPF/SkiaSharp/SQLite 架构、桌面双窗口、基础经营闭环、完整现场存档和像素渲染能够工作。用户随后决定正式版本从新的 `0.1.0` 开始，因此这些编号不再属于正式产品路线。

## 原型交付

- 五层生产架构：Domain、Application、Infrastructure、Rendering、Desktop。
- 420×280 透明桌面小店与 1180×720 管理窗口。
- 10 种商品、3 类货架、顾客状态机、收银员和补货员。
- 进货、调价、销售、现金、收入、采购成本和毛利闭环。
- SQLite 5 秒自动存档及顾客/员工现场恢复。
- SkiaSharp 逻辑像素场景、系统占位音效、DPI 与性能治理。
- 原型曾包含暂停/1x/2x/4x；正式 0.1.0 将移除此设计。

## 验证证据

- 50/50 Release 测试通过：Domain 12、Application 20、Infrastructure 6、Rendering 2、Desktop 10。
- Release 构建 0 warnings / 0 errors；10 个项目无已知 NuGet 漏洞。
- 1,800 Tick 压力运行通过；150% DPI 与打包版交互通过。
- 自包含原型包：`artifacts/HajimaoDesktopShop-1.0.0-win-x64.zip`。
- SHA-256：`0A6410D057FE83E5BD36553630E9396CBD3F412A06BBA328F74E1097D47A58F4`。

## 原始阶段报告

- `docs/progress/v0.1.0-bootstrap.md`
- `docs/progress/v0.2.0-phase-2.md`
- `docs/progress/v0.3.0-phase-3.md`
- `docs/progress/v0.4.0-phase-4.md`
- `docs/progress/v0.5.0-phase-5.md`
- `docs/progress/v1.0.0-first-playable-demo.md`

这些文件是历史证据，不随正式版本重置而改写。

