# Phase 2 开发任务列表
> [DRAFT] 本文档用于跟踪和指导Phase 2的开发工作

## 文档更新任务

### 1. 更新 requirements.md
**目标：** 将README.md中的新增需求正式纳入需求文档
**优先级：** 高
**负责人：** TBD
**时间估算：** 2天

**具体任务：**
- [ ] 添加生产容器保护机制需求（新增需求14）
- [ ] 更新交互式选择需求的描述，增加Podman命令提示要求
- [ ] 增强智能清理功能的安全性要求
- [ ] 更新项目容器智能过滤的具体要求
- [ ] 调整需求优先级，反映README.md的重点强调

**参考文档：** [../requirements/requirements-delta.md](file:///Users/zhaoyu/Downloads/coding/deck/deck-dotnet/.kiro/specs/deck-dotnet/requirements/requirements-delta.md)

### 2. 更新 design.md  
**目标：** 将架构设计与README.md描述的用户体验对齐
**优先级：** 高
**负责人：** TBD
**时间估算：** 3天

**具体任务：**
- [ ] 更新 InteractiveService 设计，增加Podman命令教育功能
- [ ] 增加生产容器保护的服务设计（ProductionContainerProtectionService？）
- [ ] 更新 CleaningService 设计，增加生产容器检测逻辑
- [ ] 完善用户体验相关的接口设计（错误提示、引导信息等）
- [ ] 更新数据模型，支持容器类型识别（dev/test/prod）

**参考文档：** [../analysis/readme-vs-design-analysis.md](file:///Users/zhaoyu/Downloads/coding/deck/deck-dotnet/.kiro/specs/deck-dotnet/analysis/readme-vs-design-analysis.md)（差异分析）

### 3. 更新 start-guide.md
**目标：** 调整开发指南，反映当前实现状态和新的优先级
**优先级：** 中
**负责人：** TBD
**时间估算：** 1天

**具体任务：**
- [ ] 更新MVP阶段的功能范围定义
- [ ] 调整开发优先级顺序
- [ ] 增加被禁用服务的处理说明
- [ ] 更新验证阶段的测试用例

## 实现任务

### Phase 2A: 解除禁用，修复核心（第1-2周）

#### 1. 启用和完善 ThreeLayerWorkflowService
**优先级：** 关键
**负责人：** TBD
**时间估算：** 3天
**依赖：** 无

- [ ] 评估当前Stub实现的功能范围
- [ ] 决定启用策略（完整启用 vs 部分功能启用）
- [ ] 实现三层配置选择的核心逻辑
- [ ] 集成到 StartCommand 中
- [ ] 编写单元测试

#### 2. 完善 ImagesUnifiedService 
**优先级：** 关键
**负责人：** TBD
**时间估算：** 4天
**依赖：** ThreeLayerWorkflowService部分功能

- [ ] 实现 GetUnifiedResourceListAsync 方法
- [ ] 实现三层资源关联映射逻辑
- [ ] 实现 GetCleaningOptionsAsync 方法  
- [ ] 集成到 ImagesCommand 中
- [ ] 编写单元测试

#### 3. 增强 InteractiveService
**优先级：** 高
**负责人：** TBD
**时间估算：** 2天
**依赖：** 无

- [ ] 实现 DisplayPodmanCommandHintAsync 方法
- [ ] 优化选择界面的输出格式
- [ ] 增加用户友好的错误处理
- [ ] 所有交互点添加Podman命令提示
- [ ] 编写交互测试

### Phase 2B: 新增需求实现（第3周）

#### 1. 生产容器保护机制
**优先级：** 高
**负责人：** TBD
**时间估算：** 3天
**依赖：** ContainerService基础功能

- [ ] 在 ContainerService 中添加容器类型检测
- [ ] 在 CleaningService 中实现生产容器保护逻辑
- [ ] 添加生产容器操作的特殊确认流程
- [ ] 提供生产容器停止的专门指导
- [ ] 编写保护机制测试

#### 2. 智能项目容器过滤增强
**优先级：** 中
**负责人：** TBD
**时间估算：** 2天
**依赖：** ContainerService基础功能

- [ ] 完善 ListProjectRelatedContainersAsync 实现
- [ ] 改进项目容器识别算法
- [ ] 优化 `deck ps` 命令的输出格式
- [ ] 编写过滤逻辑测试

#### 3. 用户体验优化
**优先级：** 中
**负责人：** TBD
**时间估算：** 2天
**依赖：** 相关命令基础实现

- [ ] 标准化所有命令的错误消息格式
- [ ] 实现统一的帮助信息样式
- [ ] 优化进度显示和状态反馈
- [ ] 增加操作确认的友好提示

## 测试任务

### 集成测试
**优先级：** 高
**负责人：** TBD
**时间估算：** 3天

- [ ] 端到端的 `deck start` 流程测试
- [ ] 三层配置选择的完整流程测试  
- [ ] 清理命令的安全性测试
- [ ] 生产容器保护的测试用例

### 用户体验测试
**优先级：** 中
**负责人：** TBD
**时间估算：** 2天

- [ ] 交互式选择的易用性测试
- [ ] 错误场景的用户引导测试
- [ ] Podman命令提示的教育效果测试

## 里程碑

### Milestone 1: 核心功能恢复（2周末）
- ThreeLayerWorkflowService 启用并可用
- ImagesUnifiedService 基本功能完成  
- `deck start` 和 `deck images` 命令可以完整工作

### Milestone 2: 新需求完成（3周末）
- 生产容器保护机制完全实现
- 所有交互式命令支持Podman命令提示
- 用户体验达到README.md描述的水平

### Milestone 3: 测试完成（4周末）  
- 所有核心功能通过集成测试
- 用户体验测试通过
- 准备发布候选版本

## 风险评估

### 高风险项
- ThreeLayerWorkflowService 的启用复杂度未知
- 生产容器保护的业务逻辑需要仔细设计

### 缓解措施
- 先做小范围验证测试
- 及时同步进度，发现问题及早调整
- 保持文档和实现的同步更新

## 决策点

### 立即需要确认：
1. ThreeLayerWorkflowService 启用策略？
2. 生产容器保护的具体规则定义？
3. MVP2 的功能范围边界？

### 建议在Phase 2A完成后确认：
1. 是否需要调整Phase 2B的优先级？
2. 用户体验优化的具体标准？