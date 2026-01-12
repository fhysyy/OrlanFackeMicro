using FakeMicro.Interfaces.Models;
using FakeMicro.Interfaces;
using FakeMicro.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace FakeMicro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IClusterClient clusterClient, ILogger<AuthController> logger)
        {
            _clusterClient = clusterClient;
            _logger = logger;
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var requestId = HttpContext.Items["RequestId"]?.ToString() ?? "unknown";
            
            try
            {
                _logger.LogInformation("[{RequestId}] 用户注册请求 - 用户名: {Username}, 邮箱: {Email}", 
                    requestId, request.Username, request.Email);
                
                var authGrain = _clusterClient.GetGrain<IAuthenticationGrain>(Guid.Empty);
                var result = await authGrain.RegisterAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation("[{RequestId}] 用户注册成功 - 用户名: {Username}, 用户ID: {UserId}", 
                        requestId, request.Username, result.User?.Id);
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        token = result.Token,
                        refreshToken = result.RefreshToken,
                        expiresAt = result.ExpiresAt,
                        user = result.User,
                        requestId = requestId
                    });
                }
                else
                {
                    _logger.LogWarning("用户注册失败: {Username} - {Message}", request.Username, result.Message);
                    return BadRequest(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户注册异常: {Username}", request.Username);
                return StatusCode(500, new { success = false, message = "注册服务异常" });
            }
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var requestId = HttpContext.Items["RequestId"]?.ToString() ?? "unknown";
            
            try
            {
                _logger.LogInformation("[{RequestId}] 用户登录请求 - 用户名: {Username}", requestId, request.UsernameOrEmail);
                
                var authGrain = _clusterClient.GetGrain<IAuthenticationGrain>(Guid.Empty);
                var result = await authGrain.LoginAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation("[{RequestId}] 用户登录成功 - 用户名: {Username}, 用户ID: {UserId}", 
                        requestId, request.UsernameOrEmail, result.User?.Id);
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        token = result.Token,
                        refreshToken = result.RefreshToken,
                        expiresAt = result.ExpiresAt,
                        user = result.User,
                        requestId = requestId
                    });
                }
                else
                {
                    _logger.LogWarning("[{RequestId}] 用户登录失败 - 用户名: {Username}, 原因: {Message}", 
                        requestId, request.UsernameOrEmail, result.Message);
                    return Unauthorized(new { 
                        success = false, 
                        message = result.Message,
                        errorCode = "invalid_credentials",
                        requestId = requestId,
                        timestamp = DateTime.UtcNow,
                        details = new { username = request.UsernameOrEmail }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "用户登录异常: {Username}", request.UsernameOrEmail);
                return StatusCode(500, new { success = false, message = $"登录服务异常{ex.Message}{ex.StackTrace}" });
            }
        }

        /// <summary>
        /// 刷新Token
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                // 使用AuthenticationGrain进行令牌刷新
                var authGrain = _clusterClient.GetGrain<IAuthenticationGrain>(Guid.Empty);
                var result = await authGrain.RefreshTokenAsync(request);

                if (result.Success)
                {
                    _logger.LogInformation("令牌刷新成功，用户ID: {UserId}", result.User?.Id);
                    return Ok(new
                    {
                        success = true,
                        token = result.Token,
                        refreshToken = result.RefreshToken,
                        expiresAt = result.ExpiresAt,
                        user = result.User
                    });
                }
                else
                {
                    _logger.LogWarning("令牌刷新失败: {ErrorMessage}", result.ErrorMessage);
                    return Unauthorized(new { success = false, message = result.ErrorMessage ?? "刷新令牌失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token刷新异常");
                return StatusCode(500, new { success = false, message = "Token刷新异常" });
            }
        }

        /// <summary>
        /// 修改密码
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out long userIdLong))
                {
                    _logger.LogWarning("无效的用户ID格式: {UserId}", userId);
                    return Unauthorized(new { success = false, message = "无效的用户信息" });
                }

                var userGrain = _clusterClient.GetGrain<IUserGrain>(userId);
                var result = await userGrain.ChangePasswordAsync(request.CurrentPassword, request.NewPassword);

                if (result.Success)
                {
                    _logger.LogInformation("密码修改成功: {UserId}", userId);
                    return Ok(new { success = true, message = "密码修改成功" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "原密码错误或用户不存在" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "密码修改异常");
                return StatusCode(500, new { success = false, message = "密码修改异常" });
            }
        }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out long userIdLong))
                {
                    _logger.LogWarning("无效的用户ID格式: {UserId}", userId);
                    return Unauthorized(new { success = false, message = "无效的用户信息" });
                }

                var userGrain = _clusterClient.GetGrain<IUserGrain>(userId);
                var user = await userGrain.GetUserAsync();

                if (user == null)
                    return NotFound(new { success = false, message = "用户不存在" });

                return Ok(new { success = true, user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户信息失败");
                return StatusCode(500, new { success = false, message = "获取用户信息失败" });
            }
        }

        /// <summary>
        /// 更新用户资料
        /// </summary>
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody]UserProfileUpdateRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out long userIdLong))
                {
                    _logger.LogWarning("无效的用户ID格式: {UserId}", userId);
                    return Unauthorized(new { success = false, message = "无效的用户信息" });
                }

                var userGrain = _clusterClient.GetGrain<IUserGrain>(userId);
                
                // 首先获取当前用户信息
                var currentUser = await userGrain.GetUserAsync();
                if (currentUser == null)
                {
                    return Unauthorized(new { success = false, message = "用户不存在" });
                }
                
                // 更新用户资料
                currentUser.DisplayName = request.DisplayName ?? currentUser.DisplayName;
                currentUser.Phone = request.PhoneNumber ?? currentUser.Phone;
                
                var result = await userGrain.UpdateUserAsync(currentUser);

                if (result.Success)
                {
                    _logger.LogInformation("用户资料更新成功: {UserId}", userId);
                    return Ok(new { success = true, message = "资料更新成功" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "资料更新失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新用户资料失败");
                return StatusCode(500, new { success = false, message = "更新用户资料失败" });
            }
        }

        /// <summary>
        /// 发送邮箱验证邮件
        /// </summary>
        [HttpPost("send-email-verification")]
        [Authorize]
        public async Task<IActionResult> SendEmailVerification()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out long userIdLong))
                {
                    _logger.LogWarning("无效的用户ID格式: {UserId}", userId);
                    return Unauthorized(new { success = false, message = "无效的用户信息" });
                }

                var userGrain = _clusterClient.GetGrain<IUserGrain>(userId);
                var result = await userGrain.SendEmailVerificationAsync();

                if (result.Success)
                {
                    return Ok(new { success = true, message = "验证邮件已发送" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "发送验证邮件失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送邮箱验证邮件失败");
                return StatusCode(500, new { success = false, message = "发送验证邮件失败" });
            }
        }

        /// <summary>
        /// 验证邮箱
        /// </summary>
        [HttpPost("verify-email")]
        [Authorize]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out long userIdLong))
                {
                    _logger.LogWarning("无效的用户ID格式: {UserId}", userId);
                    return Unauthorized(new { success = false, message = "无效的用户信息" });
                }

                var userGrain = _clusterClient.GetGrain<IUserGrain>(userId);
                var result = await userGrain.VerifyEmailAsync(request.Token);

                if (result.Success)
                {
                    return Ok(new { success = true, message = "邮箱验证成功" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "邮箱验证失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "邮箱验证失败");
                return StatusCode(500, new { success = false, message = "邮箱验证失败" });
            }
        }

        /// <summary>
        /// 发送手机验证码
        /// </summary>
        [HttpPost("send-phone-verification")]
        [Authorize]
        public async Task<IActionResult> SendPhoneVerification()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out long userIdLong))
                {
                    _logger.LogWarning("无效的用户ID格式: {UserId}", userId);
                    return Unauthorized(new { success = false, message = "无效的用户信息" });
                }

                var userGrain = _clusterClient.GetGrain<IUserGrain>(userId);
                var result = await userGrain.SendPhoneVerificationAsync();

                if (result.Success)
                {
                    return Ok(new { success = true, message = "验证码已发送" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "发送验证码失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送手机验证码失败");
                return StatusCode(500, new { success = false, message = "发送验证码失败" });
            }
        }

        /// <summary>
        /// 验证手机
        /// </summary>
        [HttpPost("verify-phone")]
        [Authorize]
        public async Task<IActionResult> VerifyPhone([FromBody] VerifyPhoneRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out long userIdLong))
                {
                    _logger.LogWarning("无效的用户ID格式: {UserId}", userId);
                    return Unauthorized(new { success = false, message = "无效的用户信息" });
                }

                var userGrain = _clusterClient.GetGrain<IUserGrain>(userId);
                var result = await userGrain.VerifyPhoneAsync(request.Code);

                if (result.Success)
                {
                    return Ok(new { success = true, message = "手机验证成功" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "手机验证失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "手机验证失败");
                return StatusCode(500, new { success = false, message = "手机验证失败" });
            }
        }

        /// <summary>
        /// 请求密码重置
        /// </summary>
        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetRequest request)
        {
            try
            {
                var userQueryGrain = _clusterClient.GetGrain<IUserQueryGrain>(Guid.Empty);
                var userId = await userQueryGrain.FindUserByUsernameOrEmailAsync(request.UsernameOrEmail);

                if (userId.HasValue)
                {
                    var userGrain = _clusterClient.GetGrain<IUserGrain>(userId.ToString());
                    var result = await userGrain.RequestPasswordResetAsync();

                if (result.Success)
                    {
                        return Ok(new { success = true, message = "密码重置邮件已发送" });
                    }
                }

                // 出于安全考虑，无论用户是否存在都返回相同的信息
                return Ok(new { success = true, message = "如果用户存在，密码重置邮件已发送" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "请求密码重置失败");
                return StatusCode(500, new { success = false, message = "请求密码重置失败" });
            }
        }

        /// <summary>
        /// 重置密码
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var userQueryGrain = _clusterClient.GetGrain<IUserQueryGrain>(Guid.Empty);
                var userId = await userQueryGrain.FindUserByUsernameOrEmailAsync(request.UsernameOrEmail);

                if (userId.HasValue)
                {
                    var userGrain = _clusterClient.GetGrain<IUserGrain>(userId.ToString());
                    var result = await userGrain.ResetPasswordAsync(request.Token, request.NewPassword);

                if (result.Success)
                    {
                        return Ok(new { success = true, message = "密码重置成功" });
                    }
                }

                return BadRequest(new { success = false, message = "密码重置失败" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "密码重置失败");
                return StatusCode(500, new { success = false, message = "密码重置失败" });
            }
        }

        /// <summary>
        /// 获取用户会话列表
        /// </summary>
        [HttpGet("sessions")]
        [Authorize]
        public async Task<IActionResult> GetSessions()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out long userIdLong))
                {
                    _logger.LogWarning("无效的用户ID格式: {UserId}", userId);
                    return Unauthorized(new { success = false, message = "无效的用户信息" });
                }

                var userGrain = _clusterClient.GetGrain<IUserGrain>(userId);
                var sessions = await userGrain.GetSessionsAsync();

                return Ok(new { success = true, sessions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取用户会话失败");
                return StatusCode(500, new { success = false, message = "获取用户会话失败" });
            }
        }

        /// <summary>
        /// 终止指定会话
        /// </summary>
        [HttpPost("sessions/{sessionId}/terminate")]
        [Authorize]
        public async Task<IActionResult> TerminateSession(string sessionId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out long userIdLong))
                {
                    _logger.LogWarning("无效的用户ID格式: {UserId}", userId);
                    return Unauthorized(new { success = false, message = "无效的用户信息" });
                }

                var userGrain = _clusterClient.GetGrain<IUserGrain>(userId);
                var result = await userGrain.TerminateSessionAsync(sessionId);

                if (result.Success)
                {
                    return Ok(new { success = true, message = "会话终止成功" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "会话终止失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "终止会话失败");
                return StatusCode(500, new { success = false, message = "终止会话失败" });
            }
        }

        /// <summary>
        /// 终止其他会话
        /// </summary>
        [HttpPost("sessions/terminate-others")]
        [Authorize]
        public async Task<IActionResult> TerminateOtherSessions()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { success = false, message = "用户未认证" });

                var userGrain = _clusterClient.GetGrain<IUserGrain>(userId.ToString());
                var result = await userGrain.TerminateOtherSessionsAsync();

                if (result.Success)
                {
                    return Ok(new { success = true, message = "其他会话终止成功" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "终止其他会话失败" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "终止其他会话失败");
                return StatusCode(500, new { success = false, message = "终止其他会话失败" });
            }
        }
    }

    //public class RegisterRequest
    //{
    //    public string Username { get; set; } = string.Empty;
    //    public string Email { get; set; } = string.Empty;
    //    public string Password { get; set; } = string.Empty;
    //    public string DisplayName { get; set; } = string.Empty;
    //}

    //public class LoginRequest
    //{
    //    public string Username { get; set; } = string.Empty;
    //    public string Password { get; set; } = string.Empty;
    //}

    //public class RefreshTokenRequest
    //{
    //    public string RefreshToken { get; set; } = string.Empty;
    //}

    //public class ChangePasswordRequest
    //{
    //    public string CurrentPassword { get; set; } = string.Empty;
    //    public string NewPassword { get; set; } = string.Empty;
    //}



 


 
}