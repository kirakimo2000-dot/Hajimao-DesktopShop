# Hajimao Desktop Shop

Hajimao 是一款运行在 Windows 桌面角落的像素风放置经营增量游戏。小店会持续迎接顾客、选货、排队和结账；玩家可展开管理窗口进货、调价、查看员工与财务。

## Demo 功能

- 420×280 透明无边框桌面小店：拖动、角落吸附、锁定、鼠标穿透和双击展开。
- 1180×720 经营管理窗：10 种商品、3 类货架、补货、调价、员工、财务和缺货警告。
- 顾客进入、选货、排队、结账、离店完整状态机；收银员与补货员 FIFO 任务队列。
- 暂停、1x、2x、4x 经营速度。
- SQLite 每 5 秒自动存档，恢复资金、库存、售价、时间、速度、顾客、员工任务和窗口位置。
- SkiaSharp 420×180 逻辑像素场景、整数缩放、基础操作/成交音效和静音。

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

- 双击店铺场景或点击“经营”展开管理窗口。
- 点击商品行的 `补货 ×5` 安排补货员任务；售价每次调整 ¥0.10。
- 开启鼠标穿透前会先展开管理窗，避免失去恢复入口。
- 管理窗关闭时，桌面场景刷新频率自动降为每 2 秒一次；经营模拟仍按秒运行。

项目定位见 [docs/product-vision.md](docs/product-vision.md)，技术边界见 [docs/architecture/technical-foundation.md](docs/architecture/technical-foundation.md)，版本记录见 [CHANGELOG.md](CHANGELOG.md)。
