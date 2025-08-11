# 环境选择功能说明

## 功能概述
为 deck start 命令添加了环境选择功能，用户可以选择 development、test 或 production 环境。

## 主要变更

### 1. 新增模型和枚举
- `EnvironmentType`: 环境类型枚举 (Development, Test, Production)
- `EnvironmentOption`: 环境配置选项类
- `EnvironmentHelper`: 环境配置帮助类

### 2. 新增服务
- `IEnvironmentConfigurationService`: 环境配置服务接口
- `EnvironmentConfigurationService`: 处理 compose.yaml 和 .env 文件的环境更新

### 3. 更新的接口
- `IConsoleUIService`: 添加 `ShowEnvironmentSelection()` 方法

### 4. 更新的实现
- `ConsoleUIService`: 实现环境选择界面
- `StartCommandServiceSimple`: 在适当时机进行环境选择并应用配置

### 5. 环境特性
- **开发环境 (Development)**:
  - 容器后缀: `dev`
  - 端口偏移: 0
  - 环境变量: "Development"
  
- **测试环境 (Test)**:
  - 容器后缀: `test`  
  - 端口偏移: 1000
  - 环境变量: "Test"
  
- **生产环境 (Production)**:
  - 容器后缀: `prod`
  - 端口偏移: 2000
  - 环境变量: "Production"
  - 特殊警告提示

## 环境选择时机
- **Images 分支**: 不需要环境选择，因为已有固定配置，直接启动
- **Config 分支**: 在端口处理之后、生成 imageName 之前进行环境选择
- **Template 分支**: 
  - CreateEditableConfig: 不需要环境选择，只创建可编辑配置
  - DirectBuildAndStart: 在复制模板后、生成 imageName 之前进行环境选择

## 使用流程
1. 运行 `deck start`
2. 选择配置类型 (Images/Custom/Templates)
3. **新增** (仅Config和Template的DirectBuild): 选择环境类型 (Development/Test/Production)
4. 系统将自动：
   - 在 images 目录名称中添加环境后缀
   - 更新 compose.yaml 中的服务名、容器名、主机名
   - 更新 .env 文件中的环境变量和端口偏移

## 目录命名规则
- 原来: `{configName}-{timestamp}`
- 现在: `{configName}-{timestamp}-{environment}` (dev/test/prod)

## 文件更新内容
### compose.yaml 更新:
- 服务名称: `{projectName}-{environment}`
- 容器名称: `${PROJECT_NAME}-{environment}`  
- 主机名: `${PROJECT_NAME}-{environment}`
- 命令引用: `{projectName}-{environment} bash`

### .env 更新:
- `DOTNET_ENVIRONMENT`: Development/Test/Production
- `ASPNETCORE_ENVIRONMENT`: Development/Test/Production
- 端口偏移: 开发+0, 测试+1000, 生产+2000

## 端口规则
- 开发环境: 使用原始端口
- 测试环境: 原始端口 + 1000
- 生产环境: 原始端口 + 2000

例如：
- DEV_PORT=5000 → 开发:5000, 测试:6000, 生产:7000

## 问题修正记录

### 修正1: 重复环境后缀问题
- **问题**: 容器名出现 `avalonia-default-4-20250810-2345-prod-prod`
- **原因**: `GetContainerName` 方法重复添加环境后缀
- **解决**: 检查 baseName 是否已包含环境后缀，避免重复添加

### 修正2: 不存在的可执行文件问题  
- **问题**: `crun: executable file 'avalonia-prod' not found in $PATH`
- **原因**: compose.yaml 中 `command: avalonia-prod bash` 引用了不存在的可执行文件
- **解决**: 修改为 `command: bash`，直接启动 bash shell

## 最终命名规则
- **目录名**: `{configName}-{timestamp}-{environment}` 
  - 例：`avalonia-default-4-20250810-2345-prod`
- **容器名**: `{projectName}`（已包含环境信息）
  - 例：`avalonia-default-4-20250810-2345-prod` ✅
  - 避免：`avalonia-default-4-20250810-2345-prod-prod` ❌