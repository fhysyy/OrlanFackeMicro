/**
 * 性能优化工具类
 * 提供防抖、节流、精确更新等性能优化方法
 */

/**
 * 防抖函数
 * @param fn 要执行的函数
 * @param delay 延迟时间(ms)
 * @returns 防抖后的函数
 */
export function debounce<T extends (...args: any[]) => any>(
  fn: T,
  delay: number
): (...args: Parameters<T>) => void {
  let timeoutId: ReturnType<typeof setTimeout> | null = null;
  
  return function(...args: Parameters<T>) {
    if (timeoutId) {
      clearTimeout(timeoutId);
    }
    
    timeoutId = setTimeout(() => {
      fn(...args);
      timeoutId = null;
    }, delay);
  };
}

/**
 * 节流函数
 * @param fn 要执行的函数
 * @param limit 时间限制(ms)
 * @returns 节流后的函数
 */
export function throttle<T extends (...args: any[]) => any>(
  fn: T,
  limit: number
): (...args: Parameters<T>) => void {
  let inThrottle: boolean = false;
  
  return function(...args: Parameters<T>) {
    if (!inThrottle) {
      fn(...args);
      inThrottle = true;
      setTimeout(() => {
        inThrottle = false;
      }, limit);
    }
  };
}

/**
 * 批量执行函数
 * 将短时间内的多次调用合并为一次
 * @param fn 要执行的函数
 * @param delay 批处理延迟(ms)
 * @returns 批处理函数
 */
export function batch<T extends (...args: any[]) => any>(
  fn: T,
  delay: number = 0
): (...args: Parameters<T>) => void {
  let pendingCall: ReturnType<typeof setTimeout> | null = null;
  let lastArgs: Parameters<T> | null = null;
  
  return function(...args: Parameters<T>) {
    lastArgs = args;
    
    if (!pendingCall) {
      pendingCall = setTimeout(() => {
        if (lastArgs) {
          fn(...lastArgs);
          lastArgs = null;
        }
        pendingCall = null;
      }, delay);
    }
  };
}

/**
 * 精确更新对象属性
 * 避免整个对象替换导致的不必要渲染
 * @param target 目标对象
 * @param path 属性路径，支持点号分隔和数组索引
 * @param value 新值
 * @returns 更新后的对象
 */
export function preciseUpdate<T extends Record<string, any>>(target: T, path: string, value: any): T {
  // 创建一个新对象以触发响应式更新，但只更新必要的部分
  const result = Array.isArray(target) ? [...target] : { ...target };
  
  if (!path || path === '') {
    return value as T;
  }
  
  const keys = path.split('.');
  let current: any = result;
  let parent: any = result;
  let lastKey: string | number = keys[0];
  
  // 遍历路径，直到最后一个属性
  for (let i = 0; i < keys.length - 1; i++) {
    const key = keys[i];
    // 检查是否是数组索引
    const indexMatch = key.match(/^\[([0-9]+)\]$/);
    const propKey = indexMatch ? parseInt(indexMatch[1]) : key;
    
    // 如果目标不存在，创建一个空对象或数组
    if (current[propKey] === undefined) {
      // 下一个键是否是数组索引
      const nextIndexMatch = keys[i + 1].match(/^\[([0-9]+)\]$/);
      current[propKey] = nextIndexMatch ? [] : {};
    }
    
    // 确保创建新的引用
    current[propKey] = Array.isArray(current[propKey]) 
      ? [...current[propKey]] 
      : { ...current[propKey] };
    
    parent = current;
    current = current[propKey];
    lastKey = propKey;
  }
  
  // 设置最后一个属性的值
  const lastKeyMatch = keys[keys.length - 1].match(/^\[([0-9]+)\]$/);
  lastKey = lastKeyMatch ? parseInt(lastKeyMatch[1]) : keys[keys.length - 1];
  current[lastKey] = value;
  
  return result;
}

/**
 * 比较两个对象并获取变更的属性路径
 * @param oldObj 旧对象
 * @param newObj 新对象
 * @returns 变更的属性路径数组
 */
