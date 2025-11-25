import type { RouteRecordRaw } from 'vue-router';
import type { DynamicRouteConfig } from '../types/router';
import { authStore } from '../stores/auth';

/**
 * 菜单项接口
 */
export interface MenuItem {
  // 菜单ID
  id?: string;
  // 菜单项名称
  name: string;
  // 路由路径
  path: string;
  // 路由名称
  routeName?: string;
  // 图标
  icon?: string;
  // 是否隐藏
  hidden?: boolean;
  // 排序号
  order?: number;
  // 子菜单
  children?: MenuItem[];
}

/**
 * 检查用户是否有权限访问菜单项 - 暂时注释掉权限检查，允许所有菜单显示
 */
function hasMenuPermission(route: RouteRecordRaw): boolean {
  // 暂时允许所有菜单访问
  return true;
  /*
  const auth = authStore();
  const permission = route.meta?.permission;
  
  if (!permission) {
    return true; // 没有权限配置，默认允许访问
  }
  
  // 检查角色权限
  if (permission.roles && permission.roles.length > 0) {
    if (!permission.roles.includes(auth.user?.role)) {
      return false;
    }
  }
  
  // 检查权限代码
  if (permission.permissions && permission.permissions.length > 0) {
    if (!permission.permissions.some(p => auth.hasPermission(p))) {
      return false;
    }
  }
  
  return true;
  */
}

/**
 * 将路由转换为菜单项
 */
function routeToMenuItem(route: RouteRecordRaw): MenuItem | null {
  // 跳过不需要在菜单中显示的路由
  if (route.meta?.hidden) {
    return null;
  }
  
  // 检查权限
  if (!hasMenuPermission(route)) {
    return null;
  }
  
  const menuItem: MenuItem = {
    name: route.meta?.title as string || route.name as string || '未命名',
    path: route.path || '',
    routeName: route.name as string,
    icon: route.meta?.icon,
    order: route.meta?.order || 0
  };
  
  // 处理子路由
  if (route.children && route.children.length > 0) {
    const validChildren: MenuItem[] = [];
    
    route.children.forEach(childRoute => {
      const childMenuItem = routeToMenuItem(childRoute);
      if (childMenuItem) {
        validChildren.push(childMenuItem);
      }
    });
    
    // 按排序号排序
    validChildren.sort((a, b) => (a.order || 0) - (b.order || 0));
    
    if (validChildren.length > 0) {
      menuItem.children = validChildren;
    }
  }
  
  return menuItem;
}

/**
 * 从动态路由配置生成菜单
 */
export function generateMenuFromRoutes(routes: DynamicRouteConfig[]): MenuItem[] {
  const menuItems: MenuItem[] = [];
  
  routes.forEach(route => {
    const menuItem = routeToMenuItem(route as unknown as RouteRecordRaw);
    if (menuItem) {
      menuItems.push(menuItem);
    }
  });
  
  // 按排序号排序
  return menuItems.sort((a, b) => (a.order || 0) - (b.order || 0));
}

/**
 * 从当前已注册的路由生成菜单
 */
export function generateMenuFromRegisteredRoutes(router: any): MenuItem[] {
  const menuItems: MenuItem[] = [];
  
  // 获取主布局路由的子路由
  const mainRoute = router.getRoute('Main');
  if (mainRoute && router.getRoutes) {
    const allRoutes = router.getRoutes();
    const mainChildRoutes = allRoutes.filter(
      (route: RouteRecordRaw) => route.meta?.requiresAuth && !route.path.includes(':')
    );
    
    mainChildRoutes.forEach((route: RouteRecordRaw) => {
      const menuItem = routeToMenuItem(route);
      if (menuItem) {
        menuItems.push(menuItem);
      }
    });
  }
  
  // 按排序号排序
  return menuItems.sort((a, b) => (a.order || 0) - (b.order || 0));
}

/**
 * 查找当前激活的菜单项路径
 */
export function findActiveMenuPath(currentPath: string, menuItems: MenuItem[]): string[] {
  const activePath: string[] = [];
  
  function findActiveMenu(items: MenuItem[], parentPath: string[] = []): boolean {
    for (const item of items) {
      const currentParentPath = [...parentPath, item.path];
      
      // 精确匹配
      if (item.path === currentPath) {
        activePath.push(...currentParentPath);
        return true;
      }
      
      // 模糊匹配（如 /users/1 匹配 /users）
      if (currentPath.startsWith(item.path + '/') || currentPath === item.path) {
        if (item.children && item.children.length > 0) {
          if (findActiveMenu(item.children, currentParentPath)) {
            return true;
          }
        }
        
        // 如果没有匹配的子菜单，则激活当前菜单
        if (activePath.length === 0) {
          activePath.push(...currentParentPath);
          return true;
        }
      }
    }
    
    return false;
  }
  
  findActiveMenu(menuItems);
  return activePath;
}