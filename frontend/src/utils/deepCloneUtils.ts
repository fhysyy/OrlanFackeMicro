/**
 * 高效深拷贝工具类
 * 避免使用JSON.parse/JSON.stringify，处理循环引用，支持更多JavaScript类型
 */

/**
 * 高效深拷贝函数
 * @param obj 要拷贝的对象
 * @param cache WeakMap缓存，用于处理循环引用
 * @returns 深拷贝后的对象
 */
export function deepClone<T>(obj: T, cache = new WeakMap()): T {
  // 处理基本类型或null
  if (obj === null || typeof obj !== 'object') {
    return obj
  }

  // 检查缓存，避免循环引用
  if (cache.has(obj)) {
    return cache.get(obj) as T
  }

  // 处理日期对象
  if (obj instanceof Date) {
    return new Date(obj.getTime()) as unknown as T
  }

  // 处理正则表达式
  if (obj instanceof RegExp) {
    return new RegExp(obj.source, obj.flags) as unknown as T
  }

  // 处理Map
  if (obj instanceof Map) {
    const map = new Map() as Map<any, any>
    cache.set(obj, map)
    obj.forEach((value, key) => {
      map.set(deepClone(key, cache), deepClone(value, cache))
    })
    return map as unknown as T
  }

  // 处理Set
  if (obj instanceof Set) {
    const set = new Set() as Set<any>
    cache.set(obj, set)
    obj.forEach(value => {
      set.add(deepClone(value, cache))
    })
    return set as unknown as T
  }

  // 处理数组
  if (Array.isArray(obj)) {
    const arr = [] as any[]
    cache.set(obj, arr)
    obj.forEach((item, index) => {
      arr[index] = deepClone(item, cache)
    })
    return arr as unknown as T
  }

  // 处理普通对象
  try {
    // 尝试使用对象的构造函数创建新实例
    const cloned = new obj.constructor()
    cache.set(obj, cloned)

    // 复制所有可枚举属性
    Object.assign(
      cloned,
      ...Object.keys(obj).map(key => ({
        [key]: deepClone((obj as any)[key], cache)
      }))
    )
    return cloned as T
  } catch (error) {
    // 如果构造函数调用失败，回退到普通对象
    const cloned = {} as T
    cache.set(obj, cloned)

    for (const key in obj) {
      if (Object.prototype.hasOwnProperty.call(obj, key)) {
        (cloned as any)[key] = deepClone((obj as any)[key], cache)
      }
    }
    return cloned
  }
}

/**
 * 浅拷贝函数
 * @param obj 要浅拷贝的对象
 * @returns 浅拷贝后的对象
 */
export function shallowClone<T>(obj: T): T {
  if (obj === null || typeof obj !== 'object') {
    return obj
  }

  if (Array.isArray(obj)) {
    return [...obj] as unknown as T
  }

  return { ...obj } as T
}

/**
 * 智能拷贝函数
 * 根据对象大小自动选择合适的拷贝策略
 * @param obj 要拷贝的对象
 * @returns 拷贝后的对象
 */
export function smartClone<T>(obj: T): T {
  // 对于简单对象使用浅拷贝
  if (obj === null || typeof obj !== 'object') {
    return obj
  }

  // 检查对象复杂度
  const complexity = getObjectComplexity(obj)
  
  // 简单对象使用浅拷贝
  if (complexity < 100) {
    return shallowClone(obj)
  }

  // 复杂对象使用深拷贝
  return deepClone(obj)
}

/**
 * 计算对象复杂度
 * @param obj 要计算的对象
 * @returns 复杂度评分
 */
function getObjectComplexity(obj: any, visited = new WeakSet()): number {
  if (obj === null || typeof obj !== 'object') {
    return 1
  }

  if (visited.has(obj)) {
    return 1 // 避免循环引用导致的无限递归
  }

  visited.add(obj)
  let complexity = 0

  if (Array.isArray(obj)) {
    complexity = Math.min(obj.length, 100) // 限制最大复杂度
    obj.slice(0, 100).forEach(item => {
      complexity += getObjectComplexity(item, visited)
    })
  } else {
    const keys = Object.keys(obj)
    complexity = Math.min(keys.length, 50) // 限制最大复杂度
    keys.slice(0, 50).forEach(key => {
      complexity += getObjectComplexity(obj[key], visited)
    })
  }

  return Math.min(complexity, 1000) // 限制最大复杂度
}

/**
 * 对象比较函数
 * 高效比较两个对象是否深度相等
 * @param obj1 第一个对象
 * @param obj2 第二个对象
 * @returns 是否相等
 */
export function deepEqual(obj1: any, obj2: any): boolean {
  // 处理基本类型比较
  if (obj1 === obj2) {
    return true
  }

  // 处理null或非对象类型
  if (obj1 === null || obj2 === null || typeof obj1 !== 'object' || typeof obj2 !== 'object') {
    return false
  }

  // 处理日期比较
  if (obj1 instanceof Date && obj2 instanceof Date) {
    return obj1.getTime() === obj2.getTime()
  }

  // 处理正则表达式比较
  if (obj1 instanceof RegExp && obj2 instanceof RegExp) {
    return obj1.source === obj2.source && obj1.flags === obj2.flags
  }

  // 处理不同类型的对象
  if (obj1.constructor !== obj2.constructor) {
    return false
  }

  // 处理数组
  if (Array.isArray(obj1) && Array.isArray(obj2)) {
    if (obj1.length !== obj2.length) {
      return false
    }
    return obj1.every((item, index) => deepEqual(item, obj2[index]))
  }

  // 处理Map
  if (obj1 instanceof Map && obj2 instanceof Map) {
    if (obj1.size !== obj2.size) {
      return false
    }
    for (const [key, value] of obj1) {
      if (!obj2.has(key) || !deepEqual(value, obj2.get(key))) {
        return false
      }
    }
    return true
  }

  // 处理Set
  if (obj1 instanceof Set && obj2 instanceof Set) {
    if (obj1.size !== obj2.size) {
      return false
    }
    const arr1 = Array.from(obj1)
    const arr2 = Array.from(obj2)
    return arr1.every(item1 => arr2.some(item2 => deepEqual(item1, item2)))
  }

  // 处理普通对象
  const keys1 = Object.keys(obj1)
  const keys2 = Object.keys(obj2)

  if (keys1.length !== keys2.length) {
    return false
  }

  return keys1.every(key => {
    return keys2.includes(key) && deepEqual(obj1[key], obj2[key])
  })
}