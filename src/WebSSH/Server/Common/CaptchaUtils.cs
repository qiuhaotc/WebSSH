using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WebSSH.Server
{
    public static class CaptchaUtils
    {
        static readonly char[] characters = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
        static readonly Random random = new Random();

        public static string GenerateCaptcha(int width, int height, out byte[] captchaImages)
        {
            var captcha = string.Empty;

            for (int i = 0; i < 4; i++)
            {
                captcha += characters[random.Next(0, characters.Length)];
            }

            captchaImages = CaptchaImageUtils.GenerateCaptchaImage(width, height, captcha, random);

            return captcha;
        }
    }
}
