# Phase1 阻塞性问题修复任务清单

## 🎯 Phase1 目标
基于 `validation-gap-analysis.md` 识别的阻塞性问题，优先修复核心服务被禁用问题，确保 deck-dotnet 第一阶段功能完整可用。

## 📊 当前状态评估
- **实现完成度**: ~80%
- **主要问题**: 核心功能已实现但服务被禁用，基础功能存在缺口
- **解决关键**: 修复数据模型一致性问题，实现缺失的基础功能
- **预期效果**: 解决阻塞性问题后，完成度可提升至90%+

---

## 🛠 完成下方的任务列表需要强制遵循的规范

### 📋 工作流程
1. **任务规划**：根据本任务清单开展工作
2. **分支管理**：
   - 每个任务创建独立的Git分支
   - 分支命名规范: `feature/<task-number-description>`
3. **需求分析**：
   - 仔细阅读需求文档、设计文档
   - 仔细阅读已有代码实现
4. **开发策略**：
   - 根据任务特性决定开发顺序：
     - 先实现功能
     - 或先编写测试用例
     - 自己决定顺序，但是一定要根据验收标准写符合要求的测试用例
5. **代码实现**：完成符合需求的功能开发
6. **质量保证**：
   - 所有代码必须通过测试
   - 包括单元测试和集成测试
7. **文档更新**：更新本文档的任务状态为完成，更新 `/Users/zhaoyu/Downloads/coding/deck/deck-dotnet/.kiro/specs/deck-dotnet/tasks.md`内对应的完成状态（假如有需要更新的）
8. **代码集成**：
   - 完成后提交代码
   - 合并到develop分支
9. **任务迭代**：继续规划下一个任务

### 📈 任务执行策略
- **优先级排序**：先解决阻塞性问题，再完善核心功能，最后处理优化项目
- **时间管理**：为每个任务预估时间，保持开发进度可控
- **质量保障**：每个任务完成后进行充分测试，保持代码质量
- **文档同步**：及时更新文档，确保实现与文档一致

### ✅ 成功标准
- 所有任务通过验收标准验证
- 代码通过单元测试和集成测试
- 功能符合需求文档要求
- 不引入新的阻塞性问题

---

## 🔴 阻塞性问题（高优先级）

