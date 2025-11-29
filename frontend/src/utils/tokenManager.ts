// 安全的 Token 管理器
class TokenManager {
  private readonly ACCESS_TOKEN_KEY = 'auth_access_token'
  private readonly REFRESH_TOKEN_KEY = 'auth_refresh_token'
  private readonly TOKEN_EXPIRY_KEY = 'auth_token_expiry'

  /**
   * 存储认证 token
   * 使用 sessionStorage 而不是 localStorage，减少 XSS 风险
   */
  setTokens(accessToken: string, refreshToken?: string, expiresIn?: number): void {
    try {
      const now = Date.now()
      const expiresAt = expiresIn ? now + expiresIn * 1000 : now + 60 * 60 * 1000 // 默认1小时

      // 使用 sessionStorage 存储访问 token（会话级别）
      sessionStorage.setItem(this.ACCESS_TOKEN_KEY, accessToken)
      sessionStorage.setItem(this.TOKEN_EXPIRY_KEY, expiresAt.toString())

      // 只有在确实存在且较长有效期时才存储 refresh token
      if (refreshToken && expiresIn && expiresIn > 60 * 60) { // 超过1小时才存储
        // 可以考虑使用 httpOnly cookie，这里先用 sessionStorage
        sessionStorage.setItem(this.REFRESH_TOKEN_KEY, refreshToken)
      }
    } catch (error) {
      console.error('Failed to store tokens:', error)
    }
  }

  /**
   * 获取访问 token
   */
  getAccessToken(): string | null {
    try {
      const token = sessionStorage.getItem(this.ACCESS_TOKEN_KEY)
      const expiry = sessionStorage.getItem(this.TOKEN_EXPIRY_KEY)

      if (!token || !expiry) {
        return null
      }

      // 检查是否过期
      if (Date.now() > parseInt(expiry)) {
        this.clearTokens()
        return null
      }

      return token
    } catch (error) {
      console.error('Failed to get access token:', error)
      return null
    }
  }

  /**
   * 获取刷新 token
   */
  getRefreshToken(): string | null {
    try {
      const accessToken = this.getAccessToken()
      if (!accessToken) {
        return null
      }

      return sessionStorage.getItem(this.REFRESH_TOKEN_KEY)
    } catch (error) {
      console.error('Failed to get refresh token:', error)
      return null
    }
  }

  /**
   * 检查 token 是否有效
   */
  isTokenValid(): boolean {
    const token = this.getAccessToken()
    return !!token
  }

  /**
   * 获取 token 过期时间
   */
  getTokenExpiry(): number | null {
    try {
      const expiry = sessionStorage.getItem(this.TOKEN_EXPIRY_KEY)
      return expiry ? parseInt(expiry) : null
    } catch (error) {
      console.error('Failed to get token expiry:', error)
      return null
    }
  }

  /**
   * 检查 token 是否即将过期（5分钟内）
   */
  isTokenExpiringSoon(thresholdMinutes: number = 5): boolean {
    const expiry = this.getTokenExpiry()
    if (!expiry) return true

    return Date.now() > (expiry - thresholdMinutes * 60 * 1000)
  }

  /**
   * 清除所有 token
   */
  clearTokens(): void {
    try {
      sessionStorage.removeItem(this.ACCESS_TOKEN_KEY)
      sessionStorage.removeItem(this.REFRESH_TOKEN_KEY)
      sessionStorage.removeItem(this.TOKEN_EXPIRY_KEY)
    } catch (error) {
      console.error('Failed to clear tokens:', error)
    }
  }

  /**
   * 刷新访问 token
   * 这个方法需要在 API 调用中使用
   */
  async refreshAccessToken(): Promise<boolean> {
    const refreshToken = this.getRefreshToken()
    if (!refreshToken) {
      return false
    }

    try {
      // 这里应该调用实际的刷新 API
      // 暂时返回 false，需要在实际实现中完成
      console.warn('Token refresh not implemented yet')
      return false
    } catch (error) {
      console.error('Failed to refresh token:', error)
      this.clearTokens()
      return false
    }
  }

  /**
   * 获取认证头
   */
  getAuthHeader(): { Authorization: string } | Record<string, never> {
    const token = this.getAccessToken()
    if (token) {
      return { Authorization: `Bearer ${token}` }
    }
    return {}
  }

  /**
   * 检查是否应该自动刷新 token
   */
  shouldRefreshToken(): boolean {
    return this.isTokenValid() && this.isTokenExpiringSoon()
  }
}

// 创建单例实例
export const tokenManager = new TokenManager()

export { TokenManager }