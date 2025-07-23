Name:           deck
Version:        %{_version}
Release:        1%{?dist}
Summary:        容器化开发环境构建工具

License:        MIT
URL:            https://github.com/deck/deck-dotnet
Source0:        %{name}-%{version}.tar.gz

BuildArch:      x86_64
Requires:       glibc

%description
Deck - 甲板，容器化开发环境构建工具，模板复用，助力开发快速起步。

Deck 通过模板为开发者提供标准化的开发环境基础，让您专注于业务开发而非环境配置。
基于 .NET 9 构建，AOT编译，跨平台原生性能，支持 Windows、macOS 和 Linux 平台。

%prep
%setup -q

%build
# 可执行文件已经预构建，无需编译

%install
rm -rf $RPM_BUILD_ROOT
mkdir -p $RPM_BUILD_ROOT/usr/local/bin
cp %{_sourcedir}/Deck.Console $RPM_BUILD_ROOT/usr/local/bin/deck
chmod +x $RPM_BUILD_ROOT/usr/local/bin/deck

%clean
rm -rf $RPM_BUILD_ROOT

%post
# 创建符号链接
if [ ! -e /usr/bin/deck ]; then
    ln -s /usr/local/bin/deck /usr/bin/deck
fi
echo "Deck 安装完成！运行 'deck --version' 验证安装"

%preun
# 移除符号链接
if [ -L /usr/bin/deck ]; then
    rm -f /usr/bin/deck
fi

%files
/usr/local/bin/deck

%changelog
* Mon Jan 01 2025 Deck Team <deck@example.com> - 1.0.0-1
- Initial RPM package