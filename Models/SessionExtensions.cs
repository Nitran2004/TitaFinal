using Microsoft.AspNetCore.Http;
using System;

namespace ProyectoIdentity.Helpers
{
    public static class SessionExtensions
    {
        public static void SetInt32(this ISession session, string key, int value)
        {
            session.Set(key, BitConverter.GetBytes(value));
        }

        public static int? GetInt32(this ISession session, string key)
        {
            var data = session.Get(key);
            if (data == null || data.Length < sizeof(int))
            {
                return null;
            }
            return BitConverter.ToInt32(data, 0);
        }
    }
}