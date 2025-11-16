#!/bin/bash

# FakeMicro 项目部署脚本
set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 日志函数
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 显示帮助信息
show_help() {
    echo "FakeMicro 项目部署脚本"
    echo ""
    echo "用法: $0 [选项]"
    echo ""
    echo "选项:"
    echo "  -e, --environment ENV    部署环境 (dev/prod, 默认: dev)"
    echo "  -a, --action ACTION     执行动作 (build/up/down/restart/clean, 默认: up)"
    echo "  -h, --help              显示帮助信息"
    echo ""
    echo "示例:"
    echo "  $0 -e prod -a up        # 部署生产环境"
    echo "  $0 -e dev -a build      # 构建开发环境镜像"
    echo "  $0 -a down              # 停止当前环境服务"
}

# 默认参数
ENVIRONMENT="dev"
ACTION="up"

# 解析命令行参数
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -a|--action)
            ACTION="$2"
            shift 2
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            log_error "未知参数: $1"
            show_help
            exit 1
            ;;
    esac
done

# 验证环境参数
if [[ "$ENVIRONMENT" != "dev" && "$ENVIRONMENT" != "prod" ]]; then
    log_error "环境参数必须是 'dev' 或 'prod'"
    exit 1
fi

# 验证动作参数
if [[ "$ACTION" != "build" && "$ACTION" != "up" && "$ACTION" != "down" && "$ACTION" != "restart" && "$ACTION" != "clean" ]]; then
    log_error "动作参数必须是 'build', 'up', 'down', 'restart' 或 'clean'"
    exit 1
fi

# 设置Docker Compose文件
COMPOSE_FILE="docker-compose.yml"
if [[ "$ENVIRONMENT" == "dev" ]]; then
    COMPOSE_FILE="docker-compose.dev.yml"
fi

log_info "环境: $ENVIRONMENT"
log_info "动作: $ACTION"
log_info "Compose文件: $COMPOSE_FILE"

# 检查Docker是否安装
if ! command -v docker &> /dev/null; then
    log_error "Docker 未安装，请先安装 Docker"
    exit 1
fi

# 检查Docker Compose是否安装
if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
    log_error "Docker Compose 未安装，请先安装 Docker Compose"
    exit 1
fi

# 设置Docker Compose命令
if command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker-compose"
else
    DOCKER_COMPOSE="docker compose"
fi

# 执行部署动作
case $ACTION in
    "build")
        log_info "构建 Docker 镜像..."
        $DOCKER_COMPOSE -f $COMPOSE_FILE build
        log_success "镜像构建完成"
        ;;
        
    "up")
        log_info "启动服务..."
        $DOCKER_COMPOSE -f $COMPOSE_FILE up -d
        
        # 等待服务启动
        log_info "等待服务启动..."
        sleep 30
        
        # 检查服务状态
        if $DOCKER_COMPOSE -f $COMPOSE_FILE ps | grep -q "Up"; then
            log_success "服务启动成功"
            
            # 显示服务信息
            echo ""
            log_info "服务状态:"
            $DOCKER_COMPOSE -f $COMPOSE_FILE ps
            
            echo ""
            log_info "访问地址:"
            if [[ "$ENVIRONMENT" == "prod" ]]; then
                echo "API 网关: http://localhost:5000"
                echo "前端界面: http://localhost:3000"
                echo "RabbitMQ 管理: http://localhost:15672"
                echo "Kibana: http://localhost:5601"
            else
                echo "API 网关: http://localhost:5001"
                echo "前端界面: http://localhost:3001"
                echo "RabbitMQ 管理: http://localhost:15673"
            fi
        else
            log_error "服务启动失败"
            exit 1
        fi
        ;;
        
    "down")
        log_info "停止服务..."
        $DOCKER_COMPOSE -f $COMPOSE_FILE down
        log_success "服务已停止"
        ;;
        
    "restart")
        log_info "重启服务..."
        $DOCKER_COMPOSE -f $COMPOSE_FILE restart
        log_success "服务重启完成"
        ;;
        
    "clean")
        log_warning "清理所有容器、镜像和卷..."
        read -p "确定要清理所有Docker资源吗？(y/N): " -n 1 -r
        echo
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            $DOCKER_COMPOSE -f $COMPOSE_FILE down -v
            docker system prune -f
            log_success "清理完成"
        else
            log_info "取消清理操作"
        fi
        ;;
esac

log_success "部署脚本执行完成"