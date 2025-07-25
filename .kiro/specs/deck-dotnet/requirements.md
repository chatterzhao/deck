# 重构需求文档
Deck（/dɛk/ "代克"，甲板），容器化开发环境构建工具，模板复用，助力开发快速起步

Deck 通过模板为开发者提供标准化的开发环境基础，让您专注于业务开发而非环境配置。

Deck .NET 版基于 .NET 9 构建，AOT，跨平台原生性能，支持 Windows、macOS 和 Linux 平台。

## 项目概述

将现有技术栈为`shell`的 `deck/deck-shell` 重构为技术栈为`.NET9`的`deck/deck-dotnet`应用程序，保持原有功能的同时提升跨平台兼容性、性能和可维护性（也有少量功能的新增，优化等）。

## 需求分析

### 需求1：保持CLI体验一致性

**用户故事：** 作为开发者，我希望重构后的工具保持与原Shell版本相同的命令行界面和使用体验，这样我无需重新学习使用方法。

#### 验收标准

**核心命令组：**
1. WHEN 用户执行 `deck start` THEN 系统应显示三层配置选择界面（Images/Custom/Templates）
2. WHEN 用户执行 `deck start tauri` THEN 系统应过滤并显示tauri相关的环境选项
3. WHEN 用户执行 `deck stop [CONTAINER-NAME/ID]` THEN 系统应停止指定的容器
4. WHEN 用户执行 `deck stop` THEN 系统应显示容器列表供用户选择序号（.NET版本优化：交互式选择）
5. WHEN 用户执行 `deck restart [CONTAINER-NAME/ID]` THEN 系统应重启指定的容器
6. WHEN 用户执行 `deck restart` THEN 系统应显示容器列表供用户选择序号（.NET版本优化：交互式选择）
7. WHEN 用户执行 `deck logs [CONTAINER-NAME/ID] -f` THEN 系统应实时显示容器日志
8. WHEN 用户执行 `deck logs` THEN 系统应显示容器列表供用户选择序号（.NET版本优化：交互式选择）
9. WHEN 用户执行 `deck shell [CONTAINER-NAME/ID]` THEN 系统应进入指定容器的交互式shell
10. WHEN 用户执行 `deck shell` THEN 系统应显示容器列表供用户选择序号（.NET版本优化：交互式选择）

**配置管理命令组：**
11. WHEN 用户执行 `deck custom list` THEN 系统应列出用户自定义配置
<!-- 12. WHEN 用户执行 `deck config create` THEN 系统应创建新的配置文件 -- .NET版本放弃实现 -->
<!-- 13. WHEN 用户执行 `deck config edit` THEN 系统应编辑现有配置 -- .NET版本放弃实现 -->

**镜像三层管理命令组（.NET版本重要优化：统一管理Deck配置+Podman镜像+容器）：**
14. WHEN 用户执行 `deck images list` THEN 系统应显示三层内容的统一列表：
    - Deck Images配置 (.deck/images/ 目录内容)
    - Podman 镜像 (podman images 相关镜像)
    - 相关容器 (基于这些镜像的容器状态)
15. WHEN 用户执行 `deck images clean` THEN 系统应显示三层内容选择界面，不同选择有不同的清理逻辑：
    - 选择Deck配置：仅删除 .deck/images/ 目录
    - 选择Podman镜像：删除镜像+所有相关容器+对应的Deck配置目录
    - 选择容器：仅删除指定容器
16. WHEN 用户执行 `deck images info [TARGET]` THEN 系统应显示指定目标的详细信息
17. WHEN 用户执行 `deck images info` THEN 系统应显示三层内容列表供用户选择序号（.NET版本优化：统一交互式选择）
18. WHEN 用户执行 `deck images help` THEN 系统应显示三层管理逻辑说明

**模板管理命令组：**
19. WHEN 用户执行 `deck templates list` THEN 系统应列出可用模板
20. WHEN 用户执行 `deck templates update` THEN 系统应更新远程模板
21. WHEN 用户执行 `deck templates clean` THEN 系统应提示用户使用 `deck templates update` 替代，因为templates目录每次start都会从仓库覆盖更新，清理意义不大

**Custom配置管理命令组：**
22. WHEN 用户执行 `deck custom clean` THEN 系统应显示序号选择界面清理自定义配置（.NET版本优化：交互式选择）

**系统管理命令组：**
23. WHEN 用户执行 `deck doctor` THEN 系统应进行全面系统诊断
24. WHEN 用户执行 `deck clean` THEN 系统应显示三层配置（Images/Custom/Templates）序号选择界面（.NET版本优化：交互式选择）
25. WHEN 用户执行 `deck install podman` THEN 系统应自动安装Podman

