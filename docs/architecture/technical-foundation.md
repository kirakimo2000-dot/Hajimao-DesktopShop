# 技术基础与项目结构

## 技术栈

| 能力 | 选择 | 放置位置 |
| --- | --- | --- |
| 桌面窗口与管理界面 | .NET 10 + WPF | `HajimaoDesktopShop.Desktop` |
| MVVM | CommunityToolkit.Mvvm 8.4.2 | Desktop 的 ViewModels |
| 2D 场景 | SkiaSharp.Views.WPF 4.150.1 | `HajimaoDesktopShop.Rendering` |
| 本地存档 | SQLite + Microsoft.Data.Sqlite 10.0.10 + SQLitePCLRaw 2.1.12 | `HajimaoDesktopShop.Infrastructure` |
| JSON 配置 | System.Text.Json（.NET 内置） | Infrastructure/Configuration 与 `config/` |
| 日志 | Serilog.Extensions.Hosting 10.0.0 | Desktop 组合根与 Infrastructure/Logging |
| 测试 | xUnit + Microsoft.NET.Test.Sdk + coverlet | `tests/` |

包版本由根目录的 `Directory.Packages.props` 集中管理。`SQLitePCLRaw.bundle_e_sqlite3` 显式固定为 2.1.12，覆盖 Microsoft.Data.Sqlite 的旧版间接依赖。Rendering/Desktop 的目标框架为 `net10.0-windows10.0.19041.0`，当前最低系统版本为 Windows 10 2004。

## 依赖方向

```text
Desktop ───────► Application ───────► Domain
   │                   ▲
   ├────► Rendering ───┤
   └────► Infrastructure
                │
                └──────────────► Domain
```

关键规则：

- Domain 是纯业务模型，不依赖 WPF、SQLite、SkiaSharp 或 Serilog。
- Application 定义用例、模拟循环、存档接口和后台调度，不直接访问数据库。
- Infrastructure 实现 Application 定义的持久化与配置接口。
- Rendering 将只读场景快照绘制为画面，不修改经营状态。
- Desktop 是组合根；窗口 code-behind 只处理窗口生命周期和原生交互。

## 目录

```text
HajimaoDesktopShop/
├─ assets/
│  ├─ audio/
│  ├─ fonts/
│  └─ sprites/
├─ config/
│  ├─ customers/
│  ├─ events/
│  ├─ furniture/
│  └─ products/
├─ docs/
│  ├─ architecture/
│  └─ superpowers/plans/
├─ src/
│  ├─ HajimaoDesktopShop.Domain/
│  │  ├─ Shops/ Products/ Inventory/ Customers/
│  │  └─ Employees/ Economy/ Events/
│  ├─ HajimaoDesktopShop.Application/
│  │  ├─ Commands/ Queries/ Simulation/
│  │  └─ Services/ SaveSystem/
│  ├─ HajimaoDesktopShop.Infrastructure/
│  │  ├─ SQLite/ Repositories/
│  │  └─ Configuration/ Logging/
│  ├─ HajimaoDesktopShop.Rendering/
│  │  ├─ Scene/ Sprites/ Animation/
│  │  └─ Camera/ Effects/
│  └─ HajimaoDesktopShop.Desktop/
│     ├─ Views/ ViewModels/ Controls/
│     ├─ Themes/ Windows/
│     └─ App.xaml
└─ tests/
   ├─ HajimaoDesktopShop.Domain.Tests/
   └─ HajimaoDesktopShop.Application.Tests/
```

## 第一阶段边界

只实现透明桌面小店窗口、经营管理窗口、一间便利店、十种商品、三种货架、顾客购买状态机、收银员与补货员、库存/进货/定价、收入/成本/利润、SQLite 自动存档以及暂停/1x/2x/4x。

商业街窗口、完整城市、多人联机、复杂物流、员工社交和剧情均不进入第一阶段。
