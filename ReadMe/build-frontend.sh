#!/bin/bash

# FakeMicro 前端构建脚本
set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 日志函数
log() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $1"
}

success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 检查命令是否存在
check_command() {
    if ! command -v "$1" &> /dev/null; then
        error "命令 $1 未找到，请先安装"
        exit 1
    fi
}

# 构建函数
build_frontend() {
    log "开始构建 FakeMicro 前端项目..."
    
    # 检查 Node.js 和 npm
    check_command node
    check_command npm
    
    # 进入前端目录
    cd frontend
    
    # 检查 package.json 是否存在
    if [ ! -f "package.json" ]; then
        error "package.json 文件不存在，请检查前端项目结构"
        exit 1
    fi
    
    # 安装依赖
    log "安装项目依赖..."
    npm install
    
    if [ $? -ne 0 ]; then
        error "依赖安装失败"
        exit 1
    fi
    
    # 检查环境变量文件
    if [ ! -f ".env" ]; then
        warning ".env 文件不存在，使用 .env.example 作为模板"
        cp .env.example .env
    fi
    
    # 构建项目
    log "开始构建项目..."
    npm run build
    
    if [ $? -ne 0 ]; then
        error "项目构建失败"
        exit 1
    fi
    
    success "前端项目构建完成"
    
    # 显示构建信息
    log "构建输出目录: dist/"
    log "构建时间: $(date)"
    
    # 检查构建产物
    if [ -d "dist" ]; then
        log "构建产物检查:"
        ls -la dist/
        
        # 检查主要文件
        if [ -f "dist/index.html" ]; then
            success "主页面文件生成成功"
        else
            warning "主页面文件可能未正确生成"
        fi
        
        if [ -d "dist/assets" ]; then
            success "静态资源目录生成成功"
        else
            warning "静态资源目录可能未正确生成"
        fi
    else
        error "构建产物目录不存在"
        exit 1
    fi
}

# Docker 镜像构建函数
build_docker_image() {
    local image_name="$1"
    local tag="$2"
    
    if [ -z "$image_name" ]; then
        image_name="fakemicro-frontend"
    fi
    
    if [ -z "$tag" ]; then
        tag="latest"
    fi
    
    log "开始构建 Docker 镜像: $image_name:$tag"
    
    # 检查 Docker 是否安装
    check_command docker
    
    # 构建镜像
    docker build -t "$image_name:$tag" .
    
    if [ $? -ne 0 ]; then
        error "Docker 镜像构建失败"
        exit 1
    fi
    
    success "Docker 镜像构建完成: $image_name:$tag"
    
    # 显示镜像信息
    log "镜像信息:"
    docker images | grep "$image_name"
}

# 清理函数
clean_build() {
    log "清理构建产物..."
    
    cd frontend
    
    if [ -d "dist" ]; then
        rm -rf dist
        success "构建产物已清理"
    else
        warning "构建产物目录不存在，无需清理"
    fi
    
    if [ -d "node_modules" ]; then
        rm -rf node_modules
        success "依赖目录已清理"
    fi
}

# 帮助函数
show_help() {
    echo "FakeMicro 前端构建脚本"
    echo ""
    echo "用法: $0 [选项]"
    echo ""
    echo "选项:"
    echo "  build          构建前端项目（默认）"
    echo "  docker-build   构建 Docker 镜像"
    echo "  clean          清理构建产物"
    echo "  help          显示此帮助信息"
    echo ""
    echo "示例:"
    echo "  $0 build           # 构建前端项目"
    echo "  $0 docker-build    # 构建 Docker 镜像"
    echo "  $0 clean           # 清理构建产物"
}

# 主函数
main() {
    local action="${1:-build}"
    
    case "$action" in
        build)
            build_frontend
            ;;
        docker-build)
            build_docker_image "$2" "$3"
            ;;
        clean)
            clean_build
            ;;
        help|--help|-h)
            show_help
            ;;
        *)
            error "未知选项: $action"
            show_help
            exit 1
            ;;
    esac
}

# 脚本入口
main "$@"