**容器管理命令（.NET版本新增优化）：**
26. WHEN 用户执行 `deck ps` THEN 系统应显示当前项目相关的容器状态，比 `podman ps -a` 更智能过滤
27. WHEN 用户执行 `deck rm [CONTAINER-NAME/ID]` THEN 系统应删除指定容器
28. WHEN 用户执行 `deck rm` THEN 系统应显示容器列表供用户选择序号（.NET版本优化：交互式选择）

**基础命令：**
29. WHEN 用户执行 `deck --help` THEN 系统应显示完整的帮助信息，并提示可直接使用的Podman命令
30. WHEN 用户执行 `deck --version` THEN 系统应显示版本信息

### 需求2：跨平台原生支持

**用户故事：** 作为开发者，我希望在Windows、Linux、macOS上都能使用同一个可执行文件，无需安装额外的运行时环境。

#### 验收标准

1. WHEN 在Windows系统上运行 THEN 系统应正常工作且无需Git Bash环境
2. WHEN 在Linux系统上运行 THEN 系统应正常工作且无需额外依赖
3. WHEN 在macOS系统上运行 THEN 系统应在Intel和Apple Silicon上都能正常工作
4. WHEN 发布时 THEN 系统应提供单一可执行文件，无需安装.NET运行时
5. WHEN 检测操作系统时 THEN 系统应自动适配不同平台的路径分隔符和命令
6. WHEN 处理文件路径时 THEN 系统应使用Path.Combine()等.NET标准方法

### 需求3：三层配置体系管理

**用户故事：** 作为开发者，我希望工具能够准确实现原版的三层配置体系（Templates/Custom/Images），每层有不同的作用和工作流程。

#### 验收标准

**目录结构和权限：**
1. WHEN 首次运行时 THEN 系统应自动创建.deck目录结构
2. WHEN 同步模板时 THEN 系统应将远程模板下载到.deck/templates/目录（只读，每次覆盖更新）
3. WHEN 创建自定义配置时 THEN 系统应在.deck/custom/目录中创建可编辑的配置
4. WHEN 构建镜像时 THEN 系统应在.deck/images/目录中保存带时间戳的构建记录

**三层配置启动逻辑：**
5. WHEN 用户选择Images配置 THEN 系统应执行智能容器管理流程（检测运行中/停止/不存在状态）
6. WHEN 用户选择Custom配置 THEN 系统应执行构建新镜像流程（Custom→Images）
7. WHEN 用户选择Templates配置 THEN 系统应显示双工作流程选择：
   - 选项1：创建可编辑配置（Templates→Custom，终止命令等待用户编辑）
   - 选项2：直接构建启动（Templates→Custom→Images三步流程）

**智能容器管理（Images配置专用）：**
8. WHEN 容器运行中时 THEN 系统应直接进入容器
9. WHEN 容器已停止时 THEN 系统应重启容器后进入
10. WHEN 容器不存在但镜像存在时 THEN 系统应创建新容器
11. WHEN 容器和镜像都不存在时 THEN 系统应重新构建镜像

### 需求4：容器引擎集成和端口管理

**用户故事：** 作为开发者，我希望工具能够智能检测和管理Podman/Docker容器引擎，自动处理容器的生命周期，并解决端口冲突问题。

#### 验收标准

**容器引擎管理：**
1. WHEN 检测容器引擎时 THEN 系统应优先检测Podman，其次检测Docker
2. WHEN 容器引擎未安装时 THEN 系统应提供 `deck install podman` 自动安装功能
3. WHEN 启动容器时 THEN 系统应检查容器状态并智能处理（运行中/已停止/不存在）
4. WHEN 构建镜像时 THEN 系统应使用compose文件进行构建和启动
5. WHEN 管理容器时 THEN 系统应支持启动、停止、重启、进入容器等操作

**端口冲突检测和解决：**
6. WHEN 启动容器前 THEN 系统应检测.env配置的端口是否被占用
7. WHEN 发现端口冲突时 THEN 系统应显示占用进程的PID和停止命令
8. WHEN 端口冲突时 THEN 系统应建议修改.env文件中的端口配置
9. WHEN 端口被容器占用时 THEN 系统应提供停止相关容器的命令
10. WHEN 为新镜像分配端口时 THEN 系统应自动寻找可用端口（DEV_PORT/DEBUG_PORT/WEB_PORT等）

### 需求5：配置文件处理

**用户故事：** 作为开发者，我希望工具能够解析和验证YAML、环境变量等配置文件，确保配置的正确性。

#### 验收标准

