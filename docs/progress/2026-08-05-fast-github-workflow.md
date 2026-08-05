# 快速 GitHub 工作流阶段报告

日期：2026-08-05  
产品版本：0.1.11（本阶段只改进开发发布工具，不提升游戏版本）

## 已完成

- 将 GitHub 操作改为阶段末集中发布，开发中只保留本地小提交，不再对每个提交执行 push/fetch。
- 新增 `scripts/publish-github-branch.ps1`：先进行最多 5 秒的网页端探测；可用时只尝试一次普通 push，不可用或 push 失败时立即切换 Git Data API。
- API 路径以二进制方式读取本地 Git blob，以无 BOM UTF-8 写入 `gh api` 标准输入，避免 Windows PowerShell 管道编码导致 HTTP 400。
- 上传后逐文件比较 blob SHA，并比较完整 tree SHA；远端基线与本地 main 不一致、分支有未提交内容或 SHA 不一致时均拒绝发布。
- 真实发布演练发现 Windows PowerShell 5.1 会把失败 push 的 stderr 提升为终止错误；现已改用隔离原生进程捕获退出码与 stderr，确保失败后能进入 API 兜底。
- API 演练同时锁定 PowerShell 5.1 泛型列表数组化缺陷；树条目现通过显式 `ToArray()` 序列化，不再触发 `Argument types do not match`。
- 新增全局 `hajimao-fast-github` Codex skill，固定草稿 PR、Windows 管理员 MSI 门禁、squash 合并、版本发布和工作区清理纪律。

## 验证证据

- TDD RED：发布脚本缺失时 3/3 新契约测试按预期失败。
- TDD GREEN：可用/不可用两种计划选择与 Git 对象完整性测试 3/3 通过。
- 回归 RED/GREEN：直接调用失败 push 会提前终止的契约先失败，改为隔离进程后 3/3 聚焦测试恢复通过。
- 回归 RED/GREEN：泛型树条目列表复现 `Argument types do not match`，显式数组化后 3/3 聚焦测试恢复通过。
- PowerShell 语法解析无错误；两种注入计划分别输出 `git` 与 `api`，探测预算均为 5 秒且普通 push 次数均为 1。
- `dotnet test HajimaoDesktopShop.slnx --no-restore`：417/417 通过。
- skill 结构校验通过，共 45 行、486 词。

## 已知限制

- GitHub Windows 管理员 MSI 门禁仍需约 4 分钟；这是对真实安装/卸载行为的必要验证，不应缩短或跳过。
- API 兜底生成的远端提交 SHA 与本地功能提交可能不同，但完整 tree 必须相同，且该分支只能作为一次性 PR 分支并经门禁 squash 合并。
- 合并后的本地 main 只在网页端探测恢复后尝试一次 fetch；持续不可用时以远端 main 为准，避免反复等待。

## 下一步计划

1. 用本阶段分支实际演练新脚本和 PR 门禁，确认网络异常时能直接完成 API 兜底。
2. 后续游戏功能继续在本地按 0.1.x 小步迭代，仅在阶段完整、测试通过后执行一次 GitHub 发布。
3. 若同类网络问题在其他项目出现，再将项目脚本抽象成通用 GitHub 发布工具；当前保持 Hajimao 专用，避免过度设计。