### 1. 修复 ServiceCollectionExtensions.cs 中的模型不一致问题
**状态**: ✅ 已完成  
**优先级**: 🔥 最高  
**问题编号**: validation-gap-analysis.md 问题 #1  
**位置**: [ServiceCollectionExtensions.cs](file:///Users/zhaoyu/Downloads/coding/deck/deck-dotnet/src/Deck.Services/ServiceCollectionExtensions.cs):47-48

**问题描述**:
- ContainerService.cs（1000+行完整实现）被禁用
- ThreeLayerWorkflowService 只有桩实现，需要完整实现（而非已有完整实现被禁用）
- 原因标注为"模型不一致问题"

**任务清单**:
- ✅ 1.1 分析模型不一致的具体原因
  - ✅ 检查 ContainerService 的依赖接口定义
  - ✅ 检查 ThreeLayerWorkflowService 的依赖接口定义  
  - ✅ 识别数据模型冲突点
- ✅ 1.2 修复数据模型不一致问题
  - ✅ 统一容器相关数据模型定义
  - ✅ 修复接口签名不匹配问题
  - ✅ 确保依赖注入兼容性
- ✅ 1.3 实现完整的 ThreeLayerWorkflowService
  - ✅ 创建完整的 ThreeLayerWorkflowService 实现替代 ThreeLayerWorkflowServiceStub
  - ✅ 确保与 ContainerService 正确集成
- ✅ 1.4 重新启用核心服务
  - ✅ 取消注释 ContainerService 注册（第47行）
  - ✅ 更新 ThreeLayerWorkflowService 注册为完整实现（第45行）
  - ✅ 验证服务正常启动
- ✅ 1.5 验证功能完整性
  - ✅ 测试容器管理功能
  - ✅ 测试三层工作流程功能
  - ✅ 确保无运行时错误

**验收标准**:
- ✅ ContainerService 和 ThreeLayerWorkflowService 正常注册和启动
- ✅ 需求3（三层配置）和需求4（容器管理）的核心验收标准可以执行
- ✅ 无数据模型冲突相关的编译或运行时错误

---

### 2. 创建完整的 StartCommand 实现
**状态**: ✅ 已完成  
**优先级**: 🔥 高  
**问题编号**: validation-gap-analysis.md 问题 #2  
**问题**: 缺少完整的 `StartCommand.cs` 命令实现，`StartCommandServiceSimple.cs` 只实现了部分服务功能

**任务清单**:
- ✅ 2.1 分析 StartCommandServiceSimple.cs 的功能范围
  - ✅ 识别已实现的功能特性
  - ✅ 找出缺失的核心功能
  - ✅ 确定完整 StartCommand 需要的接口定义
- ✅ 2.2 设计完整的 StartCommand 架构
  - ✅ 定义 StartCommand 类结构（位于 [Deck.Console/Commands](file:///Users/zhaoyu/Downloads/coding/deck/deck-dotnet/src/Deck.Console/Commands) 目录）
  - ✅ 设计三层配置选择流程
  - ✅ 集成交互式选择服务
- ✅ 2.3 实现完整的 StartCommand.cs
  - ✅ 实现核心三层配置选择界面
  - ✅ 集成端口冲突检测和解决
  - ✅ 实现智能环境检测和推荐
  - ✅ 添加 Templates 双工作流程选择
- ✅ 2.4 集成和测试
  - ✅ 在 Program.cs 中集成新的 StartCommand
  - ✅ 替换或扩展 StartCommandServiceSimple 的使用
  - ✅ 创建单元测试覆盖

**验收标准**:
- ✅ 需求1标准1-2（核心三层配置选择界面）完全实现
- ✅ Start 命令支持完整的三层配置选择流程
- ✅ 集成所有相关服务（端口检测、交互式选择等）

---

### 3. 实现模板变量替换引擎
**状态**: [ ] 未开始  
**优先级**: 🔥 高  
**问题编号**: validation-gap-analysis.md 问题 #3  
**问题**: 缺失 `${VAR}` 和 `{{VAR}}` 格式的变量替换引擎

**任务清单**:
- [ ] 3.1 设计变量替换引擎架构
  - [ ] 定义 ITemplateVariableEngine 接口
  - [ ] 设计变量格式解析规则（${VAR} 和 {{VAR}}）
  - [ ] 设计变量上下文管理
- [ ] 3.2 实现 TemplateVariableEngine 核心功能
  - [ ] 实现变量模式匹配和解析
  - [ ] 实现变量值替换逻辑
  - [ ] 支持嵌套变量和条件替换
  - [ ] 添加变量验证和错误处理
- [ ] 3.3 集成到配置处理流程
  - [ ] 在 EnhancedFileOperationsService 中集成变量替换
  - [ ] 在模板复制过程中应用变量替换
  - [ ] 集成到三层配置工作流程中

**验收标准**:
- ✅ 需求5标准5 - 支持 `${VAR}` 和 `{{VAR}}` 格式的变量替换
- ✅ 模板定制化功能完全可用

---

### 4. 实现配置合并功能
**状态**: [ ] 未开始  
**优先级**: 🔥 高  
**问题编号**: validation-gap-analysis.md 问题 #5  
**问题**: 缺失基础配置与覆盖配置合并功能

**任务清单**:
- [ ] 4.1 设计配置合并架构
  - [ ] 定义 IConfigurationMerger 接口
  - [ ] 设计配置合并规则和优先级
- [ ] 4.2 实现配置合并核心功能
  - [ ] 实现基础配置与覆盖配置合并逻辑
  - [ ] 处理配置文件优先级
  - [ ] 验证合并后配置的完整性
- [ ] 4.3 集成到配置处理流程
  - [ ] 在配置加载过程中应用配置合并
  - [ ] 确保合并后的配置能正确传递给后续流程

**验收标准**:
- ✅ 需求5标准4 - 配置合并功能（基础配置与覆盖配置合并）
- ✅ 配置合并功能正常工作

---

## 🟡 功能缺口（中优先级）

### 5. 完善容器引擎抽象层
**状态**: [ ] 未开始  
**优先级**: 🟡 中  
**问题编号**: validation-gap-analysis.md 问题 #4  
**问题**: 缺乏统一的容器引擎抽象接口

**任务清单**:
- [ ] 5.1 设计容器引擎抽象层架构
  - [ ] 定义 IContainerEngine 基础接口
  - [ ] 设计 PodmanEngine 和 DockerEngine 实现
  - [ ] 定义容器引擎检测和选择逻辑
- [ ] 5.2 实现容器引擎抽象层
  - [ ] 实现 IContainerEngine 接口
  - [ ] 实现 PodmanEngine 具体实现
  - [ ] 实现 DockerEngine 具体实现（可选）
  - [ ] 实现引擎自动检测和选择
- [ ] 5.3 集成到现有系统
  - [ ] 在 SystemDetectionService 中集成引擎检测
  - [ ] 在 ContainerService 中使用抽象层
  - [ ] 在 Doctor 命令中集成检测结果

**验收标准**:
- ✅ 需求4标准1 - 检测容器引擎并优先使用Podman
- ✅ 容器操作通过统一抽象层执行
- ✅ 支持 Podman 和 Docker 的透明切换

---

### 6. 完善端口冲突检测集成
**状态**: [ ] 未开始  
**优先级**: 🟡 中  
**问题编号**: validation-gap-analysis.md 问题 #6  
**问题**: PortConflictService.cs 已实现检测逻辑，但与容器启动流程集成不完整

**任务清单**:
- [ ] 6.1 分析现有端口冲突检测功能
  - [ ] 检查 PortConflictService 的当前实现
  - [ ] 识别与容器启动流程的集成点
  - [ ] 确定缺失的集成逻辑
- [ ] 6.2 完善集成逻辑
  - [ ] 在容器启动前自动执行端口检测
  - [ ] 实现端口冲突的自动解决方案
  - [ ] 集成用户交互确认流程
- [ ] 6.3 测试和验证
  - [ ] 测试端口冲突检测的准确性
  - [ ] 验证自动解决方案的有效性
  - [ ] 确保用户体验友好

**验收标准**:
- ✅ 需求4标准6-10 完全实现
- ✅ 容器启动时自动检测和解决端口冲突
- ✅ 提供清晰的用户指导和选择

---

### 7. 建立全局错误处理机制
**状态**: [ ] 未开始  
**优先级**: 🟡 中  
**问题编号**: validation-gap-analysis.md 问题 #7  
**问题**: 缺少统一的全局异常处理中间件和自动恢复机制

**任务清单**:
- [ ] 7.1 设计全局错误处理架构
  - [ ] 定义 GlobalExceptionHandler 结构
  - [ ] 设计错误分类和处理策略
  - [ ] 设计自动恢复机制
- [ ] 7.2 实现全局错误处理系统
  - [ ] 实现 GlobalExceptionHandler 
  - [ ] 实现用户友好的错误消息生成
  - [ ] 实现错误日志记录和调试信息
- [ ] 7.3 集成到所有命令和服务
  - [ ] 在 Program.cs 中注册全局异常处理
  - [ ] 在各个命令中集成错误处理
  - [ ] 在核心服务中集成错误处理

**验收标准**:
- ✅ 统一的错误处理和用户友好的错误提示
- ✅ 自动恢复机制在可能的情况下生效
- ✅ 完整的错误日志记录用于调试

---

## ✅ 已完成任务

### 1. 修复 ServiceCollectionExtensions.cs 中的模型不一致问题
**状态**: ✅ 已完成  
**优先级**: 🔥 最高  
**问题编号**: validation-gap-analysis.md 问题 #1  
**位置**: [ServiceCollectionExtensions.cs](file:///Users/zhaoyu/Downloads/coding/deck/deck-dotnet/src/Deck.Services/ServiceCollectionExtensions.cs):47-48

**问题描述**:
- ContainerService.cs（1000+行完整实现）被禁用
- ThreeLayerWorkflowService 只有桩实现，需要完整实现（而非已有完整实现被禁用）
- 原因标注为"模型不一致问题"

**任务清单**:
- ✅ 1.1 分析模型不一致的具体原因
  - ✅ 检查 ContainerService 的依赖接口定义
  - ✅ 检查 ThreeLayerWorkflowService 的依赖接口定义  
  - ✅ 识别数据模型冲突点
- ✅ 1.2 修复数据模型不一致问题
  - ✅ 统一容器相关数据模型定义
  - ✅ 修复接口签名不匹配问题
  - ✅ 确保依赖注入兼容性
- ✅ 1.3 实现完整的 ThreeLayerWorkflowService
  - ✅ 创建完整的 ThreeLayerWorkflowService 实现替代 ThreeLayerWorkflowServiceStub
  - ✅ 确保与 ContainerService 正确集成
- ✅ 1.4 重新启用核心服务
  - ✅ 取消注释 ContainerService 注册（第47行）
  - ✅ 更新 ThreeLayerWorkflowService 注册为完整实现（第45行）
  - ✅ 验证服务正常启动
- ✅ 1.5 验证功能完整性
  - ✅ 测试容器管理功能
  - ✅ 测试三层工作流程功能
  - ✅ 确保无运行时错误

**验收标准**:
- ✅ ContainerService 和 ThreeLayerWorkflowService 正常注册和启动
- ✅ 需求3（三层配置）和需求4（容器管理）的核心验收标准可以执行
- ✅ 无数据模型冲突相关的编译或运行时错误

---

### 2. 创建完整的 StartCommand 实现
**状态**: ✅ 已完成  
**优先级**: 🔥 高  
**问题编号**: validation-gap-analysis.md 问题 #2  
**问题**: 缺少完整的 `StartCommand.cs` 命令实现，`StartCommandServiceSimple.cs` 只实现了部分服务功能

**任务清单**:
- ✅ 2.1 分析 StartCommandServiceSimple.cs 的功能范围
  - ✅ 识别已实现的功能特性
  - ✅ 找出缺失的核心功能
  - ✅ 确定完整 StartCommand 需要的接口定义
- ✅ 2.2 设计完整的 StartCommand 架构
  - ✅ 定义 StartCommand 类结构（位于 [Deck.Console/Commands](file:///Users/zhaoyu/Downloads/coding/deck/deck-dotnet/src/Deck.Console/Commands) 目录）
  - ✅ 设计三层配置选择流程
  - ✅ 集成交互式选择服务
- ✅ 2.3 实现完整的 StartCommand.cs
  - ✅ 实现核心三层配置选择界面
  - ✅ 集成端口冲突检测和解决
  - ✅ 实现智能环境检测和推荐
  - ✅ 添加 Templates 双工作流程选择
- ✅ 2.4 集成和测试
  - ✅ 在 Program.cs 中集成新的 StartCommand
  - ✅ 替换或扩展 StartCommandServiceSimple 的使用
  - ✅ 创建单元测试覆盖

**验收标准**:
- ✅ 需求1标准1-2（核心三层配置选择界面）完全实现
- ✅ Start 命令支持完整的三层配置选择流程
- ✅ 集成所有相关服务（端口检测、交互式选择等）

---

## 🟢 优化项目（低优先级）

### 8. 完善容器管理命令实现
**状态**: [ ] 未开始  
**优先级**: 🟢 低  
**问题编号**: validation-gap-analysis.md 问题 #8  

**任务清单**:
- [ ] 8.1 验证现有容器命令的完整性
- [ ] 8.2 补充缺失的容器生命周期管理功能
- [ ] 8.3 优化命令的用户体验和错误处理

---

## 📋 执行建议

### 🎯 推荐执行顺序:
1. **首先处理任务1** - 修复模型不一致问题（解除核心功能阻塞）
2. **然后处理任务2** - 完善 StartCommand 实现（恢复核心用户入口）
3. **接着处理任务3和4** - 实现变量替换引擎和配置合并功能（完善模板功能）
4. **最后处理中低优先级任务** - 根据时间和需求决定

### ⏱️ 预计时间:
- **阻塞性问题修复**: 3-5天
- **功能缺口补充**: 5-7天  
- **优化项目**: 2-3天
- **总计**: 10-15天

### 🎉 完成后预期效果:
- Phase1 功能完成度从 80% 提升到 90%+
- 核心三层配置和容器管理功能完全可用
- 为 Phase2 高级功能开发奠定坚实基础

---

**注意**: 本任务清单基于 `validation-gap-analysis.md` 的准确分析结果，聚焦于解决当前阻塞第一阶段完成的关键问题。