1. WHEN 解析compose.yaml时 THEN 系统应验证YAML语法和Docker Compose结构
2. WHEN 解析.env文件时 THEN 系统应正确处理环境变量的键值对
3. WHEN 验证配置时 THEN 系统应检查必需的配置项是否存在
4. WHEN 合并配置时 THEN 系统应支持基础配置和覆盖配置的合并
5. WHEN 模板变量替换时 THEN 系统应支持${VAR}和{{VAR}}格式的变量替换
6. WHEN 备份配置时 THEN 系统应在修改前自动创建备份

### 需求6：远程模板同步

**用户故事：** 作为开发者，我希望工具能够从远程Git仓库同步最新的模板配置，支持自定义模板仓库。

#### 验收标准

1. WHEN 首次运行时 THEN 系统应从默认仓库同步模板
2. WHEN 网络可用时 THEN 系统应自动更新模板并显示更新状态
3. WHEN 网络不可用时 THEN 系统应显示警告并使用本地模板
4. WHEN 配置自定义仓库时 THEN 系统应支持修改config.yaml中的仓库地址
5. WHEN 同步失败时 THEN 系统应提供手动下载的指导说明
6. WHEN 验证模板时 THEN 系统应检查模板文件的完整性

### 需求7：系统诊断和错误处理

**用户故事：** 作为开发者，我希望工具能够提供详细的系统诊断信息，帮助我快速定位和解决问题。

#### 验收标准

1. WHEN 执行doctor命令时 THEN 系统应检查系统要求、网络连接、容器引擎状态
2. WHEN 检测系统信息时 THEN 系统应显示操作系统、架构、内存、磁盘空间等信息
3. WHEN 发生错误时 THEN 系统应提供清晰的错误信息和解决建议
4. WHEN 检查依赖时 THEN 系统应验证必需的系统工具是否可用
5. WHEN 验证网络时 THEN 系统应检查容器镜像仓库和包管理器镜像源的可用性
6. WHEN 日志记录时 THEN 系统应支持不同级别的日志输出

### 需求8：完整配置文件支持

**用户故事：** 作为开发者，我希望工具能够支持原版的完整配置文件功能，包括模板仓库配置、容器引擎配置、网络代理等。

#### 验收标准

**config.yaml完整功能：**
1. WHEN 配置模板仓库时 THEN 系统应支持自定义仓库URL、分支、自动更新、缓存策略
2. WHEN 配置容器引擎时 THEN 系统应支持指定Podman/Docker、自动安装选项
3. WHEN 配置网络代理时 THEN 系统应支持HTTP/HTTPS代理配置
4. WHEN 验证配置文件时 THEN 系统应检查配置的完整性和正确性

**环境变量和端口配置：**
5. WHEN 处理.env文件时 THEN 系统应支持DEV_PORT/DEBUG_PORT/WEB_PORT/HTTPS_PORT/ANDROID_DEBUG_PORT等标准端口
6. WHEN 更新配置时 THEN 系统应自动更新PROJECT_NAME避免容器名冲突
7. WHEN 复制配置时 THEN 系统应包含隐藏文件（如.env、.gitignore等）

### 需求9：交互式用户体验优化（相比Shell版本的重要改进）

**用户故事：** 作为开发者，我希望.NET版本比原Shell版本更加用户友好，当我不记得容器名称或配置名称时，能够提供交互式选择界面，避免频繁使用podman命令查看列表。

#### 验收标准

**交互式选择优化：**
1. WHEN 用户执行任何需要容器名的命令但未提供参数时 THEN 系统应显示当前项目相关容器列表供选择
2. WHEN 用户执行任何需要镜像配置名的命令但未提供参数时 THEN 系统应显示配置列表供选择
3. WHEN 显示选择列表时 THEN 系统应同时显示对应的Podman命令提示
4. WHEN 用户选择后 THEN 系统应显示等效的Podman命令，教育用户直接使用Podman

**示例交互优化：**
```bash
$ deck stop
请选择要停止的容器：

当前项目相关容器：
1. nodejs-app-dev (运行中) [ID: a1b2c3d4]
2. python-api-test (已停止) [ID: e5f6g7h8]

💡 提示：您也可以直接使用 Podman 命令：
   podman stop a1b2c3d4  # 停止 nodejs-app-dev
   podman ps -a          # 查看所有容器

请输入序号（或按 Enter 取消）: 1
正在停止容器 nodejs-app-dev...
✅ 已停止容器 nodejs-app-dev (a1b2c3d4)

等效的 Podman 命令：podman stop a1b2c3d4
```

### 需求10：增强清理命令功能

**用户故事：** 作为开发者，我希望工具提供智能的清理功能，能够安全地清理不同类型的配置和镜像，并提供详细的清理选项。

#### 验收标准

**主清理命令 `deck clean`：**
1. WHEN 用户执行 `deck clean` THEN 系统应显示三层配置选择界面：
   - Images list (已构建镜像配置)
   - Custom list (用户自定义配置)  
   - Templates list (远程模板)
