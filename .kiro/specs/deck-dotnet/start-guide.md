# 重构开始指南

## 目录结构说明

当前采用独立仓库的组织方式：

```
deck/                          # 顶层目录（非git仓库）
├── .kiro/                     # 共享设计文档和规范
├── deck-dotnet/               # .NET版本（独立git仓库）
└── deck-shell/                # Shell版本（独立git仓库）
```

### 独立仓库的优势
- **独立版本控制**：每个实现有自己的git历史和版本管理
- **独立发布**：可以分别发布和管理版本，互不影响
- **清晰分离**：避免不同技术栈的文件混合
- **共享设计**：.kiro作为两个项目的共同规范和设计文档

### Git仓库配置

```bash
# 在deck顶层目录
cd deck

# 初始化各自的git仓库
cd deck-dotnet && git init && git remote add origin <dotnet-repo-url>
cd ../deck-shell && git init && git remote add origin <shell-repo-url>

# .kiro不需要git，作为两个项目的共享设计文档
```

## 第一步：初始化.NET项目

```bash
# 进入deck-dotnet目录
cd deck-dotnet

# 创建.NET项目结构
mkdir -p src/Deck.Console src/Deck.Core src/Deck.Services src/Deck.Infrastructure
mkdir -p tests/Deck.Console.Tests tests/Deck.Services.Tests
mkdir -p .github/workflows build

# 从shell版本复制共享资源（如需要）
cp -r ../deck-shell/templates ./templates
```

### 第二步：创建.NET解决方案

```bash
# 在deck-dotnet目录下执行

# 创建解决方案
dotnet new sln -n Deck

# 创建项目
dotnet new console -n Deck.Console -o src/Deck.Console
dotnet new classlib -n Deck.Core -o src/Deck.Core  
dotnet new classlib -n Deck.Services -o src/Deck.Services
dotnet new classlib -n Deck.Infrastructure -o src/Deck.Infrastructure

# 创建测试项目
dotnet new xunit -n Deck.Console.Tests -o tests/Deck.Console.Tests
dotnet new xunit -n Deck.Services.Tests -o tests/Deck.Services.Tests

# 添加到解决方案
dotnet sln add src/Deck.Console/Deck.Console.csproj
dotnet sln add src/Deck.Core/Deck.Core.csproj
dotnet sln add src/Deck.Services/Deck.Services.csproj
dotnet sln add src/Deck.Infrastructure/Deck.Infrastructure.csproj
dotnet sln add tests/Deck.Console.Tests/Deck.Console.Tests.csproj
dotnet sln add tests/Deck.Services.Tests/Deck.Services.Tests.csproj

# 配置项目引用
dotnet add src/Deck.Console/Deck.Console.csproj reference src/Deck.Core/Deck.Core.csproj
dotnet add src/Deck.Console/Deck.Console.csproj reference src/Deck.Services/Deck.Services.csproj
dotnet add src/Deck.Console/Deck.Console.csproj reference src/Deck.Infrastructure/Deck.Infrastructure.csproj

dotnet add src/Deck.Services/Deck.Services.csproj reference src/Deck.Core/Deck.Core.csproj
dotnet add src/Deck.Services/Deck.Services.csproj reference src/Deck.Infrastructure/Deck.Infrastructure.csproj

dotnet add src/Deck.Infrastructure/Deck.Infrastructure.csproj reference src/Deck.Core/Deck.Core.csproj

dotnet add tests/Deck.Console.Tests/Deck.Console.Tests.csproj reference src/Deck.Console/Deck.Console.csproj
dotnet add tests/Deck.Services.Tests/Deck.Services.Tests.csproj reference src/Deck.Services/Deck.Services.csproj
```

### 第三步：配置基础文件

#### Directory.Build.props (在deck-dotnet根目录)
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    
    <!-- AOT 兼容性 -->
    <IsAotCompatible>true</IsAotCompatible>
    <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
  </PropertyGroup>

  <PropertyGroup>
    <Version>1.0.0</Version>
    <AssemblyCompany>Deck Team</AssemblyCompany>
    <AssemblyProduct>Deck</AssemblyProduct>
    <Copyright>Copyright © 2025 Deck Team</Copyright>
  </PropertyGroup>
</Project>
```

#### .gitignore (在deck-dotnet根目录)
```
# .NET
bin/
obj/
*.user
*.suo
*.cache
*.dll
*.pdb

# 测试结果
TestResults/
coverage/

# IDE
.vs/
.vscode/
*.swp
*.swo

# 项目输出
artifacts/
publish/
build/output/

# macOS
.DS_Store

# 临时文件
*.tmp
*.temp
```

## 开发顺序

### MVP阶段（第1-2周）

1. **Deck.Core项目**
   - 创建基础模型类
   - 定义核心接口
   - 实现异常体系

2. **Deck.Infrastructure项目**
   - FileSystemService基础实现
   - SystemDetectionService基础实现
   - ProcessRunner容器引擎调用

3. **Deck.Services项目**
   - ConfigurationService配置解析
   - TemplateService基础模板管理

4. **Deck.Console项目**
   - Program.cs入口点
   - StartCommand基础实现
   - 依赖注入配置

### 验证阶段（第3周）

1. **基础功能测试**
   - `deck --version` 
   - `deck --help`
   - `deck start`基础交互

2. **核心功能验证**
   - .deck目录创建
   - 模板列表显示
   - 基础系统检测

### 迁移对比测试（第4周）

1. **功能对比测试**
   - Shell版本：`../deck-shell/bin/deck start`
   - .NET版本：`dotnet run --project src/Deck.Console -- start`

2. **输出一致性验证**
   - 命令帮助信息
   - 错误消息格式
   - 交互流程

## deck-dotnet目录结构最终形态

```
deck-dotnet/                   # .NET版本git仓库根目录
├── src/                       # .NET源码
│   ├── Deck.Console/          # 主控制台应用
│   ├── Deck.Core/             # 核心领域模型  
│   ├── Deck.Services/         # 业务服务层
│   └── Deck.Infrastructure/   # 基础设施层
├── tests/                     # 单元测试
│   ├── Deck.Console.Tests/
│   └── Deck.Services.Tests/
├── templates/                 # 模板文件（从shell版本复制）
├── build/                     # 构建脚本
├── .github/                   # CI/CD
├── Deck.sln                   # .NET解决方案
├── Directory.Build.props      # 全局构建配置
├── README.md                  # .NET版本说明
└── .gitignore                 # .NET版本Git忽略文件
```

## 开发环境要求

- .NET 9 SDK
- Visual Studio Code 或 Visual Studio
- Git
- 容器引擎（Podman/Docker）用于测试

## 第一个里程碑目标

完成MVP阶段后，应该能够：

1. `dotnet run --project src/Deck.Console -- --version` 显示版本信息
2. `dotnet run --project src/Deck.Console -- start` 显示环境选择列表
3. 基础的.deck目录创建和模板同步
4. 系统信息检测和显示

这个MVP验证了核心架构的可行性，为后续开发奠定基础。

## 与Shell版本的交互

### 功能对比验证
```bash
# 在测试项目时，可以对比两个版本的输出
cd /path/to/test-project

# Shell版本
../deck/deck-shell/bin/deck start

# .NET版本  
../deck/deck-dotnet/artifacts/deck start
# 或开发时
cd ../deck/deck-dotnet && dotnet run --project src/Deck.Console -- start
```

### 共享资源
- 模板文件可以从deck-shell复制到deck-dotnet
- .kiro设计文档对两个版本都有效
- 测试用例和验收标准保持一致