export function getChangedPaths(oldObj: any, newObj: any): string[] {
  const changedPaths: string[] = [];
  const visited = new WeakSet();
  
  function compareObjects(obj1: any, obj2: any, path: string = '') {
    // 处理基本类型或null
    if (obj1 === obj2 || 
        (obj1 === null || typeof obj1 !== 'object') || 
        (obj2 === null || typeof obj2 !== 'object')) {
      if (obj1 !== obj2) {
        changedPaths.push(path);
      }
      return;
    }
    
    // 处理循环引用
    if (visited.has(obj1) || visited.has(obj2)) {
      return;
    }
    
    visited.add(obj1);
    visited.add(obj2);
    
    // 处理不同类型的对象
    if (obj1.constructor !== obj2.constructor) {
      changedPaths.push(path);
      return;
    }
    
    // 处理数组
    if (Array.isArray(obj1) && Array.isArray(obj2)) {
      // 如果长度不同，整个数组变更
      if (obj1.length !== obj2.length) {
        changedPaths.push(path);
        return;
      }
      
      // 比较每个元素
      for (let i = 0; i < obj1.length; i++) {
        const newPath = path ? `${path}[${i}]` : `[${i}]`;
        compareObjects(obj1[i], obj2[i], newPath);
      }
      return;
    }
    
    // 处理普通对象
    const keys1 = Object.keys(obj1);
    const keys2 = Object.keys(obj2);
    
    // 检查是否有新增或删除的键
    if (keys1.length !== keys2.length || !keys1.every(key => keys2.includes(key))) {
      changedPaths.push(path);
      return;
    }
    
    // 比较每个属性
    for (const key of keys1) {
      const newPath = path ? `${path}.${key}` : key;
      compareObjects(obj1[key], obj2[key], newPath);
    }
  }
  
  compareObjects(oldObj, newObj);
  return changedPaths;
}

/**
 * 性能监控装饰器
 * @param target 目标对象
 * @param propertyKey 属性键
 * @param descriptor 属性描述符
 */
export function performanceMonitor(
  target: any,
  propertyKey: string,
  descriptor?: PropertyDescriptor
): PropertyDescriptor | void {
  // 安全检查：确保descriptor存在且有value属性
  if (!descriptor || typeof descriptor.value !== 'function') {
    console.warn(`performanceMonitor: Cannot monitor non-function property ${propertyKey}`);
    return descriptor;
  }
  
  const originalMethod = descriptor.value;
  
  descriptor.value = function(...args: any[]) {
    const startTime = performance.now();
    const result = originalMethod.apply(this, args);
    const endTime = performance.now();
    
    const executionTime = endTime - startTime;
    if (executionTime > 100) { // 超过100ms的操作记录警告
      console.warn(`Performance warning: ${propertyKey} took ${executionTime.toFixed(2)}ms to execute`);
    }
    
    return result;
  };
  
  return descriptor;
}

/**
 * 计算两个对象的差异并应用最小更新
 * @param original 原始对象
 * @param updated 更新后的对象
 * @returns 最小更新后的对象
 */
export function applyMinimalUpdate<T>(original: T, updated: T): T {
  // 如果是基本类型或null，直接返回updated
  if (original === updated || 
      original === null || typeof original !== 'object' || 
      updated === null || typeof updated !== 'object') {
    return updated;
  }
  
  // 如果类型不同，直接返回updated
  if (original.constructor !== updated.constructor) {
    return updated;
  }
  
  // 处理数组
  if (Array.isArray(original) && Array.isArray(updated)) {
    // 如果长度不同，直接返回updated
    if (original.length !== updated.length) {
      return updated;
    }
    
    // 创建新数组，但只复制发生变化的元素
    const result = [...original];
    let hasChanged = false;
    
    for (let i = 0; i < original.length; i++) {
      const oldItem = original[i];
      const newItem = updated[i];
      
      if (oldItem !== newItem) {
        const updatedItem = applyMinimalUpdate(oldItem, newItem);
        if (updatedItem !== oldItem) {
          result[i] = updatedItem;
          hasChanged = true;
        }
      }
    }
    
    return hasChanged ? result : original;
  }
  
  // 处理普通对象
  const result = { ...original };
  let hasChanged = false;
  
  // 检查所有键
  const allKeys = new Set([...Object.keys(original), ...Object.keys(updated)]);
  
  for (const key of allKeys) {
    const oldValue = (original as any)[key];
    const newValue = (updated as any)[key];
    
    if (oldValue !== newValue) {
      const updatedValue = applyMinimalUpdate(oldValue, newValue);
      if (updatedValue !== oldValue) {
        (result as any)[key] = updatedValue;
        hasChanged = true;
      }
    }
  }
  
  return hasChanged ? result : original;
}

/**
 * 安全地获取对象的深层属性
 * @param obj 目标对象
 * @param path 属性路径
 * @param defaultValue 默认值（如果属性不存在）
 * @returns 属性值或默认值
 */
