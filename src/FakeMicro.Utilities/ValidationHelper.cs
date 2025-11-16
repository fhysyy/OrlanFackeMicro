using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;

namespace FakeMicro.Utilities
{
    /// <summary>
    /// 验证助手类
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// 验证电子邮件地址格式
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 验证手机号码格式（中国）
        /// </summary>
        public static bool IsValidChineseMobile(string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile)) return false;
            
            var regex = new Regex(@"^1[3-9]\d{9}$");
            return regex.IsMatch(mobile);
        }

        /// <summary>
        /// 验证身份证号码格式（中国）
        /// </summary>
        public static bool IsValidChineseIdCard(string idCard)
        {
            if (string.IsNullOrWhiteSpace(idCard)) return false;
            
            // 15位或18位身份证号码
            var regex = new Regex(@"^\d{15}|\d{17}[\dXx]$");
            if (!regex.IsMatch(idCard)) return false;

            // 简单的校验位验证（仅对18位身份证）
            if (idCard.Length == 18)
            {
                return ValidateIdCardCheckDigit(idCard);
            }

            return true;
        }

        /// <summary>
        /// 验证URL格式
        /// </summary>
        public static bool IsValidUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            
            try
            {
                return Uri.TryCreate(url, UriKind.Absolute, out _);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 验证IP地址格式
        /// </summary>
        public static bool IsValidIpAddress(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress)) return false;
            
            var parts = ipAddress.Split('.');
            if (parts.Length != 4) return false;

            foreach (var part in parts)
            {
                if (!byte.TryParse(part, out _)) return false;
            }

            return true;
        }

        /// <summary>
        /// 验证邮政编码格式（中国）
        /// </summary>
        public static bool IsValidChinesePostalCode(string postalCode)
        {
            if (string.IsNullOrWhiteSpace(postalCode)) return false;
            
            var regex = new Regex(@"^\d{6}$");
            return regex.IsMatch(postalCode);
        }

        /// <summary>
        /// 验证银行卡号格式
        /// </summary>
        public static bool IsValidBankCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber)) return false;
            
            // 简单的银行卡号格式验证（13-19位数字）
            var regex = new Regex(@"^\d{13,19}$");
            return regex.IsMatch(cardNumber);
        }

        /// <summary>
        /// 使用Luhn算法验证银行卡号
        /// </summary>
        public static bool ValidateBankCardWithLuhn(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber)) return false;
            
            cardNumber = cardNumber.Replace(" ", "").Replace("-", "");
            if (!System.Text.RegularExpressions.Regex.IsMatch(cardNumber, @"^\d+$"))
                return false;

            int sum = 0;
            bool alternate = false;
            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int digit = int.Parse(cardNumber[i].ToString());
                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit = digit - 9;
                }
                sum += digit;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }

        /// <summary>
        /// 验证对象的数据注解属性
        /// </summary>
        public static List<ValidationResult> ValidateObject(object obj)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(obj);
            Validator.TryValidateObject(obj, context, results, true);
            return results;
        }

        /// <summary>
        /// 验证对象的数据注解属性并返回第一个错误消息
        /// </summary>
        public static string? ValidateObjectFirstError(object obj)
        {
            var errors = ValidateObject(obj);
            return errors.FirstOrDefault()?.ErrorMessage;
        }

        /// <summary>
        /// 检查对象是否通过数据注解验证
        /// </summary>
        public static bool IsValidObject(object obj)
        {
            return !ValidateObject(obj).Any();
        }

        /// <summary>
        /// 验证密码强度
        /// </summary>
        public static PasswordStrength ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return PasswordStrength.Weak;

            var score = 0;

            // 长度检查
            if (password.Length >= 8) score++;
            if (password.Length >= 12) score++;

            // 包含小写字母
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[a-z]")) score++;
            // 包含大写字母
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[A-Z]")) score++;
            // 包含数字
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[0-9]")) score++;
            // 包含特殊字符
            if (System.Text.RegularExpressions.Regex.IsMatch(password, "[^a-zA-Z0-9]")) score++;

            return score switch
            {
                <= 2 => PasswordStrength.Weak,
                <= 4 => PasswordStrength.Medium,
                _ => PasswordStrength.Strong
            };
        }

        private static bool ValidateIdCardCheckDigit(string idCard)
        {
            if (idCard.Length != 18) return false;

            int[] weights = { 7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2 };
            char[] checkDigits = { '1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2' };

            int sum = 0;
            for (int i = 0; i < 17; i++)
            {
                if (!int.TryParse(idCard[i].ToString(), out int digit))
                    return false;
                sum += digit * weights[i];
            }

            int mod = sum % 11;
            char expectedCheckDigit = checkDigits[mod];
            char actualCheckDigit = char.ToUpper(idCard[17]);

            return expectedCheckDigit == actualCheckDigit;
        }
    }

    /// <summary>
    /// 密码强度等级
    /// </summary>
    public enum PasswordStrength
    {
        Weak = 0,
        Medium = 1,
        Strong = 2
    }
}