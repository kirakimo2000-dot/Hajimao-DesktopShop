# Hajimao DesktopShop

Hajimao DesktopShop 是一款 Windows 像素风桌面挂机增量游戏。常态窗口是一条吸附在任务栏上方、始终置顶的街区；点击店面后进入该店的挂机战斗页。

## 当前版本：0.2.3

毛毛会自动把已装备的商品投向顾客，削减顾客的“需求”。在顾客离开前清空需求即可完成招待、获得现金，并随机掉落商品。商品不会消耗，也不存在补货、货架、逐笔定价、员工岗位或排班。

玩家只做三类低频决策：

- 为每家店配置 3～6 个商品槽位，平衡招待力、攻击间隔、收入倍率和特殊效果。
- 收集新商品；重复掉落会自动提升精通，长期提高威力与收益。
- 用共享现金选择下一家店。便利店均衡，折扣店客流高，精品店单客收益高但需求更强。

顾客刷新池会参考电脑的现实本地时段和当前运行事件，但游戏不显示或模拟游戏内时间。只有程序实际运行时才推进战斗，关闭后没有离线收益，也没有倍速。

## 0.2 系列内容

- 24 种可收集商品，全部可从现有顾客掉落表获得。
- 12 类顾客，具有不同需求、速度、奖励、抗性与掉落表。
- 4 个现实本地时段池和 11 种运行中事件修正。
- 24 个品牌/店铺内饰映射；当前使用轻量占位背景，后续可独立替换美术。
- 默认店员毛毛；角色、骨骼、皮肤与动作资源分离，为后续角色扩展保留接口。
- 毛毛与顾客共 10 类动作，每类严格使用 24 个逻辑帧。

## 直接运行

项目根目录始终保留最近一次成功发布的 `Hajimao DesktopShop.exe`，可直接双击启动。

自行重新生成最新便携版：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/build-portable.ps1 -Version 0.2.3 -PrunePrevious
```

成功后会生成：

- 根目录 `Hajimao DesktopShop.exe`
- `artifacts/release/0.2.3/HajimaoDesktopShop-0.2.3-win-x64-portable.zip`

便携包只有一个 EXE，解压后无需安装。存档位于当前 Windows 用户的 `LocalApplicationData/HajimaoDesktopShop/hajimao.db`。

开发启动：

```powershell
dotnet run --project src/HajimaoDesktopShop.Desktop
```

## 操作

- 新档先在三种店型中选择第一家店。
- 街区页点击店面进入该店；店铺页点击“返回街区”。
- 双击店铺战斗画面或使用店铺入口打开管理窗口。
- “挂机成果”查看本次与累计战果；“战斗策略”调整装备；“新增店铺”比较扩张选择。
- 鼠标拖动窗口；靠近任务栏会保留横向位置并吸附到任务栏上方。
- 右键菜单可锁定位置或切换鼠标穿透。

## 架构

- `HajimaoDesktopShop.Domain`：确定性战斗、商品图鉴与逐店装备规则。
- `HajimaoDesktopShop.Application`：顾客池、事件、掉落、收益、多店扩张和存档投影。
- `HajimaoDesktopShop.Rendering`：Skia 像素场景、骨骼动画、投掷物和反馈。
- `HajimaoDesktopShop.Desktop`：WPF 窗口、桌面交互与简化管理界面。
- `HajimaoDesktopShop.Infrastructure`：JSON 内容加载、SQLite 存档和诊断。

完整测试：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-all.ps1 -Configuration Release
```

项目按版本迭代，变更记录见 [CHANGELOG.md](CHANGELOG.md)，后续计划见 [docs/roadmap.md](docs/roadmap.md)。