export function getNestedProperty<T = any>(
  obj: any,
  path: string,
  defaultValue?: T
): T | undefined {
  if (!obj || typeof obj !== 'object') {
    return defaultValue;
  }
  
  const pathParts = path.split('.');
  let current: any = obj;
  
  for (const part of pathParts) {
    const match = part.match(/^(\w+)\[(\d+)\]$/);
    
    if (match) {
      const [, key, index] = match;
      if (!current[key] || !Array.isArray(current[key]) || current[key].length <= parseInt(index)) {
        return defaultValue;
      }
      current = current[key][parseInt(index)];
    } else {
      if (!current || !(part in current)) {
        return defaultValue;
      }
      current = current[part];
    }
  }
  
  return current !== undefined ? current : defaultValue;
}

/**
 * 移除对象的深层属性
 * @param obj 目标对象
 * @param path 属性路径
 * @returns 是否成功移除
 */
export function removeNestedProperty(
  obj: object,
  path: string
): boolean {
  if (!obj || typeof obj !== 'object') {
    return false;
  }
  
  const pathParts = path.split('.');
  const lastPart = pathParts.pop();
  
  if (!lastPart) {
    return false;
  }
  
  // 找到父级对象
  const parentPath = pathParts.join('.');
  const parent = parentPath ? getNestedProperty(obj, parentPath) : obj;
  
  if (!parent || typeof parent !== 'object') {
    return false;
  }
  
  // 移除属性
  const match = lastPart.match(/^(\w+)\[(\d+)\]$/);
  
  if (match) {
    const [, key, index] = match;
    if (Array.isArray(parent[key])) {
      parent[key].splice(parseInt(index), 1);
      return true;
    }
  } else {
    if (delete parent[lastPart]) {
      return true;
    }
  }
  
  return false;
}

/**
 * 创建响应式的性能监控包装器
 * @param fn 需要监控的函数
 * @param name 函数名称（用于性能日志）
 * @returns 包装后的函数
 */
export function withPerformanceMonitoring<T extends (...args: any[]) => any>(
  fn: T,
  name: string
): (...args: Parameters<T>) => ReturnType<T> {
  return (...args: Parameters<T>): ReturnType<T> => {
    const startTime = performance.now();
    
    try {
      const result = fn(...args);
      
      // 异步函数处理
      if (result instanceof Promise) {
        return result.then((value) => {
          const endTime = performance.now();
          console.debug(`[Performance] ${name}: ${endTime - startTime}ms`);
          return value;
        }) as ReturnType<T>;
      }
      
      const endTime = performance.now();
      console.debug(`[Performance] ${name}: ${endTime - startTime}ms`);
      return result;
    } catch (error) {
      const endTime = performance.now();
      console.error(`[Performance] ${name} failed after ${endTime - startTime}ms`, error);
      throw error;
    }
  };
}

/**
 * 确保值不是响应式引用
 * @param value 要解包的值
 * @returns 解包后的值
 */
export function unwrapValue<T>(value: T | any): T {
  if (typeof value === 'object' && value !== null && 'value' in value) {
    return value.value;
  }
  return value;
}

/**
 * 优化大型数组的处理，分批执行操作
 * @param items 要处理的数组
 * @param processFn 处理函数
 * @param batchSize 每批处理的数量
 * @param delay 批处理间隔（毫秒）
 * @returns Promise，在所有批次处理完成后解析
 */
export function batchProcess<T>(
  items: T[],
  processFn: (item: T, index: number) => void,
  batchSize: number = 100,
  delay: number = 0
): Promise<void> {
  return new Promise((resolve) => {
    if (!items.length) {
      resolve();
      return;
    }
    
    let processed = 0;
    const total = items.length;
    
    const processBatch = () => {
      const end = Math.min(processed + batchSize, total);
      
      // 使用requestAnimationFrame确保不阻塞UI
      if (delay <= 0) {
        for (let i = processed; i < end; i++) {
          processFn(items[i], i);
        }
        
        processed = end;
        
        if (processed < total) {
          processBatch();
        } else {
          resolve();
        }
      } else {
        // 使用setTimeout进行延迟批处理
        setTimeout(() => {
          for (let i = processed; i < end; i++) {
            processFn(items[i], i);
          }
          
          processed = end;
          
          if (processed < total) {
            processBatch();
          } else {
            resolve();
          }
        }, delay);
      }
    };
    
    processBatch();
  });
}

/**
 * 批量精确更新对象的多个属性
 * @param obj 目标对象
 * @param updates 包含路径和新值的对象
 */
export function batchPreciseUpdate<T extends object>(
  obj: T,
  updates: Record<string, any>
): void {
  Object.entries(updates).forEach(([path, value]) => {
    preciseUpdate(obj as Record<string, any>, path, value);
  });
}