# Hajimao Market

Hajimao Market 是一款运行在 Windows 桌面角落的像素风放置经营增量游戏。小店会持续迎接顾客、选货、排队和结账；玩家可展开管理窗口进货、调价、查看员工与财务。

## 当前版本：0.4.0 采购与自动化

- 经营始终按现实 1x 推进，没有暂停或倍速；旧原型存档会迁移并丢弃历史速度字段。
- 玩家累计经验决定等级，等级解锁商品和新店铺；开局只有一家店。
- 多店共享业务资金，各店独立记录收入、进货成本和毛利。
- 商品具有不同进价、售价、单位毛利和毛利率；员工具有岗位、效率和精确工时费。
- 所有已开店铺在同一固定时间线上按店铺 ID 稳定 Tick；新店无需重建模拟器即可加入。
- 价格、服务、队列、整洁度和时段共同影响进店与购买，并返回可解释的整数分项。
- 收银效率决定结账耗时，清洁效率决定整洁度恢复；工资按分钟从共享资金原子结算。
- 每 1,440 个经营分钟生成逐店日结：客流、成交、流失、毛利、工资、净利润和平均队列。
- 完整保存并恢复玩家、多店、商品、员工工资余数、结账队列、整洁度、日结累计和确定性随机状态。
- 本地批发、区域配送、厂家直供分别提供即时高价、标准配送和慢速低价采购选择。
- 渠道具有固定成本倍率、最低采购量和配送时间；下单即付款，到货不重复扣款。
- 自动补货按现货与在途库存共同判断，可配置触发量、目标量、首选渠道和缺货应急采购。
- SQLite schema v4 兼容迁移旧 v1/v2/v3 存档，并完整保存采购策略、订单序号和在途状态。
- 关闭期间默认最多结算 8 小时；离线与在线复用同一逐秒 Tick 管线，没有倍速捷径。
- 系统时间倒退时不补算并返回异常标记；超出上限时明确报告封顶。
- 通知区域图标提供显示小店、打开管理和退出；桌面与管理窗口不显示普通任务栏按钮。
- 原型已有的顾客、库存、交易、SQLite 存档和 Skia 像素场景继续作为兼容适配层运行。

0.4.0 继续优先完成 Domain/Application/Infrastructure 底层。采购命令、自动化和完整存档已可供后续管理前端接入，但当前 WPF 仍运行原型单店兼容适配器；正式采购页面仍随 0.7.0 管理前端制作。

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