2. WHEN 用户输入序号 THEN 系统应进入对应的子清理流程
3. WHEN 用户选择删除项目后 THEN 系统应要求输入 y/n 确认

**三层统一清理命令 `deck images clean`（.NET版本重要优化）：**
4. WHEN 用户执行命令时 THEN 系统应显示三层内容的统一选择列表：
   - Deck Images配置部分：列出 .deck/images/ 下的配置目录
   - Podman镜像部分：列出 podman images 中相关的镜像
   - 相关容器部分：列出基于这些镜像的所有容器
5. WHEN 用户选择Deck配置目录时 THEN 系统应提供配置级清理选项：
   - 仅删除配置目录（保留Podman镜像和容器）
   - 删除配置目录+停止相关容器
   - 删除配置目录+所有相关容器+对应镜像
6. WHEN 用户选择Podman镜像时 THEN 系统应警告并提供镜像级清理选项：
   - ⚠️ 警告：将同步删除基于此镜像的所有容器+对应的Deck配置目录
   - 选项：强制删除镜像+所有相关容器+Deck配置
   - 选项：删除镜像+容器+配置+构建缓存（不推荐）
7. WHEN 用户选择容器时 THEN 系统应提供容器级清理选项：
   - 仅删除容器（保留镜像和Deck配置）
   - 删除容器+清理相关配置
8. WHEN 任何清理操作时 THEN 系统应详细显示将要删除的内容并要求 y/n 确认

**自定义配置清理命令 `deck custom clean`：**
9. WHEN 用户执行命令时 THEN 系统应列出 .deck/custom/ 下的所有配置并显示序号
10. WHEN 用户选择配置后 THEN 系统应要求输入 y/n 确认删除

**模板清理命令 `deck templates clean`（保留但建议替代方案）：**
11. WHEN 用户执行命令时 THEN 系统应显示提示信息：
    ```
    💡 提示：templates 目录每次执行 deck start 时都会从远程仓库自动覆盖更新
    
    建议使用以下命令替代：
    - deck templates update  # 立即从仓库更新模板
    - 直接执行 deck start   # 会自动更新并使用最新模板
    
    清理 templates 目录意义不大，因为会被自动覆盖。
    
    是否仍要继续清理操作？(y/n):
    ```
12. WHEN 用户选择继续时 THEN 系统应列出 .deck/templates/ 下的模板供选择
13. WHEN 用户选择后 THEN 系统应再次提醒并要求最终确认

### 需求11：项目环境检测

**用户故事：** 作为开发者，我希望工具能够智能检测当前项目的技术栈类型，推荐合适的开发环境配置。

#### 验收标准

1. WHEN 检测Tauri项目时 THEN 系统应识别Cargo.toml和package.json的组合
2. WHEN 检测Flutter项目时 THEN 系统应识别pubspec.yaml文件
3. WHEN 检测Avalonia项目时 THEN 系统应识别包含Avalonia引用的.csproj文件
4. WHEN 检测到多种环境时 THEN 系统应列出所有检测到的环境类型
5. WHEN 未检测到已知环境时 THEN 系统应显示所有可用选项
6. WHEN 推荐环境时 THEN 系统应在选项列表中突出显示推荐的配置

### 需求12：镜像权限管理

**用户故事：** 作为开发者，我希望工具能够正确管理images目录的权限，防止误操作导致的配置丢失。

#### 验收标准

1. WHEN 访问images目录时 THEN 系统应只允许修改.env文件中的运行时变量
2. WHEN 尝试修改compose.yaml时 THEN 系统应阻止操作并提供说明
3. WHEN 尝试修改Dockerfile时 THEN 系统应阻止操作并提供说明
4. WHEN 尝试重命名镜像目录时 THEN 系统应阻止操作并说明原因
5. WHEN 显示权限说明时 THEN 系统应清楚解释哪些内容可以修改，哪些不能修改
6. WHEN 验证镜像目录名时 THEN 系统应检查目录名格式是否符合预期

### 需求13：AOT发布支持

**用户故事：** 作为开发者，我希望工具能够编译为原生可执行文件，提供更快的启动速度和更小的分发包。

#### 验收标准

1. WHEN 使用AOT编译时 THEN 系统应生成单一的原生可执行文件
2. WHEN 在不同平台编译时 THEN 系统应支持Windows、Linux、macOS的AOT编译
3. WHEN 启动应用时 THEN AOT版本应比普通版本有更快的启动速度
4. WHEN 分发应用时 THEN AOT版本应无需安装.NET运行时
5. WHEN 处理反射代码时 THEN 系统应正确配置AOT兼容性
6. WHEN 构建CI/CD时 THEN 系统应支持GitHub Actions自动构建AOT版本