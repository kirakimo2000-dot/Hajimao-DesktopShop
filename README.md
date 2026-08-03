# Hajimao Desktop Shop

Hajimao 是一款运行在 Windows 桌面角落的像素风放置经营增量游戏。小店会持续迎接顾客、选货、排队和结账；玩家可展开管理窗口进货、调价、查看员工与财务。

## 当前版本：0.1.0 玩法底层

- 经营始终按现实 1x 推进，没有暂停或倍速；旧原型存档会迁移并丢弃历史速度字段。
- 玩家累计经验决定等级，等级解锁商品和新店铺；开局只有一家店。
- 多店共享业务资金，各店独立记录收入、进货成本和毛利。
- 商品具有不同进价、售价、单位毛利和毛利率；员工具有岗位、效率和精确工时费。
- 通知区域图标提供显示小店、打开管理和退出；桌面与管理窗口不显示普通任务栏按钮。
- 原型已有的顾客、库存、交易、SQLite 存档和 Skia 像素场景继续作为兼容适配层运行。

0.1.0 先完成 Domain/Application 玩法底层。等级、多店和新员工模型尚未接入正式管理页面；管理前端计划在 0.7.0 制作。

## 运行

开发环境要求 Windows 10 2004+ 与 .NET 10 SDK：

```powershell
dotnet restore
dotnet build HajimaoDesktopShop.slnx
dotnet test HajimaoDesktopShop.slnx
dotnet run --project src/HajimaoDesktopShop.Desktop
```

发布包生成后，直接运行 `HajimaoDesktopShop.Desktop.exe`。存档位于当前 Windows 用户的 `LocalApplicationData/HajimaoDesktopShop/hajimao.db`。

## 操作提示

- 双击店铺场景、点击“经营”或使用通知区域菜单展开管理窗口。
- 点击商品行的 `补货 ×5` 安排补货员任务；售价每次调整 ¥0.10。
- 开启鼠标穿透前会先展开管理窗，避免失去恢复入口。
- 关闭桌面小店只会隐藏窗口，经营模拟和自动存档继续运行；需要从通知区域菜单明确退出程序。

项目定位见 [docs/product-vision.md](docs/product-vision.md)，技术边界见 [docs/architecture/technical-foundation.md](docs/architecture/technical-foundation.md)，版本记录见 [CHANGELOG.md](CHANGELOG.md)。
